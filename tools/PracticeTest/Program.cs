using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services;
using GameDamageCalculator.Services.BattleEngine;

// 연습전투 테스트: sheet_to_json.py가 만든 team.json → 영웅별 스탯창(펫·진형·버프 제외) 계산.
//   인게임 스탯창과 1:1 비교해 스탯 도출을 먼저 검증한다. (시뮬 전투 비교는 팀 5명·펫·자리 입력 후 추가)
// 사용법: dotnet run --project tools/PracticeTest -- tools/PracticeTest/team.json [요일=월요일] [후열=나타] [펫성급=6] [sim]
//   sim 인자 시: 시트 기어 고정 + 보호진형으로 공성 시뮬(빔 로테) → results/practice/ 에 전투로그 저장.
Console.OutputEncoding = Encoding.UTF8;
var path = args.Length > 0 ? args[0] : "tools/PracticeTest/team.json";
string day = args.Length > 1 ? args[1] : "월요일";
var backRow = (args.Length > 2 ? args[2] : "나타").Split(',').ToList();
int petStar = args.Length > 3 ? int.Parse(args[3]) : 6;
bool runSim = args.Contains("sim");
double cdMargin = double.Parse(args.FirstOrDefault(a => a.StartsWith("margin="))?.Substring(7) ?? "0");
double basicSec = double.Parse(args.FirstOrDefault(a => a.StartsWith("basic="))?.Substring(6) ?? "2.0");
bool ownStart = !args.Contains("ownend");   // 기본 = 시뮬 기본(시전 시작부터). "ownend" = 시전 완료 후부터
var team = new List<BattleCharacter>();   // 시트 순서 = 진형번호 오름차순 = 동속공 행동 순서
using var doc = JsonDocument.Parse(File.ReadAllText(path));

// 시트 오타 정정 (사용자 확인: "포식자" 세트는 없음 → 복수자)
var setAlias = new Dictionary<string, string> { ["포식자"] = "복수자" };

