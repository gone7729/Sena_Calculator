// results/siege/siege_{요일}_{초월}초월_{펫}.json 을
//   web/src/data/siegeBuilds.json 으로 집계한다.
//   사용: node scripts/sync-siege.mjs
//   재탐색(SiegeBatchAllDays) 후 한 번 실행.
//
//   ★루트 카탈로그(PROFILES)는 전 루트(6초월 델로/리첼/윈디 · 12초월 윈디)를 그대로 둔다.
//     단, "결과 파일이 실제로 존재하는 루트만" 출력에 포함한다(동적). 따라서:
//       - 지금은 6초월 윈디만 생성돼 있으면 → 6초월 윈디만 뜬다(나머지 토글 자동 숨김).
//       - 나중에 델로/리첼/12초월을 다시 생성해 재실행하면 → 자동으로 다시 나타난다.
//     (델로/리첼/12초월은 폐기가 아니라 "추후 재생성 예정".)
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

// 루트 카탈로그(전 루트). profile.key = 출력파일 접미사의 "{TRANS}초월" 부분.
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
const outProfiles = [];   // 데이터가 있는 루트만 모은다(동적)

for (const p of PROFILES) {
  const petsWithData = [];
  const byPet = {};
  for (const pet of p.pets) {
    const byDay = {};
    for (const [key, day] of DAYS) {
      const file = join(srcDir, `siege_${day}_${p.key}_${pet}.json`);
      if (!existsSync(file)) continue;
      byDay[key] = JSON.parse(readFileSync(file, "utf8").replace(/^﻿/, ""));
      found++;
    }
    if (Object.keys(byDay).length > 0) {
      byPet[pet] = byDay;
      petsWithData.push(pet);
    } else {
      missing.push(`${p.key}/${pet}`);
    }
  }
  if (petsWithData.length > 0) {
    builds[p.key] = byPet;
    outProfiles.push({
      key: p.key,
      label: p.label,
      potential: p.potential,
      exclusive: p.exclusive,
      pets: petsWithData,
    });
  }
}

if (found === 0) {
  console.error(
    `결과 파일이 없습니다: ${srcDir}\\siege_{요일}_{초월}초월_{펫}.json\n` +
      `먼저 SiegeBatchAllDays를 실행하세요 (예: dotnet run --project tools/SiegeBatchAllDays -- 6초월).`
  );
  process.exit(1);
}

// 디폴트 루트가 없으면(아직 미생성) 첫 가용 루트/펫으로 보정.
const def = builds[DEFAULT.profile]?.[DEFAULT.pet]
  ? DEFAULT
  : { profile: outProfiles[0].key, pet: outProfiles[0].pets[0] };

const out = { default: def, profiles: outProfiles, builds };

writeFileSync(outPath, JSON.stringify(out, null, 2) + "\n", "utf8");
console.log(
  `siegeBuilds.json 갱신 완료 (${found}개 빌드, 루트 ${outProfiles
    .map((p) => `${p.key}[${p.pets.join("/")}]`)
    .join(" ")})` + (missing.length ? ` · 미생성 ${missing.join(", ")}` : "")
);
