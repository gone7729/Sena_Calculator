using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Database
{
    /// <summary>
    /// 공성전 요일별 구성 DB.
    /// 게임 구조: R1/R2 몹(룩·챈슬러) 스탯은 (몹종류·라운드)별로 요일이 달라도 동일 — 스킬만 요일별.
    /// R3 룩·챈슬러는 요일별로 스탯·스킬 모두 다름. R3엔 요일 보스(SiegeBosses)도 등장.
    /// Build()가 요일별 Enemy 인스턴스(Id 자동 할당)와 Stage를 생성한다.
    /// </summary>
    public static class SiegeDayDb
    {
        // R1/R2 몹 스탯 (요일 공통). 키 = (몹 이름, 라운드)
        public static readonly Dictionary<(string Name, int Round), BaseStatSet> MobStats = new()
        {
            [("룩", 1)]   = new BaseStatSet { Atk = 542,  Def = 689,  Hp = 8650,  Spd = 15, Cri_Dmg = 150 },
            [("룩", 2)]   = new BaseStatSet { Atk = 873,  Def = 1123, Hp = 10790, Spd = 17, Cri_Dmg = 150 },
            [("챈슬러", 1)] = new BaseStatSet { Atk = 849,  Def = 466,  Hp = 7870,  Spd = 21, Cri_Dmg = 150 },
            [("챈슬러", 2)] = new BaseStatSet { Atk = 1315, Def = 784,  Hp = 9870,  Spd = 23, Cri_Dmg = 150 },
        };

        /// <summary>공성전 감쇄 프로필 (보스/요일 공통으로 몹에도 적용).</summary>
        public class Reduction { public double Phys, Mag, Single, Triple, Multi; }

        /// <summary>(몹종류, 스킬타입) 우선순위 항목 — 빌더가 라운드별 EnemyId로 변환.</summary>
        public record PriItem(string Name, SkillType Skill);

        public class DayDef
        {
            public string Key;             // "토요일"
            public int DayIndex;           // 1=월 … 7=일 (Id 할당용)
            public string StageName;
            public string BossName;        // R3 보스 (SiegeBosses에서 조회)
            public Reduction MobReduction = new();

            // R1/R2 몹 스킬 (요일별). 스탯은 MobStats 공통.
            public List<Skill> R1Look, R1Chan, R2Look, R2Chan;

            // R3 친위대 (요일별 스탯 + 스킬)
            public BaseStatSet R3LookStats, R3ChanStats;
            public List<Skill> R3Look, R3Chan;

            // 스킬 우선순위 (라운드별)
            public List<PriItem> Pri1 = new(), Pri2 = new(), Pri3 = new();
        }

        // ===== 요일 정의 (현재 토요일만; 나머지는 데이터 확보 시 추가) =====
        public static readonly List<DayDef> Days = new()
        {
            new DayDef
            {
                Key = "토요일", DayIndex = 6,
                StageName = "토요일 공성전 (혹한의 성)", BossName = "스파이크",
                MobReduction = new Reduction { Mag = 90, Single = 70, Multi = 90 },

                R1Look = SiegeBossSkillDb.LookSkills(80, 275),
                R2Look = SiegeBossSkillDb.LookSkills(80, 275),
                R1Chan = SiegeBossSkillDb.ChancellorSkills(90, 305, r3Frost: false),
                R2Chan = SiegeBossSkillDb.ChancellorSkills(90, 305, r3Frost: false),

                R3LookStats = new BaseStatSet { Atk = 1502, Def = 1423, Hp = 40000, Spd = 19, Cri_Dmg = 150, Eff_Hit = 100 },
                R3ChanStats = new BaseStatSet { Atk = 1754, Def = 1423, Hp = 40000, Spd = 25, Cri_Dmg = 150, Eff_Hit = 100 },
                R3Look = SiegeBossSkillDb.LookSkills(100, 340),
                R3Chan = SiegeBossSkillDb.ChancellorSkills(100, 340, r3Frost: true),

                Pri1 = new() { new("챈슬러", SkillType.Skill1), new("룩", SkillType.Skill1) },
                Pri2 = new() { new("챈슬러", SkillType.Skill1), new("룩", SkillType.Skill1) },
                Pri3 = new()
                {
                    new("챈슬러", SkillType.Skill1),   // 챈슬러 분쇄
                    new("스파이크", SkillType.Skill2),  // 혹한의 지진
                    new("스파이크", SkillType.Skill1),  // 혹한의 일격
                    new("룩", SkillType.Skill1),        // 룩 투창
                },
            },
        };

        // ===== 빌더: 요일별 몹 Enemy + Stage 생성 =====
        // Id = DayIndex*100 + Round*10 + (룩 1 / 챈슬러 2). R3 wave엔 요일 보스도 추가.
        public static (List<Enemy> Mobs, Dictionary<string, Stage> Stages) Build(IEnumerable<Enemy> bosses)
        {
            var bossList = bosses.ToList();
            var mobs = new List<Enemy>();
            var stages = new Dictionary<string, Stage>();

            foreach (var d in Days)
            {
                int MobId(string name, int round) => d.DayIndex * 100 + round * 10 + (name == "룩" ? 1 : 2);

                Enemy MakeMob(string name, int round, BaseStatSet stats, List<Skill> skills)
                {
                    var e = new Enemy
                    {
                        Id = MobId(name, round),
                        Name = name,
                        EnemyType = EnemyType.Siege,
                        IsBoss = false,
                        Stats = (stats ?? new BaseStatSet()).Clone(),
                        Skills = skills ?? new List<Skill>(),
                    };
                    e.PhysicalReduction = d.MobReduction.Phys;
                    e.MagicReduction = d.MobReduction.Mag;
                    e.SingleTargetReduction = d.MobReduction.Single;
                    e.TripleTargetReduction = d.MobReduction.Triple;
                    e.MultiTargetReduction = d.MobReduction.Multi;
                    return e;
                }

                var r1Look = MakeMob("룩", 1, MobStats[("룩", 1)], d.R1Look);
                var r1Chan = MakeMob("챈슬러", 1, MobStats[("챈슬러", 1)], d.R1Chan);
                var r2Look = MakeMob("룩", 2, MobStats[("룩", 2)], d.R2Look);
                var r2Chan = MakeMob("챈슬러", 2, MobStats[("챈슬러", 2)], d.R2Chan);
                var r3Look = MakeMob("룩", 3, d.R3LookStats, d.R3Look);
                var r3Chan = MakeMob("챈슬러", 3, d.R3ChanStats, d.R3Chan);
                mobs.AddRange(new[] { r1Look, r1Chan, r2Look, r2Chan, r3Look, r3Chan });

                var boss = bossList.FirstOrDefault(b => b.Name == d.BossName);

                int IdOf(string name, int round) =>
                    name == "룩" || name == "챈슬러" ? MobId(name, round) : (boss?.Id ?? 0);

                List<SiegeSkillOrder> Pri(List<PriItem> items, int round) =>
                    items.Select(p => new SiegeSkillOrder { EnemyId = IdOf(p.Name, round), SkillType = p.Skill }).ToList();

                stages[d.Key] = new Stage
                {
                    Id = d.DayIndex,
                    Name = d.StageName,
                    StageType = EnemyType.Siege,
                    Waves = new List<StageWave>
                    {
                        new StageWave
                        {
                            WaveNumber = 1,
                            Enemies = new()
                            {
                                new StageEnemy { EnemyId = r1Look.Id, Position = 1 },
                                new StageEnemy { EnemyId = r1Chan.Id, Position = 2 },
                                new StageEnemy { EnemyId = r1Look.Id, Position = 3 },
                            },
                            SkillPriority = Pri(d.Pri1, 1),
                        },
                        new StageWave
                        {
                            WaveNumber = 2,
                            Enemies = new()
                            {
                                new StageEnemy { EnemyId = r2Look.Id, Position = 1 },
                                new StageEnemy { EnemyId = r2Chan.Id, Position = 2 },
                                new StageEnemy { EnemyId = r2Look.Id, Position = 3 },
                            },
                            SkillPriority = Pri(d.Pri2, 2),
                        },
                        new StageWave
                        {
                            WaveNumber = 3,   // 모두 보스 취급
                            Enemies = new()
                            {
                                new StageEnemy { EnemyId = r3Look.Id, Position = 1, IsBoss = true },
                                new StageEnemy { EnemyId = boss?.Id ?? 0, Position = 2, IsBoss = true },
                                new StageEnemy { EnemyId = r3Chan.Id, Position = 3, IsBoss = true },
                            },
                            SkillPriority = Pri(d.Pri3, 3),
                        },
                    },
                };
            }

            return (mobs, stages);
        }
    }
}
