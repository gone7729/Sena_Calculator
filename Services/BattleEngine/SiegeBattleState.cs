using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Models;
using GameDamageCalculator.Database;

namespace GameDamageCalculator.Services.BattleEngine
{
    /// <summary>
    /// 공성전 전투 전체 상태 (다중 적 + 라운드/턴/스킬턴/점수).
    /// 단일 보스전 BattleState와 별개의 공성전 전용 상태.
    ///
    /// 핵심 규칙(메모리 siege-simulation-rules):
    /// - 적군은 HP 0이어도 죽지 않음. 라운드 1~3 동안 70턴 누적 데미지 = 점수.
    /// - 70턴은 R1부터 연속 경과. 라운드 전환 시 턴 이어짐. 진입 턴은 0턴(스킬턴, 턴 미소모).
    /// - 선공 = 팀 총 속공치 높은 팀(같으면 랜덤).
    /// </summary>
    public class SiegeBattleState
    {
        // 아군 (기존 CharacterBattleState 재사용)
        public List<CharacterBattleState> AllyStates { get; set; } = new();

        // 현재 라운드의 적들 (다중)
        public List<SiegeEnemyState> Enemies { get; set; } = new();

        // 라운드 (1~3)
        public int CurrentRound { get; set; } = 1;

        // 누적 턴 (0~70, 라운드 넘어가도 이어짐)
        public int CurrentTurn { get; set; }

        // 누적 경과 시간(초) — 쿨다운 계산용 (행동 소요시간만큼 증가)
        public double ElapsedSeconds { get; set; }

        // 현재 진행 중인 행동이 스킬턴(0턴, 턴 미소모)인지
        public bool IsSkillTurn { get; set; }

        // 선공 여부 (아군이 선공인지)
        public bool AllyFirst { get; set; }

        // 공성전 점수 = 70턴 누적 데미지
        public double TotalScore { get; set; }

        // 라운드별 점수 (집계/표시용)
        public Dictionary<int, double> RoundScore { get; set; } = new();

        // 라운드 구성 (요일별 Stage)
        public Stage SiegeStage { get; set; }

        // 최대 턴 (기본 70)
        public int MaxTurns { get; set; } = 70;

        // 평타 로테이션 순서: 아군 속공 내림차순 + 동속공 시 자리(PartyIndex) 오름차순
        public List<int> AllySpeedOrder { get; set; } = new();
        public int AllyRotationCursor { get; set; }

        // 턴별 행동 로그
        public List<BattleTurnLog> TurnLogs { get; set; } = new();

        /// <summary>
        /// 지정 라운드의 적들을 Stage에서 생성해 Enemies에 채운다.
        /// EnemyId로 EnemyDb에서 Enemy를 조회하고 StageEnemy.Position/IsBoss를 반영한다.
        /// 공성전 적은 장비/버프가 없으므로 스탯은 Enemy.Stats를 그대로 사용.
        /// </summary>
        public void InitializeRound(int round)
        {
            CurrentRound = round;
            Enemies = new List<SiegeEnemyState>();

            var wave = SiegeStage?.Waves?.FirstOrDefault(w => w.WaveNumber == round);
            if (wave == null) return;

            foreach (var se in wave.Enemies)
            {
                var enemy = EnemyDb.AllEnemies.FirstOrDefault(e => e.Id == se.EnemyId);
                if (enemy == null) continue;

                Enemies.Add(new SiegeEnemyState
                {
                    Source = enemy,
                    Position = se.Position,
                    IsBoss = se.IsBoss,
                    CurrentHp = enemy.Stats.Hp,
                    MaxHp = enemy.Stats.Hp,
                    FinalAtk = enemy.Stats.Atk,
                    FinalDef = enemy.Stats.Def,
                    FinalSpd = enemy.Stats.Spd,
                });
            }
        }

        /// <summary>현재 라운드의 적 스킬 우선순위 (StageWave.SkillPriority).</summary>
        public List<SiegeSkillOrder> CurrentSkillPriority
            => SiegeStage?.Waves?.FirstOrDefault(w => w.WaveNumber == CurrentRound)?.SkillPriority
               ?? new List<SiegeSkillOrder>();

        /// <summary>살아있는(=행동 가능한) 적. 공성전은 HP0이어도 죽지 않으므로 전부 반환.</summary>
        public IEnumerable<SiegeEnemyState> ActiveEnemies => Enemies;

        /// <summary>아군 총 속공치 합 (선공 판정용).</summary>
        public double AllyTotalSpd => AllyStates.Sum(a => a.FinalSpd);

        /// <summary>적 총 속공치 합 (선공 판정용).</summary>
        public double EnemyTotalSpd => Enemies.Sum(e => e.FinalSpd);
    }

    /// <summary>
    /// 공성전 적 1마리의 전투 상태. 영웅처럼 스킬/쿨다운/효과를 가진다.
    /// 공성전 적은 HP가 0이어도 죽지 않으므로 CurrentHp는 음수까지 허용(데미지 누적용).
    /// </summary>
    public class SiegeEnemyState
    {
        public Enemy Source { get; set; }

        // 진형 자리 (1~5). 타겟팅 앞열 판정·동속공 행동 순서 기준.
        public int Position { get; set; }

        // 이 라운드에서 보스 취급 (R3 등). 약점공격 대상은 항상 보스.
        public bool IsBoss { get; set; }

        // HP (0 이하여도 사망하지 않음 — 데미지만 누적 점수로 집계)
        public double CurrentHp { get; set; }
        public double MaxHp { get; set; }

        // 스탯 (공성전 적은 장비/버프 없이 Enemy.Stats 그대로)
        public double FinalAtk { get; set; }
        public double FinalDef { get; set; }
        public double FinalSpd { get; set; }

        // 스킬 쿨다운 (초 기반, 행동 소요시간만큼 감소)
        public Dictionary<SkillType, double> SkillCooldowns { get; set; } = new();

        // 통합 효과 관리자 (적에게 걸린 디버프/상태이상)
        public EffectManager Effects { get; set; } = new();

        // 적에게 걸린 DoT (아군이 부여한 화상/출혈 등 지속 피해). 매 턴 틱.
        public List<SiegeDot> ActiveDots { get; set; } = new();

        // 이 적의 누적 피해(아군이 이 적에게 넣은 데미지) — 라운드/타겟 분석용
        public double TotalDamageTaken { get; set; }

        /// <summary>스킬 사용 가능 여부 (쿨다운 충족).</summary>
        public bool IsSkillReady(SkillType skillType)
            => !SkillCooldowns.TryGetValue(skillType, out var cd) || cd <= 0;
    }

    /// <summary>적에게 걸린 DoT 한 항목. 틱당 데미지는 등록 시점에 시전자 공격력·감쇄를 반영해 계산해 둔다.</summary>
    public class SiegeDot
    {
        public StatusEffectType Type { get; set; }
        public double TickDamage { get; set; }    // 매 턴 가하는 피해 (등록 시 계산: 시전자 atk × 배율 × 감쇄)
        public int RemainingTurns { get; set; }
        public string SourceName { get; set; }    // 시전 아군 (캐릭별 기여 집계용)
    }
}
