"""Coordination Dashboard — FastAPI + HTMX

실행:
    python3 scripts/dashboard.py
    또는
    bash scripts/start-dashboard.sh

데이터 소스: coordination/ 파일들 (이미 git 으로 동기화 중)
인증: 외부 노출 시 Cloudflare Access (Zero Trust) 권장

v0.5.1+: DEALOS 하드코딩 제거 — 모든 프로젝트/브랜치/sub 이름은 config.yml 참조.
"""
from __future__ import annotations

import json
import re
import subprocess
from datetime import datetime, timezone, timedelta
from pathlib import Path

import yaml
from fastapi import FastAPI, Request
from fastapi.responses import HTMLResponse, JSONResponse
from fastapi.templating import Jinja2Templates

ROOT = Path(__file__).resolve().parent.parent
COORD = ROOT / "coordination"
TEMPLATES = Jinja2Templates(directory=str(ROOT / "templates"))

# ─── config.yml 로드 ────────────────────────────────────────
_CONFIG_FILE = COORD / "config.yml"


def _load_config() -> dict:
    if not _CONFIG_FILE.exists():
        return {}
    try:
        with _CONFIG_FILE.open(encoding="utf-8") as f:
            return yaml.safe_load(f) or {}
    except Exception:
        return {}


_CFG = _load_config()
PROJECT_NAME = _CFG.get("project", {}).get("name", "project")
WORK_BRANCH = _CFG.get("git", {}).get("work_branch", "main")
HEAD_BRANCH = _CFG.get("head", {}).get("branch", "wt/head")
DASHBOARD_PORT = int(_CFG.get("dashboard", {}).get("port", 8888))
SUB_NAMES = [s.get("name") for s in _CFG.get("subs", []) if s.get("name")]
# 모든 worktree 브랜치 (head + subs)
ALL_BRANCH_NAMES = ["head"] + SUB_NAMES

# PROJECT_ROOT (설치 모드에 따라 다름) — 대시보드는 COORD_ROOT 기준으로 돌지만
# HEAD_LOCK 은 sibling worktree 에 있어서 PROJECT_ROOT parent 계산 필요
_IS_SUBDIR = ROOT.name == ".coord"
PROJECT_ROOT = ROOT.parent if _IS_SUBDIR else ROOT

app = FastAPI(title=f"{PROJECT_NAME} Coordination Dashboard")

KST = timezone(timedelta(hours=9))


def _read_yaml_field(text: str, field: str) -> str | None:
    m = re.search(rf"^\s*-?\s*\*\*{field}\*\*:\s*(.+?)\s*$", text, re.M)
    if not m:
        return None
    val = m.group(1).strip()
    # 주석 (# ...) 제거
    if "#" in val:
        val = val.split("#", 1)[0].strip()
    return val


def _read_simple_field(text: str, field: str) -> str | None:
    m = re.search(rf"^{field}:\s*(.+?)\s*$", text, re.M)
    return m.group(1).strip() if m else None


def _git(args: list[str], cwd: Path = ROOT) -> str:
    try:
        return subprocess.run(
            ["git"] + args, cwd=cwd, capture_output=True, text=True, timeout=10
        ).stdout.strip()
    except Exception:
        return ""


# ─── 데이터 수집 ───────────────────────────────────────────

def collect_inbox() -> list[dict]:
    items = []
    for f in sorted((COORD / "inbox").glob("*.md")):
        if f.name in ("_TEMPLATE.md",) or f.name.startswith("."):
            continue
        text = f.read_text(encoding="utf-8", errors="replace")
        title = text.splitlines()[0].lstrip("# ").replace("Inbox: ", "") if text else f.stem
        items.append({
            "id": _read_yaml_field(text, "id") or f.stem,
            "title": title,
            "status": _read_yaml_field(text, "status") or "?",
            "created": _read_yaml_field(text, "created") or "",
            "priority": _read_yaml_field(text, "priority") or "normal",
            "merge_commit": _read_yaml_field(text, "merge_commit") or "",
        })
    return items