foreach (var h in doc.RootElement.GetProperty("heroes").EnumerateArray())
{
    string name = h.GetProperty("name").GetString();
    var ch = CharacterDb.Characters.FirstOrDefault(c => c.Name == name);
    if (ch == null) { Console.WriteLine($"[{name}] DB에 없음"); continue; }
    int tr = h.GetProperty("transcend").GetInt32();
    bool enh = h.GetProperty("enhanced").GetProperty("skill1").GetBoolean();
    var pot = h.GetProperty("potential");

    // 장비 4부위
    var pieces = new List<Equipment>();
    var fixedNotes = new HashSet<string>();
    foreach (var g in h.GetProperty("gear").EnumerateArray())
    {
        string set = g.GetProperty("set").GetString();
        if (setAlias.TryGetValue(set, out var fixedSet)) { if (fixedNotes.Add(set)) Console.WriteLine($"  (세트 '{set}' → '{fixedSet}' 정정)"); set = fixedSet; }
        if (!EquipmentDb.SetEffects.ContainsKey(set)) Console.WriteLine($"  ⚠ 세트 '{set}' DB에 없음");
        var e = new Equipment { Name = g.GetProperty("name").GetString(), Slot = g.GetProperty("slot").GetString(),
                                SetName = set, MainStatName = g.GetProperty("main").GetString() };
        int i = 0;
        foreach (var s in g.GetProperty("subs").EnumerateArray())
        {
            e.SubSlots[i].StatName = s.GetProperty("stat").GetString();
            e.SubSlots[i].Tier = s.GetProperty("tier").GetInt32();
            i++;
        }
        pieces.Add(e);
    }

    // 장신구 (반지면 메인옵 자리를 반지 효과가 대신)
    var a = h.GetProperty("accessory");
    string Str(JsonElement e, string k) => e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    var acc = new Accessory
    {
        Grade = a.GetProperty("grade").GetInt32(),
        MainOption = Str(a, "main"), SubOption = Str(a, "sub"), RingName = Str(a, "ring"),
    };

    // 전용장비 (없으면 null)
    var exEl = h.GetProperty("exclusive");
    ExclusiveWeapon exw = exEl.ValueKind == JsonValueKind.Null ? null : new ExclusiveWeapon
    {
        Name = "시트", OwnerCharacterId = ch.Id, Atk = exEl.GetProperty("atk").GetDouble(),
        Hp = exEl.GetProperty("hp").GetDouble(), Def = exEl.GetProperty("def").GetDouble(),
        IsMagic = ch.AttackType == AttackType.Magic,
        Tuning = exEl.GetProperty("tuning").EnumerateArray().Select(t => new TuningSlot
        {
            Option = Enum.Parse<TuningOption>(t.GetProperty("option").GetString()),
            Grade = Enum.Parse<ExclusiveWeaponGrade>(t.GetProperty("grade").GetString()),
        }).ToList(),
    };

    var loadout = new EquipmentLoadout { Weapon1 = pieces[0], Weapon2 = pieces[1], Armor1 = pieces[2], Armor2 = pieces[3], Accessory = acc };
    var input = new StatCalculationInput
    {
        Character = ch, TranscendLevel = tr, IsSkillEnhanced = enh,
        Equipments = loadout.GetEquipments(), EquipSets = loadout.GetActiveSets(),
        PotentialAtkLevel = pot.GetProperty("atk").GetInt32(), PotentialDefLevel = pot.GetProperty("def").GetInt32(),
        PotentialHpLevel = pot.GetProperty("hp").GetInt32(),
        Accessory = acc, ExclusiveWeapon = exw,
        // 스탯창 = 펫·진형·파티버프 없음
    };
    var r = new StatCalculator().Calculate(input);
    ch.ExclusiveWeapon = exw;   // 시뮬은 Character.ExclusiveWeapon을 읽는다 (없으면 null = 미장착)
    team.Add(new BattleCharacter
    {
        Character = ch, TranscendLevel = tr, IsSkillEnhanced = enh, Equipment = loadout,
        PotentialAtkLevel = input.PotentialAtkLevel, PotentialDefLevel = input.PotentialDefLevel, PotentialHpLevel = input.PotentialHpLevel,
        IsBackPosition = backRow.Contains(name),
    });
    var d = r.DisplayStats;
    var self = ch.Passive?.GetTotalSelfBuff(enh, tr) ?? new PermanentBuff();

    // 인게임 스탯창 = 패시브 자버프 + 장신구 옵션 줄(메인/부옵) 제외. 장신구 성급 보너스(공·방·생%)는 포함.
    //   실측: 나타 치확 63 = 전투 96 − 패시브 33 / 비스킷 치확 89 = 전투 99 − 장신구 부옵 10
    var accAll = acc.GetTotalStats();
    var accGrade = new Accessory { Grade = acc.Grade }.GetTotalStats();
    var accOpt = new BaseStatSet { Cri = accAll.Cri - accGrade.Cri, Cri_Dmg = accAll.Cri_Dmg - accGrade.Cri_Dmg,
                                   Wek = accAll.Wek - accGrade.Wek, Blk = accAll.Blk - accGrade.Blk };
    self.Cri += accOpt.Cri; self.Cri_Dmg += accOpt.Cri_Dmg; self.Wek += accOpt.Wek;
    string slot = h.TryGetProperty("slot", out var sl) ? sl.ToString() : "?";
    string P(double total, double pas) => pas > 0 ? $"   (전투값 {total:0.#} = + 패시브·장신구옵션 {pas:0.#})" : "";
    double selfAtk = ch.AttackType == AttackType.Magic ? self.MagicAtk_Rate : self.Atk_Rate;
    Console.WriteLine($"\n===== [진형번호{slot}] {name} ({ch.Type}) 초월{tr} · 세트 {string.Join("+", loadout.GetActiveSets().Select(s => $"{s.SetName}{s.PieceCount}"))}"
        + $" · 장신구 {acc.Grade}성 {acc.RingName ?? acc.MainOption}/{acc.SubOption ?? "-"} =====");
    Console.WriteLine($"  공격력   {r.FinalAtk / (1 + selfAtk / 100.0),8:N0}" + (selfAtk > 0 ? $"   (패시브 공% {selfAtk} 적용 시 {r.FinalAtk:N0})" : ""));
    Console.WriteLine($"  방어력   {r.FinalDef,8:N0}");
    Console.WriteLine($"  생명력   {r.FinalHp,8:N0}");
    Console.WriteLine($"  속공     {r.FinalSpd,8:N0}");
    Console.WriteLine($"  치명확률 {d.Cri - self.Cri,7:0.#}%" + P(d.Cri, self.Cri));
    Console.WriteLine($"  치명피해 {d.Cri_Dmg - self.Cri_Dmg,7:0.#}%" + P(d.Cri_Dmg, self.Cri_Dmg));
    Console.WriteLine($"  약점확률 {d.Wek - self.Wek,7:0.#}%" + P(d.Wek, self.Wek));
    Console.WriteLine($"  약점피해 {d.Wek_Dmg - self.Wek_Dmg,7:0.#}%" + P(d.Wek_Dmg, self.Wek_Dmg));
    Console.WriteLine($"  막기확률 {d.Blk - accOpt.Blk,7:0.#}%" + P(d.Blk, accOpt.Blk));
    double amp = (exw?.Tuning ?? new List<TuningSlot>()).Where(t => t.Option == TuningOption.피해증폭).Sum(t => ExclusiveWeapon.GetTuningValue(t.Option, t.Grade));
    Console.WriteLine($"  피해증폭 {amp,7:0.#}%");
    Console.WriteLine($"  [스탯창 외] 피증 {d.Dmg_Dealt:0.#}%(피해증폭 합산 모델) · 보스피증 {d.Dmg_Dealt_Bos:0.#}% · 1-3인기 {d.Dmg_Dealt_1to3:0.#}%");
}

