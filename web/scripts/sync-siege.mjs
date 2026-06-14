// results/siege/siege_{요일}_{초월}초월_{펫}.json (4루트 × 7요일)을
//   web/src/data/siegeBuilds.json 으로 집계한다.
//   사용: node scripts/sync-siege.mjs
//   재탐색(SiegeBatchAllDays: 6초월 델로 / 6초월 리첼 / 6초월 윈디 / 12초월[윈디]) 후 한 번 실행.
import { readFileSync, writeFileSync, existsSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const repoRoot = join(dirname(fileURLToPath(import.meta.url)), "..", "..");
const srcDir = join(repoRoot, "results", "siege");
const outPath = join(repoRoot, "web", "src", "data", "siegeBuilds.json");

const DAYS = [
  ["월", "월요일"],
  ["화", "화요일"],
  ["수", "수요일"],
  ["목", "목요일"],
  ["금", "금요일"],
  ["토", "토요일"],
  ["일", "일요일"],
];

// 4개 탐색 루트. profile.key = 출력파일 접미사의 "{TRANS}초월" 부분.
const PROFILES = [
  {
    key: "6초월",
    label: "6초월",
    potential: 0,
    exclusive: false,
    rings: false,
    pets: ["델로", "리첼", "윈디"],
  },
  {
    key: "12초월",
    label: "12초월",
    potential: 3,
    exclusive: true,
    rings: true,
    pets: ["윈디"],
  },
];

const DEFAULT = { profile: "6초월", pet: "윈디" };

const builds = {};
const missing = [];
let found = 0;

for (const p of PROFILES) {
  builds[p.key] = {};
  for (const pet of p.pets) {
    const byDay = {};
    for (const [key, day] of DAYS) {
      const file = join(srcDir, `siege_${day}_${p.key}_${pet}.json`);
      if (!existsSync(file)) {
        missing.push(`${p.key}/${pet}/${day}`);
        continue;
      }
      byDay[key] = JSON.parse(readFileSync(file, "utf8").replace(/^﻿/, ""));
      found++;
    }
    builds[p.key][pet] = byDay;
  }
}

if (found === 0) {
  console.error(
    `결과 파일이 없습니다: ${srcDir}\\siege_{요일}_{초월}초월_{펫}.json\n` +
      `먼저 SiegeBatchAllDays를 4개 루트로 실행하세요 (6초월 델로 / 6초월 리첼 / 6초월 윈디 / 12초월).`
  );
  process.exit(1);
}

const out = {
  default: DEFAULT,
  profiles: PROFILES.map(({ key, label, potential, exclusive, pets }) => ({
    key,
    label,
    potential,
    exclusive,
    pets,
  })),
  builds,
};

writeFileSync(outPath, JSON.stringify(out, null, 2) + "\n", "utf8");
console.log(
  `siegeBuilds.json 갱신 완료 (${found}개 빌드)` +
    (missing.length ? ` · 누락 ${missing.length}: ${missing.join(", ")}` : "")
);
