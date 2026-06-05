using System.Collections.Generic;

namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 적 모델 (보스/일반몹 통합)
    /// </summary>
    public class Enemy
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public EnemyType EnemyType { get; set; }
        public bool IsBoss { get; set; } = true;

        // 공성전용
        public string DayOfWeek { get; set; }

        // 레이드용
        public int Difficulty { get; set; }

        // 기본 스탯 (n인기 감쇄는 Stats.Dmg_Rdc_Single/Triple/Multi 사용)
        public BaseStatSet Stats { get; set; } = new BaseStatSet();

        // 적 공격의 피해 타입 (물리/마법) — 아군 타입별 피해무효(강자사냥 물리면역 등) 판정용.
        //   공성 보스는 대부분 물리 근접(델론즈=공격형) → 기본 물리.
        public AttackType AttackType { get; set; } = AttackType.Physical;

        // ===== 패시브 시스템 =====
        // 고유 버프 (물리/마법 받피감, 일반 받피감 등)
        public PermanentBuff InnateBuff { get; set; }

        // 고유 디버프 (받피증, 취약 등 - 적 자체가 가진 취약성)
        public PermanentDebuff InnateDebuff { get; set; }

        // 조건부 버프 (조건부 방증 등)
        public TimedBuff ConditionalBuff { get; set; }
        public string ConditionalBuffCondition { get; set; }

        // 스택형 방어력 증가
        public bool IsStackableDefenseIncrease { get; set; } = false;
        public int MaxDefenseStack { get; set; } = 0;
        public double DefenseIncreasePerStack { get; set; }

        // 스킬 (적이 사용하는 스킬)
        public List<Skill> Skills { get; set; } = new();

        // 공성전: 이 적이 아군 공격에 피격될 때마다 공격력 최고 적의 스킬 쿨타임 N초 감소 (일요일 몹 패시브). 0이면 없음.
        public double SiegeHitCdReduce { get; set; }

        // 공성전: 아군 사망 시 이 보스가 얻는 모든 피해 무효화 횟수 (델론즈 「죽음의 경계」: 피격 4회). 0이면 없음.
        //   직격 1회당 1 차감(점수 미집계), DoT는 무효지만 차감 안 함.
        public int OnAllyDeathNullifyHits { get; set; }

        // ===== 하위호환 속성 (Phase 3 전환 완료 전까지 유지) =====
        public double PhysicalReduction
        {
            get => InnateBuff?.Phys_Dmg_Rdc ?? 0;
            set { EnsureInnateBuff(); InnateBuff.Phys_Dmg_Rdc = value; }
        }
        public double MagicReduction
        {
            get => InnateBuff?.Mag_Dmg_Rdc ?? 0;
            set { EnsureInnateBuff(); InnateBuff.Mag_Dmg_Rdc = value; }
        }
        public double SingleTargetReduction
        {
            get => Stats.Dmg_Rdc_Single;
            set => Stats.Dmg_Rdc_Single = value;
        }
        public double TripleTargetReduction
        {
            get => Stats.Dmg_Rdc_Triple;
            set => Stats.Dmg_Rdc_Triple = value;
        }
        public double MultiTargetReduction
        {
            get => Stats.Dmg_Rdc_Multi;
            set => Stats.Dmg_Rdc_Multi = value;
        }
        public double DamageReduction
        {
            get => InnateBuff?.Dmg_Rdc ?? 0;
            set { EnsureInnateBuff(); InnateBuff.Dmg_Rdc = value; }
        }
        public double DamageTakenIncrease
        {
            get => InnateDebuff?.Dmg_Taken_Increase ?? 0;
            set { EnsureInnateDebuff(); InnateDebuff.Dmg_Taken_Increase = value; }
        }
        public double Vulnerability
        {
            get => InnateDebuff?.Vulnerability ?? 0;
            set { EnsureInnateDebuff(); InnateDebuff.Vulnerability = value; }
        }
        public double DefenseIncrease
        {
            get => ConditionalBuff?.Def_Rate ?? DefenseIncreasePerStack;
            set
            {
                if (IsStackableDefenseIncrease)
                {
                    DefenseIncreasePerStack = value;
                }
                else
                {
                    EnsureConditionalBuff();
                    ConditionalBuff.Def_Rate = value;
                }
            }
        }
        public string DefenseIncreaseCondition
        {
            get => ConditionalBuffCondition;
            set => ConditionalBuffCondition = value;
        }

        // 조건부 방어력 감소 (미사용이지만 호환)
        public double DefenseDecrease { get; set; }
        public string DefenseDecreaseCondition { get; set; }

        /// <summary>
        /// 실제 방어력 계산 (조건 적용)
        /// </summary>
        public double GetEffectiveDefense(bool isConditionMet)
        {
            double baseDef = Stats.Def;

            if (isConditionMet && DefenseIncrease > 0)
            {
                baseDef *= (1 + DefenseIncrease / 100.0);
            }

            return baseDef;
        }

        /// <summary>
        /// 스택형 방어력 증가 계산
        /// </summary>
        public double GetStackableDefenseIncrease(int currentStack)
        {
            if (!IsStackableDefenseIncrease) return 0;

            int clampedStack = System.Math.Clamp(currentStack, 0, MaxDefenseStack);
            return DefenseIncreasePerStack * clampedStack;
        }

        private void EnsureInnateBuff()
        {
            InnateBuff ??= new PermanentBuff();
        }

        private void EnsureInnateDebuff()
        {
            InnateDebuff ??= new PermanentDebuff();
        }

        private void EnsureConditionalBuff()
        {
            ConditionalBuff ??= new TimedBuff();
        }
    }

    /// <summary>
    /// 적 타입
    /// </summary>
    public enum EnemyType
    {
        Siege,          // 공성전
        Raid,           // 레이드
        Forest,         // 강림
        GrowthDungeon,  // 성장던전
        Other,
        Mob             // 일반몹
    }
}
