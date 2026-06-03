using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services.BattleEngine;

// ========================================================================
// 화요일 실측 완전 잠금 시뮬 — "같은 입력 = 같은 결과" 검증.
// 엑셀의 5캐릭 세팅(메인/부옵/장신구/세공/잠재/전용무기/진형/자리)을 손수 그대로 구성하고
// 실측 70턴 행동 시퀀스(20개 아군 스킬턴)를 RotationPlan으로 강제 → SiegeBattleSimulator 직접 호출.
// 실측 총점 6,862,365 (R1=25,170 / R2=31,450 / R3=6,805,745).
// ========================================================================

const string DAY = "화요일";
const double REAL_SCORE = 6_862_365.0;

// ----- 헬퍼 -----
static Equipment Wpn(string set, string main, params (string Stat, int Tier)[] subs)
    => Eq("무기", set, main, subs);
static Equipment Arm(string set, string main, params (string Stat, int Tier)[] subs)
    => Eq("방어구", set, main, subs);
static Equipment Eq(string slot, string set, string main, (string Stat, int Tier)[] subs)
{
    var e = new Equipment { Slot = slot, SetName = set, MainStatName = main, EnhanceLevel = 5 };
    for (int i = 0; i < 4 && i < subs.Length; i++)
    {
        e.SubSlots[i].StatName = subs[i].Stat;
        e.SubSlots[i].Tier = subs[i].Tier;
    }
    return e;
}
static Accessory Acc6(string name, string main, string sub = null) => new()
{
    Grade = 6, MainOption = main, SubOption = sub,
    // Name은 게임 진영 효과 매칭용. AccessoryEffectDb에서 사용.
};

// ----- 5캐릭 BattleCharacter 빌드 (엑셀 데이터 그대로) -----

// 나타 (Id=118, 마법형, T4 잠재 1/1/1) — 무기/방어구 복수자, 토벌의 반지, 공용 전용무기
var nataChar = CharacterDb.Characters.First(c => c.Id == 118);
nataChar.ExclusiveWeapon = ExclusiveWeaponDb.Universal(magic: true); // 마공 247만
var nata = new BattleCharacter
{
    Character = nataChar, IsSkillEnhanced = true, TranscendLevel = 4,
    PotentialAtkLevel = 1, PotentialDefLevel = 1, PotentialHpLevel = 1,
    Equipment = new EquipmentLoadout
    {
        Weapon1 = Wpn("복수자", "치명타피해%", ("공격력", 2), ("치명타피해%", 4), ("공격력%", 2), ("약점공격확률%", 1)),
        Weapon2 = Wpn("복수자", "치명타피해%", ("공격력", 2), ("치명타피해%", 4), ("치명타확률%", 1), ("방어력%", 1)),
        Armor1  = Arm("복수자", "공격력%", ("약점공격확률%", 4), ("치명타피해%", 1), ("치명타확률%", 2), ("공격력%", 2)),
        Armor2  = Arm("복수자", "공격력%", ("치명타피해%", 5), ("막기확률%", 1), ("치명타확률%", 1), ("약점공격확률%", 2)),
        Accessory = Acc6("토벌의 반지", "보피증%", "약점공격확률%"),
    },
};

// 미호 (Id=103, 마법형, T9 잠재 3/3/3) — 복수자, 토벌의 반지, 전용무기 미장착
var mihoChar = CharacterDb.Characters.First(c => c.Id == 103);
mihoChar.ExclusiveWeapon = null;
var miho = new BattleCharacter
{
    Character = mihoChar, IsSkillEnhanced = true, TranscendLevel = 9,
    PotentialAtkLevel = 3, PotentialDefLevel = 3, PotentialHpLevel = 3,
    Equipment = new EquipmentLoadout
    {
        Weapon1 = Wpn("복수자", "치명타확률%", ("치명타피해%", 5), ("공격력", 1), ("공격력%", 2), ("치명타확률%", 1)),
        Weapon2 = Wpn("복수자", "치명타피해%", ("공격력", 1), ("치명타확률%", 5), ("치명타피해%", 1), ("공격력%", 2)),
        Armor1  = Arm("복수자", "공격력%", ("치명타확률%", 4), ("치명타피해%", 1), ("약점공격확률%", 2), ("효과저항%", 1)),
        Armor2  = Arm("복수자", "공격력%", ("방어력%", 1), ("약점공격확률%", 3), ("치명타확률%", 1), ("치명타피해%", 4)),
        Accessory = Acc6("토벌의 반지", "보피증%", "피증%"),
    },
};

