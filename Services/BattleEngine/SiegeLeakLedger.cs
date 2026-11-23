using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameDamageCalculator.Services.BattleEngine
{
    // ============================================================================
    // 누수 장부 (2026-11-22, 시도 기록의 다음 단계).
    //   한 전투 안에서 "이 팀이 한 번이라도 만든 최고 상태"를 기준으로, 각 타격이 그 상태가 아니어서 덜 들어간 딜을
    //   계수별로 쌓는다. 기준이 자기 전투 안에 있으므로 외부 대조 자료가 필요 없다.
    //     · 계수 누수: 그 계수 하나만 최고 상태였다면 더 들어갔을 딜 (타격별 DamageCalculator 재계산 — 근사식 아님)
    //         공격자 쪽(영웅별 최고): 공버프 / 공감(적이 건 공격력 감소) / 치명 / 약점 / 피증 / 방무
    //         대상 쪽(같은 종류[보스·일반]의 적 전체 최고): 방깎 / 받피증(취약·보스취약·받피증) / 디버프 수
    //         조건: 스킬의 조건부 추가 피해가 미충족
    //     · 깎인 딜: 계산된 피해 중 점수가 되지 못한 몫 — 면역 / 무효 / 링크 / 보호막 / 오버킬(R1·R2) / 피해1고정
    //     · 사망: 영웅별 죽어 있던 턴 수 + 추정 손실(살아 있을 때의 턴당 딜 × 죽은 턴)
    //   계수 누수는 "한 계수씩"이라 서로 더하면 실제보다 작다(곱연산) — 전 계수 동시 최고는 AllBest.
    //   기준은 각 필드의 최고값(같은 순간에 함께 있었는지는 따지지 않음) → 달성 가능한 상한이 아니라 "가동률 부족"의 크기.
    //   읽기 전용 계측 — 전투·점수 불변. SiegeBattleConfig.RecordLeak=true 인 재생에서만 동작.
    // ============================================================================
    public class SiegeLeakLedger
    {
        public double Score { get; set; }           // 전투 총점
        public double HitScored { get; set; }       // 타격 점수 합(계수 분해 대상)
        public double DotScored { get; set; }       // DoT 점수(계수 분해 제외)
        public Dictionary<string, double> Factor { get; set; } = new();   // 계수별 누수
        public double AllBest { get; set; }         // 전 계수가 동시에 최고 상태였다면 더 들어갔을 딜
        public Dictionary<string, double> Lost { get; set; } = new();     // 깎인 딜(사유별)
        public Dictionary<string, int> DeadTurns { get; set; } = new();   // 영웅별 죽어 있던 턴 수
        public double DeathLossEst { get; set; }    // 사망 손실 추정(그 영웅의 생존 중 턴당 딜 × 죽은 턴) — 지원 영웅의 버프 손실은 미포함
        public Dictionary<string, double> IdleSec { get; set; } = new();  // "영웅:스킬" → 쿨이 돈 뒤 놀린 시간 합(초). 스킬턴이 모자라 생기는 대기도 포함 — 참고용
        public int Fallbacks { get; set; }          // 계획한 스킬을 못 쓰고 다른 스킬로 대체된 스킬턴 수
        public int Holds { get; set; }              // 일부러 비운 스킬턴 수
        public int SkillTurns { get; set; }         // 도달한 아군 스킬턴 수
        public Dictionary<string, double> Best { get; set; } = new();     // 기준이 된 최고 상태(전투 중 값이 변한 것만)
        public List<Row> Rows { get; set; } = new();
        public int RecalcMismatch { get; set; }     // 재계산이 원래 계산과 어긋난 타격 수(0이어야 함)

        /// <summary>영웅 × 스킬 × 시전 스텝 단위의 누수. 평타·협공·패시브 발동은 Step=-1로 전투 전체 합산.</summary>
        public class Row
        {
            public int Step { get; set; } = -1;     // 아군 스킬턴 순번(스킬 시전만). -1 = 스킬턴 밖
            public int Turn { get; set; } = -1;     // 시전 턴(Step>=0일 때)
            public string Hero { get; set; }
            public string Skill { get; set; }
            public double Scored { get; set; }
            public double AllBest { get; set; }
            public Dictionary<string, double> Factor { get; set; } = new();
            public Dictionary<string, double> Lost { get; set; }
            public string Targets { get; set; }     // 이 시전이 건 버프·디버프·해제 대상(예: "버프→라이언,아일린")
        }

        /// <summary>실행된 스킬 순서(Feasibility)에서 쿨 대기·폴백·홀드를 채운다.</summary>
        public void AddFeasibility(IEnumerable<BuildStepFeasibility> feas)
        {
            if (feas == null) return;
            var reached = feas.Where(f => f.Reached).ToList();
            SkillTurns = reached.Select(f => f.StepIndex).Distinct().Count();   // 폴백 스텝은 (계획 실패 + 자동 시전) 두 줄이라 스텝 번호로 센다
            foreach (var f in reached)
            {
                if (f.Hold) { Holds++; continue; }
                if (!f.IsAuto && !f.ExecutedAsPlanned) { Fallbacks++; continue; }
                if (!f.IsAuto && f.CooldownGated && f.Slack > 0)
                {
                    string k = $"{f.HeroName}:{f.SkillName}";
                    IdleSec[k] = Math.Round(IdleSec.GetValueOrDefault(k) + f.Slack, 1);
                }
                // 실제로 시전된 스킬 — 피해가 없는 버프 전용 시전도 행으로 남기고, 건 대상을 적는다
                var row = Rows.FirstOrDefault(r => r.Step == f.StepIndex && r.Hero == f.HeroName && r.Skill == f.SkillName);
                if (row == null)
                    Rows.Add(row = new Row { Step = f.StepIndex, Turn = f.Turn, Hero = f.HeroName, Skill = f.SkillName });
                var parts = new List<string>();
                if (f.BuffTargets is { Count: > 0 }) parts.Add("버프→" + string.Join(",", f.BuffTargets));
                if (f.DebuffTargets is { Count: > 0 }) parts.Add("디버프→" + string.Join(",", f.DebuffTargets));
                if (f.DispelTargets is { Count: > 0 }) parts.Add("해제→" + string.Join(",", f.DispelTargets));
                if (parts.Count > 0) row.Targets = string.Join(" ", parts);
            }
            Rows = Rows.OrderBy(r => r.Step < 0 ? int.MaxValue : r.Step).ThenByDescending(r => r.Scored).ToList();
        }
    }

    /// <summary>시뮬이 전투 중 호출하는 누수 기록기. 끝나면 Finish가 장부를 만든다.</summary>
    public class SiegeLeakRecorder
    {
        public static readonly string[] Factors = { "방깎", "받피증", "공버프", "공감", "치명", "약점", "피증", "방무", "디버프 수", "조건" };

        private readonly DamageCalculator _calc;
        public SiegeLeakRecorder(DamageCalculator calc) { _calc = calc; }

        public int CurStep = -1;    // 지금 진행 중인 아군 스킬턴 순번(-1 = 스킬턴 밖)

        // ── 기준(최고 상태) ──
        private class AllyBest
        {
            public string Name;
            public double MidAtk = double.NegativeInfinity, MidAtkMin = double.PositiveInfinity;
            public double AtkRedMin = double.PositiveInfinity, AtkRedMax = double.NegativeInfinity;
            public readonly double[] Max = Fill(AllyFields, double.NegativeInfinity), Min = Fill(AllyFields, double.PositiveInfinity);
        }
        private class EnemyBest
        {
            public readonly double[] Max = Fill(EnemyFields, double.NegativeInfinity), Min = Fill(EnemyFields, double.PositiveInfinity);
            public int DebuffCount = 0, DebuffCountMin = int.MaxValue;
        }
        private static double[] Fill(int n, double v) { var a = new double[n]; Array.Fill(a, v); return a; }

        // 공격자 쪽 필드: 치확 치피 약확 약피 | 피증 타입피증 보스피증 1~3인 1인 3인 4~5인 | 방무
        private const int AllyFields = 12;
        private static readonly string[] AllyFieldNames = { "치확", "치피", "약확", "약피", "피증", "타입피증", "보스피증", "1~3인 피증", "1인 피증", "3인 피증", "4~5인 피증", "방무" };
        private static double[] AllyVec(DamageCalculator.DamageInput i) => new[]
        {
            i.CritChance, i.CritDamage, i.WeakChance, i.WeakpointDmg,
            i.DmgDealt, i.DmgDealtType, i.DmgDealtBoss, i.Dmg1to3, i.Dmg1, i.Dmg3, i.Dmg4to5, i.ArmorPen,
        };
        // 대상 쪽 필드: 방깎 취약 보스취약 받피증(물리) 받피증(마법)
        private const int EnemyFields = 5;
        private static readonly string[] EnemyFieldNames = { "방깎", "취약", "보스취약", "받피증(물리)", "받피증(마법)" };

        private readonly Dictionary<int, AllyBest> _ally = new();
        private readonly EnemyBest[] _enemy = { new EnemyBest(), new EnemyBest() };   // [0]=일반, [1]=보스

        /// <summary>아군 한 명의 지금 상태를 기준에 반영. input은 그 아군의 아무 타격용 DamageInput(공격자 쪽 필드만 읽음).</summary>
        public void SampleAlly(int partyIndex, string name, DamageCalculator.DamageInput input, double midAtk, double atkRed)
        {
            if (!_ally.TryGetValue(partyIndex, out var b)) _ally[partyIndex] = b = new AllyBest { Name = name };
            b.MidAtk = Math.Max(b.MidAtk, midAtk); b.MidAtkMin = Math.Min(b.MidAtkMin, midAtk);
            b.AtkRedMin = Math.Min(b.AtkRedMin, atkRed); b.AtkRedMax = Math.Max(b.AtkRedMax, atkRed);
            var v = AllyVec(input);
            for (int k = 0; k < AllyFields; k++)
            {
                if (Math.Abs(v[k]) > 1e5) continue;   // 약점 강제(±1e6) 입력은 기준에서 제외
                if (v[k] > b.Max[k]) b.Max[k] = v[k];
                if (v[k] < b.Min[k]) b.Min[k] = v[k];
            }
        }

        /// <summary>적 한 명의 지금 디버프 상태를 기준에 반영.</summary>
        public void SampleEnemy(bool isBoss, double defRed, double vul, double bossVul, double takenPhys, double takenMagic, int debuffCount)
        {
            var b = _enemy[isBoss ? 1 : 0];
            var v = new[] { defRed, vul, bossVul, takenPhys, takenMagic };
            for (int k = 0; k < EnemyFields; k++)
            {
                if (v[k] > b.Max[k]) b.Max[k] = v[k];
                if (v[k] < b.Min[k]) b.Min[k] = v[k];
            }
            b.DebuffCount = Math.Max(b.DebuffCount, debuffCount);
            b.DebuffCountMin = Math.Min(b.DebuffCountMin, debuffCount);
        }

        // ── 타격 ──
        private class Calc
        {
            public object Ally, Target;
            public int PartyIndex; public bool Boss, Magic;
            public DamageCalculator.DamageInput Input;
            public double ReductionMult, PCond, ExtraMult, Final;
            public double MidAtk, AtkRed, S3;
        }
        private class Hit
        {
            public int Step, Turn; public string Hero, Label;
            public List<Calc> Calcs;
            public double Scored; public bool Capped;
            public Dictionary<string, double> Lost;
            public void Lose(string why, double amount)
            {
                if (amount <= 0) return;
                Lost ??= new();
                Lost[why] = Lost.GetValueOrDefault(why) + amount;
            }
        }
        private readonly List<Calc> _pending = new();
        private readonly List<Hit> _hits = new();
        private readonly Dictionary<(string Hero, string Label), (double Scored, double Lost)> _dots = new();
        private readonly Dictionary<string, int> _deadTurns = new();
        private int _turns;

        /// <summary>피해 계산 1건(적용 전). final = 감쇄·추가 곱까지 끝난 값(가중치 적용 전).</summary>
        public void OnCalc(object ally, object target, int partyIndex, bool targetIsBoss, bool magic,
            DamageCalculator.DamageInput input, double reductionMult, double pCond, double extraMult, double final,
            double midAtk, double atkRed, double stage3Rate)
        {
            _pending.Add(new Calc
            {
                Ally = ally, Target = target, PartyIndex = partyIndex, Boss = targetIsBoss, Magic = magic,
                Input = input.Clone(), ReductionMult = reductionMult, PCond = pCond, ExtraMult = extraMult, Final = final,
                MidAtk = midAtk, AtkRed = atkRed, S3 = 1 + stage3Rate / 100.0,
            });
        }

        /// <summary>피해 적용 시작 — 이 (공격자, 대상)의 대기 중 계산을 가져가 타격 1건을 연다. 반환값은 시뮬이 Lose/Scored를 채운다.</summary>
        public object BeginHit(object ally, object target, string hero, string label, int turn)
        {
            var h = new Hit { Step = CurStep, Turn = turn, Hero = hero, Label = label, Calcs = new List<Calc>() };
            for (int i = _pending.Count - 1; i >= 0; i--)
                if (ReferenceEquals(_pending[i].Ally, ally) && ReferenceEquals(_pending[i].Target, target))
                { h.Calcs.Add(_pending[i]); _pending.RemoveAt(i); }
            _hits.Add(h);
            return h;
        }
        public static void Lose(object hit, string why, double amount) => (hit as Hit)?.Lose(why, amount);
        public static void Scored(object hit, double scored, bool capped)
        {
            if (hit is Hit h) { h.Scored = scored; h.Capped = capped; }
        }

        public void OnDot(string hero, string label, double scored, double lost)
        {
            var k = (hero ?? "?", label ?? "DoT");
            var cur = _dots.GetValueOrDefault(k);
            _dots[k] = (cur.Scored + scored, cur.Lost + lost);
        }

        /// <summary>턴 1개 경과 — 죽어 있는 아군 이름들.</summary>
        public void OnTurn(IEnumerable<string> deadAllies)
        {
            _turns++;
            foreach (var n in deadAllies) _deadTurns[n] = _deadTurns.GetValueOrDefault(n) + 1;
        }

        // ── 재계산 ──
        private double Eval(Calc c, DamageCalculator.DamageInput inp, double pCond)
        {
            double raw;
            if (pCond > 0 && pCond < 1)
            {
                inp.IsSkillConditionMet = true; double met = _calc.Calculate(inp).FinalDamage;
                inp.IsSkillConditionMet = false; double unmet = _calc.Calculate(inp).FinalDamage;
                inp.IsSkillConditionMet = true;
                raw = pCond * met + (1 - pCond) * unmet;
            }
            else raw = _calc.Calculate(inp).FinalDamage;
            return raw * Math.Max(0, c.ReductionMult) * c.ExtraMult;
        }

        private static double RedMult(double s3, double atkRed)
            => s3 > 0 ? Math.Max(0, s3 - atkRed / 100.0) / s3 : Math.Max(0, 1 - atkRed / 100.0);

        /// <summary>한 계수(또는 전부)를 기준 상태로 바꾼 입력을 만든다. pCond는 조건 계수일 때만 1로.</summary>
        private DamageCalculator.DamageInput Apply(Calc c, string factor, ref double pCond)
        {
            var i = c.Input.Clone();
            bool all = factor == null;
            _ally.TryGetValue(c.PartyIndex, out var ab);
            var eb = _enemy[c.Boss ? 1 : 0];
            double Up(double cur, double best) => double.IsInfinity(best) ? cur : Math.Max(cur, best);

            if (ab != null)
            {
                if (all || factor == "공버프")
                {
                    double best = Up(c.MidAtk, ab.MidAtk);
                    if (1 + c.MidAtk / 100.0 > 0) i.FinalAtk *= (1 + best / 100.0) / (1 + c.MidAtk / 100.0);
                }
                if (all || factor == "공감")
                {
                    double best = double.IsInfinity(ab.AtkRedMin) ? c.AtkRed : Math.Min(c.AtkRed, ab.AtkRedMin);
                    double cur = RedMult(c.S3, c.AtkRed);
                    if (cur > 0) i.FinalAtk *= RedMult(c.S3, best) / cur;
                }
                if (all || factor == "치명") { i.CritChance = Up(i.CritChance, ab.Max[0]); i.CritDamage = Up(i.CritDamage, ab.Max[1]); }
                if (all || factor == "약점")
                {
                    if (Math.Abs(i.WeakChance) < 1e5) i.WeakChance = Up(i.WeakChance, ab.Max[2]);   // 약점 강제 입력은 그대로
                    i.WeakpointDmg = Up(i.WeakpointDmg, ab.Max[3]);
                }
                if (all || factor == "피증")
                {
                    i.DmgDealt = Up(i.DmgDealt, ab.Max[4]); i.DmgDealtType = Up(i.DmgDealtType, ab.Max[5]);
                    i.DmgDealtBoss = Up(i.DmgDealtBoss, ab.Max[6]); i.Dmg1to3 = Up(i.Dmg1to3, ab.Max[7]);
                    i.Dmg1 = Up(i.Dmg1, ab.Max[8]); i.Dmg3 = Up(i.Dmg3, ab.Max[9]); i.Dmg4to5 = Up(i.Dmg4to5, ab.Max[10]);
                }
                if (all || factor == "방무") i.ArmorPen = Up(i.ArmorPen, ab.Max[11]);
            }
            if (all || factor == "방깎") i.DefReduction = Up(i.DefReduction, eb.Max[0]);
            if (all || factor == "받피증")
            {
                i.Vulnerability = Up(i.Vulnerability, eb.Max[1]);
                i.BossVulnerability = Up(i.BossVulnerability, eb.Max[2]);
                i.DmgTakenIncrease = Up(i.DmgTakenIncrease, eb.Max[c.Magic ? 4 : 3]);
            }
            if ((all || factor == "디버프 수") && i.PerDebuffBonusPercent > 0)
                i.TargetDebuffCount = Math.Max(i.TargetDebuffCount, eb.DebuffCount);
            if (all || factor == "조건") { i.IsSkillConditionMet = true; pCond = 1; }
            return i;
        }

        public SiegeLeakLedger Finish(double totalScore, IReadOnlyDictionary<string, double> heroDamage)
        {
            var led = new SiegeLeakLedger { Score = Math.Round(totalScore) };
            foreach (var f in Factors) led.Factor[f] = 0;
            var rows = new Dictionary<(int Step, string Hero, string Label), SiegeLeakLedger.Row>();
            SiegeLeakLedger.Row RowOf(int step, int turn, string hero, string label)
            {
                if (!rows.TryGetValue((step, hero, label), out var r))
                    rows[(step, hero, label)] = r = new SiegeLeakLedger.Row { Step = step, Turn = step >= 0 ? turn : -1, Hero = hero, Skill = label };
                return r;
            }

            foreach (var h in _hits)
            {
                var row = RowOf(h.Step, h.Turn, h.Hero, h.Label);
                row.Scored += h.Scored;
                led.HitScored += h.Scored;
                if (h.Lost != null)
                    foreach (var kv in h.Lost)
                    {
                        led.Lost[kv.Key] = led.Lost.GetValueOrDefault(kv.Key) + kv.Value;
                        row.Lost ??= new();
                        row.Lost[kv.Key] = row.Lost.GetValueOrDefault(kv.Key) + kv.Value;
                    }
                // 더 세게 쳐도 점수가 늘지 않는 타격(오버킬·피해1고정·전부 흡수/무효)은 계수 누수 없음
                if (h.Scored <= 0 || h.Capped || h.Calcs.Count == 0) continue;
                double wSum = h.Calcs.Sum(c => c.Final);
                if (wSum <= 0) continue;
                var ratio = new double[Factors.Length + 1];   // 마지막 = 전 계수 동시
                foreach (var c in h.Calcs)
                {
                    if (c.Final <= 0) continue;
                    double p0 = c.PCond;
                    double baseD = Eval(c, c.Input.Clone(), p0);
                    if (Math.Abs(baseD - c.Final) > 1e-6 * Math.Max(1, c.Final)) { led.RecalcMismatch++; continue; }
                    for (int k = 0; k <= Factors.Length; k++)
                    {
                        double p = p0;
                        var inp = Apply(c, k < Factors.Length ? Factors[k] : null, ref p);
                        double d = Eval(c, inp, p);
                        if (d > baseD) ratio[k] += (d / baseD - 1) * (c.Final / wSum);
                    }
                }
                for (int k = 0; k < Factors.Length; k++)
                {
                    double leak = ratio[k] * h.Scored;
                    if (leak <= 0) continue;
                    led.Factor[Factors[k]] += leak;
                    row.Factor[Factors[k]] = row.Factor.GetValueOrDefault(Factors[k]) + leak;
                }
                double allLeak = ratio[Factors.Length] * h.Scored;
                led.AllBest += allLeak; row.AllBest += allLeak;
            }

            foreach (var kv in _dots)
            {
                led.DotScored += kv.Value.Scored;
                var row = RowOf(-1, -1, kv.Key.Hero, kv.Key.Label + "(DoT)");
                row.Scored += kv.Value.Scored;
                if (kv.Value.Lost > 0)
                {
                    led.Lost["면역"] = led.Lost.GetValueOrDefault("면역") + kv.Value.Lost;
                    row.Lost ??= new();
                    row.Lost["면역"] = row.Lost.GetValueOrDefault("면역") + kv.Value.Lost;
                }
            }

            foreach (var kv in _deadTurns)
            {
                led.DeadTurns[kv.Key] = kv.Value;
                int alive = _turns - kv.Value;
                if (alive > 0 && heroDamage != null && heroDamage.TryGetValue(kv.Key, out double dmg))
                    led.DeathLossEst += dmg / alive * kv.Value;
            }

            // 기준 상태(전투 중 값이 변한 것만 — 변하지 않은 값은 누수가 생길 수 없다)
            for (int b = 0; b < 2; b++)
            {
                var eb = _enemy[b];
                string tag = b == 1 ? "" : "(일반 적)";
                for (int k = 0; k < EnemyFields; k++)
                    if (!double.IsInfinity(eb.Max[k]) && eb.Max[k] - eb.Min[k] > 1e-9)
                        led.Best[$"{EnemyFieldNames[k]}{tag}"] = Math.Round(eb.Max[k], 1);
                if (eb.DebuffCountMin != int.MaxValue && eb.DebuffCount > eb.DebuffCountMin)
                    led.Best[$"디버프 수{tag}"] = eb.DebuffCount;
            }
            foreach (var ab in _ally.Values)
            {
                if (!double.IsInfinity(ab.MidAtk) && ab.MidAtk - ab.MidAtkMin > 1e-9) led.Best[$"{ab.Name} 공버프"] = Math.Round(ab.MidAtk, 1);
                if (!double.IsInfinity(ab.AtkRedMax) && ab.AtkRedMax - ab.AtkRedMin > 1e-9) led.Best[$"{ab.Name} 공감(최대)"] = Math.Round(ab.AtkRedMax, 1);
                for (int k = 0; k < AllyFields; k++)
                    if (!double.IsInfinity(ab.Max[k]) && ab.Max[k] - ab.Min[k] > 1e-9)
                        led.Best[$"{ab.Name} {AllyFieldNames[k]}"] = Math.Round(ab.Max[k], 1);
            }

            led.HitScored = Math.Round(led.HitScored); led.DotScored = Math.Round(led.DotScored);
            led.AllBest = Math.Round(led.AllBest); led.DeathLossEst = Math.Round(led.DeathLossEst);
            foreach (var f in Factors) led.Factor[f] = Math.Round(led.Factor[f]);
            foreach (var k in led.Lost.Keys.ToList()) led.Lost[k] = Math.Round(led.Lost[k]);
            foreach (var r in rows.Values)
            {
                r.Scored = Math.Round(r.Scored); r.AllBest = Math.Round(r.AllBest);
                foreach (var k in r.Factor.Keys.ToList()) r.Factor[k] = Math.Round(r.Factor[k]);
                if (r.Lost != null) foreach (var k in r.Lost.Keys.ToList()) r.Lost[k] = Math.Round(r.Lost[k]);
            }
            led.Rows = rows.Values.Where(r => r.Scored > 0 || r.Lost != null)
                .OrderBy(r => r.Step < 0 ? int.MaxValue : r.Step).ThenByDescending(r => r.Scored).ToList();
            return led;
        }
    }

    /// <summary>시도 1건의 누수 장부(파일 한 줄). results/siege/leaks_{요일}_{프로필}_{팀}.jsonl — 시도 기록에서 다시 만들 수 있는 파생 자료라 매번 덮어쓴다.</summary>
    public class SiegeLeakRecord
    {
        public string Fingerprint { get; set; }
        public double Score { get; set; }
        public double RankScore { get; set; }
        public double DeathCount { get; set; }
        public string Phase { get; set; }
        public string StartOrder { get; set; }
        public bool Adopted { get; set; }           // 어느 단계에서든 채택된 적이 있는 빌드
        public bool Final { get; set; }             // 어느 출발 순서의 탐색이 끝까지 남긴 빌드
        public List<string> ForcedBuffOrder { get; set; }   // null이 아니면 "버프 수령자 강제" 가정 평가(빌드만으로는 안 나오는 전투)
        public string Formation { get; set; }
        public List<string> BackRow { get; set; }
        public List<string> Order { get; set; }
        public bool ReplayOk { get; set; }          // 지금 엔진으로 다시 돌린 점수가 기록과 같은가(다르면 장부를 믿지 말 것)
        public double ReplayScore { get; set; }
        public SiegeLeakLedger Leak { get; set; }
    }

    public static class SiegeLeakStore
    {
        private static readonly JsonSerializerOptions Opts = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public static void Save(string path, IEnumerable<SiegeLeakRecord> recs)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllLines(path, recs.Select(r => JsonSerializer.Serialize(r, Opts)), new UTF8Encoding(false));
        }

        public static List<SiegeLeakRecord> Load(string path)
        {
            var list = new List<SiegeLeakRecord>();
            if (!File.Exists(path)) return list;
            foreach (var line in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try { var r = JsonSerializer.Deserialize<SiegeLeakRecord>(line, Opts); if (r != null) list.Add(r); }
                catch { }
            }
            return list;
        }
    }
}
