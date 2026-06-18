using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services.BattleEngine;

// ============================================================================
// 목요일 — 유저 실측 계정 스펙(공성전_목요일.xlsx)을 그대로 고정하고:
//   ① 유저 손빌드 로테(실측 1400만)  ②빔 자동탐색 로테
// 를 동일 기어로 돌려 점수를 비교한다(버그 진단: 모델이 손빌드를 빔보다 높게 보는가?).
// 영웅별 초월 12/12/10/6/9 · 잠재 3·3·3 / 3·3·3 / 3·0·0 / 0·0·0 / 0·0·0.
// 진형 보호 · 라이언 후열 · 타카 4번 · 돼오는 타카와 속공동률 시 뒷순서.
// 펫 윈디 6성·강화3·잠재 모공64%.
// ============================================================================

static Equipment Wpn(string set, string main, params (string Stat, int Tier)[] subs) => Eq("무기", set, main, subs);
static Equipment Arm(string set, string main, params (string Stat, int Tier)[] subs) => Eq("방어구", set, main, subs);
static Equipment Eq(string slot, string set, string main, (string Stat, int Tier)[] subs)
{
    var e = new Equipment { Slot = slot, SetName = set, MainStatName = main, EnhanceLevel = 5 };
    for (int i = 0; i < 4 && i < subs.Length; i++) { e.SubSlots[i].StatName = subs[i].Stat; e.SubSlots[i].Tier = subs[i].Tier; }
    return e;
}
static Accessory Acc6(string ring, string main, string sub) => new() { Grade = 6, RingName = ring, MainOption = main, SubOption = sub };

static ExclusiveWeapon Excl(string name, int ownerId, bool isMagic, params (TuningOption Opt, ExclusiveWeaponGrade Gr)[] t)
    => new() { Name = name, OwnerCharacterId = ownerId, Atk = 247, IsMagic = isMagic,
               Tuning = t.Select(x => new TuningSlot { Option = x.Opt, Grade = x.Gr }).ToList() };

var L = ExclusiveWeaponGrade.전설; var R = ExclusiveWeaponGrade.희귀;
var ATK = TuningOption.모든공격력; var DMG = TuningOption.피해증폭;

// ===== 라이언 (id 2, 12초월, 잠재 3/3/3, 복수자4, 전용 모공12+12·피증4+4) =====
var ryanC = CharacterDb.Characters.First(c => c.Id == 2);
ryanC.ExclusiveWeapon = Excl("라이언 전용", 2, false, (ATK, L), (ATK, L), (DMG, L), (DMG, L));
var ryan = new BattleCharacter
{
    Character = ryanC, IsSkillEnhanced = true, TranscendLevel = 12,
    PotentialAtkLevel = 3, PotentialDefLevel = 3, PotentialHpLevel = 3,
    IsBackPosition = true,
    Equipment = new EquipmentLoadout
    {
        Weapon1 = Wpn("복수자", "치명타확률%", ("공격력", 2), ("치명타피해%", 3), ("치명타확률%", 3), ("약점공격확률%", 1)),
        Weapon2 = Wpn("복수자", "치명타피해%", ("방어력%", 1), ("치명타피해%", 5), ("치명타확률%", 2), ("공격력%", 1)),
        Armor1  = Arm("복수자", "공격력%", ("공격력%", 2), ("치명타피해%", 2), ("치명타확률%", 4), ("약점공격확률%", 1)),
        Armor2  = Arm("복수자", "공격력%", ("치명타피해%", 4), ("공격력%", 2), ("치명타확률%", 1), ("약점공격확률%", 2)),
        Accessory = Acc6("토벌의 반지", "보피증%", "치명타확률%"),
    },
};

// ===== 타카 (id 1, 12초월, 잠재 3/3/3, 복수자4, 전용 모공7+7·피증2.4+2.4) =====
var takaC = CharacterDb.Characters.First(c => c.Id == 1);
takaC.ExclusiveWeapon = Excl("타카 전용", 1, false, (ATK, R), (ATK, R), (DMG, R), (DMG, R));
var taka = new BattleCharacter
{
    Character = takaC, IsSkillEnhanced = true, TranscendLevel = 12,
    PotentialAtkLevel = 3, PotentialDefLevel = 3, PotentialHpLevel = 3,
    Equipment = new EquipmentLoadout
    {
        Weapon1 = Wpn("복수자", "치명타피해%", ("약점공격확률%", 2), ("치명타피해%", 5), ("치명타확률%", 1), ("공격력", 1)),
        Weapon2 = Wpn("복수자", "치명타확률%", ("약점공격확률%", 1), ("치명타피해%", 2), ("치명타확률%", 5), ("공격력", 1)),
        Armor1  = Arm("복수자", "공격력%", ("공격력%", 1), ("치명타피해%", 4), ("치명타확률%", 3), ("효과적중%", 1)),
        Armor2  = Arm("복수자", "공격력%", ("치명타피해%", 5), ("막기확률%", 1), ("치명타확률%", 1), ("약점공격확률%", 2)),
        Accessory = Acc6("토벌의 반지", "보피증%", "치명타확률%"),
    },
};

