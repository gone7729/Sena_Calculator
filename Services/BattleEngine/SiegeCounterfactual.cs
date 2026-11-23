using System;
using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Services.BattleEngine
{
    // ============================================================================
    // 반사실 확인 (2026-11-23) — 시도 기록(1) → 누수 장부(2) 다음 단계.
    //   누수 장부는 "이 타격이 최고 상태가 아니어서 덜 들어갔다"까지만 말한다. 그게 실제로 되찾을 수 있는 딜인지는
    //   스킬 순서에서 **행동 하나만** 바꿔(장비·자리는 그대로) 다시 돌려 봐야 안다.
    //     편집 6종: 이동(한 시전을 다른 자리로 — 사이의 시전이 한 칸씩 밀림) / 맞바꿈(두 시전 교환, 거리 제한 없음) /
    //               교체(그 칸을 다른 영웅·스킬로) / 끼움(새 시전을 끼워 넣고 뒤를 한 칸씩 미룸) / 빼기(그 시전을 빼고 뒤를 당김) / 홀드(그 칸을 비움)
    //   점수 변화의 분해: 상한 = 점수 + 동시 누수(= 그 전투의 모든 타격이 그 전투의 최고 상태로 들어갔을 때의 점수).
    //     Δ점수 = Δ상한 − Δ누수.  Δ누수 < 0 = 누수를 막았다.  Δ상한 < 0 = 얻을 수 있던 딜 자체가 줄었다
    //     (버스트 횟수 감소·최고 상태 하락·사망). 누수를 막아도 상한이 그만큼 내려가면 되찾은 게 아니다.
    //   로테 다듬기(SiegeOptimizer.PolishPlan)가 보는 수는 교체와 3칸 이내 맞바꿈뿐 — 이동·끼움·빼기·먼 맞바꿈은 여기서 처음 본다.
    // ============================================================================
    public class SiegePlanEdit
    {
        public string Kind { get; set; }            // 이동 / 맞바꿈 / 교체 / 끼움 / 빼기 / 홀드
        public int From { get; set; } = -1;         // 기준 플랜의 스텝(0부터)
        public int To { get; set; } = -1;           // 이동: 옮겨 간 위치 · 맞바꿈: 상대 스텝
        public RotationDecision Cast { get; set; }  // 교체·끼움에서 새로 넣는 시전
        public List<RotationDecision> Plan { get; set; }
        public int[] NewPos { get; set; }           // 기준 스텝 → 편집 후 위치(-1 = 그 시전이 사라짐)

        /// <summary>기준 스텝 k의 시전 앞에, 원래는 그 뒤에 있던 시전(또는 새로 끼운 시전)이 오게 되는가
        ///   — "k가 맞기 전에 상태를 더 만들어 둔다"는 가설에 해당하는 편집인지.</summary>
        public bool PutsSomethingBefore(int k)
        {
            if (NewPos == null || k < 0 || k >= NewPos.Length || NewPos[k] < 0) return false;
            if (Kind == "끼움") return From <= k;
            for (int m = k + 1; m < NewPos.Length; m++)
                if (NewPos[m] >= 0 && NewPos[m] < NewPos[k]) return true;
            return false;
        }
    }

    /// <summary>플랜 하나를 돌린 결과(누수 장부 포함).</summary>
    public class SiegeCounterfactualRun
    {
        public SiegePlanEdit Edit { get; set; }     // null = 기준(편집 없음)
        public List<RotationDecision> Plan { get; set; }
        public double Score { get; set; }
        public double Rank { get; set; }
        public int Deaths { get; set; }
        public SiegeLeakLedger Leak { get; set; }
        public double AllBest => Leak?.AllBest ?? 0;
        public double Ceiling => Score + AllBest;   // 모든 타격이 그 전투의 최고 상태로 들어갔다면
    }

    public static class SiegeCounterfactual
    {
        public static string Sig(IEnumerable<RotationDecision> plan) => string.Join(",", plan.Select(d => d.ToString()));

        private static RotationDecision Copy(RotationDecision d) => new() { HeroIndex = d.HeroIndex, Skill = d.Skill, Hold = d.Hold };
        private static List<RotationDecision> Clone(List<RotationDecision> plan) => plan.Select(Copy).ToList();

        /// <summary>플랜을 누수 장부를 켜고 한 번 돌린다(탐색과 같은 조건·시드 777).</summary>
        public static SiegeCounterfactualRun Run(SiegeOptimizerConfig config, List<BattleCharacter> party, string formation,
            List<RotationDecision> plan, SiegePlanEdit edit = null)
        {
            var r = SiegeOptimizer.ReplayWithCasts(config, party, formation, plan, leak: true);
            return new SiegeCounterfactualRun
            {
                Edit = edit, Plan = plan, Score = r.TotalScore, Rank = r.RankScore, Deaths = r.Deaths?.Count ?? 0, Leak = r.Leak,
            };
        }

        /// <summary>
        /// 기준 플랜에서 행동 하나만 바꾼 플랜 전부(같은 플랜은 한 번만). steps = 편집 대상 스텝 수(전투에서 실제로 도달한 스킬턴 수).
        /// 교체·끼움 후보 = 파티 영웅 × 스킬1·스킬2 (PolishPlan과 같은 풀).
        /// </summary>
        public static List<SiegePlanEdit> SingleEdits(List<RotationDecision> plan, int steps, List<BattleCharacter> party)
        {
            int n = Math.Min(steps, plan.Count);
            var list = new List<SiegePlanEdit>();
            var seen = new HashSet<string> { Sig(plan) };
            int[] Identity() => Enumerable.Range(0, n).ToArray();
            void Add(SiegePlanEdit e) { if (seen.Add(Sig(e.Plan))) list.Add(e); }

            var casts = new List<RotationDecision>();
            for (int h = 0; h < party.Count; h++)
                foreach (var sk in new[] { SkillType.Skill1, SkillType.Skill2 })
                    if (party[h].Character.Skills?.Any(s => s.SkillType == sk) == true)
                        casts.Add(new RotationDecision { HeroIndex = h, Skill = sk });

            // 이동: j의 시전을 i 자리로. i<j면 i..j-1이 한 칸 뒤로, i>j면 j+1..i가 한 칸 앞으로.
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    if (i == j || plan[j].Hold) continue;
                    var p = Clone(plan);
                    var d = p[j]; p.RemoveAt(j); p.Insert(i, d);
                    var pos = Identity();
                    pos[j] = i;
                    if (i < j) for (int m = i; m < j; m++) pos[m] = m + 1;
                    else for (int m = j + 1; m <= i; m++) pos[m] = m - 1;
                    Add(new SiegePlanEdit { Kind = "이동", From = j, To = i, Plan = p, NewPos = pos });
                }
            // 맞바꿈: 거리 제한 없음 (인접 맞바꿈은 이동과 같은 플랜이라 위에서 이미 들어감)
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                {
                    var p = Clone(plan);
                    (p[i], p[j]) = (p[j], p[i]);
                    var pos = Identity();
                    pos[i] = j; pos[j] = i;
                    Add(new SiegePlanEdit { Kind = "맞바꿈", From = i, To = j, Plan = p, NewPos = pos });
                }
            // 교체: 그 칸을 다른 영웅·스킬로 (원래 시전은 사라짐)
            for (int i = 0; i < n; i++)
                foreach (var c in casts)
                {
                    if (!plan[i].Hold && plan[i].HeroIndex == c.HeroIndex && plan[i].Skill == c.Skill) continue;
                    var p = Clone(plan);
                    p[i] = Copy(c);
                    var pos = Identity();
                    pos[i] = -1;
                    Add(new SiegePlanEdit { Kind = "교체", From = i, Cast = c, Plan = p, NewPos = pos });
                }
            // 끼움: i 앞에 새 시전을 넣고 i 이후를 한 칸씩 미룬다
            for (int i = 0; i < n; i++)
                foreach (var c in casts)
                {
                    var p = Clone(plan);
                    p.Insert(i, Copy(c));
                    var pos = Identity();
                    for (int m = i; m < n; m++) pos[m] = m + 1;
                    Add(new SiegePlanEdit { Kind = "끼움", From = i, Cast = c, Plan = p, NewPos = pos });
                }
            // 빼기: 그 시전을 빼고 뒤를 한 칸씩 당긴다 (끝 칸은 플랜의 다음 칸 또는 자동 로테가 채움)
            for (int i = 0; i < n; i++)
            {
                var p = Clone(plan);
                p.RemoveAt(i);
                var pos = Identity();
                pos[i] = -1;
                for (int m = i + 1; m < n; m++) pos[m] = m - 1;
                Add(new SiegePlanEdit { Kind = "빼기", From = i, Plan = p, NewPos = pos });
            }
            // 홀드: 그 스킬턴을 비운다
            for (int i = 0; i < n; i++)
            {
                if (plan[i].Hold) continue;
                var p = Clone(plan);
                p[i] = new RotationDecision { Hold = true };
                var pos = Identity();
                pos[i] = -1;
                Add(new SiegePlanEdit { Kind = "홀드", From = i, Plan = p, NewPos = pos });
            }
            return list;
        }

        /// <summary>"영웅 스킬이름" (홀드는 "(홀드)").</summary>
        public static string CastName(RotationDecision d, List<BattleCharacter> party)
        {
            if (d == null || d.Hold) return "(홀드)";
            if (d.HeroIndex < 0 || d.HeroIndex >= party.Count) return "?";
            var c = party[d.HeroIndex].Character;
            return $"{c.Name} {c.Skills?.FirstOrDefault(s => s.SkillType == d.Skill)?.Name ?? d.Skill.ToString()}";
        }

        /// <summary>편집 설명. stepLabel(i) = 기준 플랜 i번 칸의 표기(예: "#5 T12 라이언 광풍참").</summary>
        public static string Describe(SiegePlanEdit e, List<BattleCharacter> party, Func<int, string> stepLabel) => e.Kind switch
        {
            "이동" => $"이동  {stepLabel(e.From)}  →  {stepLabel(e.To)} {(e.To < e.From ? "앞" : "뒤")}",
            "맞바꿈" => $"맞바꿈  {stepLabel(e.From)}  ↔  {stepLabel(e.To)}",
            "교체" => $"교체  {stepLabel(e.From)}  ⇒  {CastName(e.Cast, party)}",
            "끼움" => $"끼움  {CastName(e.Cast, party)}  →  {stepLabel(e.From)} 앞",
            "빼기" => $"빼기  {stepLabel(e.From)}",
            "홀드" => $"홀드  {stepLabel(e.From)}",
            _ => e.Kind,
        };

        /// <summary>기준 전투의 한 시전 행(영웅·스킬의 n번째 시전)에 대응하는 편집 후 전투의 행. 없으면 null.</summary>
        public static SiegeLeakLedger.Row MatchRow(SiegeLeakLedger baseLeak, SiegeLeakLedger.Row baseRow, SiegeLeakLedger other)
        {
            if (baseLeak == null || other == null || baseRow == null || baseRow.Step < 0) return null;
            bool Same(SiegeLeakLedger.Row r) => r.Step >= 0 && r.Hero == baseRow.Hero && r.Skill == baseRow.Skill;
            int occ = baseLeak.Rows.Where(Same).OrderBy(r => r.Step).ToList().IndexOf(baseRow);
            if (occ < 0) return null;
            return other.Rows.Where(Same).OrderBy(r => r.Step).Skip(occ).FirstOrDefault();
        }
    }
}
