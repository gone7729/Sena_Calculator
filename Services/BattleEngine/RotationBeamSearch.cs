using System.Collections.Generic;
using System.Linq;

namespace GameDamageCalculator.Services.BattleEngine
{
    /// <summary>
    /// 공성전 스킬 로테이션 빔 서치. 각 아군 스킬턴마다 "어떤 아군이 어떤 스킬을 쓸지(또는 홀드)"를
    /// 플랜으로 고정하고, 고정 시드 풀시뮬 점수로 평가. 깊이(스킬턴)별로 상위 K개 플랜만 확장한다.
    ///
    /// 결정론적 재실행 방식(상태 복제 없음): 부분 플랜을 끝부터는 자동(PickAllySkill)으로 채워
    /// 항상 70턴 풀시뮬 점수를 얻고, 그 점수로 빔을 추린다. "버프 켜고→버스트" 같은 타이밍 정렬을
    /// 홀드/순서 분기로 자연히 탐색한다.
    /// </summary>
    public class RotationBeamSearch
    {
        public class Result
        {
            public List<RotationDecision> Plan { get; set; } = new();
            public double Score { get; set; }
            public SiegeBattleResult Battle { get; set; }
            public int Evaluated { get; set; }
            public List<double> ScoreByDepth { get; set; } = new();   // 깊이별 최고점 추이
            // 생존 트랙: RankScore(사망 페널티 반영) 최고 플랜. raw 최고와 다를 수 있다(예: 토요일
            //   해제 로테로 사망을 피한 플랜). 호출부가 둘 다 재평가해 RankScore 우선으로 채택한다.
            public List<RotationDecision> RankPlan { get; set; }
            public double RankScore { get; set; }
        }

        private class Beam
        {
            public List<RotationDecision> Plan;
            public double Score;          // 이 플랜의 풀시뮬 점수(디폴트 꼬리 포함)
            public double Rank;           // RankScore(=Score − 사망페널티) — 생존 트랙 랭킹용
            public double LookScore;      // 1-스텝 lookahead 점수(자식 중 최고) — 가지치기 랭킹용
            public double LookRank;       // 1-스텝 lookahead RankScore — 생존 트랙 가지치기용
            public List<RotationDecisionPoint> Dps;
            public SiegeBattleResult Battle;
        }

        private readonly int _seed;
        private int _evaluated;
        private int _counterAvgSeeds = 1;   // >1이면 각 플랜을 반격 ON N시드 평균으로 평가(금요일)

        public RotationBeamSearch(int seed = 777) => _seed = seed;

