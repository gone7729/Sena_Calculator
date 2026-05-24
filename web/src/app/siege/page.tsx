"use client";

import { useEffect, useMemo, useState } from "react";
import charactersData from "@/data/characters.json";

interface Hero {
  id: number;
  name: string;
  grade: string;
  type: string;
  attackType: string;
}

interface PetInfo {
  id: number;
  name: string;
  rarity: string;
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

// 공성전 백엔드 (.NET SiegeApi). 로컬 개발 기본값, 배포 시 NEXT_PUBLIC_SIEGE_API로 덮어쓰기.
const API_BASE = process.env.NEXT_PUBLIC_SIEGE_API ?? "http://localhost:5179";

interface PartyMember {
  id: number;
  name: string;
  transcend: number;
  position: number;
  totalDamage: number;
  damageShare: number;
}

interface TurnLog {
  turn: number;
  actor: string;
  isAlly: boolean;
  actionType: string;
  skillName: string;
  damage: number;
  description: string;
}

interface OptimizeResult {
  score: number;
  formation: string;
  evaluatedCount: number;
  totalTurns: number;
  roundsCleared: number;
  roundScore: Record<string, number>;
  party: PartyMember[];
  turnLogs: TurnLog[];
}

export default function SiegePage() {
  const [day, setDay] = useState("토");
  const [grade, setGrade] = useState("전체");
  const [role, setRole] = useState("전체");
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set());
  const [transcend, setTranscend] = useState<Record<number, number>>({});
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<OptimizeResult | null>(null);

  // 펫 (검증 시 게임 세팅 그대로 맞추기 위함)
  const [pets, setPets] = useState<PetInfo[]>([]);
  const [petName, setPetName] = useState("");
  const [petStar, setPetStar] = useState(6);
  const [petEnhance, setPetEnhance] = useState(0);
  const [petOpt, setPetOpt] = useState({ atk: 0, def: 0, hp: 0 });

  useEffect(() => {
    fetch(`${API_BASE}/api/siege/pets`)
      .then((r) => (r.ok ? r.json() : []))
      .then((d: PetInfo[]) => setPets(d))
      .catch(() => setPets([]));
  }, []);

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