def collect_escalations() -> list[dict]:
    """work_branch 의 escalations + origin/<head_branch> 의 미반영 둘 다."""
    items = []
    seen_ids = set()
    esc_dir = COORD / "escalations"
    if esc_dir.exists():
        for f in sorted(esc_dir.glob("*.md")):
            if f.name.startswith("."): continue
            text = f.read_text(encoding="utf-8", errors="replace")
            esc_id = _read_yaml_field(text, "id") or f.stem
            seen_ids.add(esc_id)
            verdict_match = re.search(r"## 판사 판결\s*\n(.+?)(?:\n##|\n$)", text, re.S)
            ruling = (verdict_match.group(1).strip()[:200] if verdict_match else "")
            items.append({
                "id": esc_id,
                "status": _read_yaml_field(text, "status") or "?",
                "created": _read_yaml_field(text, "created") or "",
                "ruling": ruling,
                "source": WORK_BRANCH,
            })
    # origin/<head_branch> 에만 있는 것
    head_listing = _git(["ls-tree", "-r", f"origin/{HEAD_BRANCH}", "coordination/escalations/"])
    for line in head_listing.splitlines():
        parts = line.split("\t", 1)
        if len(parts) != 2: continue
        path = parts[1]
        name = Path(path).stem
        if name.startswith("_") or name == ".gitkeep": continue
        # head 의 파일에서 id 추출
        content = _git(["show", f"origin/{HEAD_BRANCH}:{path}"])
        eid = _read_yaml_field(content, "id") or name
        if eid in seen_ids: continue
        verdict_match = re.search(r"## 판사 판결\s*\n(.+?)(?:\n##|\n$)", content, re.S)
        items.append({
            "id": eid,
            "status": _read_yaml_field(content, "status") or "?",
            "created": _read_yaml_field(content, "created") or "",
            "ruling": (verdict_match.group(1).strip()[:200] if verdict_match else ""),
            "source": f"{HEAD_BRANCH} only",
        })
    return items


def collect_review_inbox_pending() -> list[dict]:
    """origin/<head_branch> 의 reviewed: false 인 것."""
    items = []
    listing = _git(["ls-tree", "-r", f"origin/{HEAD_BRANCH}", "coordination/review-inbox/"])
    for line in listing.splitlines():
        parts = line.split("\t", 1)
        if len(parts) != 2: continue
        path = parts[1]
        name = Path(path).name
        if name.startswith("_") or name == ".gitkeep": continue
        content = _git(["show", f"origin/{HEAD_BRANCH}:{path}"])
        if re.search(r"^reviewed:\s*false\s*$", content, re.M):
            title = next((ln for ln in content.splitlines() if ln.startswith("# Review:")), name)
            items.append({"file": name, "title": title.lstrip("# ")})
    return items


def collect_active_plan() -> dict | None:
    """가장 최근 plan (status: in_progress 또는 needs-rework)."""
    plans_dir = COORD / "plans"
    if not plans_dir.exists():
        return None
    for f in sorted(plans_dir.glob("*.md"), key=lambda p: p.stat().st_mtime, reverse=True):
        if f.name.startswith("_"): continue
        text = f.read_text(encoding="utf-8", errors="replace")
        status = _read_yaml_field(text, "status") or ""
        if status in ("in_progress", "needs-rework", "blocked"):
            steps = re.findall(r"### Step \d+:.*\n.*?status\*\*:\s*(\w+)", text, re.M)
            done = sum(1 for s in steps if s == "done")
            return {
                "id": _read_yaml_field(text, "inbox id") or f.stem,
                "file": f.name,
                "status": status,
                "step_done": done,
                "step_total": len(steps),
                "percent": int(done * 100 / len(steps)) if steps else 0,
            }
    return None


def _extract_int(text: str | None, default: int = 0) -> int:
    """문자열에서 첫 번째 정수만 추출 (주석/공백 무시)."""
    if not text:
        return default
    m = re.match(r"\s*(\d+)", text)
    return int(m.group(1)) if m else default


