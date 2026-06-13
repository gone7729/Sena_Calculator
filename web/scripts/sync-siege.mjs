// results/siege/siege_{요일}_{프로필}.json 7개를 web/src/data/siegeBuilds.json 으로 집계한다.
//   사용: node scripts/sync-siege.mjs [프로필]   (기본 프로필: 12초월잠재3)
//   재탐색(SiegeBatchAllDays) 후 한 번 실행하면 웹에 반영된다.
import { readFileSync, writeFileSync, existsSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const profile = process.argv[2] ?? "6초월잠재3";
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

const out = {};
const missing = [];
for (const [key, day] of DAYS) {
  const p = join(srcDir, `siege_${day}_${profile}.json`);
  if (!existsSync(p)) {
    missing.push(day);
    continue;
  }
  out[key] = JSON.parse(readFileSync(p, "utf8").replace(/^﻿/, ""));
}

if (Object.keys(out).length === 0) {
  console.error(`결과 파일이 없습니다: ${srcDir}\\siege_*_${profile}.json`);
  process.exit(1);
}

writeFileSync(outPath, JSON.stringify(out, null, 2) + "\n", "utf8");
console.log(
  `siegeBuilds.json 갱신 완료 (${Object.keys(out).join(",")})` +
    (missing.length ? ` · 누락: ${missing.join(",")}` : "")
);