bool runStart = args.Contains("start");
bool runHit = args.Contains("hit");
bool runReplay = args.Contains("replay");
if (!runSim && !runStart && !runHit && !runReplay) return;

// 공통: 펫·진형
var petEl = doc.RootElement.GetProperty("pet");
var pet = PetDb.GetByName(petEl.GetProperty("name").GetString());
int petEnh = petEl.GetProperty("enhance").GetInt32();
double petAtk = petEl.GetProperty("potentialAtkRate").GetDouble();
const string FORMATION = "보호 진형";

if (runReplay)
{
    // ===================== 유저 실전 로테 재생 (연습전투 대조) =====================
    //   2026-10 월요일 연습전투 기록: R1 나타2 / R2 미호2 / R3 18스텝. 실측 11,434,011(약점 2회 미발동) · 최고 12,090,565.
    var userSteps = new (string Hero, SkillType Sk)[]
    {
        ("나타", SkillType.Skill2), ("미호", SkillType.Skill2),                                   // R1 나타2, R2 미호2(교만) — 0턴 클리어 (유저 정정)
        ("비스킷", SkillType.Skill1), ("비스킷", SkillType.Skill2), ("미호", SkillType.Skill1), ("리나", SkillType.Skill2),
        ("나타", SkillType.Skill2), ("나타", SkillType.Skill1), ("미호", SkillType.Skill2), ("비스킷", SkillType.Skill2),
        ("미호", SkillType.Skill1), ("비스킷", SkillType.Skill1), ("나타", SkillType.Skill2), ("나타", SkillType.Skill1),
        ("미호", SkillType.Skill2), ("비스킷", SkillType.Skill2), ("미호", SkillType.Skill1), ("리나", SkillType.Skill2),
        ("나타", SkillType.Skill2), ("나타", SkillType.Skill1),
    };
    // 빔 로테(인게임 시험: 12,557,097 · 턴28 혼천 딱 0초 · 턴32 교만 0.n초 부족) — "beamplan" 인자 시 이걸로 재생
    var beamSteps = new (string Hero, SkillType Sk)[]
    {
        ("미호", SkillType.Skill2), ("비스킷", SkillType.Skill1), ("비스킷", SkillType.Skill2), ("미호", SkillType.Skill1),
        ("나타", SkillType.Skill2), ("리나", SkillType.Skill2), ("미호", SkillType.Skill2), ("나타", SkillType.Skill1),
        ("미호", SkillType.Skill1), ("나타", SkillType.Skill2), ("미호", SkillType.Skill2), ("비스킷", SkillType.Skill1),
        ("나타", SkillType.Skill1), ("미호", SkillType.Skill1), ("나타", SkillType.Skill2), ("리나", SkillType.Skill2),
        ("미호", SkillType.Skill2), ("나타", SkillType.Skill1), ("미호", SkillType.Skill1), ("나타", SkillType.Skill2),
    };
    if (args.Contains("beamplan")) userSteps = beamSteps;
    int Rx(string n) => team.FindIndex(b => b.Character.Name == n);
    var uplan = userSteps.Select(x => new RotationDecision { HeroIndex = Rx(x.Hero), Skill = x.Sk }).ToList();
    foreach (bool force in new[] { false, true })
    {
        var rcfg = new SiegeBattleConfig { AllyParty = team, FormationName = FORMATION, SiegeStage = EnemyDb.SiegeStages[day],
            AllyPet = pet, PetStar = petStar, PetEnhance = petEnh, PetOptionAtkRate = petAtk, MaxTurns = 70,
            RotationPlan = uplan, AllyDeathPenalty = 1_000_000, RecordFeasibility = true, DiagForceCritWeak = force, BasicAttackSeconds = basicSec, OwnCooldownFromCastStart = ownStart };
        var rr = new SiegeBattleSimulator(777).Simulate(rcfg);
        Console.WriteLine($"\n\n########## 유저 로테 재생 · {(force ? "치명·약점 항상 발동(상한)" : "치명·약점 기댓값(시뮬 기본)")} ##########");
        double cum = 0;
        Console.WriteLine("  라운드(누적): " + string.Join(" / ", rr.RoundScore.OrderBy(k => k.Key).Select(k => { cum += k.Value; return $"R{k.Key} {cum:N0}"; })));
        Console.WriteLine($"  총점 {rr.TotalScore:N0} · 사망 {rr.Deaths.Count} · 경과 {rr.ElapsedSeconds:N0}초");
        Console.WriteLine("  영웅별: " + string.Join(" / ", rr.CharacterResults.OrderByDescending(c => c.TotalDamage).Select(c => $"{c.CharacterName} {c.TotalDamage:N0}")));
        if (force)
        {
            // 상한 재생의 아군 스킬 타격 내역 (풀버프 기준값과 비교: 나타 혼천 루디 722,319 / 화첨 405,436)
            Console.WriteLine("  -- 아군 스킬 타격 (치명·약점 전발동) --");
            foreach (var l in rr.TurnLogs.Where(l => l.IsAlly && l.DamageDealt > 0 && l.ActionType == ActionType.SkillAttack))
                Console.WriteLine($"    턴 {l.Turn,2} {l.ActorName}/{l.SkillName}: {l.Description}");
            continue;
        }
        PrintTimeline(rr);
        {
            // 리나 따뜻한 울림 쿨 예측 (평타 쿨감 미수령 → 순수 경과시간): 첫 R3 시전 후 각 스킬턴 시작 시점 잔여 쿨
            var lc2 = new SiegeBattleConfig { AllyParty = team, FormationName = FORMATION, SiegeStage = EnemyDb.SiegeStages[day],
                AllyPet = pet, PetStar = petStar, PetEnhance = petEnh, PetOptionAtkRate = petAtk, MaxTurns = 70,
                RotationPlan = uplan, AllyDeathPenalty = 1_000_000, DiagTimeLedger = true, BasicAttackSeconds = basicSec, OwnCooldownFromCastStart = ownStart };
            var lr2 = new SiegeBattleSimulator(777).Simulate(lc2);
            var led = lr2.TimeLedger;
            int ci = led.FindIndex(e => e.Why.Contains("리나 따뜻한 울림"));
            if (ci >= 0)
            {
                double castEnd = led[ci].Elapsed, castStart = led[ci].Elapsed - led[ci].Sec;
                double cdStart = ownStart ? castStart : castEnd;
                Console.WriteLine($"\n  -- 리나 울림 쿨 예측 (턴 {led[ci].Turn} 시전, 쿨 120, 평타 {basicSec}초, 쿨시작={(ownStart ? "시전시작" : "시전완료")}) --");
                foreach (var e in led.Skip(ci + 1).Where(e => e.Why.StartsWith("[")))
                {
                    double before = e.Elapsed - e.Sec;   // 이 스킬 시전 직전 시각
                    double rem = 120 - (before - cdStart);
                    if (rem < -5) break;
                    Console.WriteLine($"    턴 {e.Turn,2} {e.Why,-28} 직전: 울림 잔여 {rem,6:0.0}초");
                }
            }
        }
        Console.WriteLine("\n  -- 유저 로테 스텝별 쿨 여유 (재시전만, 초) --");
        foreach (var f in rr.Feasibility.Where(f => f.Reached && !f.Hold && f.CooldownGated))
            Console.WriteLine($"    턴 {f.Turn,2} {f.HeroName} {f.SkillName}: 여유 {f.Slack:0.0}초 (경과 {f.Elapsed:0.0}초)");
        // 턴별 평타 행동자 (버프 지속 = 보유자 평타마다 1턴 감소 → 셈으로 유지 여부 확인)
        Console.WriteLine("\n  -- 턴별 평타 행동자 --");
        var basics = rr.TurnLogs.Where(l => l.DamageDealt > 0 && (l.SkillName == "평타" || l.SkillName == "기본 공격"))
            .GroupBy(l => l.Turn).Select(g => $"{g.Key}:{g.First().ActorName}");
        Console.WriteLine("    " + string.Join(" ", basics));
    }
}

