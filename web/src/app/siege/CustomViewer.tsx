"use client";

import { useEffect, useMemo, useRef, useState } from "react";
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
const LIST_MIN_HEIGHT = 480; // 영웅 리스트 최소 높이(px) — 우측 패널이 짧아도 리스트가 찌부러지지 않도록

// 펫 시뮬 고정값: 펫 윈디 6성 강화3, 펫 잠재 공옵 72%(18×4). 초월은 전원 2/4/6 3루트로 탐색.
const SIM_PET = { name: "윈디", star: 6, enhance: 3, optAtk: 72 };

// 공성전 백엔드 (.NET SiegeApi). 로컬 개발 기본값, 배포 시 NEXT_PUBLIC_SIEGE_API로 덮어쓰기.
const API_BASE = process.env.NEXT_PUBLIC_SIEGE_API ?? "http://localhost:5179";

interface PartyMember {
  id: number;
  name: string;
  role: string;
  transcend: number;
  position: number;
  totalDamage: number;
  damageShare: number;
  isBackRow: boolean;
  isBuffTarget: boolean;
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

interface SkillStep {
  step: number;
  hero: string;
  skill: string;
}

interface RouteResult {
  transcend: number;
  cached: boolean;
  score: number;
  formation: string;
  evaluatedCount: number;
  totalTurns: number;
  roundsCleared: number;
  roundScore: Record<string, number>;
  gearLog: string[];
  party: PartyMember[];
  skillOrder: SkillStep[];
  turnLogs: TurnLog[];
}

interface RoutesResponse {
  status?: "done" | "running" | "error";
  jobId?: string;
  day: string;
  includeExclusive: boolean;
  routes: Record<string, RouteResult>;
  error?: string;
}

const TRANSCEND_ROUTES = [6];

export default function CustomViewer() {
  const [day, setDay] = useState("토");
  const [grade, setGrade] = useState("전체");
  const [role, setRole] = useState("전체");
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set());
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [routes, setRoutes] = useState<RoutesResponse | null>(null);
  // 표시 중인 초월 루트 탭 (2/4/6)
  const [activeTr, setActiveTr] = useState(6);
  // 전용장비 전체 포함(조율 탐색) / 전체 제외
  const [includeExclusive, setIncludeExclusive] = useState(true);
  // 펫: 시뮬(고정) / 커스텀(직접 설정)
  const [petMode, setPetMode] = useState<"sim" | "custom">("sim");

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

  // 백그라운드 잡 폴링 타이머
  const pollRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  useEffect(() => () => { if (pollRef.current) clearTimeout(pollRef.current); }, []);

  // 우측 패널(선택 영웅·펫·탐색) 높이를 측정해 좌측 리스트 패널 높이를 맞춤 → 리스트 내부 스크롤
  const rightRef = useRef<HTMLElement>(null);
  const [rightHeight, setRightHeight] = useState<number | undefined>();