// ===== 레이첼 (id 301, 10초월, 잠재 3/0/0, 복수자4, 전용 모공7·피증4+2.4+2.4) =====
var rachelC = CharacterDb.Characters.First(c => c.Id == 301);
bool rachelMag = rachelC.AttackType == AttackType.Magic;
rachelC.ExclusiveWeapon = Excl("레이첼 전용", 301, rachelMag, (ATK, R), (DMG, L), (DMG, R), (DMG, R));
var rachel = new BattleCharacter
{
    Character = rachelC, IsSkillEnhanced = true, TranscendLevel = 10,
    PotentialAtkLevel = 3, PotentialDefLevel = 0, PotentialHpLevel = 0,
    Equipment = new EquipmentLoadout
    {
        Weapon1 = Wpn("복수자", "치명타확률%", ("생명력%", 2), ("공격력%", 1), ("치명타확률%", 5), ("공격력", 1)),
        Weapon2 = Wpn("복수자", "치명타피해%", ("약점공격확률%", 1), ("치명타피해%", 1), ("치명타확률%", 5), ("공격력%", 1)),
        Armor1  = Arm("복수자", "공격력%", ("공격력%", 1), ("치명타피해%", 3), ("치명타확률%", 2), ("공격력", 3)),
        Armor2  = Arm("복수자", "공격력%", ("치명타피해%", 4), ("효과저항%", 1), ("치명타확률%", 2), ("공격력", 1)),
        Accessory = Acc6("토벌의 반지", "보피증%", "치명타확률%"),
    },
};

// ===== 돼오 (id 15, 6초월, 잠재 0/0/0, 복수자4, 전용 없음) =====
var dwaeoC = CharacterDb.Characters.First(c => c.Id == 15);
dwaeoC.ExclusiveWeapon = null;
var dwaeo = new BattleCharacter
{
    Character = dwaeoC, IsSkillEnhanced = true, TranscendLevel = 6,
    PotentialAtkLevel = 0, PotentialDefLevel = 0, PotentialHpLevel = 0,
    Equipment = new EquipmentLoadout
    {
        Weapon1 = Wpn("복수자", "치명타확률%", ("약점공격확률%", 2), ("치명타피해%", 2), ("치명타확률%", 2), ("공격력%", 3)),
        Weapon2 = Wpn("복수자", "치명타피해%", ("공격력%", 2), ("치명타피해%", 1), ("치명타확률%", 5), ("공격력", 1)),
        Armor1  = Arm("복수자", "공격력%", ("공격력%", 2), ("치명타피해%", 5), ("치명타확률%", 1), ("공격력", 1)),
        Armor2  = Arm("복수자", "공격력%", ("치명타피해%", 1), ("효과저항%", 1), ("치명타확률%", 4), ("약점공격확률%", 2)),
        Accessory = Acc6("토벌의 반지", "보피증%", "1-3인기%"),
    },
};

// ===== 비스킷 (id 201, 9초월, 잠재 0/0/0, 수문장4, 권능반지 — 딜 비중 0, 시뮬 기본 근사) =====
var biskitC = CharacterDb.Characters.First(c => c.Id == 201);
biskitC.ExclusiveWeapon = null;
var biskit = new BattleCharacter
{
    Character = biskitC, IsSkillEnhanced = true, TranscendLevel = 9,
    PotentialAtkLevel = 0, PotentialDefLevel = 0, PotentialHpLevel = 0,
    Equipment = new EquipmentLoadout
    {
        Weapon1 = Wpn("수문장", "치명타확률%", ("치명타확률%", 3), ("약점공격확률%", 2), ("공격력%", 2), ("생명력%", 1)),
        Weapon2 = Wpn("수문장", "치명타확률%", ("공격력", 2), ("치명타확률%", 4), ("치명타피해%", 1), ("공격력%", 2)),
        Armor1  = Arm("수문장", "공격력%", ("치명타확률%", 3), ("공격력%", 3), ("약점공격확률%", 1), ("효과적중%", 1)),
        Armor2  = Arm("수문장", "공격력%", ("막기확률%", 2), ("공격력%", 3), ("치명타확률%", 2), ("공격력", 1)),
        Accessory = Acc6("권능의 반지", "권능", "약점공격확률%"),
    },
};

