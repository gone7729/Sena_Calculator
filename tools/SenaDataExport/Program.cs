using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Models.Effects;

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
    Put("shieldAtkRatio", b.Shield_AtkRatio);
    Put("shieldDefRatio", b.Shield_DefRatio);
    Put("blessing", b.Blessing);
    Put("coopChance", b.Coop_Chance);
    Put("cooldownReduction", b.Cooldown_Reduction);
    return d;
}

// 초월 구조화 필드 → 한글 효과 텍스트 합성 (Effect 텍스트가 비었을 때 폴백).
var buffLabels = new Dictionary<string, string>
{
    ["atkRate"] = "공격력", ["magicAtkRate"] = "마법공격력", ["defRate"] = "방어력", ["hpRate"] = "생명력",
    ["cri"] = "치명확률", ["criDmg"] = "치명피해", ["criBonusDmg"] = "치명추가피해",
    ["wek"] = "약점확률", ["wekDmg"] = "약점피해", ["wekBonusDmg"] = "약점추가피해",
    ["dmgDealt"] = "피해증가", ["dmgDealtType"] = "타입피증", ["dmgDealtBoss"] = "보스피증",
    ["dmgDealt1to3"] = "1-3인기피증", ["dmgDealt4to5"] = "4-5인기피증",
    ["markEnergeia"] = "에네르게이아", ["markPurify"] = "정화표식",
    ["armPen"] = "방어무시", ["dmgRdc"] = "받피감", ["physDmgRdc"] = "물리받피감", ["magDmgRdc"] = "마법받피감",
    ["dmgRdcMulti"] = "받피감(곱)", ["blk"] = "막기", ["healBonus"] = "회복량", ["effRes"] = "효과저항",
    ["effHit"] = "효과적중", ["shieldHpRatio"] = "보호막(체력비례)", ["blessing"] = "축복",
    ["coopChance"] = "협공확률", ["cooldownReduction"] = "쿨감소",
};
// 버프 1필드 → 표현. 보호막/축복/쿨감은 단위/문구 특수처리, 나머지는 "라벨 N% 증가/감소".
string BuffClause(string k, double v) => k switch
{
    "shieldHpRatio" => $"보호막(최대 생명력 {v:0.##}% 비례)",
    "shieldAtkRatio" => $"보호막(공격력 {v:0.##}% 비례)",
    "shieldDefRatio" => $"보호막(방어력 {v:0.##}% 비례)",
    "blessing" => $"축복({v:0.##}%)",
    "cooldownReduction" => $"쿨 {v:0.##}초 감소",
    _ => $"{(buffLabels.TryGetValue(k, out var l) ? l : k)} {System.Math.Abs(v):0.##}% {(v < 0 ? "감소" : "증가")}",
};
string StatName(StatType st) => st.ToString() switch
{
    "Spd" => "속공",
    "Atk" => "공격력",
    "MagicAtk" => "마법공격력",
    "Def" => "방어력",
    "Hp" => "생명력",
    _ => st.ToString(),
};
List<string> DebuffParts(DebuffSet d)
{
    var p = new List<string>();
    if (d == null) return p;
    void Put(double v, string label) { if (v != 0) p.Add($"{label} {v:0.##}%"); }
    Put(d.Def_Reduction, "방깎"); Put(d.Blk_Red, "막기감소");
    Put(d.Dmg_Taken_Increase, "받피증"); Put(d.Phys_Dmg_Taken_Increase, "물리받피증");
    Put(d.Mag_Dmg_Taken_Increase, "마법받피증"); Put(d.Vulnerability, "취약"); Put(d.Boss_Vulnerability, "보스취약");
    Put(d.Dmg_Reduction, "주는피해감소"); Put(d.Cri_Dmg_Reduction, "치명피해감소");
    Put(d.Atk_Reduction, "공깎"); Put(d.MagicAtk_Reduction, "마공깎"); Put(d.Spd_Reduction, "속도감소");
    Put(d.Cri_Reduction, "치명확률감소"); Put(d.Wek_Reduction, "약점확률감소");
    Put(d.Heal_Reduction, "받는 회복량 감소"); Put(d.Eff_Red, "효과저항감소"); Put(d.Eff_Hit_Red, "효과적중감소");
    if (d.Cooldown_Increase != 0) p.Add($"쿨증가 {d.Cooldown_Increase:0.##}초");
    if (d.Unrecover != 0) p.Add("회복불가");
    return p;
}
string DescribeSkillEffect(SkillEffect e)
{
    if (e == null) return null;
    string chance = e.Chance > 0 && e.Chance < 100 ? $"({e.Chance:0.##}%)" : "";
    string dur = e.Duration > 0 ? $"[{e.Duration}턴]" : "";
    string fullChance = e.Chance > 0 ? $"({e.Chance:0.##}%)" : "";   // 100%도 표기 (해제/턴감용)
    var cl = new List<string>();
    if (e.Type == SkillEffectType.StatusAilment)
    {
        string nm = StatusEffectDb.Effects.TryGetValue(e.StatusType, out var sd) && !string.IsNullOrEmpty(sd.Name)
            ? sd.Name : e.StatusType.ToString();
        cl.Add(nm + chance + dur);
    }
    if (e.Type == SkillEffectType.Debuff && e.Debuff != null)
    {
        var dp = DebuffParts(e.Debuff);
        if (dp.Count > 0) cl.Add(string.Join(", ", dp) + chance + dur);
    }
    if (e.Type == SkillEffectType.Buff && e.Buff != null)
    {
        var bp = NonZeroBuff(e.Buff).Select(kv => BuffClause(kv.Key, kv.Value)).ToList();
        if (bp.Count > 0) cl.Add(string.Join(", ", bp) + chance + dur);
    }
    if (e.Type == SkillEffectType.PerEnemyDebuffDmgBonus && e.PercentPerDebuff > 0)
        cl.Add($"상대 디버프 1개당 피해증가 {e.PercentPerDebuff:0.##}%" + (e.MaxDebuffStacks > 0 ? $"(최대 {e.MaxDebuffStacks}개)" : ""));
    if (e.Type == SkillEffectType.Revive && e.ReviveHpPercent > 0)
        cl.Add($"사망 아군 {(e.TargetCount > 0 ? e.TargetCount : 1)}명 생명력 {e.ReviveHpPercent:0.##}% 부활");
    if (e.TurnReduction > 0) cl.Add($"턴제감소{fullChance} {e.TurnReduction}턴");
    if (e.DispelBuffCount > 0) cl.Add($"버프해제{fullChance} {e.DispelBuffCount}개");
    if (e.DispelDebuffCount > 0)
        cl.Add(e.Type == SkillEffectType.DebuffCleanse ? $"아군 디버프 해제 {e.DispelDebuffCount}개" : $"디버프해제{fullChance} {e.DispelDebuffCount}개");
    if (e.DamageNullification != null)
    {
        var dn = e.DamageNullification;
        string t = dn.Type switch { DamageNullType.Physical => "물리 ", DamageNullType.Magic => "마법 ", _ => "모든 " };
        string dd = dn.HitCount > 0 ? $"[피격 {dn.HitCount}회]" : (dn.Duration > 0 ? $"[{dn.Duration}턴]" : "");
        cl.Add($"{t}피해 무효화{dd}");
    }
    if (e.StatusImmunity != null)
    {
        var types = e.StatusImmunity.Types ?? System.Array.Empty<StatusEffectType>();
        string nm = types.Length == 1 && StatusEffectDb.Effects.TryGetValue(types[0], out var si) && !string.IsNullOrEmpty(si.Name)
            ? si.Name + " 면역" : "상태이상 면역";
        cl.Add(nm + (e.StatusImmunity.Duration > 0 ? $"[{e.StatusImmunity.Duration}턴]" : ""));
    }
    return cl.Count > 0 ? string.Join(", ", cl) : null;
}
string SynthTranscendEffect(SkillTranscend tr)
{
    if (tr == null) return null;
    if (!string.IsNullOrWhiteSpace(tr.Effect)) return tr.Effect;   // 명시 텍스트 우선
    var parts = new List<string>();
    foreach (var kv in NonZeroBuff(tr.Bonus))
        parts.Add($"{(buffLabels.TryGetValue(kv.Key, out var l) ? l : kv.Key)} {kv.Value:0.##}%");
    if (tr.TargetCountOverride is int tc) parts.Add($"대상 {tc}");
    if (tr.AtkCountOverride is int ac) parts.Add($"타수 {ac}");
    if (tr.HealAtkRatio > 0) parts.Add($"공{tr.HealAtkRatio:0.##}% 회복");
    if (tr.OnKillRecast != null)
        parts.Add(tr.OnKillRecast.Chance >= 100
            ? $"처치 시 재시전({tr.OnKillRecast.RatioPercent:0.##}%)"
            : $"처치 시 재시전({tr.OnKillRecast.RatioPercent:0.##}%, {tr.OnKillRecast.Chance:0.##}%확률)");
    if (tr.Effects != null)
        foreach (var e in tr.Effects) { var s = DescribeSkillEffect(e); if (s != null) parts.Add(s); }
    return parts.Count > 0 ? string.Join(", ", parts) : null;
}

