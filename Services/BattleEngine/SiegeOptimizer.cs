using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services.Optimizer;

namespace GameDamageCalculator.Services.BattleEngine
{
    /// <summary>공성전 탐색 입력.</summary>
    public class SiegeOptimizerConfig
    {
        public List<BattleCharacter> FixedMembers { get; set; } = new();   // 필수 포함 영웅(버퍼·면역·디버퍼 등)
        public List<BattleCharacter> Candidates { get; set; } = new();     // 나머지 후보 풀
        public Stage SiegeStage { get; set; }
        public Pet AllyPet { get; set; }
        public int PetStar { get; set; }
        public int PetEnhance { get; set; }   // 펫 스킬강화 (0=미강화, 1~3)
        public double PetOptionAtkRate { get; set; }
        public double PetOptionDefRate { get; set; }
        public double PetOptionHpRate { get; set; }
        public int MaxTurns { get; set; } = 70;
        public int PartySize { get; set; } = 5;

        // 장비 미지정 영웅에게 장비 옵티마이저로 최적 장비를 자동 장착할지 (R3 보스 기준 1회)
        public bool AutoEquip { get; set; } = true;
    }

    /// <summary>공성전 탐색 결과 (최고딜 팀 + 진형).</summary>
    public class SiegeOptimizerResult
    {
        public List<BattleCharacter> BestParty { get; set; } = new();
        public string BestFormation { get; set; }
        public double BestScore { get; set; }
        public SiegeBattleResult BestResult { get; set; }
        public int EvaluatedCount { get; set; }     // 평가한 (조합 × 진형) 수
    }

    /// <summary>
    /// 공성전 탐색: 영웅 조합 전수탐색 × 아군 진형 3종(기본/밸런스/보호) 순회 × 공성전 시뮬 → 최고딜 팀.
    /// 후보가 5~8명이라 C(N,5)×3 수준이라 전수탐색이 현실적([[simulator-optimizer-plan]]).
    /// 장비는 입력 BattleCharacter 그대로 사용 — 장비 옵티마이저(영웅×진형 캐싱) 연동은 후속.
    /// </summary>
    public class SiegeOptimizer
    {
        private readonly SiegeBattleSimulator _sim = new();

        // 공성전 아군 순회 진형 (공격 진형 제외 — 기획)
        private static readonly string[] Formations = { "기본 진형", "밸런스 진형", "보호 진형" };

        public SiegeOptimizerResult Optimize(SiegeOptimizerConfig config)
        {
            int remaining = config.PartySize - config.FixedMembers.Count;
            if (remaining < 0) remaining = 0;

            // 장비 자동 장착 (R3 보스 기준, 영웅별 1회) — 팀 탐색 전에 끝내 재사용
            if (config.AutoEquip) EquipCandidates(config);

            SiegeOptimizerResult best = null;
            int evaluated = 0;

            foreach (var combo in Combinations(config.Candidates, remaining))
            {
                var team = new List<BattleCharacter>(config.FixedMembers);
                team.AddRange(combo);
                if (team.Count == 0) continue;

                foreach (var formation in Formations)
                {
                    var simConfig = new SiegeBattleConfig
                    {
                        AllyParty = team,
                        FormationName = formation,
                        SiegeStage = config.SiegeStage,
                        AllyPet = config.AllyPet,
                        PetStar = config.PetStar,
                        PetEnhance = config.PetEnhance,
                        PetOptionAtkRate = config.PetOptionAtkRate,
                        PetOptionDefRate = config.PetOptionDefRate,
                        PetOptionHpRate = config.PetOptionHpRate,
                        MaxTurns = config.MaxTurns,
                    };
                    var result = _sim.Simulate(simConfig);
                    evaluated++;

                    if (best == null || result.TotalScore > best.BestScore)
                    {
                        best = new SiegeOptimizerResult
                        {
                            BestParty = team,
                            BestFormation = formation,
                            BestScore = result.TotalScore,
                            BestResult = result,
                        };
                    }
                }
            }

            if (best != null) best.EvaluatedCount = evaluated;
            return best ?? new SiegeOptimizerResult { EvaluatedCount = 0 };
        }

