using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;

// 영웅 데이터를 웹용 JSON으로 추출
// 출력: web/src/data/characters.json

// BuffSet의 0이 아닌 필드만 camelCase 키 딕셔너리로 변환 (웹 표시용)
static Dictionary<string, double> NonZeroBuff(BuffSet b)
{
    var d = new Dictionary<string, double>();
    if (b == null) return d;
    void Put(string k, double v) { if (v != 0) d[k] = v; }
    Put("atkRate", b.Atk_Rate);
    Put("magicAtkRate", b.MagicAtk_Rate);
    Put("defRate", b.Def_Rate);
    Put("hpRate", b.Hp_Rate);
    Put("cri", b.Cri);
    Put("criDmg", b.Cri_Dmg);
    Put("criBonusDmg", b.CriBonusDmg);
    Put("wek", b.Wek);
    Put("wekDmg", b.Wek_Dmg);
    Put("wekBonusDmg", b.WekBonusDmg);
    Put("dmgDealt", b.Dmg_Dealt);
    Put("dmgDealtType", b.Dmg_Dealt_Type);
    Put("markEnergeia", b.Mark_Energeia);
    Put("markPurify", b.Mark_Purify);
    Put("dmgDealtBoss", b.Dmg_Dealt_Bos);
    Put("dmgDealt1to3", b.Dmg_Dealt_1to3);
    Put("dmgDealt4to5", b.Dmg_Dealt_4to5);
    Put("armPen", b.Arm_Pen);
    Put("dmgRdc", b.Dmg_Rdc);
    Put("physDmgRdc", b.Phys_Dmg_Rdc);
    Put("magDmgRdc", b.Mag_Dmg_Rdc);
    Put("dmgRdcMulti", b.Dmg_Rdc_Multi);
    Put("blk", b.Blk);
    Put("healBonus", b.Heal_Bonus);
    Put("effRes", b.Eff_Res);
    Put("effHit", b.Eff_Hit);
    Put("shieldHpRatio", b.Shield_HpRatio);
    Put("blessing", b.Blessing);
    Put("coopChance", b.Coop_Chance);
    Put("cooldownReduction", b.Cooldown_Reduction);
    return d;
}

