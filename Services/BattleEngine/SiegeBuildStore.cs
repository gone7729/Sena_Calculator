using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Services.BattleEngine
{
    // ============================================================================
    // 탐색된 공성 빌드 저장/불러오기.
    //   탐색은 한 번에 1시간 안팎이 걸리므로, 찾은 빌드(장비·장신구·전용장비·자리·로테)를 구조화해 저장해 두고
    //   다음부터는 탐색 없이 불러와 재생한다. 저장 파일은 PracticeTest team JSON(heroes/pet)의 상위 호환이다.
    //   로테는 영웅 이름으로 저장한다(자리 순서가 바뀌어도 복원 가능). heroes 순서 = 진형 번호 순.
    // ============================================================================
    public class SiegeSavedBuild
    {
        public int Version { get; set; } = 1;
        public string Day { get; set; }
        public string Profile { get; set; }
        public string Formation { get; set; }
        public List<string> BackRow { get; set; } = new();
        public double Score { get; set; }
        public double RankScore { get; set; }
        public int Deaths { get; set; }
        public bool InGameBuffTargeting { get; set; }
        public string EngineHash { get; set; }
        public string GitCommit { get; set; }
        public string SavedAt { get; set; }
        public string StartOrder { get; set; }          // 이 빌드를 찾은 탐색의 출발 순서(참고)
        public double SearchMinutes { get; set; }
        public List<HeroDto> Heroes { get; set; } = new();
        public PetDto Pet { get; set; }
        public List<StepDto> Rotation { get; set; } = new();
        public List<string> Log { get; set; } = new();   // 옵티마이저 장비·후처리 로그
        public string Route { get; set; }                // 탐색 경로: "고점" / "정석" (SiegeSearchRoute)
        public double? TargetScore { get; set; }         // 고점 경로: 유저가 입력한 인게임 최고점(추격 목표)
        public List<StageDto> Stages { get; set; } = new();     // 이 빌드를 찾은 탐색의 단계별 점수(정석 → 고점 사다리)
        public List<RunDto> History { get; set; } = new();      // 이 팀·경로로 돌린 탐색 기록(출발 순서별 결과)

        public class StageDto
        {
            public string Name { get; set; }
            public double Score { get; set; }
            public double RankScore { get; set; }
            public int Deaths { get; set; }
            public string Order { get; set; }            // 그 시점 진형 번호 순
            public bool Standard { get; set; }           // 정석 경로에 속하는 단계(이후 단계는 고점 경로 전용)
        }
        public class RunDto
        {
            public string StartOrder { get; set; }
            public string FinalOrder { get; set; }
            public double Score { get; set; }
            public double RankScore { get; set; }
            public int Deaths { get; set; }
            public double Minutes { get; set; }
            public string At { get; set; }
            public string EngineHash { get; set; }
        }

        public class HeroDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Slot { get; set; }                // 진형 번호(1~5)
            public bool Back { get; set; }
            public int Transcend { get; set; }
            public bool Awakened { get; set; }
            public PotentialDto Potential { get; set; } = new();
            public EnhancedDto Enhanced { get; set; } = new();
            public List<GearDto> Gear { get; set; } = new();
            public AccessoryDto Accessory { get; set; }
            public ExclusiveDto Exclusive { get; set; }
        }
        public class PotentialDto { public int Atk { get; set; } public int Def { get; set; } public int Hp { get; set; } }
        public class EnhancedDto
        {
            public bool Normal { get; set; } = true;
            public bool Skill1 { get; set; }
            public bool Skill2 { get; set; } = true;
            public bool Passive { get; set; } = true;
        }
        public class GearDto
        {
            public string Slot { get; set; }
            public string Name { get; set; }
            public string Set { get; set; }
            public string Main { get; set; }
            public int Enhance { get; set; } = 5;
            public List<SubDto> Subs { get; set; } = new();
        }
        public class SubDto { public string Stat { get; set; } public int Tier { get; set; } }
        public class AccessoryDto
        {
            public int Grade { get; set; }
            public string Main { get; set; }
            public string Sub { get; set; }
            public string Ring { get; set; }
        }
        public class ExclusiveDto
        {
            public string Name { get; set; }
            public double Atk { get; set; }
            public double Hp { get; set; }
            public double Def { get; set; }
            public bool IsMagic { get; set; }
            public List<TuningDto> Tuning { get; set; } = new();
        }
        public class TuningDto { public string Option { get; set; } public string Grade { get; set; } }
        public class PetDto
        {
            public string Grade { get; set; } = "전설";
            public string Name { get; set; }
            public int Star { get; set; }
            public int Enhance { get; set; }
            public double PotentialAtkRate { get; set; }
            public double PotentialDefRate { get; set; }
            public double PotentialHpRate { get; set; }
        }
        public class StepDto
        {
            public string Hero { get; set; }
            public string Skill { get; set; }    // SkillType 이름
            public bool Hold { get; set; }
        }
    }

    public static class SiegeBuildStore
    {
        private static readonly JsonSerializerOptions Opts = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        /// <summary>현재 파티(진형 번호 순)·자리·로테를 저장용 객체로 옮긴다. 점수·메타는 호출자가 채운다.</summary>
        public static SiegeSavedBuild Capture(List<BattleCharacter> party, string formation, IEnumerable<string> backRow,
            List<RotationDecision> plan)
        {
            var back = new HashSet<string>(backRow ?? Enumerable.Empty<string>());
            var b = new SiegeSavedBuild { Formation = formation, BackRow = back.ToList() };
            for (int i = 0; i < party.Count; i++)
            {
                var bc = party[i];
                var lo = bc.Equipment;
                var ex = bc.Character.ExclusiveWeapon;
                b.Heroes.Add(new SiegeSavedBuild.HeroDto
                {
                    Id = bc.Character.Id, Name = bc.Character.Name, Slot = i + 1, Back = back.Contains(bc.Character.Name),
                    Transcend = bc.TranscendLevel, Awakened = bc.IsAwakened,
                    Potential = new() { Atk = bc.PotentialAtkLevel, Def = bc.PotentialDefLevel, Hp = bc.PotentialHpLevel },
                    Enhanced = new() { Skill1 = bc.IsSkillEnhanced, Skill2 = bc.IsSkillEnhanced, Passive = bc.IsSkillEnhanced, Normal = bc.IsSkillEnhanced },
                    Gear = (lo?.GetEquipments() ?? Enumerable.Empty<Equipment>()).Select(e => new SiegeSavedBuild.GearDto
                    {
                        Slot = e.Slot, Name = e.Name, Set = e.SetName, Main = e.MainStatName, Enhance = e.EnhanceLevel,
                        Subs = e.SubSlots.Where(s => !string.IsNullOrEmpty(s.StatName))
                            .Select(s => new SiegeSavedBuild.SubDto { Stat = s.StatName, Tier = s.Tier }).ToList(),
                    }).ToList(),
                    Accessory = lo?.Accessory == null ? null : new SiegeSavedBuild.AccessoryDto
                    {
                        Grade = lo.Accessory.Grade, Main = lo.Accessory.MainOption, Sub = lo.Accessory.SubOption, Ring = lo.Accessory.RingName,
                    },
                    Exclusive = ex == null ? null : new SiegeSavedBuild.ExclusiveDto
                    {
                        Name = ex.Name, Atk = ex.Atk, Hp = ex.Hp, Def = ex.Def, IsMagic = ex.IsMagic,
                        Tuning = ex.Tuning.Select(t => new SiegeSavedBuild.TuningDto { Option = t.Option.ToString(), Grade = t.Grade.ToString() }).ToList(),
                    },
                });
            }
            foreach (var d in plan ?? new List<RotationDecision>())
            {
                if (d.Hold) { b.Rotation.Add(new SiegeSavedBuild.StepDto { Hold = true }); continue; }
                b.Rotation.Add(new SiegeSavedBuild.StepDto
                {
                    Hero = d.HeroIndex >= 0 && d.HeroIndex < party.Count ? party[d.HeroIndex].Character.Name : null,
                    Skill = d.Skill.ToString(),
                });
            }
            return b;
        }

        /// <summary>
        /// 저장된 빌드를 전투용 파티로 되살린다(진형 번호 순, 후열 표시 포함).
        /// 전용장비는 시뮬이 Character.ExclusiveWeapon을 읽으므로 DB 영웅 객체에 다시 꽂는다 —
        /// 같은 영웅으로 다른 탐색을 돌리면 덮어써지므로, 그 뒤에 쓰려면 다시 호출한다.
        /// </summary>
        public static List<BattleCharacter> ToParty(SiegeSavedBuild b)
        {
            var party = new List<BattleCharacter>();
            foreach (var h in b.Heroes.OrderBy(x => x.Slot))
            {
                var ch = CharacterDb.Characters.FirstOrDefault(c => c.Id == h.Id) ?? CharacterDb.Characters.First(c => c.Name == h.Name);
                var pieces = h.Gear.Select(g =>
                {
                    var e = new Equipment { Name = g.Name, Slot = g.Slot, SetName = g.Set, MainStatName = g.Main, EnhanceLevel = g.Enhance };
                    for (int i = 0; i < g.Subs.Count && i < e.SubSlots.Count; i++)
                    {
                        e.SubSlots[i].StatName = g.Subs[i].Stat;
                        e.SubSlots[i].Tier = g.Subs[i].Tier;
                    }
                    return e;
                }).ToList();
                EquipmentLoadout lo = null;
                if (pieces.Count == 4)
                    lo = new EquipmentLoadout { Weapon1 = pieces[0], Weapon2 = pieces[1], Armor1 = pieces[2], Armor2 = pieces[3] };
                if (lo != null && h.Accessory != null)
                    lo.Accessory = new Accessory { Grade = h.Accessory.Grade, MainOption = h.Accessory.Main, SubOption = h.Accessory.Sub, RingName = h.Accessory.Ring };
                ch.ExclusiveWeapon = h.Exclusive == null ? null : new ExclusiveWeapon
                {
                    Name = h.Exclusive.Name ?? "", OwnerCharacterId = ch.Id, Atk = h.Exclusive.Atk, Hp = h.Exclusive.Hp, Def = h.Exclusive.Def,
                    IsMagic = h.Exclusive.IsMagic,
                    Tuning = h.Exclusive.Tuning.Select(t => new TuningSlot
                    {
                        Option = Enum.Parse<TuningOption>(t.Option), Grade = Enum.Parse<ExclusiveWeaponGrade>(t.Grade),
                    }).ToList(),
                };
                party.Add(new BattleCharacter
                {
                    Character = ch, TranscendLevel = h.Transcend, IsSkillEnhanced = h.Enhanced?.Skill1 ?? true, IsAwakened = h.Awakened,
                    PotentialAtkLevel = h.Potential?.Atk ?? 0, PotentialDefLevel = h.Potential?.Def ?? 0, PotentialHpLevel = h.Potential?.Hp ?? 0,
                    Equipment = lo, IsBackPosition = h.Back,
                });
            }
            return party;
        }

        /// <summary>저장된 로테(영웅 이름 기준)를 주어진 파티 순서의 인덱스로 옮긴다. 파티에 없는 영웅의 칸은 홀드.</summary>
        public static List<RotationDecision> ToPlan(SiegeSavedBuild b, List<BattleCharacter> party)
        {
            var plan = new List<RotationDecision>();
            foreach (var s in b.Rotation)
            {
                int hi = s.Hold || s.Hero == null ? -1 : party.FindIndex(p => p.Character?.Name == s.Hero);
                if (hi < 0 || !Enum.TryParse<SkillType>(s.Skill, out var sk)) { plan.Add(new RotationDecision { Hold = true }); continue; }
                plan.Add(new RotationDecision { HeroIndex = hi, Skill = sk });
            }
            return plan;
        }

        public static void Save(string path, SiegeSavedBuild b)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllText(path, JsonSerializer.Serialize(b, Opts), new UTF8Encoding(false));
        }

        /// <summary>없거나 읽지 못하면 null.</summary>
        public static SiegeSavedBuild Load(string path)
        {
            if (!File.Exists(path)) return null;
            try { return JsonSerializer.Deserialize<SiegeSavedBuild>(File.ReadAllText(path), Opts); }
            catch { return null; }
        }
    }
}
