"use client";

import { useState } from "react";
import buildsData from "@/data/siegeBuilds.json";
import HeroIcon from "@/components/HeroIcon";

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
  isAuto?: boolean; // 빔 명시 플랜 밖(자동 로테가 채운) 후반 스텝
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

interface ProfileMeta {
  key: string;
  label: string;
  potential: number;
  exclusive: boolean;
  pets: string[];
}

interface BuildsData {
  default: { profile: string; pet: string };
  profiles: ProfileMeta[];
  // profile.key → pet → dayKey(월/화/...) → build
  builds: Record<string, Record<string, Record<string, SiegeBuild>>>;
}

// 요일 표시 순서 + 라벨 (번들 키 = 월/화/.../일)
const DAY_ORDER = ["월", "화", "수", "목", "금", "토", "일"];

// 펫 버튼 라벨 (펫 키 → 표시). 델로 = 치확/치피("크리"), 리첼 = 약확/약공증, 윈디 = 공%.
const PET_LABEL: Record<string, string> = {
  델로: "델로 · 치확/치피",
  리첼: "리첼 · 약확/약공증",
  윈디: "윈디 · 공%",
};

const data = buildsData as unknown as BuildsData;

// "토요일 공성전 (혹한의 성)" → "혹한의 성"
function castleName(boss: string): string {
  const m = boss.match(/\(([^)]+)\)/);
  return m ? m[1] : boss;
}

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

interface GearItem {
  main: string;
  subs: string[];
}

interface HeroGear {
  setName?: string; // "복수자4"
  weapons: GearItem[];
  armors: GearItem[];
  accessory?: { grade?: string; main?: string; sub?: string; raw: string };
  exclusive?: string; // 전용조율 "탄성×4"
  stats?: string; // 기어스탯(버프전)
}

// 옵티마이저 텍스트 로그를 구조화 (무기/방어구 메인·부옵, 장신구, 전용조율, 기어스탯)
function parseHeroGear(lines: string[]): HeroGear {
  const g: HeroGear = { weapons: [], armors: [] };
  const flat = lines
    .flatMap((l) => l.split("\n"))
    .map((l) => l.trim())
    .filter(Boolean);
  for (const l of flat) {
    if (l.startsWith("세트 선택")) continue; // 세트 후보 비교 로그는 생략
    if (l.startsWith("세트 ")) {
      g.setName = l.slice(3).trim();
      continue;
    }
    let m = l.match(/^(무기|방어구)\d+:\s*메인\s*(.+?)\s*\|\s*부옵\s*(.+)$/);
    if (m) {
      (m[1] === "무기" ? g.weapons : g.armors).push({
        main: prettyOpt(m[2]),
        subs: m[3].split(",").map((s) => prettyOpt(s.trim())),
      });
      continue;
    }
    m = l.match(/^장신구:\s*(.+)$/);
    if (m) {
      const raw = m[1];
      const mm = raw.match(/^(\S*성)?\s*메인\s*(.+?)\s*\/\s*부\s*(.+)$/);
      g.accessory = mm ? { grade: mm[1], main: mm[2], sub: mm[3], raw } : { raw };
      continue;
    }
    m = l.match(/^전용조율:\s*(.+)$/);
    if (m) {
      g.exclusive = m[1];
      continue;
    }
    m = l.match(/^기어스탯\(버프전\):\s*(.+)$/);
    if (m) g.stats = m[1];
  }
  return g;
}

// "치명타확률% 24%" / "공격력% 5" → "치명타확률 24%" / "공격력 5%" (스탯명 %를 수치로 이동)
function prettyOpt(s: string): string {
  const m = s.match(/^(.+?)%\s+([\d.]+)%?$/);
  return m ? `${m[1]} ${m[2]}%` : s;
}

// "복수자4" → "복수자 4세트"
function setLabel(s?: string): string {
  if (!s) return "세트";
  const m = s.match(/^(.+?)(\d)$/);
  return m ? `${m[1]} ${m[2]}세트` : s;
}

// 장비 한 부위 = 한 줄 (부위 라벨 · 메인옵 · 부옵 칩) — 컴팩트 표
function GearRow({
  slot,
  main,
  subs,
  grade,
}: {
  slot: string;
  main: string;
  subs: string[];
  grade?: string;
}) {
  return (
    <div className="gear-row">
      <span className="gear-slot">{slot}</span>
      <span className="gear-main">
        {grade ? <span className="gear-grade">{grade}</span> : null}
        {main}
      </span>
      <span className="gear-subs">
        {subs.length === 0 ? (
          <span className="gear-sub gear-sub-none">-</span>
        ) : (
          subs.map((s, j) => (
            <span key={j} className="gear-sub">
              {s}
            </span>
          ))
        )}
      </span>
    </div>
  );
}