// 패시브 PersistentEffect 1개 → (대상, 설명 텍스트). 대상별 그룹핑용.
(string target, string text)? DescribePersistentEffect(PersistentEffect e)
{
    if (e == null) return null;
    string target = e.Target switch
    {
        EffectTarget.Self => "본인",
        EffectTarget.Enemy or EffectTarget.AllEnemies => "적군",
        _ => "아군",   // Party, SelfAndHighestAtkAlly 등
    };
    // 트리거 조건 프리픽스 (값만 출력하면 오해 → "부활 시"·"스킬 사용 시" 등 명시).
    //   Immunity/TriggeredFixedDamage는 자체 문구가 있으므로 프리픽스 생략(아래 케이스에서 cond="" 처리).
    string trig = "";
    if (e.TriggerHpThreshold > 0)
        trig = $"생명력 {e.TriggerHpThreshold:0.##}% 이하 시";
    else if (e.ApplyMode == ApplyMode.Triggered)
        trig = e.TriggerCondition switch
        {
            TriggerCondition.SelfDeath => "사망 시",
            TriggerCondition.OnRevival => "부활 시",
            TriggerCondition.AllyDeath => "아군 사망 시",
            TriggerCondition.EnemyDeath => "적 처치 시",
            TriggerCondition.SkillOnly => "스킬 사용 시",
            TriggerCondition.NormalOnly => "기본공격 시",
            TriggerCondition.AllAttack => "공격 시",
            TriggerCondition.OnHit => "피격 시",
            TriggerCondition.OnTurnStart => "턴 시작 시",
            _ => "",
        };
    string cond = trig != "" ? trig + " " : "";
    if (!string.IsNullOrWhiteSpace(e.Condition))
        cond += cond != "" ? $"({e.Condition.TrimEnd()}) " : e.Condition.TrimEnd() + " ";

    var parts = new List<string>();
    switch (e.Type)
    {
        case PersistentEffectType.Buff when e.Buff != null:
            parts.AddRange(NonZeroBuff(e.Buff).Select(kv => BuffClause(kv.Key, kv.Value)));
            break;
        case PersistentEffectType.Debuff when e.Debuff != null:
            parts.AddRange(DebuffParts(e.Debuff));
            break;
        case PersistentEffectType.StatusAilment:
            parts.Add(StatusEffectDb.Effects.TryGetValue(e.StatusType, out var sd) && !string.IsNullOrEmpty(sd.Name) ? sd.Name : e.StatusType.ToString());
            break;
        case PersistentEffectType.Immunity when e.StatusImmunity != null:
        {
            var types = e.StatusImmunity.Types ?? System.Array.Empty<StatusEffectType>();
            string nm = types.Length == 1 && StatusEffectDb.Effects.TryGetValue(types[0], out var s1) && !string.IsNullOrEmpty(s1.Name)
                ? s1.Name + " 면역" : "상태이상 면역";
            string idur = e.StatusImmunity.Duration > 0 ? $"[{e.StatusImmunity.Duration}턴]" : "";
            string retrigger = e.ApplyMode == ApplyMode.Triggered && e.TriggerCondition == TriggerCondition.NormalOnly ? ", 평타 시 재발동" : "";
            parts.Add(nm + idur + retrigger);
            cond = "";   // 면역은 "평타 시 재발동" 자체 문구 → 트리거 프리픽스 생략
            break;
        }
        case PersistentEffectType.TriggeredHeal:
            if (e.TriggeredHealAtkRatio > 0) parts.Add($"공격력 {e.TriggeredHealAtkRatio:0.##}% 회복");
            if (e.TriggeredHealHpRatio > 0) parts.Add($"최대 생명력 {e.TriggeredHealHpRatio:0.##}% 회복");
            if (e.TriggeredHealDefRatio > 0) parts.Add($"방어력 {e.TriggeredHealDefRatio:0.##}% 회복");
            break;
        case PersistentEffectType.Revival: parts.Add("사망 시 부활"); cond = ""; break;
        case PersistentEffectType.DamageNullification:
        {
            var dn = e.DamageNullification;
            string t = dn?.Type switch { DamageNullType.Physical => "물리 ", DamageNullType.Magic => "마법 ", _ => "모든 " };
            string detail = "";
            if (dn != null)
            {
                if (dn.HitCount > 0) detail += $"[피격 {dn.HitCount}회]";
                if (dn.Duration > 0) detail += $"[{dn.Duration}턴]";
            }
            parts.Add($"{t}피해 무효화{detail}" + (e.OncePerBattle ? "(전투당 1회)" : ""));
            break;
        }
        case PersistentEffectType.FocusTarget: parts.Add("강자주시(공격력 최고 적군 고정 타게팅)"); break;
        case PersistentEffectType.TriggeredFixedDamage when e.TriggeredFixedDamage != null:
        {
            var f = e.TriggeredFixedDamage;
            string trigWord = f.TriggerOn == TriggerCondition.NormalOnly ? "기본공격" : "공격";
            string dmg = f.AtkRatio > 0 ? $"물공 {f.AtkRatio:0.##}%" : $"고정 {f.FixedDamage:0.##}";
            string s = $"{trigWord} {f.TriggerCount}회마다 {dmg} 추가공격";
            if (f.DispelBuffCount > 0) s += $", 추가공격 시 버프해제 {f.DispelBuffCount}개({f.DispelBuffChance:0.##}%)";
            parts.Add(s);
            cond = "";   // 자체 "N회마다" 문구 → 트리거 프리픽스 생략
            break;
        }
        case PersistentEffectType.Lifesteal: parts.Add("흡혈(준 피해량 비례 회복)"); break;
        case PersistentEffectType.Authority: parts.Add("권능(치명타 생존 1회)"); break;
        case PersistentEffectType.CoopAttack when e.CoopAttack != null:
        {
            var co = e.CoopAttack;
            string dmg = co.Ratio > 0 ? $"공격력 {co.Ratio:0.##}%" : "";
            if (co.TargetMaxHpRatio > 0) dmg += (dmg != "" ? " + " : "") + $"최대 생명력 {co.TargetMaxHpRatio:0.##}%";
            parts.Add($"협공({co.TriggerChance:0.##}%) {dmg}".TrimEnd());
            break;
        }
        case PersistentEffectType.StatScaling when e.StatScaling != null:
        {
            var sc = e.StatScaling;
            string max = sc.MaxValue > 0 ? $"(최대 {sc.MaxValue:0.##})" : "";
            parts.Add($"{StatName(sc.SourceStat)} 비례 {StatName(sc.TargetStat)} 증가{max}");
            break;
        }
        case PersistentEffectType.TriggeredSkillCast when e.TriggeredSkillCast != null:
        {
            var ts = e.TriggeredSkillCast;
            string when = ts.TriggerOn == TriggerCondition.EnemyDeath ? $"적 {ts.TriggerCount}명 사망 시" : $"{ts.TriggerCount}회마다";
            parts.Add($"{when} 스킬 발동(공격력 {ts.Ratio:0.##}%)");
            break;
        }
        case PersistentEffectType.MarkAttack when e.MarkAttack != null:
        {
            var mk = e.MarkAttack;
            string dmg = mk.Ratio > 0 ? $"공격력 {mk.Ratio:0.##}%" : "";
            if (mk.TargetMaxHpRatio > 0) dmg += (dmg != "" ? " + " : "") + $"최대 생명력 {mk.TargetMaxHpRatio:0.##}%";
            parts.Add($"표식 {mk.MaxStacks}중첩 시 {dmg}".TrimEnd());
            break;
        }
        case PersistentEffectType.CooldownReset when e.CooldownReset != null:
            parts.Add(e.CooldownReset.ReduceSeconds > 0 ? $"아군 쿨 {e.CooldownReset.ReduceSeconds:0.##}초 감소" : "쿨타임 초기화");
            break;
        case PersistentEffectType.PainEndurance: parts.Add("고통 인내(받은 피해 분산)"); break;
        case PersistentEffectType.PerEnemyDebuffDmgBonus:
            parts.Add($"상대 디버프 1개당 피해증가 {e.PercentPerDebuff:0.##}%" + (e.MaxDebuffStacks > 0 ? $"(최대 {e.MaxDebuffStacks}개)" : ""));
            break;
        case PersistentEffectType.BuffDispel when e.DispelBuffCount > 0: parts.Add($"버프해제 {e.DispelBuffCount}개"); break;
        case PersistentEffectType.BuffTurnReduction when e.TurnReduction > 0: parts.Add($"버프 {e.TurnReduction}턴 감소"); break;
        case PersistentEffectType.DebuffCleanse when e.DispelDebuffCount > 0: parts.Add($"디버프 해제 {e.DispelDebuffCount}개"); break;
        default:
            if (!string.IsNullOrWhiteSpace(e.Condition)) { parts.Add(e.Condition.Trim()); cond = ""; }
            break;
    }
    if (parts.Count == 0) return null;
    string suffix = (e.Type == PersistentEffectType.Buff || e.Type == PersistentEffectType.Debuff)
        ? (e.Duration > 0 ? $"[{e.Duration}턴]" : "[상시]")
        : (e.Duration > 0 ? $"[{e.Duration}턴]" : "");
    return (target, cond + string.Join(", ", parts) + suffix);
}
// 패시브의 스강+초월 적용 상태 Effects → 대상별 그룹 [{target, items[]}]. 가시성용 구조화.
//   초월 Effects는 같은 정체성(대상·타입·상태이상)의 enhanced 효과를 덮어쓴다(상위 버전).
object PassiveEffectGroups(Passive p)
{
    if (p == null) return null;
    var effs = new List<PersistentEffect>(p.GetLevelData(true)?.Effects ?? new List<PersistentEffect>());
    if (p.TranscendBonuses != null)
        foreach (var kv in p.TranscendBonuses.Where(t => t.Key <= 12).OrderBy(t => t.Key))
            if (kv.Value.Effects != null)
                foreach (var te in kv.Value.Effects)
                {
                    int idx = effs.FindIndex(x => x.Target == te.Target && x.Type == te.Type && x.StatusType == te.StatusType);
                    if (idx >= 0) effs[idx] = te;   // 초월이 enhanced 덮어씀
                    else effs.Add(te);
                }
    if (effs.Count == 0) return null;
    var byTarget = new Dictionary<string, List<string>> { ["아군"] = new(), ["본인"] = new(), ["적군"] = new() };
    foreach (var e in effs)
    {
        var r = DescribePersistentEffect(e);
        if (r is { } v && !string.IsNullOrWhiteSpace(v.text)) byTarget[v.target].Add(v.text);
    }
    var groups = byTarget.Where(kv => kv.Value.Count > 0).Select(kv => new { target = kv.Key, items = kv.Value }).ToList();
    return groups.Count > 0 ? groups : null;
}
// 패시브에 구조화 초월 Effects가 있는지 (있으면 그룹에 합쳐지므로 별도 텍스트 주석 불필요).
bool PassiveHasTranscendEffects(Passive p) =>
    p?.TranscendBonuses != null && p.TranscendBonuses.Any(t => t.Key <= 12 && t.Value.Effects != null && t.Value.Effects.Count > 0);

