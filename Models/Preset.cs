using System;
using System.Collections.Generic;

namespace GameDamageCalculator.Models
{
    public class Preset
    {
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // 캐릭터
        public string CharacterName { get; set; }
        public string SkillName { get; set; }
        public int TranscendLevel { get; set; }
        public bool IsSkillEnhanced { get; set; }
        
        // 잠재능력
        public int PotentialAtk { get; set; }
        public int PotentialDef { get; set; }
        public int PotentialHp { get; set; }
        
        // 세트
        public string EquipSet1 { get; set; }
        public string EquipSet2 { get; set; }
        public int SetCount1 { get; set; }
        
        // 메인옵션
        public string MainWeapon1 { get; set; }
        public string MainWeapon2 { get; set; }
        public string MainArmor1 { get; set; }
        public string MainArmor2 { get; set; }
        
        // 서브옵션 (9개)
        public Dictionary<string, int> SubOptions { get; set; } = new Dictionary<string, int>();
        
        // 장신구
        public int AccessoryGrade { get; set; }
        public string AccessoryOption { get; set; }
        public string AccessorySubOption { get; set; }
        
        // 진형
        public string Formation { get; set; }
        public bool IsBackPosition { get; set; }
        
        // 펫
        public string PetName { get; set; }
        public int PetStar { get; set; }
        public double PetAtkRate { get; set; }
        public double PetDefRate { get; set; }
        public double PetHpRate { get; set; }
        
        // 보스
        public string BossType { get; set; }  // Siege, Raid, Guild
        public string BossName { get; set; }
        
        // 버프/디버프 체크 상태
        public Dictionary<string, bool> BuffChecks { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, int> BuffStates { get; set; } = new Dictionary<string, int>();  // 4상태 버튼
    }
    
    public class SubOptionData
    {
        public string Type { get; set; }
        public int Tier { get; set; }
    }
    
    public class PetOptionData
    {
        public string Type { get; set; }
        public double Value { get; set; }
    }
}
