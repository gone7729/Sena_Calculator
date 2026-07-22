using System.Globalization;
using System.Reflection;
using System.Text;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;

// ============================================================================
// 효과 해석 전수 덤프 — 리팩터 무회귀 검증용.
//   모든 영웅 × (스킬 × 티어 | 패시브 × 티어)에 대해 "시뮬레이터가 실제로 쓰는 접근자"를 호출해
//   해석 결과를 결정적(정렬된) 텍스트로 출력한다. 리팩터 전후 diff가 비면 동작 불변.
//   ※ 필드를 리플렉션으로 훑으므로 BuffSet/DebuffSet에 필드가 늘어도 자동 반영된다.
// ============================================================================

string outPath = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "effect-dump.txt");

var sb = new StringBuilder();

// 객체의 비-기본값 프로퍼티만 "이름=값" 정렬 나열. 0/null/false는 생략해 노이즈 제거.
static string Props(object o)
{
    if (o == null) return "(null)";
    var parts = new List<string>();
    foreach (var p in o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
    {
        if (p.GetIndexParameters().Length > 0) continue;
        object v;
        try { v = p.GetValue(o); } catch { continue; }
        if (v == null) continue;
        switch (v)
        {
            case double d when d == 0: continue;
            case int i when i == 0: continue;
            case bool b when !b: continue;
            case string s when string.IsNullOrEmpty(s): continue;
            case System.Collections.ICollection c when c.Count == 0: continue;
        }
        // 중첩 객체(BuffSet 등)는 재귀로 펼침 — 참조 주소가 아니라 값으로 비교해야 의미가 있다
        string text = v switch
        {
            double d => d.ToString("0.####", CultureInfo.InvariantCulture),
            string or bool or int or Enum => v.ToString(),
            System.Collections.IEnumerable en when v is not string =>
                "[" + string.Join(" | ", en.Cast<object>().Select(Props)) + "]",
            _ => "{" + Props(v) + "}",
        };
        parts.Add($"{p.Name}={text}");
    }
    parts.Sort(StringComparer.Ordinal);
    return string.Join(", ", parts);
}

static string Line(string label, object o)
{
    string body = Props(o);
    return string.IsNullOrEmpty(body) || body == "(null)" ? null : $"      {label}: {body}";
}

foreach (var c in CharacterDb.Characters.OrderBy(x => x.Id))
{
    sb.AppendLine($"═══ [{c.Id}] {c.Name} ({c.Grade}/{c.Type}, {c.AttackType}) ═══");

    // ── 스킬 ──
    foreach (var s in c.Skills.OrderBy(x => x.Id))
    {
        sb.AppendLine($"  SKILL {s.Id} {s.Name} [{s.SkillType}]");
        foreach (var (enh, tx, tier) in new[] { (false, 0, "base"), (true, 0, "enh"), (true, 12, "tr12") })
        {
            var ld = s.GetLevelData(enh);
            sb.AppendLine($"    {tier}: target={s.GetTargetCount(enh, tx)} atk={s.GetAtkCount(enh, tx)} " +
                          $"cd={s.GetCooldown(enh, tx).ToString("0.##", CultureInfo.InvariantCulture)}");
            // 시뮬이 읽는 경로 그대로 — StatusEffects는 (리팩터 대상인) 변환 getter를 통과한다
            foreach (var line in new[]
            {
                Line("levelData", ld),
                Line("statusEffects", ld?.StatusEffects),
                Line("effectiveEffects", s.GetEffectiveEffects(enh, tx)),
                Line("totalBonus", s.GetTotalBonus(enh, tx)),
                Line("consumeExtra", s.GetTotalConsumeExtra(enh, tx)),
                Line("transcend", s.GetTranscendBonus(tx)),
            })
                if (line != null) sb.AppendLine(line);
        }
    }

    // ── 패시브 ──
    if (c.Passive != null)
    {
        var p = c.Passive;
        sb.AppendLine($"  PASSIVE {p.Name}");
        foreach (var (enh, tx, tier) in new[] { (false, 0, "base"), (true, 0, "enh"), (true, 12, "tr12") })
        {
            sb.AppendLine($"    {tier}: maxStacks={p.GetMaxStacks(enh, tx)}");
            foreach (var line in new[]
            {
                Line("levelData", p.GetLevelData(enh)),
                Line("totalSelfBuff", p.GetTotalSelfBuff(enh, tx)),
                Line("condSelfBuff", p.GetConditionalSelfBuff(enh, tx)),
                Line("partyBuff", p.GetPartyBuff(enh, tx)),
                Line("condPartyBuff", p.GetConditionalPartyBuff(enh, tx)),
                Line("debuff", p.GetDebuff(enh, tx)),
                Line("condDebuff", p.GetConditionalDebuff(enh, tx)),
                Line("transcend", p.GetTranscendBonus(tx)),
            })
                if (line != null) sb.AppendLine(line);
            // 직업 한정 파티버프(수혜 클래스별로 값이 갈리는 케이스) — 클래스별로 따로 찍어 누락 방지
            foreach (var cls in new[] { "공격형", "마법형", "만능형", "방어형", "지원형" })
            {
                var pb = Line($"partyBuff[{cls}]", p.GetPartyBuff(enh, tx, cls));
                if (pb != null) sb.AppendLine(pb);
            }
        }
    }
}

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
Console.WriteLine($"덤프 완료 → {outPath} ({sb.Length:N0} chars, {CharacterDb.Characters.Count} heroes)");
