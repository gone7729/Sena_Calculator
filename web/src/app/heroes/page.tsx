"use client";

import { Fragment, useEffect, useMemo, useRef, useState } from "react";
import charactersData from "@/data/characters.json";
import HeroIcon from "@/components/HeroIcon";

interface SkillTier {
  cooldown: number;
  target: number;
  atk: number;
  ratio: number;
  hpRatio: number;
  defRatio: number;
  spdRatio: number;
  targetMaxHpRatio: number;
  targetCurrentHpRatio: number;
  atkCap: number;
  lostHpBonusMax: number;
  currentHpBonusMax: number;
  condRatioBonus: number;
  condExtraDmg: number;
  condDmgBonus: number;
  condExtraDmgSelfHp: number;
  healAtkRatio: number;
  healHpRatio: number;
  healDmgRatio: number;
  healDefRatio: number;
  fixedDamage: number;
  penetrate?: boolean;
  bonus?: Record<string, number>;
  effects?: string[];
  effect?: string;
}

// 스킬 자체 보너스(치피추가·약점추가피해·방어무시 등)를 "배율처럼" 한 줄씩
function skillBonusLines(bonus?: Record<string, number>): string[] {
  if (!bonus) return [];
  const lbl: Record<string, (v: number) => string> = {
    criDmg: (v) => `치명타피해 +${v}%`,
    criBonusDmg: (v) => `치명 시 추가피해 ${v}%`,
    wekDmg: (v) => `약점피해 +${v}%`,
    wekBonusDmg: (v) => `약점 시 추가피해 ${v}%`,
    cri: (v) => `치명확률 +${v}%`,
    wek: (v) => `약점확률 +${v}%`,
    armPen: (v) => `방어무시 ${v}%`,
    dmgDealt: (v) => `피해증가 ${v}%`,
    dmgDealtType: (v) => `타입피증 ${v}%`,
    dmgDealtBoss: (v) => `보스피증 ${v}%`,
    dmgDealt1to3: (v) => `1-3인기피증 ${v}%`,
    dmgDealt4to5: (v) => `4-5인기피증 ${v}%`,
    atkRate: (v) => `공격력 +${v}%`,
    magicAtkRate: (v) => `마법공격력 +${v}%`,
  };
  return Object.entries(bonus).map(([k, v]) => (lbl[k] ? lbl[k](v) : `${k} ${v}%`));
}

// 스킬 티어의 데미지 변수들을 "배율처럼" 한 줄씩 출력할 문자열로 변환
function skillDmgLines(t: SkillTier): string[] {
  const lines: string[] = [];
  const times = t.atk > 1 ? ` × ${t.atk}타` : "";
  const cap = t.atkCap ? ` (공격력 ${t.atkCap}% 제한)` : "";
  if (t.hpRatio) lines.push(`생명력 ${t.hpRatio}% 비례${times}`);
  if (t.defRatio) lines.push(`방어력 ${t.defRatio}% 비례${times}`);
  if (t.targetMaxHpRatio) lines.push(`최대 생명력 ${t.targetMaxHpRatio}%${times}${cap}`);
  if (t.targetCurrentHpRatio) lines.push(`현재 생명력 ${t.targetCurrentHpRatio}%${times}${cap}`);
  if (t.lostHpBonusMax) lines.push(`잃은 생명력 비례 최대 ${t.lostHpBonusMax}% 피해증가`);
  if (t.currentHpBonusMax) lines.push(`현재 생명력 비례 최대 ${t.currentHpBonusMax}% 피해증가`);
  if (t.condRatioBonus) lines.push(`조건부 배율 +${t.condRatioBonus}%`);
  if (t.condExtraDmg) lines.push(`조건부 추가피해 ${t.condExtraDmg}%`);
  if (t.condDmgBonus) lines.push(`조건부 피해증가 ${t.condDmgBonus}%`);
  if (t.condExtraDmgSelfHp) lines.push(`자가 잃은HP 비례 추가피해 ${t.condExtraDmgSelfHp}%`);
  if (t.healAtkRatio) lines.push(`공격력 ${t.healAtkRatio}% 회복`);
  if (t.healHpRatio) lines.push(`생명력 ${t.healHpRatio}% 회복`);
  if (t.healDmgRatio) lines.push(`가한 피해량 ${t.healDmgRatio}% 회복`);
  if (t.healDefRatio) lines.push(`방어력 ${t.healDefRatio}% 회복`);
  if (t.fixedDamage) lines.push(`고정 피해 ${t.fixedDamage.toLocaleString()}`);
  if (t.penetrate) lines.push(`관통(대상 피해 면역 무시)`);
  return lines;
}