  const runSearch = async () => {
    setLoading(true);
    setError(null);
    setResult(null);
    try {
      const members = selectedHeroes.map((h) => ({
        id: h.id,
        transcend: transcend[h.id] ?? 6,
      }));
      const pet = petName
        ? {
            name: petName,
            star: petStar,
            enhance: petStar === 6 ? petEnhance : 0,
            optAtkRate: petOpt.atk,
            optDefRate: petOpt.def,
            optHpRate: petOpt.hp,
          }
        : null;
      const res = await fetch(`${API_BASE}/api/siege/optimize`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ day, members, pet }),
      });
      if (!res.ok) {
        const body = await res.json().catch(() => null);
        throw new Error(body?.error ?? `요청 실패 (${res.status})`);
      }
      setResult(await res.json());
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      setError(
        msg.includes("fetch")
          ? `백엔드(${API_BASE})에 연결할 수 없습니다. SiegeApi 서버가 실행 중인지 확인하세요.`
          : msg
      );
    } finally {
      setLoading(false);
    }
  };

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

          {/* ===== 펫 (검증용) ===== */}
          <h3 className="siege-result-sub">펫</h3>
          <div className="siege-pet">
            <div className="siege-pet-row">
              <span className="siege-pet-label">펫</span>
              <select
                className="siege-transcend siege-pet-select"
                value={petName}
                onChange={(e) => setPetName(e.target.value)}
              >
                <option value="">없음</option>
                {pets.map((p) => (
                  <option key={p.id} value={p.name}>
                    {p.name} ({p.rarity})
                  </option>
                ))}
              </select>
            </div>

            {petName && (
              <>
                <div className="siege-pet-row">
                  <span className="siege-pet-label">성급</span>
                  <select
                    className="siege-transcend"
                    value={petStar}
                    onChange={(e) => setPetStar(Number(e.target.value))}
                  >
                    {[4, 5, 6].map((s) => (
                      <option key={s} value={s}>
                        {s}성
                      </option>
                    ))}
                  </select>
                  <span className="siege-pet-label">강화</span>
                  <select
                    className="siege-transcend"
                    value={petStar === 6 ? petEnhance : 0}
                    disabled={petStar !== 6}
                    title={petStar !== 6 ? "강화는 6성에서만 가능" : undefined}
                    onChange={(e) => setPetEnhance(Number(e.target.value))}
                  >
                    {[0, 1, 2, 3].map((n) => (
                      <option key={n} value={n}>
                        {n === 0 ? "강화 없음" : `+${n}`}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="siege-pet-row">
                  <span className="siege-pet-label">옵션%</span>
                  {([
                    ["atk", "공"],
                    ["def", "방"],
                    ["hp", "체"],
                  ] as const).map(([k, lbl]) => (
                    <label key={k} className="siege-pet-opt">
                      {lbl}
                      <input
                        type="number"
                        min={0}
                        value={petOpt[k]}
                        onChange={(e) =>
                          setPetOpt((prev) => ({ ...prev, [k]: Number(e.target.value) }))
                        }
                      />
                    </label>
                  ))}
                </div>
              </>
            )}
          </div>

          <button
            type="button"
            className="siege-search-btn"
            disabled={selectedHeroes.length < MIN_PARTY || loading}
            onClick={runSearch}
          >
            {loading
              ? "탐색 중…"
              : selectedHeroes.length < MIN_PARTY
                ? `탐색 실행 (${MIN_PARTY}명 이상 선택)`
                : "탐색 실행"}
          </button>

          {error && <div className="siege-error">{error}</div>}
        </section>
      </div>

      {/* ===== 탐색 결과 ===== */}
      {result && (
        <section className="panel siege-result">
          <h2 className="panel-title">탐색 결과</h2>
          <p className="panel-subtitle">
            {boss} 공성전 · {result.formation} · {result.evaluatedCount}개 조합 평가 ·{" "}
            {result.roundsCleared}라운드 클리어 · {result.totalTurns}턴
          </p>

          {/* 총점 + 라운드별 */}
          <div className="siege-score-row">
            <div className="siege-score-total">
              <span className="siege-score-label">총 점수</span>
              <span className="siege-score-value">
                {Math.round(result.score).toLocaleString()}
              </span>
            </div>
            {Object.entries(result.roundScore).map(([r, s]) => (
              <div key={r} className="siege-score-round">
                <span className="siege-score-label">R{r}</span>
                <span className="siege-score-value">{Math.round(s).toLocaleString()}</span>
              </div>
            ))}
          </div>

          {/* 정배 (팀 구성 + 기여도) */}
          <h3 className="siege-result-sub">정배 ({result.formation})</h3>
          <div className="siege-party">
            {result.party.map((p) => (
              <div key={p.position} className="siege-party-row">
                <span className="siege-party-pos">{p.position}</span>
                <span className="siege-party-name">
                  {p.name}
                  <span className="siege-party-tr">{p.transcend}초월</span>
                </span>
                <div className="siege-party-bar-wrap">
                  <div
                    className="siege-party-bar"
                    style={{ width: `${Math.min(100, p.damageShare)}%` }}
                  />
                </div>
                <span className="siege-party-dmg">
                  {Math.round(p.totalDamage).toLocaleString()} ({p.damageShare.toFixed(1)}%)
                </span>
              </div>
            ))}
          </div>

          {/* 턴 로그 */}
          <h3 className="siege-result-sub">턴 로그 ({result.turnLogs.length})</h3>
          <div className="siege-log">
            {result.turnLogs.map((t, i) => (
              <div
                key={i}
                className={`siege-log-row${t.isAlly ? "" : " enemy"}`}
              >
                <span className="siege-log-turn">T{t.turn}</span>
                <span className="siege-log-actor">{t.actor}</span>
                <span className="siege-log-desc">
                  {t.skillName && <b>{t.skillName}</b>} {t.description}
                </span>
              </div>
            ))}
          </div>

          <p className="siege-note">
            ※ 템세팅(안정형·고점형)은 장비 옵티마이저 연동 후 제공됩니다.
          </p>
        </section>
      )}
    </>
  );
}
