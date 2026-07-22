// 영웅 "주 사용처" 분류 — 이 파일이 값의 단일 기준이다.
// 항목을 추가/변경하면 영웅 페이지 편집 UI와 저장 검증에 자동 반영된다.
// (저장된 값은 web/src/data/heroUsage.json — 웹 편집 UI가 API로 갱신)

export interface UsageGroup {
  /** 그룹 제목 (편집 UI 소제목) */
  group: string;
  /** 이 그룹의 사용처 값들 — 저장되는 문자열 그대로 */
  items: string[];
}

export const USAGE_GROUPS: UsageGroup[] = [
  { group: "공성전", items: ["공성전"] },
  { group: "모험", items: ["모험/스토리", "성장던전"] },
  // 강림은 보스별로 구분 (EnemyDb.ForestBosses 기준)
  { group: "강림", items: ["강림-태오", "강림-카일", "강림-연희", "강림-카르마"] },
  { group: "PvP", items: ["결투장", "총력전", "길드전"] },
];

/** 전체 사용처 값 (순서 = 표시 순서) */
export const ALL_USAGES: string[] = USAGE_GROUPS.flatMap((g) => g.items);

const USAGE_SET = new Set(ALL_USAGES);

/** 정의된 사용처인지 — 저장 시 검증용 */
export function isValidUsage(v: unknown): v is string {
  return typeof v === "string" && USAGE_SET.has(v);
}

/** 저장 순서를 ALL_USAGES 순으로 정규화 (중복·미정의 값 제거) */
export function normalizeUsages(values: unknown): string[] {
  if (!Array.isArray(values)) return [];
  const picked = new Set(values.filter(isValidUsage));
  return ALL_USAGES.filter((u) => picked.has(u));
}

/** PvP 계열 여부 — 배지 색 구분용 */
export function isPvpUsage(v: string): boolean {
  return USAGE_GROUPS.find((g) => g.group === "PvP")?.items.includes(v) ?? false;
}