interface Skill {
  id: number;
  name: string;
  skillType: string;
  enhanceAdds?: string;
  tiers: { base: SkillTier; enhanced: SkillTier; transcend: SkillTier };
}

interface BaseStats {
  atk: number;
  def: number;
  hp: number;
  spd: number;
  cri: number;
  criDmg: number;
  wek: number;
  wekDmg: number;
}

interface Hero {
  id: number;
  name: string;
  grade: string;
  type: string;
  attackType: string; // "Physical" | "Magic"
  transcendType: string;
  baseStats: BaseStats;
  transcend6: BaseStats;
  transcend12: BaseStats;
  passive?: {
    name: string;
    maxStacks: number;
    buffTiers?: {
      base: Record<string, number>;
      enhanced: Record<string, number>;
      transcend: Record<string, number>;
    };
    effectTiers?: {
      base?: string;
      enhanced?: string;
      transcend?: string;
    };
    effectGroups?: { target: string; items: string[] }[];
  } | null;
  skills: Skill[];
  tags?: string[]; // 버프/디버프 태그 (공략 데이터 연동 예정)
}

const heroes = charactersData as Hero[];

// 패시브 buffTier 스탯 코드 → 한글 라벨 (대부분 % 값)
const PASSIVE_STAT_LABELS: Record<string, string> = {
  atkRate: "공격력",
  magicAtkRate: "마법공격력",
  defRate: "방어력",
  cri: "치명타확률",
  criDmg: "치명타피해",
  wek: "약점확률",
  wekDmg: "약점피해",
  armPen: "방어무시",
  blk: "막기",
  dmgDealt: "피해증가",
  dmgDealt1to3: "1-3인기피증",
  dmgDealtType: "타입피증",
  dmgRdc: "받피감",
  dmgRdcMulti: "받피감(곱)",
  magDmgRdc: "마법받피감",
  physDmgRdc: "물리받피감",
  effHit: "효과적중",
  effRes: "효과저항",
  healBonus: "회복량",
  blessing: "축복",
  markPurify: "정화표식",
  shieldHpRatio: "보호막(체력비례)",
};

const passiveStatLabel = (key: string) => PASSIVE_STAT_LABELS[key] ?? key;

const GRADES = ["전체", "전설", "희귀"];
const ROLES = ["전체", "공격형", "마법형", "만능형", "방어형", "지원형"];

// 버프/효과 태그
const BUFF_TAGS = [
  "물공증", "마공증", "물피증", "마피증", "피증", "치피증", "약피증",
  "1-3인기", "4-5인기", "보피증", "쿨감/쿨초", "방어", "감쇄",
  "막기확률", "받피감", "받회증", "효적증", "효적확률증", "효저증",
  "행동제어면역", "디버프해제",
];

// 디버프/감소 태그
const DEBUFF_TAGS = [
  "방깎", "물리취약", "마법취약", "받피증", "받물피증", "받마피증",
  "물공감", "마공감", "물피감", "마피감", "피감", "막기확률감소",
  "치피감", "약피감", "치확감소", "약공감소", "받회감", "효저깎",
  "버프해제", "턴감", "쿨증",
];

// 상태이상 태그 (StatusEffect.Name 기준)
const STATUS_TAGS = [
  "기절", "침묵", "빙결", "석화", "빙극", "마비", "감전", "수면", "혼란", "진탕",
  "실명", "도발", "화상", "출혈", "중독", "즉사", "폭탄",
];


const SKILL_TYPE_LABEL: Record<string, string> = {
  Normal: "평타",
  Normal2: "평타2",
  Skill1: "1스킬",
  Skill2: "2스킬",
  Skill3: "3스킬",
  Skill4: "4스킬",
};

function atkLabel(attackType: string) {
  return attackType === "Magic" ? "마법" : "물리";
}

