#!/usr/bin/env python3
"""
Convert remaining legacy fields in CharacterDB.cs to new Effects system.
Approach: reconstruct each PassiveLevelData/SkillLevelData/Transcend block cleanly.
Skip already-converted characters: 타카(Id=1), 비담(Id=5), 카구라(Id=6), 여포(Id=13).
"""
import re

filepath = r"c:\Users\rhkde\Desktop\sena\Sena_Calculator\DB\CharacterDB.cs"

with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

# ============================================================
# HELPER
# ============================================================
def find_matching_brace(text, start):
    depth = 0
    i = start
    while i < len(text):
        if text[i] == '{':
            depth += 1
        elif text[i] == '}':
            depth -= 1
            if depth == 0:
                return i
        i += 1
    return -1


def convert_sse_to_se(sse_text):
    """Convert 'new SkillStatusEffect { props }' text to SkillEffect text"""
    m = re.search(r'new SkillStatusEffect\s*\{([^}]*)\}', sse_text)
    if not m:
        return None
    props_str = m.group(1).strip()

    parts = ['Target = EffectTarget.Enemy', 'Type = SkillEffectType.StatusAilment']
    prop_map = {
        'Type': 'StatusType', 'Stacks': 'Stacks', 'Chance': 'Chance',
        'Duration': 'Duration', 'CustomAtkRatio': 'CustomAtkRatio',
        'CustomHpRatio': 'CustomHpRatio', 'CustomAtkCap': 'CustomAtkCap',
        'CustomArmorPen': 'CustomArmorPen', 'CustomFixedDamage': 'CustomFixedDamage',
        'CustomTargetMaxHpRatio': 'CustomTargetMaxHpRatio',
        'CustomTargetCurrentHpRatio': 'CustomTargetCurrentHpRatio',
        'CustomHpConversionRatio': 'CustomHpConversionRatio',
        'CustomTriggerCount': 'CustomTriggerCount', 'MaxConsume': 'MaxConsume',
    }
    for pm in re.finditer(r'(\w+)\s*=\s*([^,}\s]+(?:\.\w+)?)', props_str):
        key, val = pm.group(1).strip(), pm.group(2).strip()
        if key in prop_map:
            parts.append(f'{prop_map[key]} = {val}')
    return 'new SkillEffect { ' + ', '.join(parts) + ' }'


# ============================================================
# PASS 1: SkillTranscend conversions
# ============================================================

# 1a. Debuff = new TimedDebuff in SkillTranscend
def st_debuff_replace(m):
    pre, inner, post = m.group(1), m.group(2).strip().rstrip(','), m.group(3)
    if 'Effects = new List<SkillEffect>' in pre:
        return m.group(0)
    return f'{pre}Effects = new List<SkillEffect> {{\n                new SkillEffect {{ Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet {{ {inner} }} }}\n            }}{post}'

content = re.sub(
    r'(new SkillTranscend\s*\{[^}]*?)Debuff = new TimedDebuff\s*\{\s*([^}]+?)\s*\}([^}]*?\})',
    st_debuff_replace, content
)

# 1b. PartyBuff = new TimedBuff in SkillTranscend
def st_partybuff_replace(m):
    pre, inner, post = m.group(1), m.group(2).strip().rstrip(','), m.group(3)
    if 'Effects = new List<SkillEffect>' in pre:
        return m.group(0)
    return f'{pre}Effects = new List<SkillEffect> {{\n                new SkillEffect {{ Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet {{ {inner} }} }}\n            }}{post}'

content = re.sub(
    r'(new SkillTranscend\s*\{[^}]*?)PartyBuff = new TimedBuff\s*\{\s*([^}]+?)\s*\}([^}]*?\})',
    st_partybuff_replace, content
)

# 1c. StatusEffects in SkillTranscend
# Find each SkillTranscend block that has StatusEffects but no Effects
pos = 0
while True:
    m = re.search(r'new SkillTranscend\s*\{', content[pos:])
    if not m:
        break
    block_start = pos + m.start()
    brace_start = content.index('{', block_start + len('new SkillTranscend'))
    brace_end = find_matching_brace(content, brace_start)
    if brace_end < 0:
        pos = block_start + 1
        continue

    block = content[brace_start:brace_end+1]
    if 'StatusEffects = new List<SkillStatusEffect>' in block and 'Effects = new List<SkillEffect>' not in block:
        # Extract SSE items
        items = []
        for sse_m in re.finditer(r'new SkillStatusEffect\s*\{[^}]*\}', block):
            se = convert_sse_to_se(sse_m.group(0))
            if se:
                items.append(se)
        if items:
            effects_str = ',\n                    '.join(items)
            new_block = re.sub(
                r'StatusEffects = new List<SkillStatusEffect>\s*\{.*?\}(?:\s*\})?',
                f'Effects = new List<SkillEffect>\n                {{\n                    {effects_str}\n                }}',
                block, flags=re.DOTALL
            )
            content = content[:brace_start] + new_block + content[brace_end+1:]

    pos = block_start + 1

