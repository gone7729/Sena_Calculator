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

            // ===== 월요일 — 루디. 1스킬: 생명력 전환(90→80→70%) + 기절[3턴]. 감쇄 물리90/1인70/5인90. 쿨 룩70/챈70 =====
            // 우선순위: 루디2 → 루디1 → 챈1 → 룩1
            Day(1, "월요일", "월요일 공성전 (수호자의 성)", "루디",
                new Reduction { Phys = 90, Single = 70, Multi = 90 },
                status: StatusEffectType.Stun, hpConv: new[] { 90.0, 80.0, 70.0 }, r3Def: 1030,
                pri3: new() { B("루디", SkillType.Skill2), B("루디", SkillType.Skill1), Chan1, Look1 }),

            // ===== 화요일 — 아일린. 1스킬: 감전[3턴]. R3 룩 추가: 모든 아군(보스측) 피해면역[2턴](모델 미반영). 물리90. 쿨 룩80/챈70 =====
            // 우선순위: 아일린1 → 아일린2 → 룩1 → 챈1
            Day(2, "화요일", "화요일 공성전 (포디나의 성)", "아일린",
                new Reduction { Phys = 90, Single = 70, Multi = 90 },
                status: StatusEffectType.Shock, lookCd: 80, chanCd: 70, r3Def: 1814,
                r3LookExtra: "모든 아군(보스측) 모든 피해 면역[2턴] (모델 미반영)",
                pri3: new() { B("아일린", SkillType.Skill1), B("아일린", SkillType.Skill2), Look1, Chan1 }),

            // ===== 수요일 — 레이첼. 1스킬: 화상[3턴]. 물리90. 쿨 룩70/챈70 =====
            // 우선순위: 레이첼1 → 레이첼2 → 챈1 → 룩1
            Day(3, "수요일", "수요일 공성전 (불멸의 성)", "레이첼",
                new Reduction { Phys = 90, Single = 70, Multi = 90 },
                status: StatusEffectType.Burn, r3Def: 1814,
                pri3: new() { B("레이첼", SkillType.Skill1), B("레이첼", SkillType.Skill2), Chan1, Look1 }),

            // ===== 목요일 — 델론즈. 1스킬: 침묵[3턴]. 감쇄 마법90. 쿨 룩70/챈70 =====
            // 우선순위: 델론즈1 → 델론즈2 → 룩1 → 챈1
            Day(4, "목요일", "목요일 공성전 (죽음의 성)", "델론즈",
                new Reduction { Mag = 90, Single = 70, Multi = 90 },
                status: StatusEffectType.Silence, r3Def: 1344,
                pri3: new() { B("델론즈", SkillType.Skill1), B("델론즈", SkillType.Skill2), Look1, Chan1 }),

            // ===== 금요일 — 제이브. 룩=기절, 챈슬러=용염(화상 120%)[3턴]. 감쇄 마법90. 쿨 룩70/챈70 =====
            // 우선순위: 제이브1 → 제이브2 → 챈1 → 룩1
            Day(5, "금요일", "금요일 공성전 (고대용의 성)", "제이브",
                new Reduction { Mag = 90, Single = 70, Multi = 90 },
                status: StatusEffectType.Stun, r3Def: 1123,
                chanStatus: StatusEffectType.Burn, chanSkillName: "용염", chanStatusAtkRatio: 120,
                pri3: new() { B("제이브", SkillType.Skill1), B("제이브", SkillType.Skill2), Chan1, Look1 }),

            // ===== 일요일 — 크리스. 1스킬: 즉사[3턴]. 감쇄 5인기 90%만. 쿨 룩70/챈70. (몹 쿨감 패시브: 피격 시 쿨-15초 — 모델 미반영) =====
            // 우선순위: 크리스2 → 크리스1 → 룩1 → 챈1
            Day(7, "일요일", "일요일 공성전 (지옥의 성)", "크리스",
                new Reduction { Multi = 90 },
                status: StatusEffectType.InstantDeath, r3Def: 2725,
                pri3: new() { B("크리스", SkillType.Skill2), B("크리스", SkillType.Skill1), Look1, Chan1 }),
        };

        // R3 친위대 스탯은 요일별 미제공 → 토요일 값을 임시 placeholder로 사용 (실측 확보 시 요일별 교체).
        private static BaseStatSet R3LookPlaceholder() => new() { Atk = 1502, Def = 1423, Hp = 40000, Spd = 19, Cri_Dmg = 150, Eff_Hit = 100 };
        private static BaseStatSet R3ChanPlaceholder() => new() { Atk = 1754, Def = 1423, Hp = 40000, Spd = 25, Cri_Dmg = 150, Eff_Hit = 100 };

        /// <summary>
        /// 요일 DayDef 생성 헬퍼. 룩/챈슬러 1스킬은 단일 ratio(R1 80/275, R2 90/305, R3 100/340) + 상태이상[3턴].
        /// pri3 = R3 스킬 우선순위(보스+몹). R1/R2 우선순위는 pri3에서 룩/챈만 추려 사용(보스 없음).
        /// lookCd/chanCd = 1스킬 쿨(라운드 동일). hpConv: 생명력 전환%(월). chan* : 챈슬러만 다른 상태(금 용염).
        /// </summary>
        private static DayDef Day(int dayIndex, string key, string stageName, string bossName, Reduction red,
            StatusEffectType status, List<PriItem> pri3, double lookCd = 70, double chanCd = 70,
            double[] hpConv = null, string r3LookExtra = null,
            StatusEffectType? chanStatus = null, string chanSkillName = "1스킬", double? chanStatusAtkRatio = null,
            double r3Def = 0)
        {
            // R3 친위대 표준 스탯: Atk 룩1502/챈1754, Hp 40000, Spd 룩19/챈25, 효적100, 치피150. 방어력만 요일별.
            BaseStatSet R3Std(double atk, double spd) => new() { Atk = atk, Def = r3Def, Hp = 40000, Spd = spd, Cri_Dmg = 150, Eff_Hit = 100 };
            double Hp(int round) => hpConv == null ? 0 : hpConv[round - 1];
            var cs = chanStatus ?? status;
            List<Skill> Look(int round, double n, double s1, string extra = null) =>
                SiegeBossSkillDb.MobSkills(n, s1, lookCd, status, statusDur: 3, hpConvPct: Hp(round), extra: extra);
            List<Skill> Chan(int round, double n, double s1) =>
                SiegeBossSkillDb.MobSkills(n, s1, chanCd, cs, skill1Name: chanSkillName, statusDur: 3,
                    statusAtkRatio: chanStatusAtkRatio, hpConvPct: chanStatus == null ? Hp(round) : 0);

            // R1/R2: pri3에서 룩/챈만 (보스 제외) — 순서 유지
            var mobPri = pri3.Where(p => p.Name == "룩" || p.Name == "챈슬러").ToList();

            return new DayDef
            {
                Key = key, DayIndex = dayIndex, StageName = stageName, BossName = bossName, MobReduction = red,
                R1Look = Look(1, 80, 275), R1Chan = Chan(1, 80, 275),
                R2Look = Look(2, 90, 305), R2Chan = Chan(2, 90, 305),
                R3Look = Look(3, 100, 340, r3LookExtra), R3Chan = Chan(3, 100, 340),
                R3LookStats = r3Def > 0 ? R3Std(1502, 19) : R3LookPlaceholder(),
                R3ChanStats = r3Def > 0 ? R3Std(1754, 25) : R3ChanPlaceholder(),
                Pri1 = mobPri, Pri2 = mobPri, Pri3 = pri3,
            };
        }

        // R3 스킬 우선순위 표기 헬퍼 (프로퍼티 — Days 필드 초기화 순서와 무관하게 접근 시 평가)
        private static PriItem B(string boss, SkillType s) => new(boss, s);
        private static PriItem Look1 => new("룩", SkillType.Skill1);
        private static PriItem Chan1 => new("챈슬러", SkillType.Skill1);

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