// 한 레벨의 구조화 효과/보너스 라인 모음 (스강 델타 비교용).
List<string> SkillEffectLines(SkillLevelData l)
{
    var r = new List<string>();
    if (l?.Effects != null) foreach (var e in l.Effects) { var s = DescribeSkillEffect(e); if (s != null) r.Add(s); }
    if (l?.Bonus != null) foreach (var kv in NonZeroBuff(l.Bonus)) r.Add(BuffClause(kv.Key, kv.Value));
    return r;
}
// 숫자/괄호/공백 제거 → "종류" 식별자 (값 변화는 같은 종류로 보고 무시, 신규 효과만 델타로 잡기 위함).
string KindKey(string s) => new string(s.Where(ch =>
    !char.IsDigit(ch) && ch != '%' && ch != '[' && ch != ']' && ch != '(' && ch != ')' && ch != ' ' && ch != '.' && ch != ',').ToArray());
// 스강이 추가하는 효과(=base에 없던 종류만). 단순 배율/수치 증가는 제외.
string SkillEnhanceAdds(Skill s)
{
    var baseKinds = SkillEffectLines(s.GetLevelData(false)).Select(KindKey).ToHashSet();
    var adds = SkillEffectLines(s.GetLevelData(true)).Where(x => !baseKinds.Contains(KindKey(x))).ToList();
    return adds.Count > 0 ? string.Join(", ", adds) : null;
}

