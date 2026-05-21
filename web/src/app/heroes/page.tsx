"use client";

import { Fragment, useEffect, useMemo, useRef, useState } from "react";
import charactersData from "@/data/characters.json";

interface SkillTier {
  cooldown: number;
  target: number;
  atk: number;
  ratio: number;
}

interface Skill {
  id: number;
  name: string;
  skillType: string;
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
  passive?: { name: string; description?: string; maxStacks: number } | null;
  skills: Skill[];
  tags?: string[]; // 버프/디버프 태그 (공략 데이터 연동 예정)
}

const heroes = charactersData as Hero[];

const GRADES = ["전체", "전설", "영웅"];
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
  "버프해제", "턴감",
];

// 상태이상 태그 (StatusEffect.Name 기준)
const STATUS_TAGS = [
  "기절", "침묵", "빙결", "석화", "빙극", "마비", "감전", "수면", "혼란", "진탕",
  "실명", "도발", "화상", "출혈", "중독", "즉사", "폭탄",
];

const BUFF_SET = new Set(BUFF_TAGS);
const STATUS_SET = new Set(STATUS_TAGS);

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
                  className={`hero-row${selectedId === h.id ? " selected" : ""}`}
                  onClick={() => setSelectedId(h.id)}
                >
                  <span className="hero-row-name">{h.name}</span>
                  <span className="hero-row-tags">
                    <span className={`tag${h.grade === "전설" ? " grade-legend" : ""}`}>
                      {h.grade}
                    </span>
                    <span
                      className={`tag ${h.attackType === "Magic" ? "atk-magic" : "atk-physical"}`}
                    >
                      {h.type}
                    </span>
                    {h.tags?.map((t) => (
                      <span
                        key={t}
                        className={`tag ${
                          BUFF_SET.has(t) ? "tag-buff" : STATUS_SET.has(t) ? "tag-status" : "tag-debuff"
                        }`}
                      >
                        {t}
                      </span>
                    ))}
                  </span>
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

              <div className="detail-top">
                <div className="hero-image">이미지</div>

                {/* 스킬 (가운데, 마우스 호버 시 상세) */}
                <div className="content-box">
                  <div className="label">스킬</div>
                  {selected.skills.map((s) => (
                    <div className="skill-item" key={s.id}>
                      {s.skillType === "Normal"
                        ? "기본공격"
                        : `${SKILL_TYPE_LABEL[s.skillType] ?? s.skillType} · ${s.name}`}
                      <div className="skill-tooltip">
                        <div className="tt-name">{s.name}</div>
                        <div className="tt-table">
                          <span className="tt-th" />
                          <span className="tt-th">기본</span>
                          <span className="tt-th">강화</span>
                          <span className="tt-th">초월</span>
                          {(
                            [
                              ["쿨타임", (t: SkillTier) => `${t.cooldown}`],
                              ["대상 수", (t: SkillTier) => (t.target > 0 ? `${t.target}` : "-")],
                              ["공격 횟수", (t: SkillTier) => `${t.atk}`],
                              ["배율", (t: SkillTier) => `${t.ratio}%`],
                            ] as [string, (t: SkillTier) => string][]
                          ).map(([label, f]) => (
                            <Fragment key={label}>
                              <span className="tt-key">{label}</span>
                              <span>{f(s.tiers.base)}</span>
                              <span>{f(s.tiers.enhanced)}</span>
                              <span>{f(s.tiers.transcend)}</span>
                            </Fragment>
                          ))}
                        </div>
                      </div>
                    </div>
                  ))}
                  {selected.passive && (
                    <div className="skill-item">
                      {`패시브 · ${selected.passive.name}`}
                      <div className="skill-tooltip">
                        <div className="tt-name">{selected.passive.name}</div>
                        <div className="tt-meta">
                          {selected.passive.description ?? "패시브 효과"}
                          {selected.passive.maxStacks > 1
                            ? ` · 최대 ${selected.passive.maxStacks}중첩`
                            : ""}
                        </div>
                      </div>
                    </div>
                  )}
                </div>

                <div className="tip-box">
                  <div className="label">Tip</div>
                  <div className="line">~~~~~~~~~~</div>
                </div>
              </div>

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