        /// <summary>
        /// 장비 미지정 영웅에게 장비 옵티마이저로 최적 장비를 장착한다 (R3 보스를 타깃으로 영웅별 1회).
        /// 팀/진형 탐색 전에 한 번만 수행하고 결과(Equipment)를 그대로 재사용한다.
        /// </summary>
        private void EquipCandidates(SiegeOptimizerConfig config)
        {
            var boss = ResolveBoss(config.SiegeStage);
            if (boss == null) return;

            var optimizer = new EquipmentOptimizer();
            foreach (var bc in config.FixedMembers.Concat(config.Candidates))
            {
                if (bc == null || bc.Equipment != null) continue;   // 이미 장비 지정 시 유지
                var soloConfig = new BattleConfig
                {
                    AllyParty = new List<BattleCharacter> { bc },
                    TargetEnemy = boss,
                    FormationName = "기본 진형",
                    AllyPet = config.AllyPet,
                    PetStar = config.PetStar,
                    PetEnhance = config.PetEnhance,
                    PetOptionAtkRate = config.PetOptionAtkRate,
                    PetOptionDefRate = config.PetOptionDefRate,
                    PetOptionHpRate = config.PetOptionHpRate,
                };
                var opt = optimizer.OptimizeForCharacterFast(bc, soloConfig, 0, GetGearConstraints(bc));
                bc.Equipment = opt?.BestLoadout;
            }
        }

        // 딜러 = 공격형·마법형·만능형 / 딜러제외(서포터·탱커) = 지원형·방어형
        private static bool IsDealer(BattleCharacter bc)
        {
            var t = bc.Character.Type;
            return t == "공격형" || t == "마법형" || t == "만능형";
        }

        /// <summary>
        /// 역할 기반 장비 탐색 제약. 치명·약점은 확률 기반 기댓값이라 치확%·약확%도 의미가 있어 포함.
        /// 딜러: 세트 복수자/암살자/추적자/선봉장. 딜러제외: 복수자/수문장(+받피감/생명력/방어력).
        /// </summary>
        private static GearConstraints GetGearConstraints(BattleCharacter bc)
        {
            return IsDealer(bc)
                ? new GearConstraints
                {
                    AllowedSets = new[] { "복수자", "암살자", "추적자", "선봉장" },
                    WeaponMains = new[] { "치명타확률%", "치명타피해%", "공격력%", "약점공격확률%" },
                    ArmorMains = new[] { "공격력%" },
                    SubOptions = new[] { "치명타확률%", "치명타피해%", "약점공격확률%", "공격력%", "공격력" },
                }
                : new GearConstraints
                {
                    AllowedSets = new[] { "복수자", "수문장" },
                    WeaponMains = new[] { "치명타확률%", "치명타피해%", "공격력%", "약점공격확률%" },
                    ArmorMains = new[] { "공격력%", "받피감%" },
                    SubOptions = new[] { "치명타확률%", "치명타피해%", "약점공격확률%", "공격력%", "공격력", "생명력%", "방어력%" },
                };
        }

        /// <summary>
        /// 스테이지 마지막 라운드(R3)의 보스 적을 EnemyDb에서 조회 (장비 평가 타깃).
        /// Enemy.IsBoss=true인 적(예: 스파이크)을 우선 — 보스피증(복수자 등)이 평가에 반영되도록.
        /// (룩/챈슬러 친위대는 R3에서 보스 취급이나 Enemy.IsBoss=false라 그대로 쓰면 보스피증이 빠짐.)
        /// </summary>
        private static Enemy ResolveBoss(Stage stage)
        {
            var wave = stage?.Waves?.OrderByDescending(w => w.WaveNumber).FirstOrDefault();
            if (wave?.Enemies == null || wave.Enemies.Count == 0) return null;

            Enemy Lookup(StageEnemy se) => se == null ? null : EnemyDb.AllEnemies.FirstOrDefault(e => e.Id == se.EnemyId);

            // 1순위: StageEnemy.IsBoss이면서 Enemy.IsBoss=true (실제 보스)
            foreach (var se in wave.Enemies.Where(e => e.IsBoss))
            {
                var en = Lookup(se);
                if (en != null && en.IsBoss) return en;
            }
            // 2순위: 보스 취급 적, 3순위: 아무 적
            return Lookup(wave.Enemies.FirstOrDefault(e => e.IsBoss) ?? wave.Enemies.FirstOrDefault());
        }

        /// <summary>pool에서 k개 조합 (C(N,k)). k=0이면 빈 조합 하나.</summary>
        private static IEnumerable<List<BattleCharacter>> Combinations(List<BattleCharacter> pool, int k)
        {
            if (k <= 0) { yield return new List<BattleCharacter>(); yield break; }
            if (pool == null || pool.Count < k) yield break;

            int n = pool.Count;
            var idx = new int[k];
            for (int i = 0; i < k; i++) idx[i] = i;

            while (true)
            {
                yield return idx.Select(i => pool[i]).ToList();

                int p = k - 1;
                while (p >= 0 && idx[p] == n - k + p) p--;
                if (p < 0) yield break;
                idx[p]++;
                for (int i = p + 1; i < k; i++) idx[i] = idx[i - 1] + 1;
            }
        }
    }
}