var heroes = CharacterDb.Characters.Select(c =>
{
    var bs = c.GetBaseStats(); // 등급/타입별 기본 스탯
    var t6 = c.GetTranscendStats(6);   // 1~6초월 누적 보너스
    var t12 = c.GetTranscendStats(12); // 1~12초월 누적 보너스

    // 초월 보너스(%·고정)를 기본 스탯에 적용한 최종 스탯 (장비/버프 제외)
    object Stat(BaseStatSet t) => new
    {
        atk = Math.Round(bs.Atk * (1 + t.Atk_Rate / 100.0) + t.Atk),
        def = Math.Round(bs.Def * (1 + t.Def_Rate / 100.0) + t.Def),
        hp = Math.Round(bs.Hp * (1 + t.Hp_Rate / 100.0) + t.Hp),
        spd = bs.Spd + t.Spd,
        cri = bs.Cri + t.Cri,
        criDmg = bs.Cri_Dmg + t.Cri_Dmg,
        wek = bs.Wek + t.Wek,
        wekDmg = bs.Wek_Dmg + t.Wek_Dmg,
    };

    // ===== 스킬·패시브 효과 → 버프/디버프 필터 태그 =====
    var tags = new SortedSet<string>();
    bool isMagic = c.AttackType == AttackType.Magic;

    void AddBuff(BuffSet b)
    {
        if (b == null) return;
        if (b.Atk_Rate > 0) tags.Add("물공증");
        if (b.MagicAtk_Rate > 0) tags.Add("마공증");
        if (b.Dmg_Dealt > 0) tags.Add("피증");
        if (b.Dmg_Dealt_Type > 0 || b.Mark_Energeia > 0 || b.Mark_Purify > 0)
            tags.Add(isMagic ? "마피증" : "물피증");
        if (b.Cri_Dmg > 0) tags.Add("치피증");
        if (b.Wek_Dmg > 0) tags.Add("약피증");
        if (b.Dmg_Dealt_1to3 > 0) tags.Add("1-3인기");
        if (b.Dmg_Dealt_4to5 > 0) tags.Add("4-5인기");
        if (b.Dmg_Dealt_Bos > 0) tags.Add("보피증");
        if (b.Def_Rate > 0) tags.Add("방어");
        if (b.Blk > 0) tags.Add("막기확률");
        if (b.Dmg_Rdc > 0 || b.Phys_Dmg_Rdc > 0 || b.Mag_Dmg_Rdc > 0 || b.Dmg_Rdc_Multi > 0)
            tags.Add("받피감");
        if (b.Heal_Bonus > 0) tags.Add("받회증");
        if (b.Eff_Hit > 0) tags.Add("효적증");
        if (b.Eff_Res > 0) tags.Add("효저증");
    }

    void AddDebuff(DebuffSet d)
    {
        if (d == null) return;
        if (d.Def_Reduction > 0) tags.Add("방깎");
        if (d.Vulnerability > 0) tags.Add(isMagic ? "마법취약" : "물리취약");
        if (d.Dmg_Taken_Increase > 0) tags.Add("받피증");
        if (d.Phys_Dmg_Taken_Increase > 0) tags.Add("받물피증");
        if (d.Mag_Dmg_Taken_Increase > 0) tags.Add("받마피증");
        if (d.Atk_Reduction > 0) tags.Add(isMagic ? "마공감" : "물공감");
        if (d.Dmg_Reduction > 0) tags.Add("피감");
        if (d.Blk_Red > 0) tags.Add("막기확률감소");
        if (d.Cri_Dmg_Reduction > 0) tags.Add("치피감");
        if (d.Heal_Reduction > 0) tags.Add("받회감");
        if (d.Eff_Red > 0) tags.Add("효저깎");
    }

    void AddStatus(StatusEffectType st)
    {
        if (st == StatusEffectType.None) return;
        if (StatusEffectDb.Effects.TryGetValue(st, out var sd) && !string.IsNullOrEmpty(sd.Name))
            tags.Add(sd.Name);
    }

    var pas = c.Passive;
    if (pas != null)
    {
        AddBuff(pas.GetTotalSelfBuff(true, 12));
        AddBuff(pas.GetPartyBuff(true, 12));
        AddBuff(pas.GetConditionalSelfBuff(true, 12));
        AddBuff(pas.GetConditionalPartyBuff(true, 12));
        AddDebuff(pas.GetDebuff(true, 12));
        AddDebuff(pas.GetConditionalDebuff(true, 12));
        foreach (var plv in new[] { pas.GetLevelData(false), pas.GetLevelData(true) })
            if (plv?.Effects != null) foreach (var e in plv.Effects) AddStatus(e.StatusType);
        var pt = pas.GetTranscendBonus(12);
        if (pt?.Effects != null) foreach (var e in pt.Effects) AddStatus(e.StatusType);
    }
    foreach (var sk in c.Skills)
    {
        foreach (var lvl in new[] { sk.GetLevelData(false), sk.GetLevelData(true) })
        {
            if (lvl == null) continue;
            AddBuff(lvl.Bonus);
            AddBuff(lvl.SelfBuff);
            AddBuff(lvl.PartyBuff);
            AddBuff(lvl.PreCastBuff);
            AddDebuff(lvl.DebuffEffect);
            if (lvl.Effects != null)
                foreach (var e in lvl.Effects) { AddBuff(e.Buff); AddDebuff(e.Debuff); AddStatus(e.StatusType); }
        }
        var st = sk.GetTranscendBonus(12);
        AddBuff(st.Bonus);
        AddBuff(st.PartyBuff);
        AddDebuff(st.Debuff);
        if (st.Effects != null)
            foreach (var e in st.Effects) { AddBuff(e.Buff); AddDebuff(e.Debuff); AddStatus(e.StatusType); }
    }

    return new
    {
        id = c.Id,
        name = c.Name,
        grade = c.Grade,                     // 전설 / 영웅 / 희귀
        type = c.Type,                       // 공격형 / 마법형 / 만능형 / 지원형 / 방어형
        attackType = c.AttackType.ToString(), // Physical / Magic
        transcendType = c.TranscendType.ToString(),
        baseStats = Stat(new BaseStatSet()),
        transcend6 = Stat(t6),
        transcend12 = Stat(t12),
    passive = c.Passive == null ? null : new
    {
        name = c.Passive.Name,
        description = c.Passive.Description,
        maxStacks = c.Passive.GetMaxStacks(true, 12),
        // 티어별 패시브 버프 값 (자버프+파티버프 합산 스냅샷, override 반영). 비0 필드만.
        buffTiers = new
        {
            @base = NonZeroBuff(c.Passive.GetTotalSelfBuff(false, 0)),
            enhanced = NonZeroBuff(c.Passive.GetTotalSelfBuff(true, 0)),
            transcend = NonZeroBuff(c.Passive.GetTotalSelfBuff(true, 12)),
        }
    },
    skills = c.Skills.Select(s =>
    {
        var l0 = s.GetLevelData(false);
        var l1 = s.GetLevelData(true);
        var tr = s.GetTranscendBonus(12);
        // target은 선언된 레벨값을 그대로 노출 (0 = 대상 없는 유틸 스킬 → 웹에서 빈칸 표시)
        return new
        {
            id = s.Id,
            name = s.Name,
            skillType = s.SkillType.ToString(),
            tiers = new
            {
                @base = new { cooldown = s.GetCooldown(false, 0), target = l0.TargetCount, atk = s.GetAtkCount(false, 0), ratio = l0.Ratio, effect = l0.Effect },
                enhanced = new { cooldown = s.GetCooldown(true, 0), target = l1.TargetCount, atk = s.GetAtkCount(true, 0), ratio = l1.Ratio, effect = l1.Effect },
                transcend = new { cooldown = s.GetCooldown(true, 12), target = tr.TargetCountOverride ?? l1.TargetCount, atk = s.GetAtkCount(true, 12), ratio = l1.Ratio, effect = tr.Effect },
            },
        };
    }).ToList(),
        tags = tags.ToList(),
    };
}).ToList();

var options = new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

string json = JsonSerializer.Serialize(heroes, options);

// tools/SenaDataExport/bin/... 에서 실행되므로 리포 루트 기준 경로 계산
string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string outDir = Path.Combine(repoRoot, "web", "src", "data");
Directory.CreateDirectory(outDir);
string outPath = Path.Combine(outDir, "characters.json");
File.WriteAllText(outPath, json);

Console.WriteLine($"Exported {heroes.Count} heroes → {outPath}");
