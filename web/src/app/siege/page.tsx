"use client";

import { useMemo, useState } from "react";
import charactersData from "@/data/characters.json";

interface Hero {
  id: number;
  name: string;
  grade: string;
  type: string;
  attackType: string;
}

const heroes = charactersData as Hero[];

// 공성전 요일별 보스 (EnemyDb.SiegeBosses)
const DAYS: { key: string; boss: string }[] = [
  { key: "월", boss: "루디" },
  { key: "화", boss: "아일린" },
  { key: "수", boss: "레이첼" },
  { key: "목", boss: "델론즈" },
  { key: "금", boss: "제이브" },
  { key: "토", boss: "스파이크" },
  { key: "일", boss: "크리스" },
];

const GRADES = ["전체", "전설", "희귀"];
const ROLES = ["전체", "공격형", "마법형", "만능형", "방어형", "지원형"];
const MAX_PARTY = 8;
const MIN_PARTY = 5;

export default function SiegePage() {
  const [day, setDay] = useState("토");
  const [grade, setGrade] = useState("전체");
  const [role, setRole] = useState("전체");
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set());
  const [transcend, setTranscend] = useState<Record<number, number>>({});

  const filtered = useMemo(
    () =>
      heroes.filter((h) => {
        if (grade !== "전체" && h.grade !== grade) return false;
        if (role !== "전체" && h.type !== role) return false;
        return true;
      }),
    [grade, role]
  );

  // 선택 순서 유지를 위해 selectedIds를 순회하지 않고 heroes에서 필터
  const selectedHeroes = heroes.filter((h) => selectedIds.has(h.id));
  const boss = DAYS.find((d) => d.key === day)?.boss ?? "";

  const toggleHero = (id: number) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else if (next.size < MAX_PARTY) next.add(id);
      return next;
    });
    setTranscend((prev) => (prev[id] != null ? prev : { ...prev, [id]: 6 }));
  };

  const setHeroTranscend = (id: number, level: number) =>
    setTranscend((prev) => ({ ...prev, [id]: level }));

  return (
    <>
      <h1 className="page-title">공성전</h1>

      {/* ===== 요일 선택 ===== */}
      <div className="filter-block">
        <div className="filter-row">
          <span className="filter-label">요일</span>
          <div className="chip-group">
            {DAYS.map((d) => (
              <button
                key={d.key}
                type="button"
                className={`chip${day === d.key ? " active" : ""}`}
                onClick={() => setDay(d.key)}
              >
                {d.key} · {d.boss}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* ===== Two columns ===== */}
      <div className="columns">
        {/* Left: 영웅 선택 */}
        <section className="panel panel-list">
          <h2 className="panel-title">영웅 선택</h2>
          <p className="panel-subtitle">
            {filtered.length}명 · 선택 {selectedHeroes.length}/{MAX_PARTY}
          </p>

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
          </div>

          <div className="hero-list">
            {filtered.length === 0 ? (
              <div className="hero-list-empty">조건에 맞는 영웅이 없습니다.</div>
            ) : (
              filtered.map((h) => (
                <button
                  key={h.id}
                  type="button"
                  className={`hero-card${selectedIds.has(h.id) ? " selected" : ""}`}
                  onClick={() => toggleHero(h.id)}
                >
                  <div className="hero-card-img">이미지</div>
                  <div className="hero-card-name">{h.name}</div>
                </button>
              ))
            )}
          </div>
        </section>

        {/* Right: 선택 영웅 초월 + 탐색 */}
        <section className="panel">
          <h2 className="panel-title">선택 영웅 · 초월</h2>
          <p className="panel-subtitle">
            {boss} 공성전 · {selectedHeroes.length}명 (최소 {MIN_PARTY}명)
          </p>

          {selectedHeroes.length === 0 ? (
            <div className="hero-list-empty">왼쪽 목록에서 영웅을 선택하세요.</div>
          ) : (
            <div className="siege-selected">
              {selectedHeroes.map((h) => (
                <div key={h.id} className="siege-selected-row">
                  <span className="siege-selected-name">{h.name}</span>
                  <select
                    className="siege-transcend"
                    value={transcend[h.id] ?? 6}
                    onChange={(e) => setHeroTranscend(h.id, Number(e.target.value))}
                  >
                    {Array.from({ length: 13 }, (_, t) => (
                      <option key={t} value={t}>
                        {t}초월
                      </option>
                    ))}
                  </select>
                  <button type="button" className="chip" onClick={() => toggleHero(h.id)}>
                    제거
                  </button>
                </div>
              ))}
            </div>
          )}

          <button
            type="button"
            className="siege-search-btn"
            disabled={selectedHeroes.length < MIN_PARTY}
            onClick={() => alert("탐색 백엔드 연동 예정 (SiegeOptimizer)")}
          >
            {selectedHeroes.length < MIN_PARTY
              ? `탐색 실행 (${MIN_PARTY}명 이상 선택)`
              : "탐색 실행"}
          </button>
        </section>
      </div>
    </>
  );
}
