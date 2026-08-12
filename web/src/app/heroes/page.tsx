"use client";

import { Fragment, useCallback, useEffect, useMemo, useRef, useState } from "react";
import charactersData from "@/data/characters.json";
import HeroIcon from "@/components/HeroIcon";
import { USAGE_GROUPS, isPvpUsage } from "@/data/usageCategories";

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

// 스킬 자체 보너스 키 → 한글 라벨
const BONUS_LABELS: Record<string, string> = {
  criDmg: "치명타피해",
  criBonusDmg: "치명 시 추가피해",
  wekDmg: "약점피해",
  wekBonusDmg: "약점 시 추가피해",
  cri: "치명확률",
  wek: "약점확률",
  armPen: "방어무시",
  dmgDealt: "피해증가",
  dmgDealtType: "타입피증",
  dmgDealtBoss: "보스피증",
  dmgDealt1to3: "1-3인기피증",
  dmgDealt4to5: "4-5인기피증",
  atkRate: "공격력",
  magicAtkRate: "마법공격력",
};

/**
 * 스킬 티어를 "항목 → 값" 맵으로 평탄화.
 * 티어 간 비교(기본 → 스강 → 초월)를 항목 단위로 하기 위한 정규형이라,
 * 값이 바뀐 항목만 골라내는 게 문자열 비교 한 번으로 끝난다.
 */
function tierFields(t: SkillTier): Record<string, string> {
  const f: Record<string, string> = {};
  const pct = (v: number) => `${v}%`;
  if (t.cooldown) f["쿨타임"] = `${t.cooldown}초`;
  if (t.target) f["대상"] = `${t.target}명`;
  if (t.ratio) f["배율"] = pct(t.ratio);
  if (t.atk > 1) f["타수"] = `${t.atk}타`;
  if (t.hpRatio) f["생명력 비례"] = pct(t.hpRatio);
  if (t.defRatio) f["방어력 비례"] = pct(t.defRatio);
  if (t.spdRatio) f["속공 비례"] = pct(t.spdRatio);
  if (t.targetMaxHpRatio) f["대상 최대생명력"] = pct(t.targetMaxHpRatio);
  if (t.targetCurrentHpRatio) f["대상 현재생명력"] = pct(t.targetCurrentHpRatio);
  if (t.atkCap) f["공격력 상한"] = pct(t.atkCap);
  if (t.lostHpBonusMax) f["잃은HP 비례 피증(최대)"] = pct(t.lostHpBonusMax);
  if (t.currentHpBonusMax) f["현재HP 비례 피증(최대)"] = pct(t.currentHpBonusMax);
  if (t.condRatioBonus) f["조건부 배율"] = pct(t.condRatioBonus);
  if (t.condExtraDmg) f["조건부 추가피해"] = pct(t.condExtraDmg);
  if (t.condDmgBonus) f["조건부 피해증가"] = pct(t.condDmgBonus);
  if (t.condExtraDmgSelfHp) f["자가 잃은HP 추가피해"] = pct(t.condExtraDmgSelfHp);
  if (t.healAtkRatio) f["공격력 비례 회복"] = pct(t.healAtkRatio);
  if (t.healHpRatio) f["생명력 비례 회복"] = pct(t.healHpRatio);
  if (t.healDmgRatio) f["피해량 비례 회복"] = pct(t.healDmgRatio);
  if (t.healDefRatio) f["방어력 비례 회복"] = pct(t.healDefRatio);
  if (t.fixedDamage) f["고정피해"] = t.fixedDamage.toLocaleString();
  if (t.penetrate) f["관통"] = "적용";
  for (const [k, v] of Object.entries(t.bonus ?? {})) {
    f[BONUS_LABELS[k] ?? k] = `${v > 0 ? "+" : ""}${v}%`;
  }
  return f;
}

/** 패시브 버프 맵 → "항목 → 값" (스킬과 같은 정규형) */
function passiveFields(buff?: Record<string, number>): Record<string, string> {
  const f: Record<string, string> = {};
  for (const [k, v] of Object.entries(buff ?? {})) {
    f[passiveStatLabel(k)] = `${v > 0 ? "+" : ""}${v}%`;
  }
  return f;
}

interface FieldChange {
  key: string;
  /** 이전 티어 값 — 새로 생긴 항목이면 undefined */
  from?: string;
  to: string;
}