# ============================================================
# PASS 2: SkillLevelData StatusEffects + DebuffEffect + PartyBuff/SelfBuff
# ============================================================

# Find each SkillLevelData block
pos = 0
while True:
    m = re.search(r'new SkillLevelData\s*\{', content[pos:])
    if not m:
        break
    block_start = pos + m.start()
    brace_start = content.index('{', block_start + len('new SkillLevelData'))
    brace_end = find_matching_brace(content, brace_start)
    if brace_end < 0:
        pos = block_start + 1
        continue

    block = content[brace_start:brace_end+1]

    # Skip if already has Effects
    if 'Effects = new List<SkillEffect>' in block:
        pos = brace_end + 1
        continue

    skill_effects = []
    fields_to_remove = []

    # StatusEffects
    if 'StatusEffects = new List<SkillStatusEffect>' in block:
        se_start = block.index('StatusEffects = new List<SkillStatusEffect>')
        # Find the list block
        list_brace_start = block.index('{', se_start + len('StatusEffects = new List<SkillStatusEffect>'))
        list_brace_end = find_matching_brace(block, list_brace_start)
        if list_brace_end > 0:
            se_block = block[se_start:list_brace_end+1]
            for sse_m in re.finditer(r'new SkillStatusEffect\s*\{[^}]*\}', se_block):
                se = convert_sse_to_se(sse_m.group(0))
                if se:
                    skill_effects.append(se)
            fields_to_remove.append((se_start, list_brace_end+1))

    # DebuffEffect
    debuff_m = re.search(r'DebuffEffect = new TimedDebuff\s*\{\s*([^}]+?)\s*\}', block)
    if debuff_m:
        inner = debuff_m.group(1).strip().rstrip(',')
        skill_effects.append(f'new SkillEffect {{ Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet {{ {inner} }} }}')
        fields_to_remove.append((debuff_m.start(), debuff_m.end()))

    # SelfBuff = new TimedBuff (not PermanentBuff)
    selfbuff_m = re.search(r'(?<!Conditional)SelfBuff = new TimedBuff\s*\{\s*([^}]+?)\s*\}', block)
    if selfbuff_m:
        inner = selfbuff_m.group(1).strip().rstrip(',')
        skill_effects.append(f'new SkillEffect {{ Target = EffectTarget.Self, Type = SkillEffectType.Buff, Buff = new BuffSet {{ {inner} }} }}')
        fields_to_remove.append((selfbuff_m.start(), selfbuff_m.end()))

    # PartyBuff = new TimedBuff (not PermanentBuff)
    partybuff_m = re.search(r'(?<!Conditional)PartyBuff = new TimedBuff\s*\{\s*([^}]+?)\s*\}', block)
    if partybuff_m:
        inner = partybuff_m.group(1).strip().rstrip(',')
        skill_effects.append(f'new SkillEffect {{ Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet {{ {inner} }} }}')
        fields_to_remove.append((partybuff_m.start(), partybuff_m.end()))

    if not skill_effects:
        pos = brace_end + 1
        continue

    # Remove old fields from block (reverse order to preserve indices)
    new_block = block
    fields_to_remove.sort(key=lambda x: x[0], reverse=True)
    for rs, re_ in fields_to_remove:
        # Remove field and surrounding comma/whitespace
        before = new_block[:rs]
        after = new_block[re_:]

        # Strip trailing comma and whitespace from what we're removing
        after = after.lstrip()
        if after.startswith(','):
            after = after[1:].lstrip()
            if after.startswith('\n'):
                after = after[1:]

        # Strip leading comma from before if needed
        before = before.rstrip()
        if before.endswith(','):
            before = before[:-1].rstrip()

        # Reconstruct
        if after and after[0] not in ('}', '\n'):
            new_block = before + ',\n                                ' + after
        else:
            new_block = before + '\n                                ' + after

    # Clean up: remove empty lines and fix indentation
    # Remove multiple consecutive blank lines
    new_block = re.sub(r'\n\s*\n\s*\n', '\n', new_block)

    # Add Effects list
    effects_str = ',\n                                    '.join(skill_effects)
    effects_field = f'Effects = new List<SkillEffect>\n                                {{\n                                    {effects_str}\n                                }}'

    # Insert before the closing brace
    last_brace = new_block.rfind('}')
    before_close = new_block[:last_brace].rstrip()
    # Ensure we have a comma before Effects
    if before_close and before_close[-1] not in ('{', ','):
        before_close += ','
    new_block = before_close + '\n                                ' + effects_field + '\n                            }'

    content = content[:brace_start] + new_block + content[brace_end+1:]
    pos = brace_start + len(new_block)