// 비스킷 (Id=201, 지원형, T9 잠재 0/0/0) — 복수자, 권능의 반지, 전용무기 미장착
var biskitChar = CharacterDb.Characters.First(c => c.Id == 201);
biskitChar.ExclusiveWeapon = null;
var biskit = new BattleCharacter
{
    Character = biskitChar, IsSkillEnhanced = true, TranscendLevel = 9,
    PotentialAtkLevel = 0, PotentialDefLevel = 0, PotentialHpLevel = 0,
    Equipment = new EquipmentLoadout
    {
        Weapon1 = Wpn("복수자", "치명타확률%", ("치명타확률%", 2), ("약점공격확률%", 2), ("공격력%", 3), ("생명력%", 1)),
        Weapon2 = Wpn("복수자", "치명타확률%", ("공격력", 2), ("치명타확률%", 4), ("치명타피해%", 1), ("공격력%", 2)),
        Armor1  = Arm("복수자", "공격력%", ("치명타확률%", 3), ("공격력%", 3), ("약점공격확률%", 1), ("효과적중%", 1)),
        Armor2  = Arm("복수자", "공격력%", ("막기확률%", 2), ("공격력%", 3), ("치명타확률%", 2), ("공격력", 1)),
        Accessory = Acc6("권능의 반지", "권능", "약점공격확률%"),
    },
};

// 클로에 (Id=255, 방어형, T12 잠재 0/0/0) — 수문장, 샐러맨더의 반지, 전용무기 미장착
var chloeChar = CharacterDb.Characters.First(c => c.Id == 255);
chloeChar.ExclusiveWeapon = null;
var chloe = new BattleCharacter
{
    Character = chloeChar, IsSkillEnhanced = true, TranscendLevel = 12,
    PotentialAtkLevel = 0, PotentialDefLevel = 0, PotentialHpLevel = 0,
    Equipment = new EquipmentLoadout
    {
        Weapon1 = Wpn("수문장", "방어력%", ("방어력", 5), ("막기확률%", 1), ("속공", 1), ("생명력%", 2)),
        Weapon2 = Wpn("수문장", "방어력%", ("방어력%", 3), ("막기확률%", 2), ("속공", 2), ("생명력%", 1)),
        Armor1  = Arm("수문장", "막기확률%", ("생명력", 2), ("방어력", 1), ("효과적중%", 2), ("막기확률%", 3)),
        Armor2  = Arm("수문장", "막기확률%", ("방어력%", 1), ("속공", 2), ("치명타피해%", 3), ("막기확률%", 2)),
        Accessory = Acc6("샐러맨더의 반지", "샐러맨더", null),
    },
};

// 리나 (Id=202, 지원형, T10 잠재 0/0/0) — 성기사, 샐러맨더의 반지, 전용무기 미장착
var lenaChar = CharacterDb.Characters.First(c => c.Id == 202);
lenaChar.ExclusiveWeapon = null;
var lena = new BattleCharacter
{
    Character = lenaChar, IsSkillEnhanced = true, TranscendLevel = 10,
    PotentialAtkLevel = 0, PotentialDefLevel = 0, PotentialHpLevel = 0,
    Equipment = new EquipmentLoadout
    {
        Weapon1 = Wpn("성기사", "생명력%", ("방어력%", 4), ("공격력%", 1), ("속공", 1), ("생명력", 2)),
        Weapon2 = Wpn("성기사", "생명력%", ("방어력", 3), ("막기확률%", 1), ("속공", 1), ("생명력%", 2)),
        Armor1  = Arm("성기사", "생명력%", ("생명력%", 2), ("생명력", 3), ("효과적중%", 2), ("치명타피해%", 1)),
        Armor2  = Arm("성기사", "생명력%", ("생명력%", 1), ("속공", 3), ("치명타확률%", 4), ("생명력", 1)),
        Accessory = Acc6("샐러맨더의 반지", "샐러맨더", null),
    },
};

// 진형/자리: 밸런스 진형, 후열=미호·나타. 자리 순서: 클로에(0)·리나(1)·비스킷(2)·미호(3)·나타(4).
chloe.IsBackPosition = false;
lena.IsBackPosition  = false;
biskit.IsBackPosition = false;
miho.IsBackPosition   = true;
nata.IsBackPosition   = true;

var party = new List<BattleCharacter> { chloe, lena, biskit, miho, nata };