if (runHit)
{
    // ===================== 1타 데미지 분해 (연습전투 대조) =====================
    //   R3 루디 대상, 액티브 버프(비스킷 장비강화·리나 따뜻한울림) + 액티브 디버프(미호 살육의춤·리나 따뜻한울림 방깎).
    //   상시 패시브(비스킷 방깎·나타 3인기 등)·펫(윈디 공%·보스취약)은 항상 적용. 루디 링크/보호막 해제 상태.
    int Ix(string n) => team.FindIndex(b => b.Character.Name == n);
    var active = new List<(int, SkillType)> { (Ix("비스킷"), SkillType.Skill1), (Ix("리나"), SkillType.Skill2), (Ix("미호"), SkillType.Skill1) }; if (args.Contains("noactive")) active.Clear();
    // act=비스킷1,리나2 형식으로 액티브 스킬 직접 지정
    var actArg = args.FirstOrDefault(a => a.StartsWith("act="));
    if (actArg != null)
        active = actArg.Substring(4).Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => (Ix(t[..^1]), t[^1] == '1' ? SkillType.Skill1 : SkillType.Skill2)).ToList();
    var hcfg = new SiegeBattleConfig { AllyParty = team, FormationName = FORMATION, SiegeStage = EnemyDb.SiegeStages[day],
        AllyPet = pet, PetStar = petStar, PetEnhance = petEnh, PetOptionAtkRate = petAtk, MaxTurns = 70 };
    string dealerName = args.FirstOrDefault(a => a.StartsWith("dealer="))?.Substring(7) ?? "나타";
    int di = Ix(dealerName);
    var dealerCh = team[di].Character;
    var calc = new DamageCalculator();
    double ampPct = (dealerCh.ExclusiveWeapon?.Tuning ?? new List<TuningSlot>()).Where(t => t.Option == TuningOption.피해증폭).Sum(t => ExclusiveWeapon.GetTuningValue(t.Option, t.Grade));
    Console.WriteLine($"\n\n########## 1타 데미지 · {day} R3 루디 · {dealerName} · 액티브: 비스킷 장비강화 / 리나 따뜻한울림 / 미호 살육의춤 · 링크 해제 ##########");
    bool first = true;
    foreach (var sk in dealerCh.Skills.Where(k => k.SkillType is SkillType.Normal or SkillType.Skill1 or SkillType.Skill2))
    {
        foreach (var (tgt, hp) in new[] { ("루디", 100.0), ("루디", 0.0), ("룩", 0.0), ("챈슬러", 0.0) })
        {
            var hb = new SiegeBattleSimulator(777).EvaluateHitBreakdown(hcfg, di, sk, 3, tgt, hp, active);
            if (first)
            {
                var ip = hb.Input;
                foreach (var lg in hb.Logs) Console.WriteLine("    · " + lg);
                Console.WriteLine($"  [{dealerName} 버프] {hb.DealerBuffs}");
                Console.WriteLine($"  [루디 디버프] {hb.TargetDebuffs}");
                Console.WriteLine($"  공격력 {ip.FinalAtk:N0} · 치피 {ip.CritDamage}% · 약피 {ip.WeakpointDmg}% · 피증 {ip.DmgDealt}% · 피해증폭 ×{1 + ip.DmgAmp / 100:0.00} · 보스피증 {ip.DmgDealtBoss}% · 3인기 {ip.Dmg1to3}% · 방관 {ip.ArmorPen}%");
                Console.WriteLine($"  루디 방어 {hb.TargetDef} · 방깎 {ip.DefReduction}% · 받피증 {ip.DmgTakenIncrease}% · 취약 {ip.Vulnerability}% · 보스취약 {ip.BossVulnerability}% · 공성감쇄 ×{hb.ReductionMult:F2}");
                first = false;
            }
            var d = hb.Detail;
            Console.WriteLine($"\n  ▶ {sk.Name} (배율 {d.SkillRatio * 100:0}% · 방어계수 {d.DefCoefficient:F3} · 피증계수 {d.DamageMultiplier:F3} · 잃은HP ×{d.LostHpMultiplier:F3}) — {tgt}(방어 {hb.TargetDef}) HP {hp}%");
            Console.WriteLine($"     치명+약점 {hb.Combos[(true, true)],10:N0} | 치명만 {hb.Combos[(true, false)],10:N0} | 약점만 {hb.Combos[(false, true)],10:N0} | 둘다X {hb.Combos[(false, false)],10:N0}   (기댓값 {hb.Expected:N0})");
        }
    }
}

