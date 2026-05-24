// 세나리 장비 페이지 크롤러 (namu.wiki) — 장신구 등 장비 데이터 덤프
// 사용법: node tools/equip_crawl.js > tools/crawled_txt/_장비.txt
// (siege_crawl.js의 toText 로직 재사용)

const { execSync } = require("child_process");

const UA =
  "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
  "(KHTML, like Gecko) Chrome/120.0 Safari/537.36";

// /w/세븐나이츠 리버스/장비
const PAGE =
  "/w/%EC%84%B8%EB%B8%90%EB%82%98%EC%9D%B4%EC%B8%A0%20%EB%A6%AC%EB%B2%84%EC%8A%A4/%EC%9E%A5%EB%B9%84";

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

console.log(toText(fetchHtml(PAGE)));
