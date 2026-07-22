namespace GameDamageCalculator.Models.Effects
{
    /// <summary>
    /// 조건부 효과의 발동 조건 종류.
    /// 게임 표기를 그대로 옮길 수 있도록 비교 방향(이하/미만/이상)을 타입으로 구분한다.
    /// </summary>
    public enum SkillConditionType
    {
        /// <summary>조건 없음 — 항상 발동.</summary>
        None = 0,

        /// <summary>대상 현재 생명력% ≤ Threshold ("30% 이하일 경우")</summary>
        TargetHpAtOrBelow,
        /// <summary>대상 현재 생명력% &lt; Threshold ("30% 미만일 경우")</summary>
        TargetHpBelow,
        /// <summary>대상 현재 생명력% ≥ Threshold ("50% 이상일 경우")</summary>
        TargetHpAtOrAbove,

        /// <summary>자신 현재 생명력% ≤ Threshold</summary>
        SelfHpAtOrBelow,

        /// <summary>대상이 지정 상태이상 보유 ("대상 [출혈] 상태일 경우")</summary>
        TargetHasStatus,
        /// <summary>자신이 지정 상태이상/자세 보유 ("[저격 자세]일 경우")</summary>
        SelfHasStatus,

        /// <summary>대상의 지정 스탯이 자신보다 낮음 ("대상 속공이 자신보다 낮을 경우")</summary>
        TargetStatLowerThanSelf,

        /// <summary>대상이 [방어력 증가] 버프 보유 (상시/턴제 모두)</summary>
        TargetHasDefenseBuff,

        /// <summary>살아있는 적이 1명뿐 ("단일 적군")</summary>
        SingleEnemy,
    }

    /// <summary>
    /// 조건부 추가피해·배율보너스·피증의 발동 조건.
    ///
    /// 이 값이 없으면(null) 조건 없이 항상 발동한다 — 조건을 아직 입력하지 않은 스킬은
    /// 종전과 같이 동작하므로, DB를 한 번에 다 채우지 않아도 안전하다.
    /// 판정은 전투 상태를 아는 시뮬레이터가 수행한다(SkillConditionEvaluator).
    /// </summary>
    public class SkillCondition
    {
        public SkillConditionType Type { get; set; } = SkillConditionType.None;

        /// <summary>HP 조건의 기준값(%) — 예: 30 ("30% 이하")</summary>
        public double Threshold { get; set; }

        /// <summary>상태이상 조건의 대상 상태 — 예: Bleeding("출혈")</summary>
        public StatusEffectType Status { get; set; } = StatusEffectType.None;

        /// <summary>스탯 비교 조건의 비교 스탯 — 예: Spd("속공")</summary>
        public StatType Stat { get; set; } = StatType.None;

        /// <summary>게임 원문 표기 (표시·검수용). 판정에는 쓰이지 않는다.</summary>
        public string Desc { get; set; }
    }
}
