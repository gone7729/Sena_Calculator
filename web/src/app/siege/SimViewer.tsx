"use client";

import { useState } from "react";
import buildsData from "@/data/siegeBuilds.json";

interface TeamMember {
  id: number;
  name: string;
  role: string;
  transcend: number;
  isBackRow: boolean;
  totalDamage: number;
  damageShare: number;
}

interface SkillStep {
  step: number;
  hero: string;
  skill: string;
}

interface SiegeBuild {
  day: string;
  boss: string;
  score: number;
  formation: string;
  backRow: string[];
  roundsCleared: number;
  totalTurns: number;
  roundScore: Record<string, number>;
  team: TeamMember[];
  gear: string[];
  skillOrder: SkillStep[];
}

// 요일 표시 순서 + 라벨 (번들 키 = 월/화/.../일)
const DAY_ORDER = ["월", "화", "수", "목", "금", "토", "일"];

const builds = buildsData as unknown as Record<string, SiegeBuild>;

// gear[] 텍스트를 영웅 이름별로 그룹화 ("[나타] ..." 접두사 기준)
function groupGearByHero(gear: string[]): Record<string, string[]> {
  const map: Record<string, string[]> = {};
  for (const line of gear) {
    const m = line.match(/^\[([^\]]+)\]/);
    if (!m) continue;
    const name = m[1];
    (map[name] ??= []).push(line.replace(/^\[[^\]]+\]\s*/, ""));
  }
  return map;
}

export default function SimViewer() {
  const available = DAY_ORDER.filter((k) => builds[k]);
  const [selected, setSelected] = useState<string | null>(null);

  const build = selected ? builds[selected] : null;
  const gearByHero = build ? groupGearByHero(build.gear) : {};

  return (
    <>
      <p className="siege-mode-note" style={{ marginTop: 0 }}>
        요일을 선택하면 <b>12초월·잠재3 정식 추천 빌드</b>(사용 영웅·세팅·스킬 순서·예상 점수)를 보여줍니다.
        세팅을 직접 조절하려면 우측 상단 <b>커스텀 뷰어</b>로 전환하세요.
      </p>

      {/* ===== 요일 카드 ===== */}
      <div className="siege-day-cards">
        {available.map((k) => {
          const b = builds[k];
          return (
            <button
              key={k}
              type="button"
              className={`siege-day-card${selected === k ? " active" : ""}`}
              onClick={() => setSelected(k)}
            >
              <span className="siege-day-card-day">{k}</span>
              <span className="siege-day-card-boss">{b.boss}</span>
              <span className="siege-day-card-score">{Math.round(b.score).toLocaleString()}</span>
              <span className="siege-day-card-team">
                {b.team.map((t) => t.name).join(" · ")}
              </span>
            </button>
          );
        })}
      </div>

      {/* ===== 선택 요일 정식 빌드 ===== */}
      {!build ? (
        <div className="hero-list-empty" style={{ marginTop: 8 }}>
          위에서 요일을 선택하면 추천 빌드가 표시됩니다.
        </div>
      ) : (
        <section className="panel siege-result">
          <h2 className="panel-title">
            {selected} · {build.boss} 공성전
          </h2>
          <p className="panel-subtitle">
            12초월·잠재3 정식 빌드 · {build.formation} · 후열 {build.backRow.join(",") || "없음"} ·{" "}
            {build.roundsCleared}R · {build.totalTurns}턴
          </p>

          {/* 총점 + 라운드별 */}
          <div className="siege-score-row">
            <div className="siege-score-total">
              <span className="siege-score-label">예상 점수</span>
              <span className="siege-score-value">{Math.round(build.score).toLocaleString()}</span>
            </div>
            {Object.entries(build.roundScore).map(([r, s]) => (
              <div key={r} className="siege-score-round">
                <span className="siege-score-label">R{r}</span>
                <span className="siege-score-value">{Math.round(s).toLocaleString()}</span>
              </div>
            ))}
          </div>

          {/* 진형 배치 (전열/후열) */}
          <h3 className="siege-result-sub">진형 배치 ({build.formation})</h3>
          <div style={{ display: "flex", flexDirection: "column", gap: 10, margin: "8px 0 18px" }}>
            {[
              { label: "후열", members: build.team.filter((p) => p.isBackRow) },
              { label: "전열", members: build.team.filter((p) => !p.isBackRow) },
            ].map((row) => (
              <div key={row.label} style={{ display: "flex", alignItems: "center", gap: 10 }}>
                <span style={{ width: 34, color: "#9ab", fontSize: 13, flexShrink: 0 }}>{row.label}</span>
                <div style={{ display: "flex", gap: 10, flexWrap: "wrap" }}>
                  {row.members.length === 0 ? (
                    <span style={{ color: "#667", fontSize: 13 }}>없음</span>
                  ) : (
                    row.members.map((p) => (
                      <div
                        key={p.id}
                        style={{
                          minWidth: 86,
                          padding: "8px 10px",
                          borderRadius: 8,
                          textAlign: "center",
                          background: "#1a2230",
                          border: "2px solid #2c3647",
                        }}
                      >
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

          {/* 최적 스킬 순서 */}
          {build.skillOrder.length > 0 && (
            <>
              <h3 className="siege-result-sub">스킬 빌드 — 최적 순서 ({build.skillOrder.length}턴)</h3>
              <div style={{ display: "flex", flexWrap: "wrap", gap: 6, margin: "8px 0 18px" }}>
                {build.skillOrder.map((s) => (
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

          {/* 정배 (팀 구성 + 기여도) */}
          <h3 className="siege-result-sub">정배 · 기여도</h3>
          <div className="siege-party">
            {build.team.map((p) => (
              <div key={p.id} className="siege-party-row">
                <span className="siege-party-name">
                  {p.name}
                  <span className="siege-party-tr">{p.transcend}초월</span>
                </span>
                <div className="siege-party-bar-wrap">
                  <div className="siege-party-bar" style={{ width: `${Math.min(100, p.damageShare)}%` }} />
                </div>
                <span className="siege-party-dmg">
                  {Math.round(p.totalDamage).toLocaleString()} ({p.damageShare.toFixed(1)}%)
                </span>
              </div>
            ))}
          </div>

          {/* 영웅별 세팅 (기어) */}
          <h3 className="siege-result-sub">영웅별 세팅</h3>
          <div className="siege-gear-grid">
            {build.team.map((p) => {
              const lines = gearByHero[p.name] ?? [];
              if (lines.length === 0) return null;
              return (
                <div key={p.id} className="siege-gear-card">
                  <div className="siege-gear-card-name">{p.name}</div>
                  <pre className="siege-gearlog">{lines.join("\n")}</pre>
                </div>
              );
            })}
          </div>
        </section>
      )}
    </>
  );
}