        /// <param name="baseConfig">기어·진형·자리·펫이 세팅된 풀시뮬 설정 (RotationPlan은 여기서 덮어씀).</param>
        /// <param name="beamWidth">유지할 상위 플랜 수 K.</param>
        /// <param name="maxDepth">탐색할 최대 스킬턴 수(보통 전체 아군 스킬턴 수면 충분).</param>
        public Result Search(SiegeBattleConfig baseConfig, int beamWidth = 12, int maxDepth = 24, int counterAvgSeeds = 1)
        {
            _evaluated = 0;
            _counterAvgSeeds = System.Math.Max(1, counterAvgSeeds);
            // 빔서치는 반격 OFF(0%)로 로테를 평가·산출 — 반격 RNG·시간경과(쿨 흔들림)에 무의존한 견고한 로테.
            //   금요일(제이브)만 영향; 그 외 보스는 Counterattack=null이라 무관. 채택 점수는 호출부가 ON으로 재평가.
            //   ★counterAvgSeeds>1(금요일): 반격 ON으로 평가하되 N시드 평균점으로 로테 선택(Run에서 처리).
            //     이 경우 OFF 강제는 Run 내부에서 Dps(구조)용으로만 적용하고 점수는 ON 평균.
            if (_counterAvgSeeds <= 1)
                baseConfig.CounterattackChanceOverride = 0;
            var init = Run(baseConfig, new List<RotationDecision>());
            var best = new Result
            {
                Plan = new List<RotationDecision>(),
                Score = init.Score,
                Battle = init.Battle,
                ScoreByDepth = { init.Score },
                RankPlan = new List<RotationDecision>(),
                RankScore = init.Rank,
            };
            var beams = new List<Beam> { init };

            for (int d = 0; d < maxDepth; d++)
            {
                var candidates = new List<Beam>();
                bool extended = false;

                foreach (var beam in beams)
                {
                    if (d >= beam.Dps.Count)
                    {
                        candidates.Add(beam);   // 더 확장할 결정점 없음(이미 전투 종료) → 완성본 유지
                        continue;
                    }
                    foreach (var choice in beam.Dps[d].Choices)
                    {
                        var newPlan = new List<RotationDecision>(beam.Plan) { choice };
                        candidates.Add(Run(baseConfig, newPlan));
                        extended = true;
                    }
                }

                if (candidates.Count == 0) break;

                // 1-스텝 lookahead 재평가: 셋업(예: 리나 따뜻한울림 피증/방깎, 레이첼 불새)은 자기 점수는
                // 낮아 즉시점수 상위 풀에 못 들고 가지치기되기 쉽다. 그러면 다음 스텝(나타 버스트)이 따라붙어
                // 점수가 크게 오르는 보상이 계산되기 전에 사라진다. → lookahead를 전체 후보에 적용해
                // "셋업→버스트" 후보가 살아남게 한다. (best는 실제 점수로 추적)
                var ranked = candidates;
                foreach (var c in ranked)
                {
                    c.LookScore = c.Score;
                    c.LookRank = c.Rank;
                    if (d + 1 < c.Dps.Count)
                        foreach (var choice2 in c.Dps[d + 1].Choices)
                        {
                            var child = Run(baseConfig, new List<RotationDecision>(c.Plan) { choice2 });
                            if (child.Score > c.LookScore) c.LookScore = child.Score;
                            if (child.Rank > c.LookRank) c.LookRank = child.Rank;
                        }
                }
                // 가지치기: raw 상위 K ∪ 생존(RankScore) 상위 K — 고점 버스트 라인과 생존 라인을 모두 유지.
                //   사망이 없는 구간에서는 두 랭킹이 동일해 풀 크기가 K 그대로(추가 비용 0).
                beams = ranked.OrderByDescending(c => c.LookScore).Take(beamWidth)
                    .Union(ranked.OrderByDescending(c => c.LookRank).Take(beamWidth))
                    .ToList();

                var top = candidates.OrderByDescending(c => c.Score).First();   // best는 실제 점수 기준
                if (top.Score > best.Score)
                {
                    best.Plan = top.Plan;
                    best.Score = top.Score;
                    best.Battle = top.Battle;
                }
                var topRank = candidates.OrderByDescending(c => c.Rank).First();
                if (topRank.Rank > best.RankScore)
                {
                    best.RankPlan = topRank.Plan;
                    best.RankScore = topRank.Rank;
                }
                best.ScoreByDepth.Add(best.Score);

                if (!extended) break;   // 모든 빔이 완성됨
            }

            best.Evaluated = _evaluated;
            return best;
        }

        private Beam Run(SiegeBattleConfig baseConfig, List<RotationDecision> plan)
        {
            _evaluated++;
            baseConfig.RotationPlan = plan;
            baseConfig.RecordDecisionPoints = true;

            if (_counterAvgSeeds <= 1)
            {
                var battle = new SiegeBattleSimulator(_seed).Simulate(baseConfig);
                return new Beam
                {
                    Plan = plan,
                    Score = battle.TotalScore,  // raw(기대값 딜) 트랙 — 고점 버스트 라인 탐색용
                    Rank = battle.RankScore,    // 생존 트랙 — 사망 페널티 반영, 최종 채택은 이 기준(옵티마이저)
                    Dps = battle.DecisionPoints,
                    Battle = battle,
                };
            }

            // [반격 ON N시드 평균] 금요일: 플랜 구조(Dps)는 OFF로 결정론 유지(시드별 턴구조 흔들림 방지),
            //   점수·생존은 반격 ON N시드 평균으로 평가 → 빔이 "평균점 최고" 로테를 고른다.
            baseConfig.CounterattackChanceOverride = 0;
            var offBattle = new SiegeBattleSimulator(_seed).Simulate(baseConfig);   // Dps(결정점) 전용
            baseConfig.CounterattackChanceOverride = null;                          // 반격 ON(보스 정의 25%)
            double sumScore = 0, sumRank = 0;
            SiegeBattleResult firstOn = null;
            for (int s = 0; s < _counterAvgSeeds; s++)
            {
                var b = new SiegeBattleSimulator(_seed + s).Simulate(baseConfig);
                sumScore += b.TotalScore;
                sumRank += b.RankScore;
                firstOn ??= b;
            }
            return new Beam
            {
                Plan = plan,
                Score = sumScore / _counterAvgSeeds,
                Rank = sumRank / _counterAvgSeeds,
                Dps = offBattle.DecisionPoints,
                Battle = firstOn,
            };
        }
    }
}