def collect_token_status() -> dict:
    bf = COORD / "token-budget.md"
    if not bf.exists():
        return {"error": "token-budget.md 없음"}
    text = bf.read_text(encoding="utf-8", errors="replace")
    limit_m = re.search(r"weekly_limit:\s*(\d+)", text)
    limit = int(limit_m.group(1)) if limit_m else 5_000_000
    used = _extract_int(_read_yaml_field(text, "used_tokens"))
    last_alert = _extract_int(_read_yaml_field(text, "last_alerted_threshold"))
    week_start = _read_yaml_field(text, "week_start_ts") or ""
    # week_start 도 # 또는 깨진 chars 제거
    for marker in ["#", "\uFFFD"]:
        if marker in week_start:
            week_start = week_start.split(marker, 1)[0].strip()
    pct = round(used * 100 / limit, 2) if limit else 0
    next_thresholds = [99,98,97,96,95,90,80,70,60,50,40,30,20,10]
    next_t = next((t for t in reversed(next_thresholds) if t > pct), None)
    # 다음 리셋
    try:
        ws = datetime.fromisoformat(week_start)
        next_reset = ws + timedelta(days=7)
        time_to_reset = next_reset - datetime.now(KST)
        days = time_to_reset.days
        hours = time_to_reset.seconds // 3600
        reset_str = f"{days}일 {hours}시간"
    except Exception:
        reset_str = "?"
    return {
        "used": used,
        "limit": limit,
        "percent": pct,
        "remaining": limit - used,
        "last_alert": last_alert,
        "next_threshold": next_t,
        "to_next_threshold": int(limit * next_t / 100 - used) if next_t else 0,
        "week_start": week_start,
        "time_to_reset": reset_str,
    }


def collect_branches() -> list[dict]:
    items = []
    # config.yml 의 subs + head 를 모두 확인
    for sub in ALL_BRANCH_NAMES:
        ahead = _git(["rev-list", "--count", f"{WORK_BRANCH}..origin/wt/{sub}"]) or "?"
        behind = _git(["rev-list", "--count", f"origin/wt/{sub}..{WORK_BRANCH}"]) or "?"
        latest = _git(["log", f"origin/wt/{sub}", "--oneline", "-1"])
        items.append({
            "name": f"wt/{sub}",
            "ahead": ahead,
            "behind": behind,
            "latest": latest,
        })
    return items


def collect_stop() -> bool:
    return (COORD / "STOP").exists()


def collect_head_lock() -> str | None:
    # head worktree 는 PROJECT_ROOT 의 sibling
    lock = PROJECT_ROOT.parent / f"{PROJECT_NAME}-wt-head" / "coordination" / "HEAD_LOCK"
    if lock.exists():
        try:
            return lock.read_text().strip()
        except Exception:
            return "(존재)"
    return None


def collect_recent_hotfixes(n: int = 3) -> list[dict]:
    hf = COORD / "hotfixes.md"
    if not hf.exists():
        return []
    text = hf.read_text(encoding="utf-8", errors="replace")
    headers = re.findall(r"^### (.+)$", text, re.M)
    return [{"title": h} for h in headers[:n]]


def collect_failed_branches() -> list[str]:
    out = _git(["branch", "-a", "--list", "*failed/*"])
    return [b.strip().lstrip("* ") for b in out.splitlines() if b.strip()][:5]


def aggregate() -> dict:
    inbox = collect_inbox()
    return {
        "project_name": PROJECT_NAME,
        "work_branch": WORK_BRANCH,
        "inbox": inbox,
        "inbox_summary": {
            "pending": sum(1 for i in inbox if i["status"] == "pending"),
            "processing": sum(1 for i in inbox if i["status"] == "processing"),
            "merged": sum(1 for i in inbox if i["status"] == "merged"),
            "total": len(inbox),
        },
        "escalations": collect_escalations(),
        "review_pending": collect_review_inbox_pending(),
        "active_plan": collect_active_plan(),
        "tokens": collect_token_status(),
        "branches": collect_branches(),
        "stop": collect_stop(),
        "head_lock": collect_head_lock(),
        "hotfixes": collect_recent_hotfixes(),
        "failed_branches": collect_failed_branches(),
        "now": datetime.now(KST).isoformat(timespec="seconds"),
    }


# ─── 라우트 ────────────────────────────────────────────────

@app.get("/", response_class=HTMLResponse)
async def index(request: Request):
    return TEMPLATES.TemplateResponse("dashboard.html", {"request": request, "data": aggregate()})


@app.get("/api/status")
async def api_status():
    return JSONResponse(aggregate())


@app.get("/partials/main", response_class=HTMLResponse)
async def partial_main(request: Request):
    """HTMX 가 5초마다 fetch — 본문만 갱신."""
    return TEMPLATES.TemplateResponse("partials/main.html", {"request": request, "data": aggregate()})


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=DASHBOARD_PORT, log_level="info")