/** prev → next 에서 값이 바뀌거나 새로 생긴 항목만. (같은 값은 버려서 "변경값만" 표시가 된다) */
function diffFields(
  prev: Record<string, string>,
  next: Record<string, string>
): FieldChange[] {
  const out: FieldChange[] = [];
  for (const [key, to] of Object.entries(next)) {
    const from = prev[key];
    if (from !== to) out.push({ key, from, to });
  }
  return out;
}

interface Skill {
  id: number;
  name: string;
  skillType: string;
  enhanceAdds?: string;
  tiers: { base: SkillTier; enhanced: SkillTier; transcend: SkillTier; awaken?: SkillTier | null };
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
      awaken?: string;
    };
    effectGroups?: { target: string; items: string[] }[];
  } | null;
  hasAwakening?: boolean;
  skills: Skill[];
  tags?: string[]; // 버프/디버프 태그 (공략 데이터 연동 예정)
  mainUsages?: string[]; // 주 사용처 (heroUsage.json 오버레이 → export 시 병합)
}

// JSON 리터럴은 필드별 유니온으로 추론돼(빈 배열 → never[], 선택적 키 → undefined) Hero와 직접 겹치지 않는다.
// 구조는 export가 보장하므로 unknown 경유로 단언.
const heroes = charactersData as unknown as Hero[];

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
  Awaken: "각성 전용",
};

function atkLabel(attackType: string) {
  return attackType === "Magic" ? "마법" : "물리";
}

/** 기본 칸: 항목 전체를 그대로 나열 */
function BaseColumn({ fields, effects }: { fields: Record<string, string>; effects?: string[] }) {
  const entries = Object.entries(fields);
  if (entries.length === 0 && !(effects ?? []).length) {
    return <span className="tier-none">-</span>;
  }
  return (
    <>
      {entries.map(([k, v]) => (
        <div key={k} className="tier-line">
          <span className="tier-key">{k}</span>
          <span className="tier-val">{v}</span>
        </div>
      ))}
      {(effects ?? []).map((e, i) => (
        <div key={`e${i}`} className="tier-effect">
          {e}
        </div>
      ))}
    </>
  );
}

/**
 * 스강/초월 칸: 직전 티어 대비 "변경된 항목만".
 * 값이 바뀐 항목은 이전값 → 새값으로, 새로 생긴 항목은 새값만 보여준다.
 */
function DiffColumn({
  changes,
  addedText,
}: {
  changes: FieldChange[];
  addedText?: string | null;
}) {
  if (changes.length === 0 && !addedText) {
    return <span className="tier-none">변경 없음</span>;
  }
  return (
    <>
      {changes.map((c) => (
        <div key={c.key} className="tier-line">
          <span className="tier-key">{c.key}</span>
          <span className="tier-val">
            {c.from !== undefined ? <span className="tier-from">{c.from} → </span> : null}
            <span className="tier-to">{c.to}</span>
          </span>
        </div>
      ))}
      {addedText ? <div className="tier-effect tier-effect-add">{addedText}</div> : null}
    </>
  );
}

/** 스킬/패시브 1개를 기본·스킬강화·초월 3칸으로 */
function TierBlock({
  title,
  subtitle,
  accent,
  base,
  enhanced,
  transcend,
  awaken,
  baseEffects,
  enhanceAdds,
  transcendEffect,
  awakenEffect,
}: {
  title: string;
  subtitle?: string;
  accent: "skill" | "passive";
  base: Record<string, string>;
  enhanced: Record<string, string>;
  transcend: Record<string, string>;
  awaken?: Record<string, string> | null;
  baseEffects?: string[];
  enhanceAdds?: string | null;
  transcendEffect?: string | null;
  awakenEffect?: string | null;
}) {
  // 스강은 기본 대비, 초월은 스강 대비 — 단계별 증분이라 중복 없이 읽힌다
  const enhChanges = diffFields(base, enhanced);
  const trChanges = diffFields(enhanced, transcend);
  // 각성은 강화 대비 (각성이 강화를 포함·대체하므로) — 데이터 있을 때만 4번째 칸 노출
  const hasAwaken = awaken != null || (awakenEffect != null && awakenEffect !== "");
  const awChanges = awaken != null ? diffFields(enhanced, awaken) : [];
  return (
    <div className={`tier-block tier-${accent}`}>
      <div className="tier-block-head">
        <span className="tier-block-name">{title}</span>
        {subtitle ? <span className="tier-block-sub">{subtitle}</span> : null}
      </div>
      <div className={hasAwaken ? "tier-grid tier-grid-4" : "tier-grid"}>
        <div className="tier-col-head">기본</div>
        <div className="tier-col-head">스킬강화</div>
        <div className="tier-col-head">초월</div>
        {hasAwaken ? <div className="tier-col-head tier-col-head-awaken">각성</div> : null}
        <div className="tier-col">
          <BaseColumn fields={base} effects={baseEffects} />
        </div>
        <div className="tier-col">
          <DiffColumn changes={enhChanges} addedText={enhanceAdds} />
        </div>
        <div className="tier-col">
          <DiffColumn changes={trChanges} addedText={transcendEffect} />
        </div>
        {hasAwaken ? (
          <div className="tier-col tier-col-awaken">
            <DiffColumn changes={awChanges} addedText={awakenEffect} />
          </div>
        ) : null}
      </div>
    </div>
  );
}