  useEffect(() => {
    const el = rightRef.current;
    if (!el) return;
    const update = () => setRightHeight(el.offsetHeight);
    update();
    const ro = new ResizeObserver(update);
    ro.observe(el);
    return () => ro.disconnect();
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
  // 현재 활성 초월 루트 결과
  const active = routes ? routes.routes[String(activeTr)] ?? null : null;

  const toggleHero = (id: number) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else if (next.size < MAX_PARTY) next.add(id);
      return next;
    });
  };

  // 백그라운드 잡 폴링 — 완료된 루트가 채워지면 탭이 하나씩 갱신됨
  const pollJob = (jobId: string) => {
    const tick = async () => {
      try {
        const r = await fetch(`${API_BASE}/api/siege/job?id=${encodeURIComponent(jobId)}`);
        if (r.ok) {
          const j = await r.json();
          setRoutes((prev) =>
            prev ? { ...prev, status: j.status, routes: { ...prev.routes, ...(j.routes ?? {}) } } : prev
          );
          if (j.status === "error") {
            setError(j.error ?? "탐색 중 오류가 발생했습니다.");
            return;
          }
          if (j.status === "done") return;
        }
      } catch {
        /* 일시적 네트워크 오류는 무시하고 재시도 */
      }
      pollRef.current = setTimeout(tick, 5000);
    };
    pollRef.current = setTimeout(tick, 4000);
  };

  const runSearch = async () => {
    if (pollRef.current) clearTimeout(pollRef.current);
    setLoading(true);
    setError(null);
    setRoutes(null);
    try {
      const isSim = petMode === "sim";
      const pet = isSim
        ? {
            name: SIM_PET.name,
            star: SIM_PET.star,
            enhance: SIM_PET.enhance,
            optAtkRate: SIM_PET.optAtk,
            optDefRate: 0,
            optHpRate: 0,
          }
        : petName
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
        body: JSON.stringify({
          day,
          heroIds: selectedHeroes.map((h) => h.id),
          includeExclusive,
          pet,
        }),
      });
      if (!res.ok) {
        const body = await res.json().catch(() => null);
        throw new Error(body?.error ?? `요청 실패 (${res.status})`);
      }
      const data: RoutesResponse = await res.json();
      setRoutes(data);
      const keys = Object.keys(data.routes);
      setActiveTr(data.routes["6"] ? 6 : keys.length ? Number(keys[0]) : 6);
      if (data.status === "running" && data.jobId) pollJob(data.jobId);
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
      {/* 펫 세팅 모드 (시뮬 고정 / 커스텀 직접) */}
      <div className="siege-petmode-row">
        <span className="filter-label">펫 세팅</span>
        <div className="siege-mode-toggle">
          <button
            type="button"
            className={`chip${petMode === "sim" ? " active" : ""}`}
            onClick={() => setPetMode("sim")}
            title="펫 윈디 6성 강화3, 잠재 공옵72% 고정 — 전원 6초월 탐색"
          >
            시뮬(고정)
          </button>
          <button
            type="button"
            className={`chip${petMode === "custom" ? " active" : ""}`}
            onClick={() => setPetMode("custom")}
            title="펫을 직접 설정"
          >
            커스텀
          </button>
        </div>
      </div>

      {petMode === "sim" && (
        <p className="siege-mode-note">
          영웅만 선택하면 <b>전원 6초월</b>로 <b>템세팅·스킬순서</b>를 탐색합니다.
          잠재 0/0/0·스킬강화·펫 <b>윈디 6성 강화+3</b> 고정. (목적 = 최적 스킬순서·템세팅, 점수는 따라옴)
        </p>
      )}

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
        <section
          className="panel panel-list"
          style={{ height: Math.max(rightHeight ?? 0, LIST_MIN_HEIGHT) }}
        >
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
        <section className="panel" ref={rightRef}>
          <h2 className="panel-title">{petMode === "custom" ? "선택 영웅 · 펫" : "선택 영웅"}</h2>
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
                  <button type="button" className="chip" onClick={() => toggleHero(h.id)}>
                    제거
                  </button>
                </div>
              ))}
            </div>
          )}

          {/* ===== 펫 ===== */}
          {petMode === "sim" ? (
            <>
              <h3 className="siege-result-sub">펫</h3>
              <div className="siege-pet-fixed">윈디 · 6성 · 강화+3 · 잠재 공격력 72% (고정)</div>
            </>
          ) : (
          <>
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
          </>
          )}

          {/* 전용장비 전체 포함/제외 */}
          <label
            style={{ display: "flex", alignItems: "center", gap: 8, margin: "14px 0 4px", fontSize: 14, cursor: "pointer" }}
            title="포함=전용무기 조율(전설 4슬롯)까지 탐색 / 제외=전용무기 미장착"
          >
            <input
              type="checkbox"
              checked={includeExclusive}
              onChange={(e) => setIncludeExclusive(e.target.checked)}
            />
            전용장비 포함 (조율 탐색)
          </label>

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

      {/* ===== 탐색 결과 (전원 6초월) ===== */}
      {routes && (
        <section className="panel siege-result">
          <h2 className="panel-title">탐색 결과</h2>

          {routes.status === "running" && (
            <div className="siege-mode-note" style={{ marginTop: 0 }}>
              새 영웅 조합이라 탐색 중입니다 — 완료되는 대로 결과가 표시됩니다.
              ({Object.keys(routes.routes).length}/{TRANSCEND_ROUTES.length} 완료, 수 분 소요)
            </div>
          )}

          {/* 초월 루트 탭 — 루트 2개 이상일 때만 (현재 6초월 단일이면 숨김) */}
          {TRANSCEND_ROUTES.length > 1 && (
          <div style={{ display: "flex", gap: 8, margin: "4px 0 14px", flexWrap: "wrap" }}>
            {TRANSCEND_ROUTES.map((t) => {
              const r = routes.routes[String(t)];
              if (!r)
                return (
                  <span key={t} className="chip" style={{ opacity: 0.5, cursor: "default" }}>
                    전원 {t}초월 · 탐색 중…
                  </span>
                );
              return (
                <button
                  key={t}
                  type="button"
                  className={`chip${activeTr === t ? " active" : ""}`}
                  onClick={() => setActiveTr(t)}
                  title={r.cached ? "캐시된 결과" : "이번에 탐색됨"}
                >
                  전원 {t}초월 · {Math.round(r.score).toLocaleString()}
                  {r.cached ? "" : " ●"}
                </button>
              );
            })}
          </div>
          )}

          {active && (
          <>
          <p className="panel-subtitle">
            {boss} 공성전 · 전원 <b>{active.transcend}초월</b> · {active.formation} ·{" "}
            전용장비 {routes.includeExclusive ? "포함" : "제외"} ·{" "}
            {active.cached ? "캐시" : `${active.evaluatedCount}개 평가`} ·{" "}
            {active.roundsCleared}R 클리어 · {active.totalTurns}턴
          </p>

          {/* 총점 + 라운드별 */}
          <div className="siege-score-row">
            <div className="siege-score-total">
              <span className="siege-score-label">총 점수</span>
              <span className="siege-score-value">
                {Math.round(active.score).toLocaleString()}
              </span>
            </div>
            {Object.entries(active.roundScore).map(([r, s]) => (
              <div key={r} className="siege-score-round">
                <span className="siege-score-label">R{r}</span>
                <span className="siege-score-value">{Math.round(s).toLocaleString()}</span>
              </div>
            ))}
          </div>

          {/* 최적 스킬 순서 (핵심) */}
          {active.skillOrder && active.skillOrder.length > 0 && (
            <>
              <h3 className="siege-result-sub">최적 스킬 순서 ({active.skillOrder.length}턴)</h3>
              <div style={{ display: "flex", flexWrap: "wrap", gap: 6, margin: "8px 0 18px" }}>
                {active.skillOrder.map((s) => (
                  <span
                    key={s.step}
                    style={{
                      display: "inline-flex",
                      alignItems: "center",
                      gap: 6,
                      padding: "5px 9px",
                      borderRadius: 8,
                      background: "#1a2230",
                      border: "1px solid #2c3647",
                      fontSize: 13,
                    }}
                  >
                    <span style={{ color: "#667", fontSize: 11 }}>{s.step}</span>
                    <b style={{ color: "#9cd" }}>{s.hero}</b>
                    <span style={{ color: "#d7c45a" }}>{s.skill || "홀드"}</span>
                  </span>
                ))}
              </div>
            </>
          )}

          {/* 진형 배치 (전열/후열 + 버프 수령 영웅 테두리 강조) */}
          <h3 className="siege-result-sub">진형 배치 ({active.formation})</h3>
          <div style={{ display: "flex", flexDirection: "column", gap: 10, margin: "8px 0 18px" }}>
            {[
              { label: "후열", members: active.party.filter((p) => p.isBackRow) },
              { label: "전열", members: active.party.filter((p) => !p.isBackRow) },
            ].map((row) => (
              <div key={row.label} style={{ display: "flex", alignItems: "center", gap: 10 }}>
                <span style={{ width: 34, color: "#9ab", fontSize: 13, flexShrink: 0 }}>{row.label}</span>
                <div style={{ display: "flex", gap: 10, flexWrap: "wrap" }}>
                  {row.members.length === 0 ? (
                    <span style={{ color: "#667", fontSize: 13 }}>없음</span>
                  ) : (
                    row.members.map((p) => (
                      <div
                        key={p.position}
                        title={p.isBuffTarget ? "버프 수령 (비스킷 장비강화·라이언 쿨감)" : undefined}
                        style={{
                          position: "relative",
                          minWidth: 86,
                          padding: "8px 10px",
                          borderRadius: 8,
                          textAlign: "center",
                          background: "#1a2230",
                          border: p.isBuffTarget ? "2px solid #f5c451" : "2px solid #2c3647",
                          boxShadow: p.isBuffTarget ? "0 0 8px rgba(245,196,81,0.45)" : "none",
                        }}
                      >
                        {p.isBuffTarget && (
                          <span
                            style={{
                              position: "absolute",
                              top: -8,
                              right: -6,
                              background: "#f5c451",
                              color: "#1a2230",
                              fontSize: 10,
                              fontWeight: 700,
                              padding: "1px 5px",
                              borderRadius: 6,
                            }}
                          >
                            버프
                          </span>
                        )}
                        <div style={{ fontWeight: 600, fontSize: 14 }}>{p.name}</div>
                        <div style={{ fontSize: 11, color: "#8aa" }}>
                          {p.role} · {p.transcend}초월
                        </div>
                        <div style={{ fontSize: 11, color: "#d7c45a", marginTop: 2 }}>
                          {p.damageShare.toFixed(0)}%
                        </div>
                      </div>
                    ))
                  )}
                </div>
              </div>
            ))}
          </div>

          {/* 정배 (팀 구성 + 기여도) */}
          <h3 className="siege-result-sub">정배 ({active.formation})</h3>
          <div className="siege-party">
            {active.party.map((p) => (
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

          {/* 자동 장착 장비 (메인옵/부옵 값) */}
          {active.gearLog && active.gearLog.length > 0 && (
            <>
              <h3 className="siege-result-sub">자동 장착 장비 (메인옵·부옵 값)</h3>
              <pre className="siege-gearlog">{active.gearLog.join("\n\n")}</pre>
            </>
          )}

          {/* 턴 로그 */}
          <h3 className="siege-result-sub">턴 로그 ({active.turnLogs.length})</h3>
          <div className="siege-log">
            {active.turnLogs.map((t, i) => (
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
          </>
          )}
        </section>
      )}
    </>
  );
}