// 스킬 티어 객체 빌더 — 배율 외 데미지 변수(HP비례·잃은HP·제한·조건부·회복)를 그대로 노출.
object SkillTierObj(SkillLevelData l, double cooldown, int atkCount, int targetCount, string effect) => new
{
    cooldown,
    target = targetCount,
    atk = atkCount,
    ratio = l.Ratio,
    hpRatio = l.HpRatio,                       // 자가 생명력 비례%
    defRatio = l.DefRatio,                      // 방어력 비례%
    spdRatio = l.SpdRatio,                      // 속공 비례%
    targetMaxHpRatio = l.TargetMaxHpRatio,      // 대상 최대 생명력 비례%
    targetCurrentHpRatio = l.TargetCurrentHpRatio, // 대상 현재 생명력 비례%
    atkCap = l.AtkCap,                          // 공격력 제한%
    lostHpBonusMax = l.LostHpBonusDmgMax,       // 잃은 생명력 비례 최대 피해증가%
    currentHpBonusMax = l.CurrentHpBonusDmgMax, // 현재 생명력 비례 최대 피증%
    condRatioBonus = l.ConditionalRatioBonus,
    condExtraDmg = l.ConditionalExtraDmg,
    condDmgBonus = l.ConditionalDmgBonus,
    condExtraDmgSelfHp = l.ConditionalExtraDmgSelfHpRatio,
    healAtkRatio = l.HealAtkRatio,              // 공격력 비례 회복%
    healHpRatio = l.HealHpRatio,                // 생명력 비례 회복%
    healDmgRatio = l.HealDmgRatio,              // 가한 피해량 비례 회복%
    healDefRatio = l.HealDefRatio,              // 방어력 비례 회복%
    fixedDamage = l.FixedDamage,                // 고정 피해값
    penetrate = l.IgnoresTurnDamageImmunity,    // 관통(대상 피해 면역 무시)
    // 스킬 자체 보너스 (치피추가·약점추가피해·방어무시 등). 이 스킬 계산에만 적용. 비0 필드만.
    bonus = NonZeroBuff(l.Bonus),
    // 구조화 효과 라인 (상태이상·디버프·버프·버프해제·턴제감소 등). l.Effects 없으면 빈 배열.
    effects = (l.Effects ?? new List<SkillEffect>()).Select(DescribeSkillEffect).Where(x => x != null).ToList(),
    effect,
};