/**
 * 주 사용처 표시 + 편집.
 * 편집은 개발 서버에서만 (API가 프로덕션 쓰기를 거부) — 서비스 전 데이터 입력 용도.
 */
function UsageSection({
  hero,
  usages,
  onSaved,
}: {
  hero: Hero;
  usages: string[];
  onSaved: (heroId: number, next: string[]) => void;
}) {
  const editable = process.env.NODE_ENV !== "production";
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState<string[]>(usages);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // 영웅이 바뀔 때의 초안 리셋은 호출부의 key={hero.id}가 처리한다(리마운트).
  // effect로 setState 하면 불필요한 연쇄 렌더가 생겨 React가 권장하지 않는 패턴.

  const toggle = (u: string) =>
    setDraft((prev) => (prev.includes(u) ? prev.filter((x) => x !== u) : [...prev, u]));

  const save = async () => {
    setSaving(true);
    setError(null);
    try {
      const res = await fetch("/api/hero-usage", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ heroId: hero.id, usages: draft }),
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data?.error ?? "저장 실패");
      onSaved(hero.id, data.usages ?? []);
      setEditing(false);
    } catch (e) {
      setError(e instanceof Error ? e.message : "저장 실패");
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="usage-box">
      <div className="usage-head">
        <span className="usage-title">주 사용처</span>
        {editable ? (
          editing ? (
            <span className="usage-actions">
              <button
                type="button"
                className="usage-btn"
                onClick={() => {
                  setDraft(usages);
                  setEditing(false);
                  setError(null);
                }}
                disabled={saving}
              >
                취소
              </button>
              <button type="button" className="usage-btn primary" onClick={save} disabled={saving}>
                {saving ? "저장 중…" : "저장"}
              </button>
            </span>
          ) : (
            <button type="button" className="usage-btn" onClick={() => setEditing(true)}>
              편집
            </button>
          )
        ) : null}
      </div>

      {error ? <div className="usage-error">{error}</div> : null}

      {editing ? (
        <div className="usage-edit">
          {USAGE_GROUPS.map((g) => (
            <div key={g.group} className="usage-group">
              <div className="usage-group-title">{g.group}</div>
              <div className="usage-group-items">
                {g.items.map((u) => (
                  <label key={u} className="usage-check">
                    <input type="checkbox" checked={draft.includes(u)} onChange={() => toggle(u)} />
                    <span>{u}</span>
                  </label>
                ))}
              </div>
            </div>
          ))}
        </div>
      ) : usages.length > 0 ? (
        <div className="usage-tags">
          {usages.map((u) => (
            <span key={u} className={`usage-tag${isPvpUsage(u) ? " pvp" : ""}`}>
              {u}
            </span>
          ))}
        </div>
      ) : (
        <div className="usage-empty">{editable ? "미지정 — 편집으로 추가" : "미지정"}</div>
      )}
    </div>
  );
}

