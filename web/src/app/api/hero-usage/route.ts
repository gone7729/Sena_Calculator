import { promises as fs } from "node:fs";
import path from "node:path";
import { normalizeUsages } from "@/data/usageCategories";

// 영웅 "주 사용처" 저장소 API.
//   저장 대상 = web/src/data/heroUsage.json  { "<heroId>": ["공성전", ...] }
//   서비스 전 데이터 입력용이라 로컬(개발 서버)에서만 쓰기를 허용한다 —
//   배포본에서 임의 수정되지 않게 프로덕션은 읽기 전용.
export const runtime = "nodejs";

const FILE = path.join(process.cwd(), "src", "data", "heroUsage.json");

type UsageMap = Record<string, string[]>;

async function readAll(): Promise<UsageMap> {
  try {
    const raw = await fs.readFile(FILE, "utf8");
    const parsed = JSON.parse(raw);
    return parsed && typeof parsed === "object" ? (parsed as UsageMap) : {};
  } catch {
    return {}; // 파일 없음/파손 → 빈 맵으로 시작
  }
}

export async function GET() {
  return Response.json(await readAll());
}

export async function POST(request: Request) {
  if (process.env.NODE_ENV === "production") {
    return Response.json(
      { error: "프로덕션에서는 편집할 수 없습니다 (개발 서버에서 입력 후 커밋하세요)" },
      { status: 403 }
    );
  }

  let body: unknown;
  try {
    body = await request.json();
  } catch {
    return Response.json({ error: "JSON 파싱 실패" }, { status: 400 });
  }

  const { heroId, usages } = (body ?? {}) as { heroId?: unknown; usages?: unknown };
  const id = typeof heroId === "number" ? heroId : Number(heroId);
  if (!Number.isFinite(id)) {
    return Response.json({ error: "heroId가 필요합니다" }, { status: 400 });
  }

  // 정의된 값만 통과 + 표시 순서로 정규화 (오타·미정의 값이 파일에 쌓이지 않게)
  const next = normalizeUsages(usages);

  const all = await readAll();
  if (next.length === 0) delete all[String(id)]; // 빈 선택은 키 자체를 제거 — 파일을 깨끗하게
  else all[String(id)] = next;

  // 키를 숫자 순으로 정렬해 저장 → diff가 안정적이라 커밋 이력이 읽힌다
  const sorted: UsageMap = {};
  for (const k of Object.keys(all).sort((a, b) => Number(a) - Number(b))) sorted[k] = all[k];

  await fs.writeFile(FILE, JSON.stringify(sorted, null, 2) + "\n", "utf8");
  return Response.json({ ok: true, heroId: id, usages: next });
}