// 주 사용처(웹에서 편집·저장) 오버레이 로드 — web/src/data/heroUsage.json { "<id>": ["공성전", ...] }.
//   시뮬 계산과 무관한 표시용 메타라 C# DB에 하드코딩하지 않고 이 파일을 단일 저장소로 둔다.
//   (웹 영웅 페이지 편집 UI → API가 이 파일을 갱신 → export가 characters.json에 병합)
var usageByHeroId = new Dictionary<int, List<string>>();
{
    string usagePath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "web", "src", "data", "heroUsage.json"));
    if (File.Exists(usagePath))
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(usagePath));
            foreach (var prop in doc.RootElement.EnumerateObject())
                if (int.TryParse(prop.Name, out var hid) && prop.Value.ValueKind == JsonValueKind.Array)
                    usageByHeroId[hid] = prop.Value.EnumerateArray()
                        .Where(v => v.ValueKind == JsonValueKind.String)
                        .Select(v => v.GetString()!).ToList();
        }
        catch (Exception ex) { Console.WriteLine($"[주사용처] 로드 실패 — 빈 값으로 진행: {ex.Message}"); }
    }
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
        if (d.Atk_Reduction > 0) tags.Add("물공감");
        if (d.MagicAtk_Reduction > 0) tags.Add("마공감");
        if (d.Cri_Reduction > 0) tags.Add("치확감소");
        if (d.Wek_Reduction > 0) tags.Add("약공감소");
        if (d.Cooldown_Increase > 0) tags.Add("쿨증");
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
        },
        // 대상별 그룹 효과 (아군/본인/적군) — 가시성용 구조화. 스강 상태.
        effectGroups = PassiveEffectGroups(c.Passive),
        // 티어별 패시브 효과 텍스트 (수치 버프 외 면역·특수효과 포함). 스킬 effect와 동일.
        effectTiers = new
        {
            @base = c.Passive.GetLevelData(false)?.Effect,
            enhanced = c.Passive.GetLevelData(true)?.Effect,
            // 구조화 초월 Effects가 그룹에 합쳐졌으면 중복이라 null. 텍스트-only 초월만 주석으로 노출.
            transcend = PassiveHasTranscendEffects(c.Passive) ? null : c.Passive.GetTranscendBonus(12)?.Effect,
        }
    },
    skills = c.Skills.Select(s =>
    {
        var l0 = s.GetLevelData(false);
        var l1 = s.GetLevelData(true);
        var tr = s.GetTranscendBonus(12);
        // GetTranscendBonus가 OnKillRecast/HealAtkRatio는 병합하지 않으므로 원본에서 보강(표시용).
        if (s.TranscendBonuses != null)
            foreach (var kv in s.TranscendBonuses.Where(t => t.Key <= 12).OrderBy(t => t.Key))
            {
                if (kv.Value.OnKillRecast != null) tr.OnKillRecast = kv.Value.OnKillRecast;
                if (kv.Value.HealAtkRatio > 0) tr.HealAtkRatio = kv.Value.HealAtkRatio;
            }
        // target은 선언된 레벨값을 그대로 노출 (0 = 대상 없는 유틸 스킬 → 웹에서 빈칸 표시)
        return new
        {
            id = s.Id,
            name = s.Name,
            skillType = s.SkillType.ToString(),
            // 스강이 추가하는 효과(주석용, base에 없던 종류만), 초월 추가는 tiers.transcend.effect.
            enhanceAdds = SkillEnhanceAdds(s),
            tiers = new
            {
                @base = SkillTierObj(l0, s.GetCooldown(false, 0), s.GetAtkCount(false, 0), l0.TargetCount, l0.Effect),
                enhanced = SkillTierObj(l1, s.GetCooldown(true, 0), s.GetAtkCount(true, 0), l1.TargetCount, l1.Effect),
                transcend = SkillTierObj(l1, s.GetCooldown(true, 12), s.GetAtkCount(true, 12), tr.TargetCountOverride ?? l1.TargetCount, SynthTranscendEffect(tr)),
            },
        };
    }).ToList(),
        tags = tags.ToList(),
        // 주 사용처 — heroUsage.json 오버레이(웹 편집 결과). 미지정이면 빈 배열.
        mainUsages = usageByHeroId.TryGetValue(c.Id, out var mu) ? mu : new List<string>(),
    };
}).ToList();