if (runStart)
{
    // ===================== 전투 시작 스탯 (펫·진형·파티 상시버프 포함 — 인게임 전투 중 스탯창과 비교) =====================
    Console.WriteLine($"\n\n########## 전투 시작 스탯 · {FORMATION} · 후열 {string.Join(",", backRow)} · 펫 {pet?.Name} {petStar}성 강화{petEnh} 잠재공{petAtk}% ##########");
    var bcfg = new BattleConfig { AllyParty = team, AllyPet = pet, PetStar = petStar, PetEnhance = petEnh, PetOptionAtkRate = petAtk, FormationName = FORMATION };
    var bsim = new BattleSimulator();
    for (int i = 0; i < team.Count; i++)
    {
        var st = bsim.InitializeCharacterState(bcfg, team[i], i);
        var ds = st.DisplayStats;
        Console.WriteLine($"  {team[i].Character.Name,-4}{(team[i].IsBackPosition ? "(후)" : "(전)")} 공 {st.FinalAtk,7:N0} · 방 {st.FinalDef,6:N0} · 생 {st.MaxHp,7:N0} · 속 {st.FinalSpd,3:N0} · 치확 {ds.Cri:0.#}% · 치피 {ds.Cri_Dmg:0.#}% · 약확 {ds.Wek:0.#}% · 약피 {ds.Wek_Dmg:0.#}%");
    }
    // 디버그 로그(stat_debug.txt): 마지막 계산 영웅(나타)의 버프 합산 내역
    try
    {
        var dbg = File.ReadAllLines("stat_debug.txt");
        Console.WriteLine("\n  [나타 버프% 내역 (stat_debug.txt)]");
        foreach (var line in dbg.SkipWhile(l => !l.Contains("[버프%")).Take(18)) Console.WriteLine("   " + line);
        foreach (var line in dbg.Where(l => l.Contains("별도 보너스") || l.Contains("진형공%") || l.Contains("기초공"))) Console.WriteLine("   " + line);
    }
    catch { }
    if (!runSim) return;
}

