namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 배틀 시뮬레이션용 캐릭터 설정
    /// </summary>
    public class BattleCharacter
    {
        public Character Character { get; set; }
        public int TranscendLevel { get; set; }
        public bool IsSkillEnhanced { get; set; }
        public bool IsPassiveConditionMet { get; set; }

        // 장비
        public EquipmentLoadout Equipment { get; set; }

        // 포텐셜
        public int PotentialAtkLevel { get; set; }
        public int PotentialDefLevel { get; set; }
        public int PotentialHpLevel { get; set; }

        // 진형 내 위치
        public bool IsBackPosition { get; set; }
    }
}