// 자리: 보호 진형, 라이언 후열. 리스트 순서 = 자리(1~5): 라이언1·레이첼2·비스킷3·타카4·돼오5.
//   → 타카 4번 / 돼오 5번(타카와 속공 동률 시 Position 큰 돼오가 뒤). 라이언만 후열.
ryan.IsBackPosition = true;
rachel.IsBackPosition = false; biskit.IsBackPosition = false; taka.IsBackPosition = false; dwaeo.IsBackPosition = false;
var party = new List<BattleCharacter> { ryan, rachel, biskit, taka, dwaeo };

// ----- 유저 손빌드 로테(실측 1400만) — 자리 인덱스: 라0·레1·비2·타3·돼4 -----
const int Hr = 0, He = 1, Hb = 2, Ht = 3, Hd = 4;
RotationDecision D(int h, SkillType s) => new() { HeroIndex = h, Skill = s };
var userRot = new List<RotationDecision>
{
    D(Hr, SkillType.Skill1), // 1 강자사냥
    D(He, SkillType.Skill1), // 2 염화
    D(Hb, SkillType.Skill1), // 3 장비강화
    D(He, SkillType.Skill2), // 4 불새
    D(Hd, SkillType.Skill1), // 5 룰렛맨
    D(Hr, SkillType.Skill2), // 6 광풍참
    D(Ht, SkillType.Skill2), // 7 죽음의무도
    D(Ht, SkillType.Skill1), // 8 바람의칼날
    D(Hr, SkillType.Skill1), // 9 강자사냥
    D(He, SkillType.Skill2), // 10 불새(갱신)
    D(Hd, SkillType.Skill1), // 11 룰렛맨
    D(Hb, SkillType.Skill1), // 12 장비강화
    D(Hr, SkillType.Skill2), // 13 광풍참
    D(Ht, SkillType.Skill2), // 14 죽음의무도
    D(Hr, SkillType.Skill1), // 15 강자사냥
    D(Ht, SkillType.Skill1), // 16 바람의칼날
    D(Hd, SkillType.Skill1), // 17 룰렛맨
    D(Ht, SkillType.Skill2), // 18 죽음의무도
    D(Hr, SkillType.Skill2), // 19 광풍참
};

// ----- 시뮬 12초월 윈디 빌드 로테(유저가 실측 ~9.69M 뽑은 그 로테) — 본인 기어로 재현 검증 -----
var simBuildRot = new List<RotationDecision>
{
    D(Hr, SkillType.Skill1), // 1 강자사냥
    D(Hr, SkillType.Skill2), // 2 광풍참
    D(Ht, SkillType.Skill2), // 3 죽음의무도
    D(He, SkillType.Skill2), // 4 불새
    D(Ht, SkillType.Skill1), // 5 바람의칼날
    D(Hd, SkillType.Skill1), // 6 룰렛맨
    D(Hb, SkillType.Skill1), // 7 장비강화
    D(Ht, SkillType.Skill2), // 8 죽음의무도
    D(Hr, SkillType.Skill1), // 9 강자사냥
    D(Hr, SkillType.Skill2), // 10 광풍참
    D(He, SkillType.Skill1), // 11 염화
    D(Ht, SkillType.Skill2), // 12 죽음의무도
    D(Hd, SkillType.Skill2), // 13 진수성참
    D(Hd, SkillType.Skill1), // 14 룰렛맨
    D(Hr, SkillType.Skill1), // 15 강자사냥
    D(Ht, SkillType.Skill1), // 16 바람의칼날
    D(Hb, SkillType.Skill1), // 17 장비강화
    D(Hr, SkillType.Skill2), // 18 광풍참
    D(Ht, SkillType.Skill2), // 19 죽음의무도
    D(Hr, SkillType.Skill1), // 20 강자사냥
};

if (!EnemyDb.SiegeStages.TryGetValue("목요일", out var stage)) { Console.WriteLine("[skip] 목요일 데이터 없음"); return; }

SiegeBattleConfig MakeCfg(List<RotationDecision> rot) => new()
{
    AllyParty = party, SiegeStage = stage, FormationName = "보호 진형",
    AllyPet = PetDb.GetByName("윈디"), PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 64,
    MaxTurns = 70, RotationPlan = rot,
};

// ① 유저 로테
var userSim = new SiegeBattleSimulator(777) { DiagSkillNames = new List<string> { "광풍참", "죽음의 무도" } };
var userRes = userSim.Simulate(MakeCfg(userRot));

// ③ 시뮬 12초월 윈디 빌드 로테(유저 실측 ~9.69M) — 본인 기어로 재현
var simSim = new SiegeBattleSimulator(777) { DiagSkillNames = new List<string> { "광풍참", "죽음의 무도" } };
var simRes = simSim.Simulate(MakeCfg(simBuildRot));