// ===================== 공성 시뮬 (시트 기어 고정) =====================
var stage = EnemyDb.SiegeStages[day];
Console.WriteLine($"\n\n########## {day} 시뮬 · {FORMATION} · 후열 {string.Join(",", backRow)} · 펫 {pet?.Name} {petStar}성 강화{petEnh} 잠재공{petAtk}% ##########");
Console.WriteLine("파티(동속공 행동순): " + string.Join(" / ", team.Select((b, i) => $"{i + 1}:{b.Character.Name}{(b.IsBackPosition ? "(후)" : "")}")));

SiegeBattleConfig Cfg(List<RotationDecision> plan) => new()
{
    AllyParty = team, FormationName = FORMATION, SiegeStage = stage,
    AllyPet = pet, PetStar = petStar, PetEnhance = petEnh, PetOptionAtkRate = petAtk, MaxTurns = 70,
    RotationPlan = plan, AllyDeathPenalty = 1_000_000, RecordFeasibility = plan != null, AllyCooldownMargin = cdMargin, BasicAttackSeconds = basicSec, OwnCooldownFromCastStart = ownStart,
};

var opt = new SiegeOptimizer().Optimize(new SiegeOptimizerConfig
{
    FixedMembers = team, Candidates = new List<BattleCharacter>(), SiegeStage = stage, MaxTurns = 70,
    AutoEquip = false, AllyPet = pet, PetStar = petStar, PetEnhance = petEnh, PetOptionAtkRate = petAtk,
    ForcedFormation = FORMATION, ForcedBackRow = backRow, DummyBackRow = false,
    OptimizeRotation = true, RotationBeamWidth = 10, RotationMaxDepth = 36,
    AllyDeathPenalty = 1_000_000, EnableSurvivalRings = false, SearchExclusiveWeapon = false, AllyCooldownMargin = cdMargin,
});
// 옵티마이저가 자리 플래그를 덮어쓸 수 있으니 재확정
foreach (var b in team) b.IsBackPosition = backRow.Contains(b.Character.Name);