var options = new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

// 진단: 패시브 Effects 중 합성기가 비어 반환하는(=누락) 효과 타입을 수집해 출력. "diag" 인자 시.
if (args.Contains("diag"))
{
    var missing = new Dictionary<string, List<string>>();
    foreach (var c in CharacterDb.Characters)
    {
        var p = c.Passive;
        if (p == null) continue;
        var effLists = new List<List<PersistentEffect>>
        {
            p.GetLevelData(false)?.Effects, p.GetLevelData(true)?.Effects,
        };
        if (p.TranscendBonuses != null)   // 초월 Effects도 스캔 (세인 PerEnemyDebuffDmgBonus 등 누락 방지)
            foreach (var kv in p.TranscendBonuses.Where(t => t.Key <= 12))
                effLists.Add(kv.Value.Effects);
        foreach (var lvl in effLists)
        {
            if (lvl == null) continue;
            foreach (var e in lvl)
            {
                var r = DescribePersistentEffect(e);
                if (r == null || string.IsNullOrWhiteSpace(r.Value.text))
                {
                    string key = e.Type.ToString();
                    if (!missing.TryGetValue(key, out var lst)) missing[key] = lst = new List<string>();
                    if (!lst.Contains(c.Name)) lst.Add(c.Name);
                }
            }
        }
    }
    Console.WriteLine("=== 패시브 누락 효과 타입 (합성 안 됨) ===");
    foreach (var kv in missing.OrderByDescending(x => x.Value.Count))
        Console.WriteLine($"  {kv.Key} ({kv.Value.Count}명): {string.Join(", ", kv.Value.Take(8))}{(kv.Value.Count > 8 ? " …" : "")}");
    if (missing.Count == 0) Console.WriteLine("  (없음)");
    Console.WriteLine();
}

string json = JsonSerializer.Serialize(heroes, options);

// tools/SenaDataExport/bin/... 에서 실행되므로 리포 루트 기준 경로 계산
string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string outDir = Path.Combine(repoRoot, "web", "src", "data");
Directory.CreateDirectory(outDir);
string outPath = Path.Combine(outDir, "characters.json");
File.WriteAllText(outPath, json);

Console.WriteLine($"Exported {heroes.Count} heroes → {outPath}");
