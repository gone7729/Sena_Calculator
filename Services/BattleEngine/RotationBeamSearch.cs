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
        }

        private class Beam
        {
            public List<RotationDecision> Plan;
            public double Score;
            public List<RotationDecisionPoint> Dps;
            public SiegeBattleResult Battle;
        }

        private readonly int _seed;
        private int _evaluated;

        public RotationBeamSearch(int seed = 777) => _seed = seed;

        /// <param name="baseConfig">기어·진형·자리·펫이 세팅된 풀시뮬 설정 (RotationPlan은 여기서 덮어씀).</param>
        /// <param name="beamWidth">유지할 상위 플랜 수 K.</param>
        /// <param name="maxDepth">탐색할 최대 스킬턴 수(보통 전체 아군 스킬턴 수면 충분).</param>
        public Result Search(SiegeBattleConfig baseConfig, int beamWidth = 12, int maxDepth = 24)
        {
            _evaluated = 0;
            var init = Run(baseConfig, new List<RotationDecision>());
            var best = new Result
            {
                Plan = new List<RotationDecision>(),
                Score = init.Score,
                Battle = init.Battle,
                ScoreByDepth = { init.Score },
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
                beams = candidates.OrderByDescending(c => c.Score).Take(beamWidth).ToList();

                var top = beams[0];
                if (top.Score > best.Score)
                {
                    best.Plan = top.Plan;
                    best.Score = top.Score;
                    best.Battle = top.Battle;
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
            var battle = new SiegeBattleSimulator(_seed).Simulate(baseConfig);
            return new Beam
            {
                Plan = plan,
                Score = battle.TotalScore,
                Dps = battle.DecisionPoints,
                Battle = battle,
            };
        }
    }
}