# ============================================================
# PASS 3: PassiveTranscend conversions
# ============================================================

# SelfBuff = new PermanentBuff
content = re.sub(
    r'(new PassiveTranscend\s*\{[^}]*?)SelfBuff = new PermanentBuff\s*\{\s*([^}]+?)\s*\}([^}]*?\})',
    lambda m: f'{m.group(1)}Effects = new List<PersistentEffect> {{\n                new PersistentEffect {{ Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet {{ {m.group(2).strip().rstrip(",")} }} }}\n            }}{m.group(3)}' if 'Effects = new List<PersistentEffect>' not in m.group(1) else m.group(0),
    content
)

# SelfBuff = new TimedBuff (in PassiveTranscend = ConditionalSelfBuff)
content = re.sub(
    r'(new PassiveTranscend\s*\{[^}]*?)SelfBuff = new TimedBuff\s*\{\s*([^}]+?)\s*\}([^}]*?\})',
    lambda m: f'{m.group(1)}Effects = new List<PersistentEffect> {{\n                new PersistentEffect {{ Target = EffectTarget.Self, Type = PersistentEffectType.Buff, IsConditional = true, Buff = new BuffSet {{ {m.group(2).strip().rstrip(",")} }} }}\n            }}{m.group(3)}' if 'Effects = new List<PersistentEffect>' not in m.group(1) else m.group(0),
    content
)

# PartyBuff = new PermanentBuff
content = re.sub(
    r'(new PassiveTranscend\s*\{[^}]*?)PartyBuff = new PermanentBuff\s*\{\s*([^}]+?)\s*\}([^}]*?\})',
    lambda m: f'{m.group(1)}Effects = new List<PersistentEffect> {{\n                new PersistentEffect {{ Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet {{ {m.group(2).strip().rstrip(",")} }} }}\n            }}{m.group(3)}' if 'Effects = new List<PersistentEffect>' not in m.group(1) else m.group(0),
    content
)

# Debuff = new PermanentDebuff
content = re.sub(
    r'(new PassiveTranscend\s*\{[^}]*?)Debuff = new PermanentDebuff\s*\{\s*([^}]+?)\s*\}([^}]*?\})',
    lambda m: f'{m.group(1)}Effects = new List<PersistentEffect> {{\n                new PersistentEffect {{ Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet {{ {m.group(2).strip().rstrip(",")} }} }}\n            }}{m.group(3)}' if 'Effects = new List<PersistentEffect>' not in m.group(1) else m.group(0),
    content
)

# ConditionalSelfBuff = new TimedBuff
content = re.sub(
    r'(new PassiveTranscend\s*\{[^}]*?)ConditionalSelfBuff = new TimedBuff\s*\{\s*([^}]+?)\s*\}([^}]*?\})',
    lambda m: f'{m.group(1)}Effects = new List<PersistentEffect> {{\n                new PersistentEffect {{ Target = EffectTarget.Self, Type = PersistentEffectType.Buff, IsConditional = true, Buff = new BuffSet {{ {m.group(2).strip().rstrip(",")} }} }}\n            }}{m.group(3)}' if 'Effects = new List<PersistentEffect>' not in m.group(1) else m.group(0),
    content
)

# ============================================================
# PASS 4: PassiveLevelData conversions
# ============================================================