// ----- RotationPlan: 실측 70턴 아군 스킬턴 20개 (자리 인덱스 0~4: 클·리·비·미·나) -----
const int H_Chloe = 0, H_Lena = 1, H_Biskit = 2, H_Miho = 3, H_Nata = 4;
RotationDecision D(int h, SkillType s) => new() { HeroIndex = h, Skill = s };
var rotation = new List<RotationDecision>
{
    D(H_Biskit, SkillType.Skill1),  // 1.  T0  비스킷1 (R1 선공)
    D(H_Nata,   SkillType.Skill1),  // 2.  T4  나타1   (R1 클리어)
    D(H_Chloe,  SkillType.Skill2),  // 3.  T4  클로에2 (R2 진입 선공)
    D(H_Biskit, SkillType.Skill2),  // 4.  T8  비스킷2 (R2 클리어)
    D(H_Lena,   SkillType.Skill2),  // 5.  T8  리나2   (R3 진입 선공)
    D(H_Miho,   SkillType.Skill1),  // 6.  T12 미호1
    D(H_Nata,   SkillType.Skill2),  // 7.  T16 나타2
    D(H_Miho,   SkillType.Skill2),  // 8.  T20 미호2
    D(H_Nata,   SkillType.Skill1),  // 9.  T24 나타1
    D(H_Chloe,  SkillType.Skill2),  // 10. T28 클로에2
    D(H_Biskit, SkillType.Skill1),  // 11. T32 비스킷1
    D(H_Nata,   SkillType.Skill2),  // 12. T36 나타2
    D(H_Miho,   SkillType.Skill1),  // 13. T40 미호1
    D(H_Lena,   SkillType.Skill2),  // 14. T44 리나2
    D(H_Miho,   SkillType.Skill2),  // 15. T48 미호2
    D(H_Nata,   SkillType.Skill1),  // 16. T52 나타1
    D(H_Chloe,  SkillType.Skill2),  // 17. T56 클로에2
    D(H_Miho,   SkillType.Skill1),  // 18. T60 미호1
    D(H_Nata,   SkillType.Skill2),  // 19. T64 나타2
    D(H_Biskit, SkillType.Skill1),  // 20. T68 비스킷1
};

// ----- 시뮬 실행 -----
if (!EnemyDb.SiegeStages.TryGetValue(DAY, out var stage))
{
    Console.WriteLine($"[skip] {DAY} 시즈 데이터 없음"); return;
}

var config = new SiegeBattleConfig
{
    AllyParty = party,
    SiegeStage = stage,
    FormationName = "밸런스 진형",
    AllyPet = PetDb.GetByName("윈디"),
    PetStar = 6,
    PetEnhance = 3,
    PetOptionAtkRate = 72,
    MaxTurns = 70,
    RotationPlan = rotation,
};

var sw = System.Diagnostics.Stopwatch.StartNew();
var sim = new SiegeBattleSimulator(seed: 777)
{
    DiagSkillNames = new List<string> { "평타", "화첨창술", "혼천릉파" },
};
var result = sim.Simulate(config);
sw.Stop();

// ===== 최적 로테이션 탐색 (팀·기어·진형·펫·자리 고정, 스킬 순서만 빔서치) =====
//   속공(행동 순서)은 기어 고정이라 불변 → 탐색 대상은 "각 스킬턴에 누가 어떤 스킬을 쓰나".
var optCfg = new SiegeBattleConfig
{
    AllyParty = party, SiegeStage = stage, FormationName = "밸런스 진형",
    AllyPet = PetDb.GetByName("윈디"), PetStar = 6, PetEnhance = 3,
    PetOptionAtkRate = 72, MaxTurns = 70,
};
var beamSw = System.Diagnostics.Stopwatch.StartNew();
var beam = new RotationBeamSearch(seed: 777).Search(optCfg, beamWidth: 10, maxDepth: 28);
beamSw.Stop();
string[] heroNm = { "클로에", "리나", "비스킷", "미호", "나타" };
string SkNm(int hi, SkillType st)
{
    var sk = party[hi].Character.Skills?.FirstOrDefault(s => s.SkillType == st);
    return sk?.Name ?? st.ToString();
}