void Report(string label, SiegeBattleResult r)
{
    Console.WriteLine($"\n[{label}] 총점 {r.TotalScore:N0} · {r.RoundsCleared}R 클리어 · 경과 {r.ElapsedSeconds:N0}초 · 사망 {r.Deaths.Count}"
        + (r.Deaths.Count > 0 ? " (" + string.Join(", ", r.Deaths.Select(x => $"T{x.Turn} {x.AllyName}←{x.Cause}")) + ")" : ""));
    Console.WriteLine("  라운드: " + string.Join(" / ", r.RoundScore.OrderBy(k => k.Key).Select(k => $"R{k.Key} {k.Value:N0}")));
    Console.WriteLine("  영웅별: " + string.Join(" / ", r.CharacterResults.OrderByDescending(c => c.TotalDamage).Select(c => $"{c.CharacterName} {c.TotalDamage:N0}")));
}

var auto = new SiegeBattleSimulator(777).Simulate(Cfg(null));
Report("자동 로테(궁→2→1 우선)", auto);

var plan = opt.BestRotationPlan.Select(x => new RotationDecision { HeroIndex = x.HeroIndex, Skill = x.Skill, Hold = x.Hold }).ToList();
var best = new SiegeBattleSimulator(777).Simulate(Cfg(plan));
Report("빔 로테", best);
PrintTimeline(best);
{
    var lc = Cfg(plan); lc.DiagTimeLedger = true;
    var lr = new SiegeBattleSimulator(777).Simulate(lc);
    Console.WriteLine("\n  -- [시간 장부] 빔 로테 턴 5~32 (턴 · +초 · 누적경과 · 사유) --");
    foreach (var e in lr.TimeLedger.Where(e => e.Turn >= 5 && e.Turn <= 32))
        Console.WriteLine($"    턴 {e.Turn,2}  +{e.Sec:0.0}s  → {e.Elapsed,6:0.0}s  {e.Why}");
    Console.WriteLine("  -- 평타 쿨감 이벤트 (턴 5~32) --");
    foreach (var l in lr.TurnLogs.Where(l => l.Turn >= 5 && l.Turn <= 32 && (l.Description ?? "").Contains("평타 쿨감")))
        Console.WriteLine($"    턴 {l.Turn,2}  {l.ActorName}: {l.Description}");
}
Console.WriteLine("\n  -- 빔 로테 스텝별 쿨 여유 (재시전만; 여유 = 시전 시각 − 쿨 완료 시각, 초) --");
foreach (var f in best.Feasibility.Where(f => f.Reached && !f.Hold && f.CooldownGated))
    Console.WriteLine($"    턴 {f.Turn,2} {f.HeroName} {f.SkillName}: 여유 {f.Slack:0.0}초 (경과 {f.Elapsed:0.0}초)");
{ var bc2 = Cfg(plan); bc2.DiagForceCritWeak = true; Console.WriteLine($"  (치명·약점 항상 발동 상한: {new SiegeBattleSimulator(777).Simulate(bc2).TotalScore:N0})"); }
Console.WriteLine("  스킬 순서: " + string.Join(" → ", best.Feasibility.Where(f => f.Reached && !f.Hold && (f.ExecutedAsPlanned || f.IsAuto))
    .Select(f => $"{f.HeroName} {f.SkillName}")));