pos = 0
while True:
    m = re.search(r'new PassiveLevelData\s*\{', content[pos:])
    if not m:
        break
    block_start = pos + m.start()
    brace_start = content.index('{', block_start + len('new PassiveLevelData'))
    brace_end = find_matching_brace(content, brace_start)
    if brace_end < 0:
        pos = block_start + 1
        continue

    block = content[brace_start:brace_end+1]

    # Skip if already has Effects
    if 'Effects = new List<PersistentEffect>' in block:
        pos = brace_end + 1
        continue

    persistent_effects = []
    fields_to_remove = []

    # SelfBuff = new PermanentBuff
    for fm in re.finditer(r'SelfBuff = new PermanentBuff\s*\{\s*([^}]+?)\s*\}', block):
        inner = fm.group(1).strip().rstrip(',')
        persistent_effects.append(f'new PersistentEffect {{ Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet {{ {inner} }} }}')
        fields_to_remove.append((fm.start(), fm.end()))

    # PartyBuff = new PermanentBuff
    for fm in re.finditer(r'PartyBuff = new PermanentBuff\s*\{\s*([^}]+?)\s*\}', block):
        inner = fm.group(1).strip().rstrip(',')
        persistent_effects.append(f'new PersistentEffect {{ Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet {{ {inner} }} }}')
        fields_to_remove.append((fm.start(), fm.end()))

    # Debuff = new PermanentDebuff
    for fm in re.finditer(r'Debuff = new PermanentDebuff\s*\{\s*([^}]+?)\s*\}', block):
        inner = fm.group(1).strip().rstrip(',')
        persistent_effects.append(f'new PersistentEffect {{ Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet {{ {inner} }} }}')
        fields_to_remove.append((fm.start(), fm.end()))

    # ConditionalSelfBuff = new TimedBuff
    for fm in re.finditer(r'ConditionalSelfBuff = new TimedBuff\s*\{\s*([^}]+?)\s*\}', block):
        inner = fm.group(1).strip().rstrip(',')
        persistent_effects.append(f'new PersistentEffect {{ Target = EffectTarget.Self, Type = PersistentEffectType.Buff, IsConditional = true, Buff = new BuffSet {{ {inner} }} }}')
        fields_to_remove.append((fm.start(), fm.end()))

    # ConditionalPartyBuff = new TimedBuff
    for fm in re.finditer(r'ConditionalPartyBuff = new TimedBuff\s*\{\s*([^}]+?)\s*\}', block):
        inner = fm.group(1).strip().rstrip(',')
        persistent_effects.append(f'new PersistentEffect {{ Target = EffectTarget.Party, Type = PersistentEffectType.Buff, IsConditional = true, Buff = new BuffSet {{ {inner} }} }}')
        fields_to_remove.append((fm.start(), fm.end()))

    # PartyBuff = new TimedBuff (non-conditional, in passive = treated as conditional)
    for fm in re.finditer(r'(?<!Conditional)PartyBuff = new TimedBuff\s*\{\s*([^}]+?)\s*\}', block):
        inner = fm.group(1).strip().rstrip(',')
        persistent_effects.append(f'new PersistentEffect {{ Target = EffectTarget.Party, Type = PersistentEffectType.Buff, IsConditional = true, Buff = new BuffSet {{ {inner} }} }}')
        fields_to_remove.append((fm.start(), fm.end()))

    if not persistent_effects:
        pos = brace_end + 1
        continue

    # Build new block by removing old fields and adding Effects
    new_block = block
    # Sort removals in reverse order
    fields_to_remove.sort(key=lambda x: x[0], reverse=True)

    for rs, re_ in fields_to_remove:
        before = new_block[:rs]
        after = new_block[re_:]

        # Clean up surrounding commas and whitespace
        before_stripped = before.rstrip()
        after_stripped = after.lstrip()

        # Remove leading comma from after
        if after_stripped.startswith(','):
            after_stripped = after_stripped[1:].lstrip()

        # Remove trailing comma from before if next char is } or it already ends with comma
        if before_stripped.endswith(',') and (after_stripped.startswith('}') or after_stripped.startswith('\n')):
            before_stripped = before_stripped[:-1].rstrip()

        # Reconstruct with proper newline
        if after_stripped.startswith('}'):
            new_block = before_stripped + '\n                        ' + after_stripped
        else:
            new_block = before_stripped + '\n                            ' + after_stripped

    # Clean up multiple blank lines
    new_block = re.sub(r'\n\s*\n\s*\n', '\n', new_block)

    # Add Effects list
    effects_str = ',\n                                '.join(persistent_effects)
    effects_field = f'Effects = new List<PersistentEffect>\n                            {{\n                                {effects_str}\n                            }}'

    # Insert before closing brace
    last_brace = new_block.rfind('}')
    before_close = new_block[:last_brace].rstrip()
    if before_close and before_close[-1] == '{':
        # Empty block, just add Effects
        new_block = before_close + '\n                            ' + effects_field + '\n                        }'
    elif before_close and before_close[-1] != ',':
        new_block = before_close + ',\n                            ' + effects_field + '\n                        }'
    else:
        new_block = before_close + '\n                            ' + effects_field + '\n                        }'

    content = content[:brace_start] + new_block + content[brace_end+1:]
    pos = brace_start + len(new_block)

# ============================================================
# Cleanup
# ============================================================
# Fix double commas
content = re.sub(r',\s*,', ',', content)
# Fix trailing commas before closing brace
content = re.sub(r',(\s*\})', r'\1', content)

with open(filepath, 'w', encoding='utf-8') as f:
    f.write(content)

print("Conversion complete!")
