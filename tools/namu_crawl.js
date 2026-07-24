// 세나리 영웅 위키 크롤러 (namu.wiki)
// 사용법:
//   node tools/namu_crawl.js 백각 베인 "브란즈&브란셀"
//   node tools/namu_crawl.js --list                 # 전체 영웅 목록 출력
// 캐릭터 이름(가나다)을 넘기면 스킬/패시브 텍스트를 stdout으로 출력한다.
// "(세븐나이츠 리버스)" 접미사는 자동으로 붙는다.

const { execSync } = require("child_process");
const fs = require("fs");
const path = require("path");

// 영웅이 아닌 스테이지/지역 (분류에 섞여 있음) — 크롤 제외
const STAGES = new Set([
  "눈보라의 대지", "달빛의 섬", "복수자의 지옥", "신비의 숲", "암흑의 무덤",
  "용의 유적지", "천자의 땅 동쪽", "천자의 땅 서쪽", "침묵의 광산", "화염의 사막",
]);

const UA =
  "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
  "(KHTML, like Gecko) Chrome/120.0 Safari/537.36";

const CATEGORY =
  "/w/%EB%B6%84%EB%A5%98:%EC%84%B8%EB%B8%90%EB%82%98%EC%9D%B4%EC%B8%A0%20%EB%A6%AC%EB%B2%84%EC%8A%A4/%EC%98%81%EC%9B%85?namespace=%EB%AC%B8%EC%84%9C";

const fetchHtml = (path) =>
  execSync(`curl -s -A "${UA}" "https://namu.wiki${path}"`, {
    maxBuffer: 1 << 28,
  }).toString();