// 아군 피격 요약 (광폭화 검증용): 적 스킬별·턴별 아군이 받은 피해
Console.WriteLine("\n  -- 아군 피격 (광폭화 검증용: 같은 적 스킬의 턴별 피해 비교) --");
foreach (var l in best.TurnLogs.Where(l => !l.IsAlly && l.DamageDealt > 0 && (l.Description ?? "").Contains("→")))
    Console.WriteLine($"    T{l.Turn,2} {l.ActorName}/{l.SkillName}: {l.Description}");

var dir = Path.Combine("results", "practice");
Directory.CreateDirectory(dir);
var logPath = Path.Combine(dir, $"battlelog_{day}_연습전투_시뮬.txt");
var sb = new StringBuilder();
sb.AppendLine($"=== {day} 연습전투 시뮬 (시트 기어 고정 · {FORMATION} · 후열 {string.Join(",", backRow)} · 총점 {best.TotalScore:N0}) ===");
foreach (var l in best.TurnLogs)
    sb.AppendLine($"T{l.Turn,2} [{(l.IsAlly ? "아" : "적")}] {l.ActorName,-6} {l.SkillName,-10} {(l.DamageDealt > 0 ? $"{l.DamageDealt,12:N0}" : "".PadLeft(12))}  {l.Description}");
File.WriteAllText(logPath, sb.ToString(), Encoding.UTF8);
Console.WriteLine($"\n  [전투로그] {logPath}");

// 스킬 타임라인: 아군 플랜 스텝 + 적 스킬 시전 + 라운드 전환 (연습전투 기록 형식과 대조)
static void PrintTimeline(SiegeBattleResult rr)
{
    var timeline = new List<(int Turn, int Ord, string Text)>();
    int ord = 0;
    foreach (var f in rr.Feasibility.Where(f => f.Reached && !f.Hold))
        timeline.Add((f.Turn, ord++, $"[아] {f.HeroName} {f.SkillName}" + (f.ExecutedAsPlanned || f.IsAuto ? "" : $"  ✗폴백: {f.FallbackReason}") + (f.IsAuto ? " (플랜 밖 자동)" : "")));
    foreach (var l in rr.TurnLogs.Where(l => !l.IsAlly && l.SkillName == "쿨감" && (l.Description ?? "").StartsWith("적 ")))
        timeline.Add((l.Turn, ord++, $"[적] {l.ActorName} {l.Description.Split('(')[0].Substring(2)}"));
    foreach (var l in rr.TurnLogs.Where(l => (l.SkillName ?? "") == "라운드 전환"))
        timeline.Add((l.Turn, ord++, $"── {l.Description}"));
    Console.WriteLine("\n  -- 스킬 타임라인 (턴 = 누적) --");
    foreach (var t in timeline.OrderBy(t => t.Turn).ThenBy(t => t.Ord)) Console.WriteLine($"    턴 {t.Turn,2}  {t.Text}");
}
