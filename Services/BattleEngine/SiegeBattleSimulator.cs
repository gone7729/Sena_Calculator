using System;
using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Services.BattleEngine
{
    /// <summary>
    /// 공성전 턴제 시뮬레이터 (2단계: 턴 진행 골격).
    /// 데이터·규칙은 메모리 siege-simulation-rules / battle-time-model.
    ///
    /// 현재 구현(2번): 선공 결정 → 스킬턴 사이클(0턴부터 2턴마다 선/후 번갈아) →
    ///   기본공격(양팀 통합 속공순) → 70턴 / 라운드 전환(R1·R2 적 전멸 시) → 점수 누적.
    /// 후속: 정확한 데미지(DamageCalculator)·정교 타겟팅(약점·앞열·R3보스)=3번, 적 행동(아군 피격·생존)=4번.
    ///   현재는 placeholder 데미지로 턴 흐름과 라운드 전환을 검증한다.
    /// </summary>
    public class SiegeBattleSimulator
    {
        private readonly BattleSimulator _baseSim = new();  // 아군 스탯 초기화 재사용
        private readonly Random _rng = new();

        public SiegeBattleResult Simulate(SiegeBattleConfig config)
        {
            var state = Initialize(config);
            RunTurnLoop(config, state);
            return BuildResult(state);
        }

        #region 초기화

        private SiegeBattleState Initialize(SiegeBattleConfig config)
        {
            var state = new SiegeBattleState
            {
                SiegeStage = config.SiegeStage,
                MaxTurns = config.MaxTurns,
            };

            // 아군 스탯 — 기존 BattleSimulator.InitializeCharacterState 재사용 (TargetEnemy 불필요)
            var tempConfig = new BattleConfig
            {
                AllyParty = config.AllyParty,
                AllyPet = config.AllyPet,
                PetStar = config.PetStar,
                PetOptionAtkRate = config.PetOptionAtkRate,
                PetOptionDefRate = config.PetOptionDefRate,
                PetOptionHpRate = config.PetOptionHpRate,
                FormationName = config.FormationName,   // PVE: 빈 값 → 진형 효과 미적용
            };
            for (int i = 0; i < config.AllyParty.Count; i++)
                state.AllyStates.Add(_baseSim.InitializeCharacterState(tempConfig, config.AllyParty[i], i));

            // 1라운드 적 생성
            state.InitializeRound(1);

            // 선공 결정 (팀 총 속공치, 동률 랜덤)
            double allySpd = state.AllyTotalSpd, enemySpd = state.EnemyTotalSpd;
            state.AllyFirst = allySpd > enemySpd || (allySpd == enemySpd && _rng.Next(2) == 0);

            return state;
        }

        #endregion

        #region 턴 루프

        private void RunTurnLoop(SiegeBattleConfig config, SiegeBattleState state)
        {
            var order = BuildActionOrder(state);
            int cursor = 0;
            int t = 0;

            while (t < state.MaxTurns)
            {
                state.CurrentTurn = t;

                // 스킬턴: 0턴부터 2턴마다, 선/후공 번갈아 (턴 미소모, 기본공격 안 함)
                if (t % 2 == 0)
                {
                    bool firstSideTurn = (t / 2) % 2 == 0;                 // 이번 스킬턴이 선공팀 차례인지
                    bool skillByAlly = firstSideTurn ? state.AllyFirst : !state.AllyFirst;
                    state.IsSkillTurn = true;
                    ProcessSkillTurn(config, state, skillByAlly);
                    state.IsSkillTurn = false;
                }

                // 기본공격: 양팀 통합 속공순 다음 유닛 (턴 소모)
                if (order.Count > 0)
                {
                    var actor = order[cursor % order.Count];
                    cursor++;
                    ExecuteBasicAttack(state, actor);
                }
                t++;

                // 라운드 전환 (R1/R2: 적 전멸 시 다음 라운드, 턴 이어짐)
                if (state.CurrentRound < 3 && AllEnemiesDown(state))
                {
                    state.CurrentRound++;
                    state.InitializeRound(state.CurrentRound);
                    order = BuildActionOrder(state);   // 적 교체 → 행동순 재구성
                    cursor = 0;
                }
            }

            state.CurrentTurn = t;
        }

        /// <summary>양팀(아군+적) 통합 속공 내림차순. 같은 팀 동속공은 자리순, 다른 팀 동속공은 랜덤.</summary>
        private List<SiegeActor> BuildActionOrder(SiegeBattleState state)
        {
            var actors = new List<SiegeActor>();
            foreach (var a in state.AllyStates)
                actors.Add(new SiegeActor { IsAlly = true, Ally = a, Spd = a.FinalSpd, Position = a.PartyIndex });
            foreach (var e in state.Enemies)
                actors.Add(new SiegeActor { IsAlly = false, Enemy = e, Spd = e.FinalSpd, Position = e.Position });

            // 속공 내림차순 그룹화 → 그룹 내: 같은 팀 자리순 유지 + 팀 간 랜덤 인터리브
            return actors
                .GroupBy(x => x.Spd)
                .OrderByDescending(g => g.Key)
                .SelectMany(g => OrderSameSpeed(g.ToList()))
                .ToList();
        }

        /// <summary>동속공 그룹: 같은 팀은 자리(Position)순, 팀 간 순서는 랜덤 인터리브.</summary>
        private List<SiegeActor> OrderSameSpeed(List<SiegeActor> group)
        {
            if (group.Count <= 1) return group;
            var allies = group.Where(x => x.IsAlly).OrderBy(x => x.Position).ToList();
            var enemies = group.Where(x => !x.IsAlly).OrderBy(x => x.Position).ToList();
            var result = new List<SiegeActor>();
            int ai = 0, ei = 0;
            while (ai < allies.Count || ei < enemies.Count)
            {
                bool pickAlly = ei >= enemies.Count || (ai < allies.Count && _rng.Next(2) == 0);
                if (pickAlly) result.Add(allies[ai++]);
                else result.Add(enemies[ei++]);
            }
            return result;
        }

        #endregion

        #region 행동

        /// <summary>스킬턴 처리 (해당 팀에 사용 가능한 스킬이 있으면 사용; 없으면 스킵).</summary>
        private void ProcessSkillTurn(SiegeBattleConfig config, SiegeBattleState state, bool byAlly)
        {
            if (byAlly)
            {
                // 아군: 쿨 충족 스킬 1개 (간략 — 속공 높은 순으로 첫 사용가능). 데미지 placeholder.
                foreach (var ally in state.AllyStates.Where(a => !a.IsDead).OrderByDescending(a => a.FinalSpd))
                {
                    var skill = PickAllySkill(ally);
                    if (skill == null) continue;
                    double dmg = PlaceholderSkillDamage(ally, skill);
                    ApplyDamageToTarget(state, ally, dmg, skill.Name);
                    double cd = skill.GetCooldown(ally.Source.IsSkillEnhanced, ally.Source.TranscendLevel);
                    if (cd > 0) ally.SkillCooldowns[skill.SkillType] = cd;
                    return; // 스킬턴엔 한 명만 (간략 — 4번에서 정교화)
                }
            }
            // else 적 스킬턴: SkillPriority 기반 사용 + 아군 피격 → 4번에서 구현
        }

        /// <summary>기본공격 (턴 소모).</summary>
        private void ExecuteBasicAttack(SiegeBattleState state, SiegeActor actor)
        {
            if (actor.IsAlly)
            {
                var ally = actor.Ally;
                if (ally.IsDead) return;
                var normal = ally.Source.Character.Skills?.FirstOrDefault(s => s.SkillType == SkillType.Normal);
                double dmg = PlaceholderBasicDamage(ally, normal);
                ApplyDamageToTarget(state, ally, dmg, normal?.Name ?? "기본 공격");
            }
            // else 적 기본공격 → 아군 피격·생존 → 4번에서 구현
        }

        /// <summary>아군이 적에게 데미지 적용 (타겟: 현재는 최저 HP/R3 보스 — 3번에서 정교화).</summary>
        private void ApplyDamageToTarget(SiegeBattleState state, CharacterBattleState ally, double dmg, string label)
        {
            if (dmg <= 0) return;
            var target = PickTarget(state);
            if (target == null) return;

            target.CurrentHp -= dmg;            // HP 0 이하 허용 (무사망)
            target.TotalDamageTaken += dmg;
            ally.TotalDamageDealt += dmg;
            state.TotalScore += dmg;
            state.RoundScore[state.CurrentRound] = state.RoundScore.GetValueOrDefault(state.CurrentRound) + dmg;

            state.TurnLogs.Add(new BattleTurnLog
            {
                Turn = state.CurrentTurn,
                ActorName = ally.Source.Character.Name,
                IsAlly = true,
                ActionType = state.IsSkillTurn ? ActionType.SkillAttack : ActionType.NormalAttack,
                SkillName = label,
                DamageDealt = dmg,
                Description = $"{ally.Source.Character.Name} → {target.Source.Name}: {dmg:N0}",
            });
        }

        /// <summary>타겟 선정 (현재: 최저 HP, R3는 보스 중 최저 HP, 동률 앞열 — 3번에서 정식 약점 로직).</summary>
        private SiegeEnemyState PickTarget(SiegeBattleState state)
        {
            IEnumerable<SiegeEnemyState> candidates = state.CurrentRound >= 3
                ? state.Enemies.Where(e => e.IsBoss)
                : state.Enemies;
            var list = candidates.ToList();
            if (list.Count == 0) list = state.Enemies;
            return list.OrderBy(e => e.CurrentHp).ThenBy(e => e.Position).FirstOrDefault();
        }

        /// <summary>R1/R2 적이 모두 HP0 이하인지 (라운드 클리어 판정).</summary>
        private bool AllEnemiesDown(SiegeBattleState state)
            => state.Enemies.Count > 0 && state.Enemies.All(e => e.CurrentHp <= 0);

        private Skill PickAllySkill(CharacterBattleState ally)
        {
            // 간략: 쿨 충족 스킬 중 SkillType 높은 순 (궁→4→3→2→1). 4번에서 로테이션 정교화.
            return ally.Source.Character.Skills?
                .Where(s => s.SkillType != SkillType.Normal && s.SkillType != SkillType.Normal2)
                .Where(s => ally.IsSkillReady(s.SkillType))
                .OrderByDescending(s => s.SkillType)
                .FirstOrDefault();
        }

        // ===== Placeholder 데미지 (3번에서 DamageCalculator 정식 연결로 교체) =====
        private double PlaceholderBasicDamage(CharacterBattleState ally, Skill normal)
        {
            double ratio = normal?.GetLevelData(ally.Source.IsSkillEnhanced)?.Ratio ?? 100;
            return ally.FinalAtk * (ratio / 100.0);
        }

        private double PlaceholderSkillDamage(CharacterBattleState ally, Skill skill)
        {
            double ratio = skill.GetLevelData(ally.Source.IsSkillEnhanced)?.Ratio ?? 100;
            int atk = skill.GetAtkCount(ally.Source.IsSkillEnhanced, ally.Source.TranscendLevel);
            return ally.FinalAtk * (ratio / 100.0) * Math.Max(1, atk);
        }

        #endregion

        #region 결과

        private SiegeBattleResult BuildResult(SiegeBattleState state)
        {
            var result = new SiegeBattleResult
            {
                TotalScore = state.TotalScore,
                RoundScore = state.RoundScore,
                TotalTurns = state.CurrentTurn,
                RoundsCleared = state.CurrentRound - 1,
                TurnLogs = state.TurnLogs,
            };
            foreach (var ally in state.AllyStates)
            {
                result.CharacterResults.Add(new SiegeCharacterResult
                {
                    CharacterName = ally.Source.Character.Name,
                    PartyIndex = ally.PartyIndex,
                    TotalDamage = ally.TotalDamageDealt,
                    DamageShare = state.TotalScore > 0 ? ally.TotalDamageDealt / state.TotalScore * 100 : 0,
                });
            }
            return result;
        }

        #endregion
    }

    /// <summary>행동 순서 정렬용 유닛 래퍼 (아군 또는 적).</summary>
    internal class SiegeActor
    {
        public bool IsAlly;
        public CharacterBattleState Ally;
        public SiegeEnemyState Enemy;
        public double Spd;
        public int Position;
    }
}