function decode(s) {
  return s
    .replace(/&#(\d+);/g, (_, n) => String.fromCharCode(+n))
    .replace(/&#x([0-9a-f]+);/gi, (_, n) => String.fromCharCode(parseInt(n, 16)))
    .replace(/&lt;/g, "<")
    .replace(/&gt;/g, ">")
    .replace(/&quot;/g, '"')
    .replace(/&nbsp;/g, " ")
    .replace(/&apos;/g, "'")
    .replace(/&amp;/g, "&");
}

function toText(h) {
  h = h
    .replace(/<script[\s\S]*?<\/script>/gi, "")
    .replace(/<style[\s\S]*?<\/style>/gi, "");
  h = h
    .replace(/<\/(tr|div|p|h[1-6]|li|table)>/gi, "\n")
    .replace(/<br\s*\/?>/gi, "\n")
    .replace(/<\/td>/gi, " | ")
    .replace(/<[^>]+>/g, "");
  return decode(h).replace(/[ \t]+\n/g, "\n").replace(/\n{3,}/g, "\n\n");
}

// 카테고리(분류) 페이지를 페이지네이션하며 영웅명 -> href 맵을 만든다.
function buildIndex() {
  const base = "https://namu.wiki";
  let url = base + CATEGORY;
  const seen = new Set();
  const map = new Map(); // 정규화된 이름 -> href
  let lastNext = null;
  for (let page = 0; url && page < 20; page++) {
    const h = fetchHtml(url.replace(base, ""));
    const re = /<a href="(\/w\/[^"]+)" title="([^"]+)"/g;
    let m;
    while ((m = re.exec(h))) {
      const href = m[1];
      const title = m[2].replace(/&amp;/g, "&");
      if (/^(분류|틀|템플릿):/.test(title) || seen.has(title)) continue;
      seen.add(title);
      // "이름(세븐나이츠 리버스)/각성" → "이름/각성" 으로 정규화 (각성은 별도 페이지로 존재)
      map.set(title.replace(/\(세븐나이츠 리버스\)/, ""), href);
    }
    const nm = h.match(/<a href="([^"]*cfrom=[^"]*)"/);
    const next = nm ? nm[1].replace(/&amp;/g, "&") : null;
    url = next && next !== lastNext ? base + next.replace(/^https?:\/\/[^/]+/, "") : null;
    lastNext = next;
  }
  return map;
}

// 인포박스/스탯표에서 등급·직업·데미지타입·4초월 스탯을 뽑는다.
function metaBlock(fullText, skillText) {
  const grab = (re) => {
    const m = fullText.match(re);
    return m ? m[1].trim() : "?";
  };
  const grade = grab(/등급\s*\|\s*([^|\n]+)/); // 전설/희귀/...
  const type = grab(/유형\s*\|\s*([^|\n]+)/); // 공격형/마법형/만능형/방어형/지원형 (= DB Type)
  // 4초월 스탯: "(4초월 이상)" 값이 붙는 스탯 행의 라벨
  let tsStat = "?";
  const tm = fullText.match(/([가-힣 ]{2,10})\s*\|\s*\(0~3초월\)/);
  if (tm) tsStat = tm[1].trim();
  // 공격력 속성: 스킬 텍스트의 물리/마법 공격력 (계산용 참고)
  const phys = (skillText.match(/물리 공격력/g) || []).length;
  const magic = (skillText.match(/마법 공격력/g) || []).length;
  const atkAttr = magic > phys ? "마법" : phys > 0 ? "물리" : "?";
  return (
    `[등급] ${grade}\n[타입] ${type}\n[공격속성] ${atkAttr}\n[4초월스탯] ${tsStat}\n`
  );
}

// 각성 페이지는 스킬 구간 끝이 "각성 - <스킬명>" 절로 이어진다.
// (일반 페이지의 skillSection이 "콘텐츠별 평가/진화"에서 끊으므로 각성 절도 함께 포함됨)
const isAwaken = (name) => name.endsWith("/각성");
// 파일명: 각성은 "이름_각성.txt", 일반은 "이름.txt". &, / 등은 _ 로.
const fileNameFor = (name) =>
  (isAwaken(name) ? name.replace(/\/각성$/, "") + "_각성" : name).replace(/[\\/&]/g, "_") + ".txt";

// 본문에서 스킬/패시브(게임 내 성능) 구간만 잘라낸다.
function skillSection(text) {
  let s = text.indexOf("스킬[편집]");
  if (s < 0) s = text.search(/4(\.\d+)?\.\s*스킬/);
  if (s < 0) s = text.indexOf("기본 공격[편집]");
  if (s < 0) s = 0;
  let e = text.indexOf("콘텐츠별 평가[편집]");
  if (e < 0) e = text.indexOf("진화[편집]");
  if (e < 0) e = text.length;
  return text.slice(Math.max(0, s - 200), e > s ? e : text.length).trim();
}

function main() {
  const args = process.argv.slice(2);
  if (args.length === 0 || args[0] === "--help") {
    console.log('사용법: node tools/namu_crawl.js <영웅명...>   |   --list');
    return;
  }
  const index = buildIndex();
  if (args[0] === "--list") {
    [...index.keys()].forEach((n) => console.log(n));
    return;
  }
  // 전체 영웅을 파일로 저장: node tools/namu_crawl.js --all
  if (args[0] === "--all") {
    const outDir = path.join(__dirname, "crawled_txt");
    fs.mkdirSync(outDir, { recursive: true });
    const names = [...index.keys()].filter((n) => !STAGES.has(n));
    const nAwaken = names.filter(isAwaken).length;
    console.log(`전체 ${names.length}건(영웅 ${names.length - nAwaken} + 각성 ${nAwaken}) 크롤링 시작 → ${outDir}`);
    let i = 0;
    for (const name of names) {
      i++;
      const full = toText(fetchHtml(index.get(name)));
      const skill = skillSection(full);
      const tag = isAwaken(name) ? "[각성] O\n" : "";
      const body = `==================== ${name} ====================\n${tag}${metaBlock(full, skill)}\n${skill}\n`;
      fs.writeFileSync(path.join(outDir, fileNameFor(name)), body, "utf8");
      console.log(`[${i}/${names.length}] ${name}`);
    }
    console.log("완료.");
    return;
  }
  for (const name of args) {
    const href = index.get(name);
    console.log("\n==================== " + name + " ====================");
    if (!href) {
      console.log("[오류] 분류 목록에서 찾지 못함. --list 로 정확한 이름 확인.");
      continue;
    }
    const full = toText(fetchHtml(href));
    const skill = skillSection(full);
    console.log(metaBlock(full, skill));
    console.log(skill);
  }
}

main();
