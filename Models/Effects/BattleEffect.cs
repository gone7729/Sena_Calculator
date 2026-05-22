namespace GameDamageCalculator.Models.Effects
{
    /// <summary>
    /// 통합 효과 모델
    /// 버프, 디버프, 상태이상을 하나의 구조로 표현
    /// </summary>
    public class BattleEffect
    {
        // === 식별 ===
        public string Id { get; set; }
        public string SourceName { get; set; }
        public EffectCategory Category { get; set; }
        public EffectTarget Target { get; set; }
        public MergeStrategy MergeStrategy { get; set; } = MergeStrategy.MaxMerge;

        // === 지속 ===
        public bool IsPermanent { get; set; }
        public int RemainingTurns { get; set; }
        public int MaxDuration { get; set; }

        // === 적용 순서 (버프 해제용) ===
        // 턴제 버프가 영웅에게 적용될 때 각 영웅별 1부터 증가하는 ID를 부여한다 (0 = 미부여/상시).
        // 버프 해제는 이 값이 작은 것(먼저 적용된 것)부터 제거한다. 상시 버프(IsPermanent)와
        // 해제 불가 효과(피해 면역/피해 무효화/권능)는 대상에서 제외.
        public int ApplyOrderId { get; set; }

        // === 효과 내용 (하나 이상 설정 가능) ===

        /// <summary>버프 스탯 (공격력%, 치피% 등)</summary>
        public BuffSet BuffValues { get; set; }

        /// <summary>디버프 스탯 (방깎%, 취약% 등)</summary>
        public DebuffSet DebuffValues { get; set; }

        /// <summary>상태이상 타입 (화상, 기절 등)</summary>
        public StatusEffectType? StatusType { get; set; }

        /// <summary>상태이상 수치 데이터</summary>
        public StatusEffectData StatusData { get; set; }

        // === 스택/확률 ===
        public int Stacks { get; set; } = 1;
        public int MaxStacks { get; set; } = 1;
        public double ApplyChance { get; set; } = 100;

        // === 헬퍼 ===
        public bool IsStatBuff => BuffValues != null;
        public bool IsStatDebuff => DebuffValues != null;
        public bool IsStatusEffect => StatusType.HasValue && StatusType != StatusEffectType.None;
        public bool IsExpired => !IsPermanent && RemainingTurns <= 0;

        /// <summary>턴 경과 처리</summary>
        public void TickTurn()
        {
            if (!IsPermanent && RemainingTurns > 0)
                RemainingTurns--;
        }

        /// <summary>복사본 생성</summary>
        public BattleEffect Clone()
        {
            return new BattleEffect
            {
                Id = Id,
                SourceName = SourceName,
                Category = Category,
                Target = Target,
                MergeStrategy = MergeStrategy,
                IsPermanent = IsPermanent,
                RemainingTurns = RemainingTurns,
                MaxDuration = MaxDuration,
                ApplyOrderId = ApplyOrderId,
                BuffValues = BuffValues?.Clone(),
                DebuffValues = DebuffValues?.Clone(),
                StatusType = StatusType,
                StatusData = StatusData, // immutable snapshot
                Stacks = Stacks,
                MaxStacks = MaxStacks,
                ApplyChance = ApplyChance,
            };
        }
    }
}
