using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Database
{
    /// <summary>
    /// 펫 데이터베이스
    /// </summary>
    public static class PetDb
    {
        public static readonly List<Pet> Pets = new List<Pet>
        {
            // ===== 전설 펫 =====
            // 이린
            new Pet
            {
                Id = 1,
                Name = "이린",
                Rarity = "전설",
                Skills = new Dictionary<int, PetSkill>
                {
                    { 4, new PetSkill { Buff = new BuffSet { Atk_Rate = 17 } } },
                    { 5, new PetSkill { Buff = new BuffSet { Atk_Rate = 19 } } },
                    { 6, new PetSkill { Buff = new BuffSet { Atk_Rate = 21 } } },
                }
            },
            // 연지
            new Pet
            {
                Id = 2,
                Name = "연지",
                Rarity = "전설",
                Skills = new Dictionary<int, PetSkill>
                {
                    { 4, new PetSkill { Buff = new BuffSet { Atk_Rate = 17 } } },
                    { 5, new PetSkill { Buff = new BuffSet { Atk_Rate = 19 } } },
                    { 6, new PetSkill { Buff = new BuffSet { Atk_Rate = 21 } } },
                }
            },
            // 윈디
            new Pet
            {
                Id = 3,
                Name = "윈디",
                Rarity = "전설",
                Skills = new Dictionary<int, PetSkill>
                {
                    { 4, new PetSkill { 
                        Buff = new BuffSet{ Atk_Rate = 8 },
                        Debuff = new DebuffSet{ Boss_Vulnerability = 15 }
                     } },
                    { 5, new PetSkill { 
                        Buff = new BuffSet{ Atk_Rate = 10 },
                        Debuff = new DebuffSet{ Boss_Vulnerability = 20 }
                     } },
                    { 6, new PetSkill { 
                        Buff = new BuffSet{ Atk_Rate = 12 },
                        Debuff = new DebuffSet{ Boss_Vulnerability = 26 }
                     } },
                }
            },
            // 리첼
            new Pet
            {
                Id = 4,
                Name = "리첼",
                Rarity = "전설",
                Skills = new Dictionary<int, PetSkill>
                {
                    { 4, new PetSkill { 
                        Buff = new BuffSet{ Wek_Dmg = 6, Wek = 10 },
                     } },
                    { 5, new PetSkill { 
                        Buff = new BuffSet{ Wek_Dmg = 8, Wek = 13 },
                     } },
                    { 6, new PetSkill { 
                        Buff = new BuffSet{ Wek_Dmg = 10, Wek = 17 },
                     } },
                }
            },
            // 델로
            new Pet
            {
                Id = 5,
                Name = "델로",
                Rarity = "전설",
                Skills = new Dictionary<int, PetSkill>
                {
                    { 4, new PetSkill { 
                        Buff = new BuffSet{ Cri_Dmg = 10, Cri = 17 },
                     } },
                    { 5, new PetSkill { 
                        Buff = new BuffSet{ Cri_Dmg = 13, Cri = 19 },
                     } },
                    { 6, new PetSkill { 
                        Buff = new BuffSet{ Cri_Dmg = 15, Cri = 21 },
                     } },
                }
            },
            // 루
            new Pet
            {
                Id = 6,
                Name = "루",
                Rarity = "전설",
                Skills = new Dictionary<int, PetSkill>
                {
                    { 4, new PetSkill { 
                        Buff = new BuffSet{ Def_Rate = 18 },
                     } },
                    { 5, new PetSkill { 
                        Buff = new BuffSet{ Def_Rate = 20 },
                     } },
                    { 6, new PetSkill { 
                        Buff = new BuffSet{ Def_Rate = 22 },
                     } },
                }
            },
            // 파이크
            new Pet
            {
                Id = 7,
                Name = "파이크",
                Rarity = "전설",
                Skills = new Dictionary<int, PetSkill>
                {
                    { 4, new PetSkill { 
                        Buff = new BuffSet{ Eff_Res = 13, Def_Rate = 9 },
                     } },
                    { 5, new PetSkill { 
                        Buff = new BuffSet{ Eff_Res = 16, Def_Rate = 11 },
                     } },
                    { 6, new PetSkill { 
                        Buff = new BuffSet{ Eff_Res = 19, Def_Rate = 13 },
                     } },
                }
            },
            // 크리
            new Pet
            {
                Id = 8,
                Name = "크리",
                Rarity = "전설",
                Skills = new Dictionary<int, PetSkill>
                {
                    { 4, new PetSkill { 
                        Buff = new BuffSet{ Eff_Hit = 13, Atk_Rate = 8 },
                     } },
                    { 5, new PetSkill { 
                        Buff = new BuffSet{ Eff_Hit = 16, Atk_Rate = 10 },
                     } },
                    { 6, new PetSkill { 
                        Buff = new BuffSet{ Eff_Hit = 19, Atk_Rate = 12 },
                     } },
                }
            },
            // 유
            new Pet
            {
                Id = 9,
                Name = "유",
                Rarity = "전설",
                Skills = new Dictionary<int, PetSkill>
                {
                    { 4, new PetSkill { 
                        Debuff = new DebuffSet{ Def_Reduction = 6, Heal_Reduction = 24 }
                     } },
                    { 5, new PetSkill { 
                        Debuff = new DebuffSet{ Def_Reduction = 8, Heal_Reduction = 28 }
                     } },
                    { 6, new PetSkill { 
                        Debuff = new DebuffSet{ Def_Reduction = 10, Heal_Reduction = 32 }
                     } },
                }
            },
            // 카람
            new Pet
            {
                Id = 9,
                Name = "카람",
                Rarity = "전설",
                Skills = new Dictionary<int, PetSkill>
                {
                    { 4, new PetSkill { 
                        Buff = new BuffSet{ Blk = 9, Dmg_Rdc = 4 }
                     } },
                    { 5, new PetSkill { 
                        Buff = new BuffSet{ Blk = 11, Dmg_Rdc = 6 }
                     } },
                    { 6, new PetSkill { 
                        Buff = new BuffSet{ Blk = 15, Dmg_Rdc = 8 }
                     } },
                }
            },
            // 멜페로
            new Pet
            {
                Id = 9,
                Name = "멜페로",
                Rarity = "전설",
                Skills = new Dictionary<int, PetSkill>
                {
                    { 4, new PetSkill { 
                        Buff = new BuffSet{ Eff_Hit = 13 }
                     } },
                    { 5, new PetSkill { 
                        Buff = new BuffSet{ Eff_Hit = 16 }
                     } },
                    { 6, new PetSkill { 
                        Buff = new BuffSet{ Eff_Hit = 19 }
                     } },
                }
            },
            
            // ===== 희귀 펫 =====
            // 노트
            new Pet
            {
                Id = 51,
                Name = "노트",
                Rarity = "희귀",
                Skills = new Dictionary<int, PetSkill>
                {
                    { 4, new PetSkill { Buff = new BuffSet { Atk_Rate = 13 } } },
                    { 5, new PetSkill { Buff = new BuffSet { Atk_Rate = 15 } } },
                    { 6, new PetSkill { Buff = new BuffSet { Atk_Rate = 17 } } },
                }
            },
        };

        public static Pet GetByName(string name)
        {
            return Pets.FirstOrDefault(p => p.Name == name);
        }

        public static List<Pet> GetByRarity(string rarity)
        {
            return Pets.Where(p => p.Rarity == rarity).ToList();
        }
    }
}