// 전투로그 + per-hit 진단 덤프 (두 로테 비교용)
void DumpLog(string tag, SiegeBattleResult r, SiegeBattleSimulator s)
{
    var lb = new StringBuilder();
    lb.AppendLine($"=== {tag} per-hit (광풍참·죽음의무도) ===");
    lb.AppendLine(s.DiagLog.ToString());
    lb.AppendLine($"=== {tag} 턴로그 ===");
    foreach (var lg in r.TurnLogs) lb.AppendLine($"T{lg.Turn,2} [{(lg.IsAlly?"아":"적")}] {lg.SkillName}: {lg.Description}");
    System.IO.File.WriteAllText(System.IO.Path.GetFullPath(System.IO.Path.Combine(
        AppContext.BaseDirectory, "..","..","..","..","..","results","siege",$"_myspec_{tag}.log")), lb.ToString(), Encoding.UTF8);
}
DumpLog("hand", userRes, userSim);
DumpLog("simbuild", simRes, simSim);

// ② 빔 자동탐색 (기어·진형·자리·펫 고정)
var beamCfg = MakeCfg(null);
var beam = new RotationBeamSearch(777).Search(beamCfg, beamWidth: 10, maxDepth: 36);

string[] nm = { "라이언", "레이첼", "비스킷", "타카", "돼오" };
string SkNm(int hi, SkillType st) => party[hi].Character.Skills?.FirstOrDefault(s => s.SkillType == st)?.Name ?? st.ToString();

var sb = new StringBuilder();
sb.AppendLine("============ 목요일 유저 실측 스펙 고정 — 유저 로테 vs 빔 로테 (동일 기어) ============");
sb.AppendLine($"보스: {stage.Name} · 진형 보호 · 라이언 후열 · 펫 윈디6성강화3 모공64%");
sb.AppendLine($"초월 라12·타12·레10·돼6·비9 / 잠재 라타3·3·3 레3·0·0 돼비0");
sb.AppendLine();
sb.AppendLine($"① 유저 손빌드 로테(실측 1400만): {userRes.TotalScore:N0}");
sb.AppendLine($"   라운드별 {string.Join(", ", userRes.RoundScore.OrderBy(k=>k.Key).Select(k=>$"R{k.Key}={k.Value:N0}"))} · 생존 {userRes.AlliesAlive}/5");
foreach (var c in userRes.CharacterResults.OrderByDescending(c => c.TotalDamage))
    sb.AppendLine($"     {c.CharacterName}: {c.TotalDamage:N0} ({c.DamageShare:F1}%)");
sb.AppendLine();
sb.AppendLine($"③ 시뮬 12초월윈디 빌드 로테(유저 실측 ~9,685,980): {simRes.TotalScore:N0}");
sb.AppendLine($"   라운드별 {string.Join(", ", simRes.RoundScore.OrderBy(k=>k.Key).Select(k=>$"R{k.Key}={k.Value:N0}"))} · 생존 {simRes.AlliesAlive}/5");
foreach (var c in simRes.CharacterResults.OrderByDescending(c => c.TotalDamage))
    sb.AppendLine($"     {c.CharacterName}: {c.TotalDamage:N0} ({c.DamageShare:F1}%)");
sb.AppendLine();
sb.AppendLine($"② 빔 자동탐색 로테: {beam.Score:N0}");
sb.AppendLine($"   라운드별 {string.Join(", ", beam.Battle.RoundScore.OrderBy(k=>k.Key).Select(k=>$"R{k.Key}={k.Value:N0}"))} · 생존 {beam.Battle.AlliesAlive}/5");
foreach (var c in beam.Battle.CharacterResults.OrderByDescending(c => c.TotalDamage))
    sb.AppendLine($"     {c.CharacterName}: {c.TotalDamage:N0} ({c.DamageShare:F1}%)");
sb.AppendLine("   빔 스킬 순서:");
for (int i = 0; i < beam.Plan.Count; i++)
{
    var d = beam.Plan[i];
    sb.AppendLine($"     {i + 1,2}. " + (d.Hold ? "(홀드)" : $"{nm[d.HeroIndex]} → {SkNm(d.HeroIndex, d.Skill)}"));
}
sb.AppendLine();
double diff = beam.Score - userRes.TotalScore;
sb.AppendLine($"▶ 빔 − 유저 = {diff:N0} ({diff/userRes.TotalScore*100:+0.0;-0.0}%)");
sb.AppendLine(diff > userRes.TotalScore * 0.02
    ? "  → 시뮬은 빔로테를 유저로테보다 높게 평가. (현실 14M>9.69M와 반대면 모델 버그/탐색 문제)"
    : "  → 시뮬도 유저로테를 빔과 비슷/높게 평가. (모델이 손빌드 우위를 포착)");

string outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(
    AppContext.BaseDirectory, "..", "..", "..", "..", "..", "results", "siege", "siege_목요일_유저스펙진단.txt"));
System.IO.File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
Console.WriteLine(sb.ToString());
Console.WriteLine($"→ {outPath}");