export default function SimViewer() {
  // 웹은 6초월만 표시 (12초월 프로필 숨김)
  const profiles = data.profiles.filter((p) => p.key !== "12초월");
  const profileByKey = (k: string) => profiles.find((p) => p.key === k) ?? profiles[0];

  // 프로필·펫 선택 (디폴트 = 6초월 / 윈디)
  const [profileKey, setProfileKey] = useState<string>(data.default.profile);
  const [pet, setPet] = useState<string>(data.default.pet);

  const profile = profileByKey(profileKey);
  // 펫이 현 프로필에서 없으면 첫 펫으로 폴백 (12초월=윈디만)
  const activePet = profile.pets.includes(pet) ? pet : profile.pets[0];

  const dayBuilds: Record<string, SiegeBuild> = data.builds[profile.key]?.[activePet] ?? {};
  const available = DAY_ORDER.filter((k) => dayBuilds[k]);

  // 기본 선택 = 오늘 요일 (없으면 첫 요일)
  const today = DAY_ORDER[(new Date().getDay() + 6) % 7];
  const [selected, setSelected] = useState<string | null>(
    available.includes(today) ? today : available[0] ?? null
  );
  // 진형 배치에서 선택한 영웅 (요일 전환 시 팀에 없으면 첫 영웅으로 폴백)
  const [heroId, setHeroId] = useState<number | null>(null);

  // 프로필 전환 — 새 프로필에 현 펫이 없으면 윈디(또는 첫 펫)로 보정.
  function changeProfile(key: string) {
    setProfileKey(key);
    const next = profileByKey(key);
    if (!next.pets.includes(pet)) setPet(next.pets.includes("윈디") ? "윈디" : next.pets[0]);
  }

  const selKey = selected && dayBuilds[selected] ? selected : available[0] ?? null;
  const build = selKey ? dayBuilds[selKey] : null;
  const gearByHero = build ? groupGearByHero(build.gear) : {};
  const selHero = build
    ? build.team.find((t) => t.id === heroId) ?? build.team[0] ?? null
    : null;
  const selGear = selHero ? parseHeroGear(gearByHero[selHero.name] ?? []) : null;
  // 최고 딜 기여 영웅(메인 딜러) 강조용
  const topShare = build ? Math.max(0, ...build.team.map((t) => t.damageShare)) : 0;
  const specNote = `${profile.label}·잠재${profile.potential}${profile.exclusive ? "·전용장비" : ""}`;

  return (
    <>
      <p className="siege-mode-note" style={{ marginTop: 0 }}>
        요일을 선택하면 <b>{specNote} 정식 추천 빌드</b>(사용 영웅·세팅·스킬 순서·예상 점수)를 보여줍니다.
        세팅을 직접 조절하려면 우측 상단 <b>커스텀 뷰어</b>로 전환하세요.
      </p>

      {/* ===== 프로필 · 펫 선택 ===== (단일 루트면 숨김) */}
      {(profiles.length > 1 || profile.pets.length > 1) && (
        <div className="siege-spec-bar">
          {profiles.length > 1 && (
            <div className="siege-spec-group">
              <span className="siege-spec-label">스펙</span>
              {profiles.map((p) => (
                <button
                  key={p.key}
                  type="button"
                  className={`siege-spec-btn${profile.key === p.key ? " active" : ""}`}
                  onClick={() => changeProfile(p.key)}
                >
                  {p.label}
                </button>
              ))}
            </div>
          )}
          {profile.pets.length > 1 && (
            <div className="siege-spec-group">
              <span className="siege-spec-label">펫</span>
              {profile.pets.map((p) => (
                <button
                  key={p}
                  type="button"
                  className={`siege-spec-btn${activePet === p ? " active" : ""}`}
                  onClick={() => setPet(p)}
                >
                  {PET_LABEL[p] ?? p}
                </button>
              ))}
            </div>
          )}
        </div>
      )}

      {/* ===== 요일 카드 ===== */}
      <div className="siege-day-cards">
        {available.map((k) => {
          const b = dayBuilds[k];
          return (
            <button
              key={k}
              type="button"
              className={`siege-day-card${selKey === k ? " active" : ""}`}
              onClick={() => {
                setSelected(k);
                setHeroId(null);
              }}
            >
              <span className="siege-day-card-head">
                <span className="siege-day-card-day">{k}</span>
                {k === today && <span className="siege-day-card-today">오늘</span>}
              </span>
              <span className="siege-day-card-boss">{castleName(b.boss)}</span>
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
          {available.length === 0
            ? "이 프로필·펫의 빌드 데이터가 아직 없습니다."
            : "위에서 요일을 선택하면 추천 빌드가 표시됩니다."}
        </div>
      ) : (
        <section className="panel siege-result">
          <h2 className="panel-title">
            {build.boss.replace(/\s*\(.*\)$/, "")} · {castleName(build.boss)}
          </h2>
          <p className="panel-subtitle">
            {specNote} · 펫 {activePet} · {build.formation} · 후열 {build.backRow.join(",") || "없음"} ·{" "}
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

          {/* 최적 스킬 순서 (스킬빌드를 템세팅 위로) */}
          {build.skillOrder.length > 0 && (
            <>
              <h3 className="siege-result-sub">스킬 빌드 — 최적 순서 ({build.skillOrder.length}턴)</h3>
              <div className="siege-skill-list">
                {build.skillOrder.map((s) => (
                  <span
                    key={s.step}
                    className={`siege-skill-chip${s.isAuto ? " auto" : ""}`}
                    title={s.isAuto ? "빔 최적화 범위 밖 — 자동 로테 시전" : undefined}
                  >
                    <span className="siege-skill-step">{s.step}</span>
                    <b className="siege-skill-hero">{s.hero}</b>
                    <span className="siege-skill-name">{s.skill || "홀드"}</span>
                    {s.isAuto && <span className="siege-skill-auto">자동</span>}
                  </span>
                ))}
              </div>
            </>
          )}

          {/* 진형 배치 (전열/후열) + 선택 영웅 세팅 — 템세팅을 아래로 */}
          <h3 className="siege-result-sub">진형 배치 ({build.formation}) · 영웅별 세팅</h3>
          <div className="siege-formation-wrap">
            <div className="siege-formation-grid">
              {[
                { label: "후열", members: build.team.filter((p) => p.isBackRow) },
                { label: "전열", members: build.team.filter((p) => !p.isBackRow) },
              ].map((row) => (
                <div key={row.label} className="siege-formation-row">
                  <span className="siege-formation-label">{row.label}</span>
                  <div className="siege-formation-cards">
                    {row.members.length === 0 ? (
                      <span className="siege-formation-empty">없음</span>
                    ) : (
                      row.members.map((p) => (
                        <button
                          key={p.id}
                          type="button"
                          className={`siege-formation-card${selHero?.id === p.id ? " active" : ""}${
                            topShare > 0 && p.damageShare === topShare ? " top" : ""
                          }`}
                          onClick={() => setHeroId(p.id)}
                        >
                          <HeroIcon id={p.id} name={p.name} />
                          <div className="siege-formation-name">{p.name}</div>
                          <div className="siege-formation-meta">
                            {p.role} · {p.transcend}초월
                          </div>
                          <div className="siege-formation-share">{p.damageShare.toFixed(0)}%</div>
                          <div className="siege-formation-barwrap">
                            <div
                              className="siege-formation-barfill"
                              style={{ width: `${Math.min(100, p.damageShare)}%` }}
                            />
                          </div>
                        </button>
                      ))
                    )}
                  </div>
                </div>
              ))}
            </div>

            {/* 선택 영웅 장비 세팅 */}
            {selHero && selGear && (
              <div className="siege-hero-gear">
                <div className="siege-hero-gear-head">
                  <HeroIcon id={selHero.id} name={selHero.name} className="hero-icon-sm" />
                  <b>{selHero.name}</b>
                  <span className="siege-hero-gear-meta">
                    {selHero.role} · {selHero.transcend}초월
                  </span>
                  {selGear.setName && (
                    <span className="gear-set-badge">{setLabel(selGear.setName)}</span>
                  )}
                  {selGear.stats && <span className="siege-hero-gear-stats">{selGear.stats}</span>}
                </div>

                {selGear.weapons.length === 0 && selGear.armors.length === 0 ? (
                  <div className="siege-formation-empty" style={{ padding: "18px 0" }}>
                    세팅 데이터가 없습니다.
                  </div>
                ) : (
                  <div className="gear-table">
                    <div className="gear-table-head">
                      <span className="gear-slot">부위</span>
                      <span>메인 옵션</span>
                      <span>부 옵션</span>
                    </div>
                    {selGear.weapons.map((w, i) => (
                      <GearRow key={`w${i}`} slot="무기" main={w.main} subs={w.subs} />
                    ))}
                    {selGear.armors.map((a, i) => (
                      <GearRow key={`a${i}`} slot="방어구" main={a.main} subs={a.subs} />
                    ))}
                    {selGear.accessory && (
                      <GearRow
                        slot="장신구"
                        grade={selGear.accessory.grade}
                        main={selGear.accessory.main ?? selGear.accessory.raw ?? "-"}
                        subs={selGear.accessory.sub ? [selGear.accessory.sub] : []}
                      />
                    )}
                    {/* 전용장비는 12초월(전용장비 포함) 프로필에서만 표시 */}
                    {profile.exclusive && selGear.exclusive && (
                      <GearRow
                        slot="전용"
                        main="조율"
                        subs={selGear.exclusive.split("+").map((s) => s.trim())}
                      />
                    )}
                  </div>
                )}
              </div>
            )}
          </div>

        </section>
      )}
    </>
  );
}