// ----- 결과 정리 -----
var sb = new StringBuilder();
sb.AppendLine("================ 화요일 완전 잠금 시뮬 (옵티마이저 우회 · 실측 입력 그대로) ================");
sb.AppendLine($"보스: {stage.Name}");
sb.AppendLine($"실측 총점: {REAL_SCORE:N0}  (R1=25,170 R2=31,450 R3=6,805,745)");
sb.AppendLine();
sb.AppendLine($"진형: 밸런스 진형 / 자리: 클로에(전열1)·리나(전열2)·비스킷(전열3)·미호(후열4)·나타(후열5)");
sb.AppendLine($"시뮬 총점: {result.TotalScore:N0}  ({result.TotalScore/REAL_SCORE*100:F1}%)  (도달턴 {result.TotalTurns}, 클리어 R{result.RoundsCleared}, {sw.ElapsedMilliseconds:N0}ms)");
sb.AppendLine($"라운드별: {string.Join(", ", result.RoundScore.OrderBy(k => k.Key).Select(k => $"R{k.Key}={k.Value:N0}"))}");
sb.AppendLine();
sb.AppendLine("캐릭터별 기여:");
foreach (var c in result.CharacterResults.OrderByDescending(c => c.TotalDamage))
    sb.AppendLine($"  {c.CharacterName}: {c.TotalDamage:N0} ({c.DamageShare:F1}%)");
sb.AppendLine($"\n생존 (70턴 종료 시): {result.AlliesAlive}/5");

// ===== 최적 로테이션 탐색 결과 =====
sb.AppendLine("\n════════════════ 최적 로테이션 탐색 (기어 고정 · 스킬순서 빔서치) ════════════════");
sb.AppendLine($"최적 총점: {beam.Score:N0}  (실측 {REAL_SCORE:N0}의 {beam.Score/REAL_SCORE*100:F1}%)  vs 실측로테 {result.TotalScore:N0}({result.TotalScore/REAL_SCORE*100:F1}%)");
sb.AppendLine($"  탐색 {beam.Evaluated:N0}회 시뮬, {beamSw.ElapsedMilliseconds:N0}ms, 깊이별최고 {string.Join("→", beam.ScoreByDepth.TakeLast(6).Select(s => $"{s/1e6:F2}M"))}");
sb.AppendLine($"  라운드별: {string.Join(", ", beam.Battle.RoundScore.OrderBy(k => k.Key).Select(k => $"R{k.Key}={k.Value:N0}"))}");
sb.AppendLine("  캐릭터별 기여:");
foreach (var c in beam.Battle.CharacterResults.OrderByDescending(c => c.TotalDamage))
    sb.AppendLine($"    {c.CharacterName}: {c.TotalDamage:N0} ({c.DamageShare:F1}%)");
sb.AppendLine($"  최적 스킬 순서 (스킬턴 {beam.Plan.Count}개):");
for (int i = 0; i < beam.Plan.Count; i++)
{
    var d = beam.Plan[i];
    sb.AppendLine($"    {i + 1,2}. " + (d.Hold ? "(홀드)" : $"{heroNm[d.HeroIndex]} → {SkNm(d.HeroIndex, d.Skill)}"));
}

sb.AppendLine("\n========== per-hit 진단 (나타 화첨창술·혼천릉파) ==========");
sb.AppendLine(sim.DiagLog.ToString());
sb.AppendLine("실측 비교 (시트1 발췌):");
sb.AppendLine("  T30 나타2(혼천): 아일린 444,043 / 챈슬러 429,938 / 룩 429,938");
sb.AppendLine("  T42 나타1(화첨): 아일린 254,054 / 챈슬러 245,984 / 룩 245,984");
sb.AppendLine("  T60 나타2:       아일린 206,308 / 챈슬러 197,813 / 룩 197,813");
sb.AppendLine("  T84 나타1:       아일린 254,054 / 챈슬러 72,348(약점만) / 룩 245,984");
sb.AppendLine("  T102 나타2:      아일린 267,709 / 챈슬러 256,685 / 룩 256,685");

sb.AppendLine($"\n턴별 행동 로그 (총 {result.TurnLogs.Count}개):");
foreach (var log in result.TurnLogs)
    sb.AppendLine($"T{log.Turn,2} [{(log.IsAlly ? "아군" : "적 ")}] {log.SkillName}: {log.Description}");

string outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(
    AppContext.BaseDirectory, "..", "..", "..", "..", "..", "siege_화요일_완전잠금.txt"));
System.IO.File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
Console.WriteLine($"시뮬 총점 {result.TotalScore:N0} ({result.TotalScore/REAL_SCORE*100:F1}% of 실측) → {outPath}");
