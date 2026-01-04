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
            new Pet
            {
                Id = 1,
                Name = "이린",
                Rarity = "전설",
                Skills = new Dictionary<int, BaseStatSet>   // PetSkill → BaseStatSet
                {
                    { 4, new BaseStatSet { Atk_Rate = 17 } },
                    { 5, new BaseStatSet { Atk_Rate = 19 } },
                    { 6, new BaseStatSet { Atk_Rate = 21 } }
                }
            },
            
            // ===== 희귀 펫 =====
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