export default function HeroesPage() {
  const [grade, setGrade] = useState("전체");
  const [role, setRole] = useState("전체");
  const [selectedTags, setSelectedTags] = useState<Set<string>>(new Set());
  const [selectedId, setSelectedId] = useState<number | null>(null);
  // 스킬 설명 단계: 기본 / 스강 / 초월

  // 우측 상세 패널 높이를 측정해 좌측 리스트 패널 높이를 맞춘다
  const detailRef = useRef<HTMLElement>(null);
  const [detailHeight, setDetailHeight] = useState<number | undefined>();

  useEffect(() => {
    const el = detailRef.current;
    if (!el) return;
    const update = () => setDetailHeight(el.offsetHeight);
    update();
    const ro = new ResizeObserver(update);
    ro.observe(el);
    return () => ro.disconnect();
  }, []);

  const toggleTag = (tag: string) => {
    setSelectedTags((prev) => {
      const next = new Set(prev);
      if (next.has(tag)) next.delete(tag);
      else next.add(tag);
      return next;
    });
  };

  const filtered = useMemo(
    () =>
      heroes.filter((h) => {
        if (grade !== "전체" && h.grade !== grade) return false;
        if (role !== "전체" && h.type !== role) return false;
        // 버프/디버프 태그: 선택된 태그를 모두 가진 영웅만 (AND 매칭)
        if (selectedTags.size > 0) {
          const tags = h.tags ?? [];
          for (const t of selectedTags) {
            if (!tags.includes(t)) return false;
          }
        }
        return true;
      }),
    [grade, role, selectedTags]
  );

  const selected = heroes.find((h) => h.id === selectedId) ?? null;

  return (
    <main className="main">
      <h1 className="page-title">영웅</h1>

      {/* ===== Filters ===== */}
      <div className="filter-block">
        <div className="filter-row">
          <span className="filter-label">등급</span>
          <div className="chip-group">
            {GRADES.map((g) => (
              <button
                key={g}
                type="button"
                className={`chip${grade === g ? " active" : ""}`}
                onClick={() => setGrade(g)}
              >
                {g}
              </button>
            ))}
          </div>
        </div>

        <div className="filter-row">
          <span className="filter-label">역할군</span>
          <div className="chip-group">
            {ROLES.map((r) => (
              <button
                key={r}
                type="button"
                className={`chip${role === r ? " active" : ""}`}
                onClick={() => setRole(r)}
              >
                {r}
              </button>
            ))}
          </div>
        </div>

        <div className="filter-row">
          <span className="filter-label">버프</span>
          <div className="chip-group">
            {BUFF_TAGS.map((t) => (
              <button
                key={t}
                type="button"
                className={`chip${selectedTags.has(t) ? " active" : ""}`}
                onClick={() => toggleTag(t)}
              >
                {t}
              </button>
            ))}
          </div>
        </div>

        <div className="filter-row">
          <span className="filter-label">디버프</span>
          <div className="chip-group">
            {DEBUFF_TAGS.map((t) => (
              <button
                key={t}
                type="button"
                className={`chip${selectedTags.has(t) ? " active" : ""}`}
                onClick={() => toggleTag(t)}
              >
                {t}
              </button>
            ))}
          </div>
        </div>

        <div className="filter-row">
          <span className="filter-label">상태이상</span>
          <div className="chip-group">
            {STATUS_TAGS.map((t) => (
              <button
                key={t}
                type="button"
                className={`chip${selectedTags.has(t) ? " active" : ""}`}
                onClick={() => toggleTag(t)}
              >
                {t}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* ===== Two columns ===== */}
      <div className="columns">
        {/* Left: list */}
        <section
          className="panel panel-list"
          style={detailHeight ? { height: detailHeight } : undefined}
        >
          <h2 className="panel-title">리스트</h2>
          <p className="panel-subtitle">
            {filtered.length}명 · {grade} / {role}
            {selectedTags.size > 0 ? ` / 태그 ${selectedTags.size}` : ""}
          </p>

          <div className="hero-list">
            {filtered.length === 0 ? (
              <div className="hero-list-empty">조건에 맞는 영웅이 없습니다.</div>
            ) : (
              filtered.map((h) => (
                <button
                  key={h.id}
                  type="button"
                  className={`hero-card${selectedId === h.id ? " selected" : ""}`}
                  onClick={() => setSelectedId(h.id)}
                >
                  <HeroIcon id={h.id} name={h.name} />
                  <div className="hero-card-name">{h.name}</div>
                </button>
              ))
            )}
          </div>
        </section>

        {/* Right: detail */}
        <section className="panel" ref={detailRef}>
          {!selected ? (
            <div className="detail-empty">왼쪽에서 영웅을 선택하세요.</div>
          ) : (
            <>
              <h2 className="panel-title">{selected.name}</h2>
              <p className="panel-subtitle">
                {selected.grade} · {selected.type} · {atkLabel(selected.attackType)} 공격
              </p>

              <div className="flex justify-start gap-8 mb-4">

                <div className="flex flex-col gap-4">
                  <div className="hero-image">이미지</div>
                  {/* 캐릭터 스탯 (이미지 아래) — Base / 6초월 / 12초월 */}
                  <div className="stat-box">
                  <div className="stat-box-title">스탯</div>
                  <div className="stat-table">
                    <span className="stat-th" />
                    <span className="stat-th">Base</span>
                    <span className="stat-th">6초월</span>
                    <span className="stat-th">12초월</span>
                    {(
                      [
                        ["atk", selected.attackType === "Magic" ? "마법공격력" : "공격력", false],
                        ["def", "방어력", false],
                        ["hp", "생명력", false],
                        ["spd", "속공", false],
                        ["cri", "치명", true],
                        ["criDmg", "치명피해", true],
                        ["wek", "약점", true],
                        ["wekDmg", "약점피해", true],
                      ] as [keyof BaseStats, string, boolean][]
                    ).map(([key, label, pct]) => {
                      const fmt = (v: number) => (pct ? `${v}%` : v.toLocaleString());
                      return (
                        <Fragment key={key}>
                          <span className="stat-key">{label}</span>
                          <span className="stat-val">{fmt(selected.baseStats[key])}</span>
                          <span className="stat-val">{fmt(selected.transcend6[key])}</span>
                          <span className="stat-val">{fmt(selected.transcend12[key])}</span>
                        </Fragment>
                      );
                    })}
                  </div>
                </div>
                
              </div>

              {/* 스킬 (스강+초월 적용 상태로 출력, 초월 추가효과는 하단 주석으로) */}
              <div className="w-full content-box">

                <div className="flex justify-between">
                  <span className="label">스킬</span>
                  <span className="text-xs text-gray-400">스강+초월 적용</span>
                </div>

                <div className="flex flex-col gap-2 mt-2">
                  {(() => {
                  const renderSkill = (skill: Skill) => {
                    // 스강+초월 적용 상태: 숫자/효과는 enhanced, 초월이 추가하는 효과는 transcend.effect
                    const e = skill.tiers.enhanced;
                    const tr = skill.tiers.transcend;
                    const ratio = tr.ratio ?? e.ratio;
                    const atk = tr.atk ?? e.atk;
                    const target = tr.target ?? e.target;
                    const cooldown = tr.cooldown ?? e.cooldown;
                    return (
                      <div
                        key={skill.id}
                        className="flex flex-col border-2 border-blue-400 rounded-xl p-2"
                      >
                        <div className="flex justify-between items-center gap-2">
                          <span className="font-bold text-sm">{skill.name}</span>
                          <span className="text-xs text-gray-500">
                            {cooldown > 0 ? `쿨 ${cooldown}초` : ""}
                          </span>
                        </div>
                        <div className="flex flex-col text-sm mt-1">
                          <span>
                            배율 {ratio}%
                            {atk > 1 ? ` × ${atk}타` : ""}
                          </span>
                          {skillDmgLines(e).map((line, i) => (
                            <span key={`d${i}`}>{line}</span>
                          ))}
                          {skillBonusLines(e.bonus).map((line, i) => (
                            <span key={`b${i}`}>{line}</span>
                          ))}
                          {(e.effects ?? []).map((line, i) => (
                            <span key={`e${i}`} className="text-gray-500">{line}</span>
                          ))}
                          <span className="text-gray-500">대상 {target}</span>
                          {/* 구조화 효과/보너스가 전혀 없을 때만 prose 폴백 (정보 손실 방지) */}
                          {(e.effects ?? []).length === 0 &&
                          (!e.bonus || Object.keys(e.bonus).length === 0) &&
                          e.effect ? (
                            <span className="text-xs text-gray-400 mt-1">{e.effect}</span>
                          ) : null}
                          {skill.enhanceAdds ? (
                            <span className="text-xs text-green-500 mt-1">{`// 스강: ${skill.enhanceAdds}`}</span>
                          ) : null}
                          {tr.effect ? (
                            <span className="text-xs text-blue-400 mt-1">{`// 초월: ${tr.effect}`}</span>
                          ) : null}
                        </div>
                      </div>
                    );
                  };
                  // 변신(분신) 영웅: Normal2/Skill3/Skill4 = 변신 상태 스킬 → 별도 그룹으로 분리.
                  const tfTypes = new Set(["Normal2", "Skill3", "Skill4"]);
                  const baseSkills = selected.skills.filter((s) => !tfTypes.has(s.skillType));
                  const tfSkills = selected.skills.filter((s) => tfTypes.has(s.skillType));
                  const trigger = selected.skills.find(
                    (s) =>
                      /분신/.test(s.tiers.enhanced.effect || "") ||
                      (s.tiers.enhanced.effects ?? []).some((x) => /분신/.test(x))
                  );
                  return (
                    <>
                      {baseSkills.map(renderSkill)}
                      {tfSkills.length > 0 ? (
                        <div className="text-xs text-purple-400 font-semibold mt-1">
                          {`🔄 분신 상태${trigger ? ` — ${trigger.name} 시전 시 전환` : ""}`}
                        </div>
                      ) : null}
                      {tfSkills.map(renderSkill)}
                    </>
                  );
                  })()}
                  {selected.passive && (() => {
                    const p = selected.passive;
                    const groups = p.effectGroups ?? [];
                    const trEffect = p.effectTiers?.transcend;
                    // 그룹이 없으면 텍스트/수치 폴백
                    const fallbackText = p.effectTiers?.enhanced;
                    const bt = p.buffTiers;
                    const stats = { ...(bt?.enhanced ?? {}), ...(bt?.transcend ?? {}) };
                    const entries = Object.entries(stats);
                    return (
                      <div className="flex flex-col border-2 border-amber-400 rounded-xl p-2">
                        <div className="flex justify-between items-center gap-2">
                          <span className="font-bold text-sm">{p.name}</span>
                          {p.maxStacks > 1 ? (
                            <span className="text-xs text-gray-500">
                              최대 {p.maxStacks}스택
                            </span>
                          ) : null}
                        </div>
                        <div className="flex flex-col text-sm mt-1 gap-2">
                          {groups.length > 0 ? (
                            groups.map((g) => (
                              <div key={g.target} className="flex flex-col">
                                <span className="font-semibold">{g.target}</span>
                                <ul className="list-disc list-inside text-gray-500">
                                  {g.items.map((it, i) => (
                                    <li key={i}>{it}</li>
                                  ))}
                                </ul>
                              </div>
                            ))
                          ) : fallbackText ? (
                            <span className="text-gray-500">{fallbackText}</span>
                          ) : entries.length > 0 ? (
                            entries.map(([k, v]) => (
                              <span key={k} className="text-gray-500">
                                {passiveStatLabel(k)} {v}%
                              </span>
                            ))
                          ) : (
                            <span className="text-gray-400">-</span>
                          )}
                          {trEffect ? (
                            <span className="text-xs text-blue-400 mt-1">{`// 초월: ${trEffect}`}</span>
                          ) : null}
                        </div>
                      </div>
                    );
                  })()}
                </div>
              </div>
              
            </div>

              

              {/* 무기 (추천 세팅 — 공략 데이터 입력 예정) */}
              <div className="equip-group">
                <h3 className="equip-group-title">무기</h3>
                <div className="set-row">
                  {[0, 1].map((i) => (
                    <div className="set-card" key={i}>
                      <div className="set-name">세트명</div>
                      <div className="opt-row">
                        <div>메인 옵션</div>
                        <div>부 옵션</div>
                      </div>
                      <div className="opt-row">
                        <div className="opt-list">옵션 명</div>
                        <div className="opt-list">
                          <div>옵션 명</div>
                          <div>옵션 명</div>
                          <div>옵션 명</div>
                          <div>옵션 명</div>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              {/* 방어구 */}
              <div className="equip-group">
                <h3 className="equip-group-title">방어구</h3>
                <div className="set-row">
                  {[0, 1].map((i) => (
                    <div className="set-card" key={i}>
                      <div className="set-name">세트명</div>
                      <div className="opt-row">
                        <div>메인 옵션</div>
                        <div>부 옵션</div>
                      </div>
                      <div className="opt-row">
                        <div className="opt-list">옵션 명</div>
                        <div className="opt-list">
                          <div>옵션 명</div>
                          <div>옵션 명</div>
                          <div>옵션 명</div>
                          <div>옵션 명</div>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </>
          )}
        </section>
      </div>
    </main>
  );
}