export default function HeroesPage() {
  const [grade, setGrade] = useState("전체");
  const [role, setRole] = useState("전체");
  const [selectedTags, setSelectedTags] = useState<Set<string>>(new Set());
  const [selectedId, setSelectedId] = useState<number | null>(null);

  // 주 사용처 오버레이 — characters.json에 구운 값을 기준으로, 편집 저장분을 덮어쓴다.
  //   (개발 중 편집한 내용이 export 전에도 화면에 바로 반영되도록)
  const [usageOverlay, setUsageOverlay] = useState<Record<string, string[]>>({});
  useEffect(() => {
    let alive = true;
    fetch("/api/hero-usage")
      .then((r) => (r.ok ? r.json() : {}))
      .then((d: unknown) => {
        if (alive && d && typeof d === "object") setUsageOverlay(d as Record<string, string[]>);
      })
      .catch(() => {
        /* 저장소를 못 읽어도 characters.json 값으로 표시 */
      });
    return () => {
      alive = false;
    };
  }, []);

  const handleUsageSaved = useCallback((heroId: number, next: string[]) => {
    setUsageOverlay((prev) => ({ ...prev, [String(heroId)]: next }));
  }, []);

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

                {/* 주 사용처 — 스탯 옆. 개발 서버에서 편집·저장 (heroUsage.json) */}
                <UsageSection
                  key={selected.id}
                  hero={selected}
                  usages={usageOverlay[String(selected.id)] ?? selected.mainUsages ?? []}
                  onSaved={handleUsageSaved}
                />
              </div>

              {/* 스킬·패시브 — 기본 / 스킬강화 / 초월 3칸. 스강·초월 칸은 직전 단계 대비 변경값만. */}
              <div className="w-full content-box">

                <div className="flex justify-between">
                  <span className="label">스킬 · 패시브</span>
                  <span className="text-xs text-gray-400">스강·초월은 변경값만</span>
                </div>

                <div className="flex flex-col gap-2 mt-2">
                  {(() => {
                  const renderSkill = (skill: Skill) => {
                    const b = skill.tiers.base;
                    const e = skill.tiers.enhanced;
                    const tr = skill.tiers.transcend;
                    const aw = skill.tiers.awaken;
                    // 기본 칸에 붙일 효과 텍스트: 구조화 라인이 있으면 그걸, 없으면 prose 폴백(정보 손실 방지)
                    const baseEffects =
                      (b.effects ?? []).length > 0
                        ? b.effects
                        : b.effect
                          ? [b.effect]
                          : [];
                    return (
                      <TierBlock
                        key={skill.id}
                        accent="skill"
                        title={skill.name}
                        subtitle={SKILL_TYPE_LABEL[skill.skillType] ?? skill.skillType}
                        base={tierFields(b)}
                        enhanced={tierFields(e)}
                        transcend={tierFields(tr)}
                        awaken={aw && skill.skillType !== "Awaken" ? tierFields(aw) : null}
                        baseEffects={baseEffects}
                        enhanceAdds={skill.enhanceAdds ?? null}
                        transcendEffect={tr.effect ?? null}
                        awakenEffect={skill.skillType !== "Awaken" ? (aw?.effect ?? null) : null}
                      />
                    );
                  };
                  // 변신(분신) 영웅: Normal2/Skill3/Skill4 = 변신 상태 스킬 → 별도 그룹으로 분리.
                  const tfTypes = new Set(["Normal2", "Skill3", "Skill4"]);
                  const awakenSkills = selected.skills.filter((s) => s.skillType === "Awaken");
                  const baseSkills = selected.skills.filter(
                    (s) => !tfTypes.has(s.skillType) && s.skillType !== "Awaken"
                  );
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
                      {awakenSkills.length > 0 ? (
                        <div className="text-xs text-amber-400 font-semibold mt-1">
                          ✦ 각성 전용 스킬 (각성 게이지 발동)
                        </div>
                      ) : null}
                      {awakenSkills.map(renderSkill)}
                    </>
                  );
                  })()}
                  {selected.passive && (() => {
                    const p = selected.passive;
                    const bt = p.buffTiers;
                    // 기본 칸 효과 텍스트: 대상별 그룹이 있으면 그걸 줄로 펴고, 없으면 기본 효과 문구
                    const groups = p.effectGroups ?? [];
                    const baseEffects =
                      groups.length > 0
                        ? groups.flatMap((g) => g.items.map((it) => `[${g.target}] ${it}`))
                        : p.effectTiers?.base
                          ? [p.effectTiers.base]
                          : [];
                    // 스강 텍스트는 기본과 다를 때만 (같으면 "변경 없음"으로 빠짐)
                    const enhText =
                      p.effectTiers?.enhanced && p.effectTiers.enhanced !== p.effectTiers?.base
                        ? p.effectTiers.enhanced
                        : null;
                    return (
                      <TierBlock
                        accent="passive"
                        title={p.name}
                        subtitle={p.maxStacks > 1 ? `패시브 · 최대 ${p.maxStacks}스택` : "패시브"}
                        base={passiveFields(bt?.base)}
                        enhanced={passiveFields(bt?.enhanced)}
                        transcend={passiveFields(bt?.transcend)}
                        awaken={p.effectTiers?.awaken ? {} : null}
                        baseEffects={baseEffects}
                        enhanceAdds={enhText}
                        transcendEffect={p.effectTiers?.transcend ?? null}
                        awakenEffect={p.effectTiers?.awaken ?? null}
                      />
                    );
                  })()}
                </div>
              </div>
            </>
          )}
        </section>
      </div>
    </main>
  );
}

