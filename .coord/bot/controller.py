"""coord Discord Bot Controller — Phase 6 MVP

로컬 머신에서 24/7 실행되면서 Discord 메시지를 등록된 프로젝트의 /inbox-send
명령으로 라우팅한다. 완료 알림은 **head 가 직접 Discord webhook 으로 송신**
하는 기존 경로를 유지 (봇은 중복 발송 안 함).

지원 명령:
    @bot <text>                  # default_project 로 /inbox-send
    @bot [project_id] <text>     # 특정 프로젝트로 /inbox-send
    @bot status [project_id]     # HEAD_LOCK + inbox pending 요약
    @bot projects                # 등록 프로젝트 목록
    @bot stop <project_id>       # coordination/STOP 시그널

실행:
    source .env.local            # DISCORD_BOT_TOKEN 로드
    python bot/controller.py     # 또는 bash scripts/start-bot.sh

환경변수:
    DISCORD_BOT_TOKEN   (필수) Discord Developer Portal 의 Bot 토큰
    COORD_BOT_CONFIG    (선택) projects.yml 경로 (기본: bot/projects.yml)
    COORD_BOT_LOG_DIR   (선택) subprocess 출력 저장 디렉토리 (기본: /tmp)

제약 (MVP):
- 봇은 단순 라우터 — plan 생성/검토는 기존 coord 경로가 담당
- 리액션 기반 승인/거부 미지원 (v0.7+ 후보)
- subprocess 출력은 파일로만 보존, Discord 로 back-post 안 함
"""
from __future__ import annotations

import asyncio
import json
import os
import re
import shlex
import shutil
import subprocess
import sys
import tempfile
import time
from dataclasses import dataclass, field
from datetime import datetime, timedelta, timezone
from pathlib import Path
from typing import Any

# Windows cp949 mojibake 방지 — start-bot.sh 미경유 직접 실행 대비
try:
    sys.stdout.reconfigure(encoding="utf-8")  # type: ignore[attr-defined]
    sys.stderr.reconfigure(encoding="utf-8")  # type: ignore[attr-defined]
except Exception:
    pass

import discord
import yaml
from discord.ext import commands

# v0.16 Option C: Anthropic SDK (optional). 없으면 자연어 대화 기능만 비활성.
try:
    import anthropic  # type: ignore[import-not-found]
except ImportError:
    anthropic = None  # type: ignore[assignment]

# ─── 설정 로드 / 저장 ───────────────────────────────────────

BOT_DIR = Path(__file__).resolve().parent
TEMPLATE_ROOT = BOT_DIR.parent
CFG_PATH = Path(os.environ.get("COORD_BOT_CONFIG", BOT_DIR / "projects.yml"))

# module-level registry state — reload 시 갱신
_RAW_CFG: dict[str, Any] = {}         # projects.yml 전체 (주석 제외 raw)
BOT_CFG: dict[str, Any] = {}
PROJECTS: dict[str, dict[str, Any]] = {}
ADMIN_IDS: set[str] = set()
ALLOWED_CHANNELS: set[int] = set()
DEFAULT_PROJECT: str | None = None


def _load_registry() -> None:
    """projects.yml 을 읽어 module state 갱신. 실패 시 기존 state 유지."""
    global _RAW_CFG, BOT_CFG, PROJECTS, ADMIN_IDS, ALLOWED_CHANNELS, DEFAULT_PROJECT
    if not CFG_PATH.exists():
        print(f"❌ {CFG_PATH} 없음 — bot/projects.yml.example 복사 후 편집", file=sys.stderr)
        sys.exit(1)
    with CFG_PATH.open(encoding="utf-8") as f:
        data = yaml.safe_load(f) or {}
    _RAW_CFG = data
    BOT_CFG = data.get("bot", {}) or {}
    PROJECTS = {
        p["id"]: p
        for p in data.get("projects", [])
        if p.get("enabled", True)  # 기본 True (명시적 enabled:false 만 제외)
    }
    ADMIN_IDS = {str(x) for x in BOT_CFG.get("admin_user_ids", []) if str(x).strip()}
    ALLOWED_CHANNELS = {int(x) for x in BOT_CFG.get("allowed_channel_ids", []) if str(x).strip()}
    DEFAULT_PROJECT = BOT_CFG.get("default_project")


def _save_registry() -> None:
    """projects.yml 을 atomic 쓰기 (tmp + rename).
    주의: PyYAML 은 주석 보존 안 함. 봇 명령으로 저장 시 주석 손실됨.
    """
    tmp = CFG_PATH.with_suffix(CFG_PATH.suffix + ".tmp")
    tmp.write_text(
        yaml.dump(_RAW_CFG, allow_unicode=True, sort_keys=False, indent=2),
        encoding="utf-8",
    )
    tmp.replace(CFG_PATH)


_load_registry()

TOKEN = os.environ.get("DISCORD_BOT_TOKEN", "").strip()
if not TOKEN:
    print("❌ DISCORD_BOT_TOKEN 환경변수 없음 — `source .env.local` 후 재실행", file=sys.stderr)
    sys.exit(1)

# v0.16: Anthropic API key (Option C 자연어 대화용, 선택)
ANTHROPIC_API_KEY = os.environ.get("ANTHROPIC_API_KEY", "").strip()
_ANTHROPIC_CLIENT: Any = None
if ANTHROPIC_API_KEY and anthropic is not None:
    try:
        _ANTHROPIC_CLIENT = anthropic.Anthropic(api_key=ANTHROPIC_API_KEY)
        print(f"[bot] ✅ Anthropic SDK 활성화 (Option C 자연어 대화 가능)", flush=True)
        print(
            f"[bot] ℹ Claude CLI spawn 은 ANTHROPIC_API_KEY 제거 후 실행 → "
            f"Max/Pro 구독 OAuth 사용 (v0.16.6)",
            flush=True,
        )
    except Exception as e:
        print(f"[bot] ⚠ Anthropic SDK 초기화 실패: {e}", file=sys.stderr, flush=True)
        _ANTHROPIC_CLIENT = None
elif not ANTHROPIC_API_KEY:
    print("[bot] ℹ ANTHROPIC_API_KEY 없음 — Option C 자연어 대화 비활성", flush=True)
elif anthropic is None:
    print("[bot] ⚠ anthropic 패키지 미설치 — `pip install anthropic` 후 재시작", flush=True)

# 로그 디렉토리 — Git Bash 의 /tmp 와 Windows Python 의 /tmp 가 다른 위치를 가리키는
# 문제 때문에 플랫폼 기본 tempdir 사용 (tempfile.gettempdir() 은 양쪽에서 동일).
LOG_DIR = Path(os.environ.get("COORD_BOT_LOG_DIR", tempfile.gettempdir()))
LOG_DIR.mkdir(parents=True, exist_ok=True)

# claude CLI 풀 경로 사전 해결 (Windows .cmd 확장자 + PATH 조회 대응)
CLAUDE_BIN = os.environ.get("COORD_CLAUDE_BIN") or shutil.which("claude")
if not CLAUDE_BIN:
    print("❌ `claude` CLI 를 PATH 에서 찾을 수 없음 — 설치 확인 또는 COORD_CLAUDE_BIN 환경변수 지정", file=sys.stderr)
    sys.exit(1)


def _resolve_claude_invocation() -> list[str]:
    """v0.16.5 + v0.28: Windows `claude.cmd` wrapper 우회.

    cmd.exe 를 통해 `.cmd` 를 실행하면 **명령줄 인자에 포함된 개행이 잘림** —
    multi-line prompt (에러 스택 등) 의 첫 줄 이후가 소실되고 flag 들도 함께 잘림.

    wrapper 두 패턴 지원:
    - **v0.28 새 claude-code (native binary)**: `"...\bin\claude.exe" %*` → claude.exe 직접 subprocess
    - **v0.16.5 구 claude-code (node + cli.js)**: `"...\cli.js" %*` → node + js 호출

    Popen 이 CreateProcess 로 실행 → cmd.exe 안 타므로 개행 보존.
    매칭 실패 시 기존 CLAUDE_BIN fallback (cmd.exe 개행 버그 재발 가능).
    """
    if os.name != "nt":
        return [CLAUDE_BIN]
    claude_path = Path(CLAUDE_BIN)
    if claude_path.suffix.lower() not in (".cmd", ".bat"):
        return [CLAUDE_BIN]
    try:
        content = claude_path.read_text(encoding="utf-8", errors="ignore")
    except Exception as e:
        print(f"[bot] claude.cmd 읽기 실패 ({e}) — fallback", flush=True)
        return [CLAUDE_BIN]

    wrapper_dir = str(claude_path.parent)

    def _resolve_path(raw: str) -> Path:
        resolved = (
            raw.replace("%dp0%", wrapper_dir + "\\")
               .replace("%~dp0", wrapper_dir + "\\")
               .replace("\\\\", "\\")
        )
        return Path(resolved)

    # v0.28: 새 패턴 먼저 — native .exe binary (최신 claude-code)
    m_exe = re.search(r'"([^"]+\.exe)"\s+%\*', content)
    if m_exe:
        exe_path = _resolve_path(m_exe.group(1))
        if exe_path.exists():
            print(
                f"[bot] ✅ Windows cmd.exe 우회 — {exe_path.name} native binary 직접 호출 (v0.28)",
                flush=True,
            )
            return [str(exe_path)]
        else:
            print(f"[bot] exe 경로 존재 안 함 ({exe_path})", flush=True)

    # v0.16.5: 구 패턴 — node + cli.js
    m_js = re.search(r'"([^"]+\.js)"\s+%\*', content)
    if m_js:
        script_path = _resolve_path(m_js.group(1))
        if not script_path.exists():
            print(f"[bot] cli.js 경로 존재 안 함 ({script_path})", flush=True)
        else:
            node_bin = shutil.which("node")
            if not node_bin:
                print("[bot] node 바이너리 없음", flush=True)
            else:
                print(
                    f"[bot] ✅ Windows cmd.exe 우회 — node + {script_path.name} 호출 (v0.16.5 구조)",
                    flush=True,
                )
                return [node_bin, str(script_path)]

    print(
        "[bot] ⚠ claude.cmd 우회 패턴 매칭 실패 — fallback (cmd.exe 개행 버그 가능)",
        flush=True,
    )
    return [CLAUDE_BIN]


CLAUDE_INVOCATION = _resolve_claude_invocation()


def _claude_cli_env() -> dict[str, str]:
    """v0.16.6: Claude CLI spawn 용 환경변수 — ANTHROPIC_API_KEY 제거.

    봇은 Python SDK (Option C 자연어 대화) 에 ANTHROPIC_API_KEY 가 필요하지만,
    spawn 되는 Claude CLI 는 저장된 OAuth 자격증명 (Max / Pro 구독) 을 써야 함.
    환경변수 그대로 상속하면 CLI 가 API 과금 모드로 전환돼 구독 한도 대신 pay-per-token
    요금이 과금됨 (크레딧 없으면 "Credit balance is too low" 즉시 거부).
    """
    env = os.environ.copy()
    env.pop("ANTHROPIC_API_KEY", None)
    env.pop("ANTHROPIC_AUTH_TOKEN", None)  # 혹시 모르니 이것도
    return env

# ─── Discord bot ───────────────────────────────────────────

intents = discord.Intents.default()
intents.message_content = True   # Privileged Intent — Developer Portal 에서 활성화 필수

bot = commands.Bot(command_prefix=commands.when_mentioned, intents=intents, help_command=None)


def _authorized(user_id: int) -> bool:
    # admin_ids 비우면 전원 허용 (비공개 서버 가정)
    if not ADMIN_IDS:
        return True
    return str(user_id) in ADMIN_IDS


def _binding_channel_id(ch: Any) -> int:
    """Thread 면 parent channel id, 일반 TextChannel 은 본인 id.
    bindings / allowed_channel_ids 는 parent 기준이므로 thread 내부에서도 정상 작동.
    """
    if isinstance(ch, discord.Thread):
        return ch.parent_id or ch.id
    return getattr(ch, "id", 0)


def _channel_ok(channel_id: int, project: dict[str, Any] | None) -> bool:
    if ALLOWED_CHANNELS and channel_id not in ALLOWED_CHANNELS:
        return False
    if project is not None:
        pc = project.get("discord_channel_id")
        ac = project.get("discord_alert_channel_id")
        bound_ids = [x for x in (pc, ac) if x and str(x).strip()]
        if bound_ids and not any(int(x) == channel_id for x in bound_ids):
            return False
    return True


def _project_channel_match(p: dict[str, Any], channel_id: int) -> bool:
    """프로젝트가 해당 channel_id 와 바인딩됐는지 — discord_channel_id (대화방) 또는
    discord_alert_channel_id (작업장) 둘 중 하나라도 매칭되면 True."""
    for key in ("discord_channel_id", "discord_alert_channel_id"):
        val = p.get(key)
        if val and str(val).strip() == str(channel_id):
            return True
    return False


def _resolve_project(
    channel_id: int | None = None, token: str | None = None
) -> tuple[str | None, dict[str, Any] | None]:
    """우선순위: (1) 명시적 project id → (2) 채널 바인딩 (대화방/작업장 모두) → (3) default_project."""
    # 1. 사용자가 명시적으로 project id 지정
    if token and token in PROJECTS:
        return token, PROJECTS[token]
    # 2. 현재 채널 바인딩 (v0.7+, v0.11+: alert 채널도 포함)
    if channel_id is not None:
        for pid, p in PROJECTS.items():
            if _project_channel_match(p, channel_id):
                return pid, p
    # 3. default_project fallback
    if DEFAULT_PROJECT and DEFAULT_PROJECT in PROJECTS:
        return DEFAULT_PROJECT, PROJECTS[DEFAULT_PROJECT]
    return None, None


def _coord_installed(path: Path) -> bool:
    """flat 또는 subdir 모드 coord 설치 여부 감지."""
    return (path / "coordination" / "config.yml").exists() \
        or (path / ".coord" / "coordination" / "config.yml").exists()


def _coord_root(project_path: Path) -> Path:
    """flat / subdir 모드 자동 감지 → COORD_ROOT 반환."""
    if (project_path / ".coord" / "coordination").is_dir():
        return project_path / ".coord"
    return project_path


# ─── subprocess 헬퍼 ──────────────────────────────────────

async def _spawn_claude_command(
    project_id: str,
    project_path: Path,
    claude_prompt: str,
    *,
    cwd: Path | None = None,
    check_head_lock: bool = True,
    label: str | None = None,
    model: str | None = None,
) -> tuple[bool, str]:
    """`claude -p <prompt>` 백그라운드 실행 공용 헬퍼.

    Args:
      project_id: 레지스트리 식별자 (로그 파일명 + 사용자 메시지용).
      project_path: PROJECT_ROOT (`/inbox-send` 등 op-side 명령 기본 cwd).
      claude_prompt: Claude 에 보낼 첫 메시지 (슬래시 명령 포함).
      cwd: 실행 디렉토리 override (head worktree 등 다른 위치 필요 시).
      check_head_lock: HEAD_LOCK 체크 여부 (head-scoped 명령은 True, op 는 False 선택 가능).
      label: 로그 파일명/응답 메시지에 쓰는 짧은 라벨 (기본: prompt 첫 단어).
      model: --model 플래그 override.
    """
    run_cwd = cwd or project_path
    coord = _coord_root(project_path)

    if check_head_lock:
        lock_file = coord / "coordination" / "HEAD_LOCK"
        if lock_file.exists():
            try:
                ts = lock_file.read_text(encoding="utf-8").strip()
            except Exception:
                ts = "(read failed)"
            return False, f"⚠ `{project_id}` head 이미 실행 중 (시작: {ts}) — 완료 후 재시도"

    # 라벨 = prompt 의 슬래시 명령 이름 (예: /inbox-send → inbox-send)
    if label is None:
        first_tok = claude_prompt.strip().split()[0] if claude_prompt.strip() else "cmd"
        label = first_tok.lstrip("/") or "cmd"

    ts = datetime.now(timezone.utc).strftime("%Y%m%d-%H%M%S")
    log_path = LOG_DIR / f"bot-{project_id}-{label}-{ts}.log"

    cmd = [
        *CLAUDE_INVOCATION, "-p", claude_prompt,
        "--permission-mode", "bypassPermissions",
        "--output-format", "json",
    ]
    if model:
        cmd += ["--model", model]

    # v0.16.6: spawn 되는 Claude CLI 가 Max 구독 OAuth 쓰도록 ANTHROPIC_API_KEY 제거.
    # (봇 자체는 Python SDK 로 이 키 사용 — Option C 자연어 대화. 하지만 CLI 로 상속
    # 되면 CLI 가 API 과금 모드로 전환돼 크레딧 필요해짐. Max 구독자면 불필요 과금.)
    spawn_env = _claude_cli_env()

    try:
        log_fh = open(log_path, "w", encoding="utf-8")
        subprocess.Popen(
            cmd,
            cwd=str(run_cwd),
            stdout=log_fh,
            stderr=subprocess.STDOUT,
            stdin=subprocess.DEVNULL,
            env=spawn_env,
        )
        return True, f"✅ `{project_id}` {label} spawn\nlog: `{log_path}`"
    except FileNotFoundError:
        return False, f"❌ `{CLAUDE_BIN}` 실행 실패 (파일 없음)"
    except Exception as e:
        return False, f"❌ spawn 실패: {e}"


async def _spawn_inbox_send(project_id: str, project_path: Path, text: str) -> tuple[bool, str]:
    """op-side `/inbox-send` — project root 에서 실행, HEAD_LOCK 체크."""
    return await _spawn_claude_command(
        project_id, project_path,
        f"/inbox-send {text}",
        label="inbox-send",
    )


async def _spawn_head_retry(project_id: str, project_path: Path, inbox_id: str) -> tuple[bool, str]:
    """head-side `/retry <inbox-id>` — head worktree 에서 실행."""
    head_wt = project_path.parent / f"{project_id}-wt-head"
    if not head_wt.is_dir():
        return False, f"⚠ head worktree 없음: `{head_wt}` — `setup-worktree.sh head` 먼저 실행"
    return await _spawn_claude_command(
        project_id, project_path,
        f"/retry {inbox_id}",
        cwd=head_wt,
        label=f"retry-{inbox_id}",
    )


async def _spawn_escalation_response(
    project_id: str, project_path: Path, action: str, esc_id: str, reason: str
) -> tuple[bool, str]:
    """op-side `/resolve-escalation` 또는 `/reject-escalation`.
    HEAD_LOCK 체크 안 함 (escalation 응답은 락 무관하게 긴급 처리)."""
    assert action in ("resolve", "reject")
    return await _spawn_claude_command(
        project_id, project_path,
        f"/{action}-escalation {esc_id} {reason}",
        check_head_lock=False,
        label=f"{action}-esc-{esc_id}",
    )


# ─── 프로젝트별 inbox 큐 (v0.10) ───────────────────────────
# HEAD_LOCK 으로 직렬화되는 head 실행을 큐로 래핑. 동시 @bot <text> 들어와도
# 순서대로 자동 spawn — 사용자가 "busy" 거부 응답을 보는 일 없음.

@dataclass
class _QueueJob:
    project_id: str
    project_path: Path
    prompt: str                     # "/inbox-send <text>"
    source_message: discord.Message  # reply 대상 (원본 사용자 메시지)
    preview: str                    # 큐 표시용 본문 요약
    enqueued_at: datetime = field(default_factory=lambda: datetime.now(timezone.utc))


_project_queues: dict[str, asyncio.Queue] = {}
_project_workers: dict[str, asyncio.Task] = {}

# 튜닝 상수
_QUEUE_WORKER_IDLE_TIMEOUT_SEC = 600   # 10분 idle 시 worker 종료
_QUEUE_HEAD_LOCK_WAIT_TIMEOUT_SEC = 1800  # 30분 HEAD_LOCK 대기 한도
# v0.45: Phase A (head spawn 대기) timeout 180s → 900s. head 가 plan 작성하는 데 3분
# 이상 걸리는 경우 false "수동 확인 필요" 가 자주 떴음. 기본 15분 + 환경변수로 override 가능.
_PHASE_A_HEAD_SPAWN_TIMEOUT_SEC = int(os.environ.get("COORD_HEAD_SPAWN_TIMEOUT_SEC", "900"))
_PHASE_B_HEAD_COMPLETE_TIMEOUT_SEC = int(os.environ.get("COORD_HEAD_COMPLETE_TIMEOUT_SEC", "3600"))
_QUEUE_HEAD_LOCK_POLL_INTERVAL_SEC = 10
# v0.16.7: heartbeat 은 주기 반복이 아닌 **마일스톤** 방식 — 10/30/60분 경과 시 1회씩만.
# 짧은 작업엔 메시지 0개, 긴 작업에도 총 최대 3개 → 채널 깨끗.
_V016_7_HEARTBEAT_MILESTONES_MIN: list[int] = [10, 30, 60]
_V016_INBOX_CREATION_WAIT_SEC = 60        # v0.16.4: /inbox-send spawn 후 inbox 파일 생성 대기 최대 (모호 판정 감지)
_V016_INBOX_CREATION_POLL_SEC = 3         # v0.16.4: inbox 생성 poll 간격


def _head_coord_dir(project_id: str, project_path: Path) -> Path:
    """head worktree 의 coordination 디렉토리 (flat/subdir 자동 감지, v0.43+).
    v0.42 subtree 설치본에서 head worktree 가 .coord/coordination 경로를 가지는데
    기존 코드가 head_wt / "coordination" 만 쓴 탓에 HEAD_LOCK / plans / review-inbox
    전부 못 읽던 버그 수정.
    """
    head_wt = project_path.parent / f"{project_id}-wt-head"
    return _coord_root(head_wt) / "coordination"


def _head_lock_path(project_id: str, project_path: Path) -> Path:
    """head worktree 의 HEAD_LOCK 경로 (v0.43+: subdir 모드 자동 대응)."""
    return _head_coord_dir(project_id, project_path) / "HEAD_LOCK"


async def _wait_for_file_state(path: Path, want_exists: bool, timeout: int, poll: int = 10) -> bool:
    """path 가 want_exists 상태가 될 때까지 대기. True = 도달, False = timeout."""
    waited = 0
    while path.exists() != want_exists and waited < timeout:
        await asyncio.sleep(poll)
        waited += poll
    return path.exists() == want_exists


async def _wait_head_lock_clear(coord_or_lock: Path, timeout: int) -> bool:
    """HEAD_LOCK 파일 clear 까지 대기. True = clear, False = timeout.
    coord_or_lock 은 coord 디렉토리 또는 이미 HEAD_LOCK 파일 경로.
    """
    if coord_or_lock.name == "HEAD_LOCK":
        lock_file = coord_or_lock
    else:
        lock_file = coord_or_lock / "coordination" / "HEAD_LOCK"
    return await _wait_for_file_state(
        lock_file, want_exists=False,
        timeout=timeout,
        poll=_QUEUE_HEAD_LOCK_POLL_INTERVAL_SEC,
    )


def _latest_inbox_id(project_path: Path) -> str | None:
    """coordination/inbox/ 에서 mtime 가장 최근 파일의 stem 반환."""
    coord = _coord_root(project_path)
    inbox_dir = coord / "coordination" / "inbox"
    if not inbox_dir.exists():
        return None
    candidates = [
        f for f in inbox_dir.glob("*.md")
        if not f.name.startswith(("_", "."))
    ]
    if not candidates:
        return None
    latest = max(candidates, key=lambda p: p.stat().st_mtime)
    return latest.stem


# v0.17: Discord 첨부 → coordination/inbox/attachments/ 저장 --------------

_V017_ALLOWED_ATTACHMENT_MIME_PREFIXES = ("image/",)         # 우선 이미지만
_V017_MAX_ATTACHMENT_BYTES = 25 * 1024 * 1024                 # Discord 기본 25MB


def _sanitize_filename(name: str) -> str:
    """파일명을 path traversal 없는 안전한 형태로."""
    # path separator 및 특수문자 치환
    safe = re.sub(r"[^A-Za-z0-9._\-가-힣]+", "_", name)
    return safe[:80] or "attachment"


async def _download_attachments(
    message: discord.Message,
    project_id: str,
    project_path: Path,
) -> tuple[list[str], list[Path]]:
    """Discord 메시지의 이미지 첨부를 coordination/inbox/attachments/ 에 저장.

    반환: (markdown 줄 목록, 저장된 파일 경로 목록).
    이미지 외 확장자 / 용량 초과 / 다운로드 실패는 조용히 skip 하고 계속 진행.
    """
    if not message.attachments:
        return [], []

    coord = _coord_root(project_path)
    save_dir = coord / "coordination" / "inbox" / "attachments"
    save_dir.mkdir(parents=True, exist_ok=True)

    saved: list[Path] = []
    lines: list[str] = []
    ts = datetime.now(timezone.utc).strftime("%Y%m%d-%H%M%S")

    for i, att in enumerate(message.attachments):
        # MIME 필터
        ctype = (att.content_type or "").lower()
        if not any(ctype.startswith(p) for p in _V017_ALLOWED_ATTACHMENT_MIME_PREFIXES):
            print(
                f"[attach] `{project_id}` skip non-image: {att.filename} ({ctype})",
                flush=True,
            )
            continue
        # 용량 체크
        if att.size and att.size > _V017_MAX_ATTACHMENT_BYTES:
            print(
                f"[attach] `{project_id}` skip too large: {att.filename} ({att.size} bytes)",
                flush=True,
            )
            continue

        # 파일명 생성: <ts>-<i>-<sanitized_name>
        safe_name = _sanitize_filename(att.filename or "attachment")
        fname = f"{ts}-{i}-{safe_name}"
        save_path = save_dir / fname
        try:
            await att.save(save_path)
        except Exception as e:
            print(f"[attach] `{project_id}` download 실패 {att.filename}: {e}", flush=True)
            continue

        saved.append(save_path)
        # inbox 본문에 들어갈 markdown — relative path (project root 기준)
        try:
            rel = save_path.relative_to(project_path)
        except ValueError:
            rel = save_path.relative_to(coord) if coord != project_path else save_path
        rel_str = str(rel).replace("\\", "/")
        lines.append(f"- [{att.filename}]({rel_str})")

    return lines, saved


# v0.19: plan / review-inbox 요약 파싱 helpers --------------------------------

_V019_PLAN_POLL_TIMEOUT_SEC = 120
_V019_PLAN_POLL_INTERVAL_SEC = 5


def _plan_path(project_id: str, project_path: Path, inbox_id: str) -> Path:
    """head worktree 의 plans/<inbox-id>.md 경로 (v0.43+: subdir 모드 자동 대응)."""
    return _head_coord_dir(project_id, project_path) / "plans" / f"{inbox_id}.md"


def _parse_plan_summary(plan_path: Path) -> dict | None:
    """plans/<id>.md 파싱 → 요약 필드 dict. 실패/부분 매칭도 best-effort."""
    if not plan_path.exists():
        return None
    try:
        text = plan_path.read_text(encoding="utf-8", errors="replace")
    except Exception:
        return None

    title_m = re.search(r"^#\s+Plan:\s*(.+)$", text, re.MULTILINE)
    title = title_m.group(1).strip() if title_m else plan_path.stem

    # Step 헤더: "### Step 1: 제목 [wt/backend]" 형식
    step_matches = re.findall(
        r"^###\s+Step\s+(\d+):\s*(.+?)(?:\s*\[wt/([\w-]+)\])?\s*$",
        text,
        re.MULTILINE,
    )
    subs = sorted({m[2] for m in step_matches if m[2]})
    steps_preview = [
        f"{sn}. {stitle.strip()[:50]}" + (f" `wt/{sub}`" if sub else "")
        for sn, stitle, sub in step_matches[:5]
    ]

    return {
        "title": title,
        "step_count": len(step_matches),
        "subs": subs,
        "steps_preview": steps_preview,
    }


async def _wait_for_plan_file(
    project_id: str,
    project_path: Path,
    inbox_id: str,
    timeout: int = _V019_PLAN_POLL_TIMEOUT_SEC,
    poll: int = _V019_PLAN_POLL_INTERVAL_SEC,
) -> Path | None:
    """plans/<inbox-id>.md 생성 대기 (head 가 plan 작성 중). 생기면 경로 반환."""
    plan_path = _plan_path(project_id, project_path, inbox_id)
    deadline = time.time() + timeout
    while time.time() < deadline:
        if plan_path.exists():
            return plan_path
        await asyncio.sleep(poll)
    return None


def _parse_review_sections(review_path: Path) -> dict:
    """review-inbox 파일에서 주요 섹션 텍스트 추출. 실패 시 빈 dict.

    section body 는 다음 **같거나 상위 레벨** 헤더까지 (하위 서브섹션은 포함).
    """
    if not review_path.exists():
        return {}
    try:
        text = review_path.read_text(encoding="utf-8", errors="replace")
    except Exception:
        return {}

    def _extract(header_re: str, level: int, max_chars: int = 500) -> str:
        # level 이하 (더 얕은) 헤더에서 멈춤. 예: level=2 → # 또는 ## 에서 멈춤 (### 는 포함)
        stop_alts = "|".join("#" * i + r"\s" for i in range(1, level + 1))
        pattern = header_re + r"[^\n]*\n(.*?)(?=\n(?:" + stop_alts + r")|\Z)"
        m = re.search(pattern, text, re.DOTALL | re.MULTILINE)
        if not m:
            return ""
        body = m.group(1).strip()
        return body[:max_chars] + ("…" if len(body) > max_chars else "")

    return {
        "change_summary": _extract(r"^##\s+변경\s+요약", level=2),
        "findings": _extract(r"^###\s+발견\s+사항", level=3),
        "verdict_reason": _extract(r"^###\s+verdict\s+근거", level=3),
    }


# v0.18.1: verdict 패턴 — markdown 리스트(- ) + 볼드(**) 감싸기까지 허용.
# 예상 매칭 형식:
#   verdict: go
#   - verdict: go
#   - **verdict**: go
#   **verdict**: needs-fix
#   -   **verdict** : block    (공백 변주)
_VERDICT_RE = re.compile(
    r"^\s*(?:-\s+)?\*{0,2}verdict\*{0,2}\s*:\s*(\w[\w-]*)",
    re.IGNORECASE | re.MULTILINE,
)


def _scan_review_dir_for_verdict(
    review_dir: Path, inbox_id: str
) -> tuple[str | None, str | None]:
    """review-inbox 디렉토리 1곳에서 verdict 스캔. 내부 helper."""
    if not review_dir.exists():
        return None, None
    for rf in sorted(review_dir.glob("*.md"), key=lambda p: p.stat().st_mtime, reverse=True):
        if rf.name.startswith(("_", ".")):
            continue
        try:
            text = rf.read_text(encoding="utf-8", errors="replace")
        except Exception:
            continue
        if inbox_id in text:
            m = _VERDICT_RE.search(text)
            if m:
                return m.group(1).lower(), rf.name
    return None, None


def _read_verdict(
    project_id: str, project_path: Path, inbox_id: str
) -> tuple[str | None, str | None]:
    """review-inbox 에서 verdict + 해당 review 파일명 반환.

    v0.16.8: head 가 review-inbox 파일을 `<project>-wt-head/` 브랜치에 커밋하므로
    head worktree 를 우선 조회. work_branch (project_path) 는 merge 된 것만 있어
    신규 verdict 는 거기에 없음 → 기존 코드는 항상 (None, None) 반환하는 버그였음.
    head worktree → work_branch 순 fallback.

    v0.43: subdir 모드에서 head worktree 의 review-inbox 가 .coord/coordination 밑에 있는데
    기존 코드가 coordination/ 만 붙여 못 읽던 버그 수정 (_head_coord_dir 사용).
    """
    # 1. head worktree 우선 (신규 verdict 여기에 기록됨)
    head_review_dir = _head_coord_dir(project_id, project_path) / "review-inbox"
    verdict, fname = _scan_review_dir_for_verdict(head_review_dir, inbox_id)
    if verdict is not None:
        return verdict, fname

    # 2. work_branch fallback (merge 된 과거 inbox)
    coord = _coord_root(project_path)
    work_review_dir = coord / "coordination" / "review-inbox"
    return _scan_review_dir_for_verdict(work_review_dir, inbox_id)


async def _spawn_and_wait(prompt: str, cwd: Path, timeout: int = 600) -> tuple[bool, str]:
    """claude -p <prompt> 를 실행하고 완료 대기 (synchronous).
    반환: (성공 여부, stdout 또는 에러 메시지).
    """
    if not CLAUDE_BIN:
        return False, "claude CLI 없음"
    cmd = [
        *CLAUDE_INVOCATION, "-p", prompt,
        "--permission-mode", "bypassPermissions",
        "--output-format", "json",
    ]
    try:
        proc = await asyncio.create_subprocess_exec(
            *cmd,
            env=_claude_cli_env(),
            cwd=str(cwd),
            stdout=asyncio.subprocess.PIPE,
            stderr=asyncio.subprocess.STDOUT,
        )
        stdout, _ = await asyncio.wait_for(proc.communicate(), timeout=timeout)
        text = stdout.decode("utf-8", errors="replace") if stdout else ""
        return (proc.returncode == 0), text
    except asyncio.TimeoutError:
        try:
            proc.kill()
        except Exception:
            pass
        return False, f"timeout ({timeout}s)"
    except Exception as e:
        return False, f"spawn 실패: {e}"


async def _resolve_anchor_channel(
    project: dict[str, Any], source_message: discord.Message
) -> discord.TextChannel | None:
    """Thread 생성 위치 결정: discord_alert_channel_id 설정돼있으면 거기,
    아니면 source_message 가 있던 TextChannel."""
    alert_id = project.get("discord_alert_channel_id")
    if alert_id and str(alert_id).strip():
        try:
            ch = bot.get_channel(int(alert_id)) or await bot.fetch_channel(int(alert_id))
        except Exception as e:
            print(f"[worker] alert_channel 조회 실패 ({alert_id}): {e}", flush=True)
            ch = None
        if isinstance(ch, discord.TextChannel):
            return ch
    # fallback: source 채널 (TextChannel 만 허용, thread 내부면 parent)
    src = source_message.channel
    if isinstance(src, discord.TextChannel):
        return src
    if isinstance(src, discord.Thread) and isinstance(src.parent, discord.TextChannel):
        return src.parent
    return None


async def _project_worker(project_id: str) -> None:
    """프로젝트별 single-consumer. 큐에서 job 꺼내 HEAD_LOCK 대기 후 spawn."""
    q = _project_queues[project_id]
    print(f"[queue] worker started for {project_id}", flush=True)
    try:
        while True:
            try:
                job: _QueueJob = await asyncio.wait_for(
                    q.get(), timeout=_QUEUE_WORKER_IDLE_TIMEOUT_SEC
                )
            except asyncio.TimeoutError:
                print(f"[queue] worker {project_id} idle timeout — exit", flush=True)
                return

            try:
                # v0.12: HEAD_LOCK 은 head worktree 의 것을 봄 (이전엔 project root 를 봐서 bug)
                head_lock = _head_lock_path(project_id, job.project_path)

                # Phase 1: 이전 head 세션의 lock 이 있으면 해제 대기
                cleared = await _wait_head_lock_clear(head_lock, _QUEUE_HEAD_LOCK_WAIT_TIMEOUT_SEC)
                if not cleared:
                    await job.source_message.reply(
                        f"❌ `{project_id}` HEAD_LOCK 30분 대기 후에도 해제 안 됨 — 이 job skip"
                    )
                    continue  # finally 가 q.task_done() 호출 (v0.16.2: double-call bug 제거)

                # v0.16.4: inbox 생성 감지는 poll 방식 (spawn 은 Popen fire-and-forget 이라
                # 직후 체크하면 Claude 가 아직 파일 쓸 시간 없음 — v0.16.2 의 eager 체크 버그 수정)
                inbox_dir = _coord_root(job.project_path) / "coordination" / "inbox"
                inbox_pre: set[str] = set()
                if inbox_dir.exists():
                    inbox_pre = {p.name for p in inbox_dir.glob("*.md") if p.name != ".gitkeep"}

                # Phase 2: spawn inbox-send (fire-and-forget)
                ok, msg = await _spawn_claude_command(
                    job.project_id,
                    job.project_path,
                    job.prompt,
                    check_head_lock=False,
                    label="inbox-send",
                )

                # v0.16.4: inbox 파일 생성을 최대 60초 poll. 생기면 정상 진행, 안 생기면 모호 판정.
                inbox_created = False
                if ok:
                    poll_deadline = time.time() + _V016_INBOX_CREATION_WAIT_SEC
                    while time.time() < poll_deadline:
                        await asyncio.sleep(_V016_INBOX_CREATION_POLL_SEC)
                        if inbox_dir.exists():
                            inbox_post = {p.name for p in inbox_dir.glob("*.md") if p.name != ".gitkeep"}
                            if inbox_post - inbox_pre:
                                inbox_created = True
                                break

                # 결과/thread 생성 위치 — alert_channel 있으면 거기, 없으면 source 채널
                project_cfg = PROJECTS.get(project_id, {})
                anchor_ch = await _resolve_anchor_channel(project_cfg, job.source_message)

                anchor_msg: discord.Message | None = None
                if anchor_ch is None:
                    try:
                        anchor_msg = await job.source_message.reply(msg)
                    except Exception:
                        pass
                else:
                    # v0.16.2: inbox 안 만들어졌으면 anchor 메시지 포맷 변경
                    if inbox_created:
                        anchor_msg = await anchor_ch.send(
                            f"📥 `{project_id}` 작업 시작\n"
                            f"본문: `{job.preview}`\n"
                            f"{msg}\n"
                            f"💡 `@bot active` 로 전체 작업 현황 확인"
                        )
                        if ok:
                            ts = datetime.now(timezone.utc).strftime("%H%M%S")
                            thread_name = f"inbox-{project_id}-{ts}"[:100]
                            try:
                                await anchor_msg.create_thread(
                                    name=thread_name,
                                    auto_archive_duration=1440,
                                )
                            except discord.Forbidden:
                                print("[thread] 권한 부족 — Create Public Threads", flush=True)
                            except Exception as e:
                                print(f"[thread] 생성 실패: {e}", flush=True)
                    else:
                        # spawn 은 성공했는데 60초 내 inbox 파일 생성 없음 → /inbox-send 가 모호 판정 or 실패
                        anchor_msg = await anchor_ch.send(
                            f"⚠ `{project_id}` inbox 생성 안 됨 ({_V016_INBOX_CREATION_WAIT_SEC}초 대기 후에도 파일 없음) — "
                            f"`/inbox-send` 가 모호하다고 거절했거나 처리 실패.\n"
                            f"로그 확인 후 좀 더 구체적으로 (파일명/경로/동작) 지시해서 다시 `@bot send <text>` 보내주세요.\n"
                            f"{msg}"
                        )
                    src_ch = job.source_message.channel
                    if src_ch and getattr(src_ch, "id", None) != anchor_ch.id:
                        try:
                            await job.source_message.reply(
                                f"➡ `{project_id}` <#{anchor_ch.id}> 에서 진행"
                            )
                        except Exception:
                            pass

                if not ok:
                    # spawn 자체 실패 → post-chain skip (finally 가 task_done)
                    continue

                # v0.16.4: 60초 poll 후에도 inbox 없음 → orchestration skip
                if not inbox_created:
                    print(
                        f"[queue] {project_id} /inbox-send spawn 성공 but "
                        f"{_V016_INBOX_CREATION_WAIT_SEC}s 내 inbox 파일 생성 없음 — orchestration skip",
                        flush=True,
                    )
                    continue

                # v0.12: post-spawn chain — head 완료 감지 → review 자동 → verdict Discord prompt
                target_ch = anchor_ch or (
                    job.source_message.channel if isinstance(job.source_message.channel, discord.TextChannel) else None
                )
                asyncio.create_task(
                    _post_spawn_orchestration(project_id, job.project_path, target_ch)
                )
            except Exception as e:
                print(f"[queue] {project_id} 처리 오류: {e}", flush=True)
                try:
                    await job.source_message.reply(f"❌ 큐 처리 실패: {e}")
                except Exception:
                    pass
            finally:
                q.task_done()
    except asyncio.CancelledError:
        print(f"[queue] worker {project_id} cancelled", flush=True)
        raise


async def _head_heartbeat(
    target_channel: discord.abc.Messageable,
    project_id: str,
    head_lock: Path,
    start_ts: float,
    milestones_min: list[int] | None = None,
) -> None:
    """v0.16.7: head 작업 중 마일스톤(기본 10/30/60분) 도달 시점에만 1회 메시지.
    짧은 작업엔 0개, 긴 작업에도 최대 3개 → 채널 깨끗.
    HEAD_LOCK 사라지거나 task cancel 되면 종료."""
    if milestones_min is None:
        milestones_min = _V016_7_HEARTBEAT_MILESTONES_MIN
    try:
        for milestone in milestones_min:
            target_elapsed = milestone * 60
            sleep_duration = target_elapsed - (time.time() - start_ts)
            if sleep_duration > 0:
                await asyncio.sleep(sleep_duration)
            if not head_lock.exists():
                return  # head 완료됨 — 남은 마일스톤 skip
            try:
                await target_channel.send(
                    f"🕐 `{project_id}` head 작업 중 ({milestone}분 경과)"
                )
            except Exception:
                pass
    except asyncio.CancelledError:
        return


async def _post_spawn_orchestration(
    project_id: str,
    project_path: Path,
    target_channel: discord.TextChannel | None,
) -> None:
    """inbox-send spawn 후 head 완료 감지 → review-inbox 자동 → verdict 판독 → Discord prompt.
    worker 는 이 chain 을 기다리지 않고 다음 job 으로 감 (asyncio.create_task 로 호출).

    v0.15: 각 phase 전환에 thread 메시지 + Phase B 에 주기적 heartbeat.
    """
    if target_channel is None:
        return

    head_lock = _head_lock_path(project_id, project_path)

    # Phase A: HEAD_LOCK 생김 대기 (head 시작)
    try:
        await target_channel.send(f"⏱ `{project_id}` head 세션 스폰 대기 중…")
    except Exception:
        pass

    appeared = await _wait_for_file_state(
        head_lock, want_exists=True, timeout=_PHASE_A_HEAD_SPAWN_TIMEOUT_SEC, poll=5
    )
    if not appeared:
        mins = _PHASE_A_HEAD_SPAWN_TIMEOUT_SEC // 60
        try:
            await target_channel.send(
                f"⚠ `{project_id}` head 세션이 시작 안 됨 ({mins}분 타임아웃). 수동 확인 필요."
            )
        except Exception:
            pass
        return

    # v0.32: "📍 head 시작 감지" 메시지 제거 (불필요한 노이즈 — heartbeat + ✅ 완료로 충분)

    # Phase B: HEAD_LOCK clear 대기 (head 완료) — 최대 60분
    # v0.15: 병렬 heartbeat task 로 장시간 작업 중 thread 에 경과 노출
    heartbeat_task = asyncio.create_task(
        _head_heartbeat(target_channel, project_id, head_lock, time.time())
    )
    try:
        cleared = await _wait_for_file_state(
            head_lock, want_exists=False, timeout=_PHASE_B_HEAD_COMPLETE_TIMEOUT_SEC, poll=15
        )
    finally:
        heartbeat_task.cancel()
        try:
            await heartbeat_task
        except Exception:
            pass

    if not cleared:
        mins = _PHASE_B_HEAD_COMPLETE_TIMEOUT_SEC // 60
        try:
            await target_channel.send(
                f"⚠ `{project_id}` head 세션이 {mins}분 내 완료 안 됨. 수동 확인 필요."
            )
        except Exception:
            pass
        return

    # v0.45: "✅ head 완료 감지 — verdict 확인 중" 메시지 제거.
    # 이어지는 "📋 Plan 요약" / "✨ self-review 감지" 메시지에 "완료 감지" 의미가 포함됨.
    # head webhook 의 🎉 완료 알림도 있어서 3중 중복이었음.

    # Phase C: 최근 inbox id 탐색
    inbox_id = _latest_inbox_id(project_path)
    if not inbox_id:
        try:
            await target_channel.send(
                f"⚠ `{project_id}` head 완료 후 inbox 파일을 찾지 못함 — verdict 판정 skip"
            )
        except Exception:
            pass
        return

    # Phase C-prep (v0.19): plan 요약 1회 포스트 (head 가 plan 작성했을 것)
    try:
        plan_path = _plan_path(project_id, project_path, inbox_id)
        plan_summary = _parse_plan_summary(plan_path)
        if plan_summary:
            subs_str = ", ".join(f"`wt/{s}`" for s in plan_summary["subs"]) or "(없음)"
            steps_str = (
                "\n".join(plan_summary["steps_preview"])
                if plan_summary["steps_preview"]
                else "(Step 헤더 없음)"
            )
            await target_channel.send(
                f"📋 `{project_id}` Plan — {plan_summary['title']}\n"
                f"  단계: **{plan_summary['step_count']}개**, sub: {subs_str}\n"
                f"```\n{steps_str}\n```"
            )
    except Exception as e:
        print(f"[v0.19 plan] {project_id} plan 요약 실패: {e}", flush=True)

    # Phase C-bis (v0.13): head 가 self-review 로 verdict 를 이미 기록했는지 확인.
    # 기록돼 있으면 Phase D spawn 생략 — 토큰/시간 절약 (head 컨텍스트엔 이미 plan/reports
    # 가 로드돼 있어 self-review 가 훨씬 저렴). 기록 없으면 기존 Phase D fallback.
    pre_verdict, pre_fname = _read_verdict(project_id, project_path, inbox_id)
    if pre_verdict is not None:
        try:
            await target_channel.send(
                f"✨ `{project_id}` head self-review 감지 — `{inbox_id}` verdict: `{pre_verdict}` (v0.13)"
            )
        except Exception:
            pass
        await _post_verdict_prompt(
            target_channel, project_id, inbox_id, pre_verdict, pre_fname,
            project_path=project_path,
        )
        return

    # Phase D (fallback): head 가 self-review 안 했을 때만 spawn
    try:
        await target_channel.send(
            f"🔎 `{project_id}` head 완료 감지 (self-review 없음) — `{inbox_id}` review 자동 spawn…"
        )
    except Exception:
        pass

    review_prompt = f"/review-inbox {inbox_id} --verdict-only"
    ok, out = await _spawn_and_wait(review_prompt, cwd=project_path, timeout=600)
    if not ok:
        try:
            await target_channel.send(
                f"❌ `{project_id}` review-inbox 자동 spawn 실패: ```{(out or '')[:500]}```"
            )
        except Exception:
            pass
        return

    # Phase E: verdict 읽기
    verdict, review_fname = _read_verdict(project_id, project_path, inbox_id)
    if verdict is None:
        try:
            await target_channel.send(
                f"⚠ `{project_id}` review 후 verdict 를 읽지 못함 — 수동 `@bot review {inbox_id}`"
            )
        except Exception:
            pass
        return

    # Phase F: Discord prompt
    await _post_verdict_prompt(
        target_channel, project_id, inbox_id, verdict, review_fname,
        project_path=project_path,
    )


_VERDICT_MARKER_FOOTER_PREFIX = "verdict-prompt|"


# v0.24: 머지 완료 후 rich summary -----------------------

def _parse_merge_info(review_path: Path) -> dict:
    """review-inbox 파일에서 merge 메타 (merged_at, commits, base_commit) 파싱.
    머지 완료 전이면 merged=False."""
    if not review_path.exists():
        return {"merged": False}
    try:
        text = review_path.read_text(encoding="utf-8", errors="replace")
    except Exception:
        return {"merged": False}

    info: dict[str, Any] = {"merged": False, "commits": []}

    # merged: true 또는 `- **merged**: true`
    if re.search(
        r"^\s*(?:-\s+)?\*{0,2}merged\*{0,2}\s*:\s*true",
        text, re.IGNORECASE | re.MULTILINE,
    ):
        info["merged"] = True

    # merged_at
    m = re.search(
        r"^\s*(?:-\s+)?\*{0,2}merged_at\*{0,2}\s*:\s*(.+?)\s*$",
        text, re.IGNORECASE | re.MULTILINE,
    )
    if m:
        info["merged_at"] = m.group(1).strip()

    # merge_commits 섹션 파싱 — "  - wt/<sub>: <hash>" 또는 "| <sub> | wt/<sub> | `<hash>` | ..."
    # 방식 1: YAML 리스트
    m = re.search(
        r"^\s*merge_commits\s*:\s*\n((?:\s+-\s+wt/[\w-]+:\s+\S+\s*\n?)+)",
        text, re.IGNORECASE | re.MULTILINE,
    )
    if m:
        for line in m.group(1).strip().split("\n"):
            mm = re.match(r"\s*-\s+wt/([\w-]+):\s+(\S+)", line)
            if mm:
                info["commits"].append((mm.group(1), mm.group(2).strip("`")))

    # 방식 2: markdown 테이블 (## 브랜치 및 커밋)
    if not info["commits"]:
        table_m = re.search(
            r"\|\s*Sub\s*\|\s*Branch\s*\|\s*Commit.*?\n(?:\|[-\s|:]+\|\s*\n)((?:\|[^\n]+\n)+)",
            text, re.IGNORECASE,
        )
        if table_m:
            for row in table_m.group(1).strip().split("\n"):
                cells = [c.strip() for c in row.strip("|").split("|")]
                if len(cells) >= 3:
                    sub, branch, commit = cells[0], cells[1], cells[2]
                    info["commits"].append((sub, commit.strip("`")))

    # base_commit_work_branch
    m = re.search(
        r"^\s*(?:-\s+)?\*{0,2}base_commit_work_branch\*{0,2}\s*:\s*(\S+)",
        text, re.IGNORECASE | re.MULTILINE,
    )
    if m:
        info["base_commit"] = m.group(1).strip("`")

    # 제목 (# Review-Inbox: 또는 #)
    m = re.search(r"^#\s+(?:Review-Inbox:\s*)?(.+)$", text, re.MULTILINE)
    if m:
        info["title"] = m.group(1).strip()

    return info


async def _post_merge_summary(
    channel: discord.abc.Messageable,
    project_id: str,
    project_path: Path,
    inbox_id: str,
    review_filename: str | None,
) -> None:
    """v0.24: 머지 완료 후 review-inbox 재파싱 → rich embed 포스트.
    변경 요약 / 발견 사항 / verdict 근거 + merge commits 포함."""
    review_path = _find_review_path(project_id, project_path, review_filename)
    if review_path is None:
        await channel.send(
            f"⚠ `{project_id}` 머지 완료됐으나 review-inbox 파일을 찾지 못해 요약 생성 실패"
        )
        return

    merge_info = _parse_merge_info(review_path)
    sections = _parse_review_sections(review_path)

    title = merge_info.get("title", inbox_id)
    embed = discord.Embed(
        title=f"🎉 머지 완료 — {title}",
        description=f"**inbox**: `{inbox_id}`\n**project**: `{project_id}`",
        color=discord.Color.green(),
    )

    # 머지 정보
    if merge_info.get("commits"):
        commits_str = "\n".join(
            f"• `wt/{sub}` → `{h[:8]}`" for sub, h in merge_info["commits"]
        )
        embed.add_field(name="🔀 머지 커밋", value=commits_str[:1000], inline=False)

    if merge_info.get("merged_at"):
        embed.add_field(
            name="⏰ 머지 시각", value=merge_info["merged_at"], inline=True,
        )

    if merge_info.get("base_commit"):
        embed.add_field(
            name="📍 base commit", value=f"`{merge_info['base_commit'][:8]}`", inline=True,
        )

    # 리뷰 섹션
    if sections.get("change_summary"):
        embed.add_field(
            name="📝 변경 요약",
            value=sections["change_summary"][:1000],
            inline=False,
        )
    if sections.get("findings"):
        embed.add_field(
            name="🔍 발견 사항",
            value=sections["findings"][:1000],
            inline=False,
        )
    if sections.get("verdict_reason"):
        embed.add_field(
            name="💬 verdict 근거",
            value=sections["verdict_reason"][:1000],
            inline=False,
        )

    # v0.29: 다음 단계 명령어 안내
    next_steps = []
    remote, work_branch, stable_branch = _git_branches(project_path)
    if work_branch != stable_branch:
        next_steps.append(
            f"• `@bot main-merge` — `{work_branch}` → `{stable_branch}` 반영 (dry-run 사전 감지)"
        )
    next_steps.append("• `@bot active` — 전체 프로젝트 작업 현황 확인")
    next_steps.append(f"• `@bot status {project_id}` — `{project_id}` 상세 상태")
    embed.add_field(
        name="💡 다음 단계",
        value="\n".join(next_steps),
        inline=False,
    )

    embed.set_footer(text=f"review: {review_filename or '?'}")

    try:
        await channel.send(embed=embed)
    except Exception as e:
        print(f"[merge-summary] 전송 실패: {e}", flush=True)
        # fallback 간단 텍스트
        try:
            await channel.send(
                f"🎉 `{project_id}` `{inbox_id}` 머지 완료 — 상세는 {review_filename}"
            )
        except Exception:
            pass


async def _find_existing_verdict_prompt(
    channel: discord.abc.Messageable, inbox_id: str, limit: int = 100
) -> discord.Message | None:
    """v0.26: 채널 최근 메시지 중 해당 inbox_id 의 verdict-prompt embed 가 있는지 스캔.
    있으면 해당 메시지 반환, 없으면 None. 중복 prompt 방지용."""
    history = getattr(channel, "history", None)
    if history is None:
        return None
    marker_prefix = f"{_VERDICT_MARKER_FOOTER_PREFIX}{inbox_id}|"
    try:
        async for msg in channel.history(limit=limit):
            if msg.author.bot is False:
                continue
            for embed in msg.embeds:
                footer = getattr(embed.footer, "text", None) if embed.footer else None
                if footer and footer.startswith(marker_prefix):
                    return msg
    except Exception as e:
        print(f"[dedup] channel history scan 실패: {e}", flush=True)
    return None


def _find_review_path(
    project_id: str, project_path: Path, review_filename: str | None
) -> Path | None:
    """review_filename 으로 head_wt → work_branch 순으로 실제 파일 찾기."""
    if not review_filename:
        return None
    head_wt = project_path.parent / f"{project_id}-wt-head"
    # v0.43: subdir 모드 대응 — 각 base 에 _coord_root 적용
    for base in (head_wt, project_path):
        candidate = _coord_root(base) / "coordination" / "review-inbox" / review_filename
        if candidate.exists():
            return candidate
    return None


async def _post_verdict_prompt(
    channel: discord.TextChannel,
    project_id: str,
    inbox_id: str,
    verdict: str,
    review_filename: str | None,
    project_path: Path | None = None,
) -> None:
    """verdict 결과 + 후속 action 리액션 prompt 를 채널에 포스트.

    v0.19: review-inbox 섹션 (변경 요약 / 발견 사항 / verdict 근거) 파싱해 embed 에 추가.
    v0.26: 채널 내 같은 inbox 의 기존 prompt 가 있으면 silent skip (자동 orch + 수동
    review 동시 트리거 시 발생하는 race 방어).
    """
    # v0.26: 모든 경로 공통 dedup
    try:
        if isinstance(channel, (discord.TextChannel, discord.Thread)):
            existing = await _find_existing_verdict_prompt(channel, inbox_id)
            if existing is not None:
                print(
                    f"[verdict-prompt] {inbox_id} 기존 prompt 있음 (msg {existing.id}) — skip",
                    flush=True,
                )
                return
    except Exception as e:
        print(f"[verdict-prompt] dedup 체크 실패: {e}", flush=True)
    if verdict == "go":
        title = "✅ Review 완료 — 머지 진행할까요?"
        base_desc = (
            f"**inbox**: `{inbox_id}`\n"
            f"**verdict**: `go` (통과)\n\n"
            f"리액션:\n"
            f"✅ → `/review-inbox {inbox_id}` 머지 단계 spawn\n"
            f"❌ → 보류 (수동 진행)"
        )
        color = discord.Color.green()
    elif verdict == "needs-fix":
        title = "⚠ Review 완료 — 수정 필요"
        base_desc = (
            f"**inbox**: `{inbox_id}`\n"
            f"**verdict**: `needs-fix`\n"
            f"**review 파일**: `{review_filename or '?'}`\n\n"
            f"리액션:\n"
            f"✅ → `/fix` 자동 spawn\n"
            f"❌ → 무시 (수동 결정)"
        )
        color = discord.Color.orange()
    elif verdict == "block":
        title = "🛑 Review 완료 — BLOCK"
        base_desc = (
            f"**inbox**: `{inbox_id}`\n"
            f"**verdict**: `block` (심각 — 자동 action 없음)\n"
            f"**review 파일**: `{review_filename or '?'}`\n\n"
            f"사용자 개입 필수. merge 금지."
        )
        color = discord.Color.red()
    else:
        title = f"ℹ Review 완료 — verdict: {verdict}"
        base_desc = (
            f"**inbox**: `{inbox_id}`\n"
            f"**verdict**: `{verdict}` (예상 외 값)\n\n"
            f"수동 확인 권장."
        )
        color = discord.Color.greyple()

    embed = discord.Embed(title=title, description=base_desc, color=color)

    # v0.19: review-inbox 섹션 파싱해 추가 필드 (project_path 전달됐을 때만)
    if project_path is not None and review_filename:
        review_path = _find_review_path(project_id, project_path, review_filename)
        if review_path is not None:
            sections = _parse_review_sections(review_path)
            if sections.get("change_summary"):
                embed.add_field(
                    name="📝 변경 요약",
                    value=sections["change_summary"][:1000],
                    inline=False,
                )
            if sections.get("findings"):
                embed.add_field(
                    name="🔍 발견 사항",
                    value=sections["findings"][:1000],
                    inline=False,
                )
            if sections.get("verdict_reason"):
                embed.add_field(
                    name="💬 verdict 근거",
                    value=sections["verdict_reason"][:1000],
                    inline=False,
                )

    embed.set_footer(text=f"{_VERDICT_MARKER_FOOTER_PREFIX}{inbox_id}|{verdict}")
    try:
        msg = await channel.send(embed=embed)
        if verdict in ("go", "needs-fix"):
            try:
                await msg.add_reaction("✅")
                await msg.add_reaction("❌")
            except Exception as e:
                print(f"[verdict-prompt] 리액션 추가 실패: {e}", flush=True)
    except Exception as e:
        print(f"[verdict-prompt] 전송 실패: {e}", flush=True)


async def _enqueue_job(project_id: str, job: _QueueJob) -> int:
    """큐에 추가 + 필요 시 worker 시작. 반환: 큐 크기 (자기 포함)."""
    if project_id not in _project_queues:
        _project_queues[project_id] = asyncio.Queue()
    q = _project_queues[project_id]
    await q.put(job)
    # worker 부활: 없거나 완료된 경우 재시작
    w = _project_workers.get(project_id)
    if w is None or w.done():
        _project_workers[project_id] = asyncio.create_task(_project_worker(project_id))
    return q.qsize()


def _queue_state_summary(project_id: str) -> dict[str, Any]:
    """@bot queue 응답용."""
    q = _project_queues.get(project_id)
    w = _project_workers.get(project_id)
    return {
        "project_id": project_id,
        "queue_size": q.qsize() if q else 0,
        "worker_running": bool(w and not w.done()),
    }


# ─── token-budget 파싱 ──────────────────────────────────────

def _parse_token_budget(coord: Path) -> dict[str, Any] | None:
    bf = coord / "coordination" / "token-budget.md"
    if not bf.exists():
        return None
    text = bf.read_text(encoding="utf-8", errors="replace")
    def _int(field: str) -> int:
        m = re.search(rf"^\s*-?\s*\*\*{field}\*\*:\s*(\d+)", text, re.M)
        return int(m.group(1)) if m else 0
    def _str(field: str) -> str:
        m = re.search(rf"^\s*-?\s*\*\*{field}\*\*:\s*(.+?)\s*$", text, re.M)
        raw = m.group(1).strip() if m else ""
        return raw.split("#")[0].strip()  # 주석 제거
    limit = _int("weekly_limit") or 5_000_000
    used = _int("used_tokens")
    return {
        "limit": limit,
        "used": used,
        "percent": round(used * 100 / limit, 2) if limit else 0,
        "week_start": _str("week_start_ts"),
        "last_alert": _int("last_alerted_threshold"),
    }


# ─── escalation id 파싱 (리액션 핸들러용) ──────────────────

# 판사 거부 알림 메시지에서 escalation id 추출
# 1순위: **escalation-id**: `<id>`   (judge-action.sh 가 v0.8+ 에서 emit)
# 2순위: escalations/.*-<id>.md      (파일 경로 fallback)
_ESC_ID_PATTERNS = [
    re.compile(r"escalation[-_ ]?id[`*:\s]+`?([0-9]{8}-[0-9]{6})`?", re.IGNORECASE),
    re.compile(r"escalations/[^\s`]*?(\d{8}-\d{6})\.md"),
]


def _extract_escalation_id(text: str) -> str | None:
    for p in _ESC_ID_PATTERNS:
        m = p.search(text)
        if m:
            return m.group(1)
    return None


def _head_lock_info(project_id: str, project_path: Path) -> tuple[bool, str | None, int]:
    """v0.23: head worktree 의 HEAD_LOCK 상태 + 경과 시간.
    반환: (active, started_at_str, elapsed_minutes)"""
    head_lock = _head_lock_path(project_id, project_path)
    if not head_lock.exists():
        return False, None, 0
    try:
        raw = head_lock.read_text(encoding="utf-8", errors="replace").strip()
        # ISO 8601 형식 (inbox-send.md 가 `date -Iseconds` 로 기록)
        try:
            ts = datetime.fromisoformat(raw)
            if ts.tzinfo is None:
                ts = ts.replace(tzinfo=timezone.utc)
            elapsed_sec = (datetime.now(timezone.utc) - ts).total_seconds()
            elapsed_min = max(0, int(elapsed_sec / 60))
            return True, ts.astimezone().strftime("%H:%M KST"), elapsed_min
        except Exception:
            return True, raw[:30], 0
    except Exception:
        return True, None, 0


def _summarize_status(project_id: str, project_path: Path) -> str:
    """v0.23: HEAD_LOCK 을 head worktree 기준으로 체크 + 경과 시간 표시."""
    coord = _coord_root(project_path)
    stop = coord / "coordination" / "STOP"
    inbox_dir = coord / "coordination" / "inbox"

    active, started, elapsed_min = _head_lock_info(project_id, project_path)
    if active:
        elapsed_str = f" ({elapsed_min}분 경과)" if elapsed_min > 0 else ""
        start_str = f" 시작: {started}" if started else ""
        running = f"🔒 작업 중{elapsed_str}{start_str}"
    else:
        running = "⚪ 대기"
    stop_flag = " 🛑 STOP 활성" if stop.exists() else ""

    pending = 0
    if inbox_dir.exists():
        for f in inbox_dir.glob("*.md"):
            if f.name.startswith(("_", ".")):
                continue
            try:
                txt = f.read_text(encoding="utf-8", errors="replace")
            except Exception:
                continue
            if "status: pending" in txt or "**status**: pending" in txt:
                pending += 1

    return f"**{project_id}**: {running}{stop_flag} — pending inbox: {pending}"


# ─── inbox / review 자동 탐색 helpers (v0.11) ────────────

def _latest_unreviewed_inbox(project_id: str, project_path: Path) -> str | None:
    """가장 최근 **status: pending** inbox 의 id 반환.

    v0.25 단순화: "미review" 를 **inbox 의 status: pending** 하나로 판정.
    이전엔 review-inbox 파일 본문에 id 언급만 있어도 skip 했는데, 실사용에서
    "verdict 는 기록됐지만 아직 머지 안 된 pending inbox" 가 자동 선택 대상에서
    빠지는 문제 발생. pending = 사용자 action 대기 = 자동 선택 후보.

    v0.18 의 status 체크는 그대로 유지 — merged/cancelled/done/failed 는 skip.
    """
    coord = _coord_root(project_path)
    inbox_dir = coord / "coordination" / "inbox"
    if not inbox_dir.exists():
        return None

    candidates = sorted(
        (f for f in inbox_dir.glob("*.md") if not f.name.startswith(("_", "."))),
        key=lambda p: p.stat().st_mtime,
        reverse=True,
    )
    if not candidates:
        return None

    for inbox_file in candidates:
        inbox_id = inbox_file.stem
        # inbox 본문의 status 헤더 체크 (v0.18 로직 유지)
        try:
            inbox_text = inbox_file.read_text(encoding="utf-8", errors="replace")
            m = re.search(
                r"^-\s*\*?\*?status\*?\*?:\s*(\S+)",
                inbox_text,
                re.IGNORECASE | re.MULTILINE,
            )
            status = m.group(1).strip().lower() if m else None
            if status is None:
                # status 헤더 없는 경우 — 보수적으로 pending 간주
                return inbox_id
            if status == "pending":
                return inbox_id
            # merged/cancelled/done/failed/superseded 등 → skip
        except Exception:
            # 파일 읽기 실패 시 보수적으로 후보 포함
            return inbox_id
    return None


def _find_needs_fix_candidate(
    project_id: str, project_path: Path
) -> tuple[str | None, str | None]:
    """review-inbox 중 `verdict: needs-fix` 최근 항목 → (제목, 파일명) 반환.

    v0.16.8: head worktree 우선 + work_branch fallback (review-inbox 는 head 가 쓰므로).
    v0.43: subdir 모드 대응 — _head_coord_dir 사용.
    """
    candidates: list[Path] = []
    # head worktree 먼저
    head_review_dir = _head_coord_dir(project_id, project_path) / "review-inbox"
    if head_review_dir.exists():
        candidates.extend(
            f for f in head_review_dir.glob("*.md")
            if not f.name.startswith(("_", "."))
        )
    # work_branch 도 포함 (merge 완료된 것에 needs-fix 가 남아있을 수 있음)
    coord = _coord_root(project_path)
    work_review_dir = coord / "coordination" / "review-inbox"
    if work_review_dir.exists():
        candidates.extend(
            f for f in work_review_dir.glob("*.md")
            if not f.name.startswith(("_", "."))
        )

    for rf in sorted(candidates, key=lambda p: p.stat().st_mtime, reverse=True):
        try:
            text = rf.read_text(encoding="utf-8", errors="replace")
        except Exception:
            continue
        # v0.18.1: markdown bold 형식 허용
        if re.search(
            r"\*{0,2}verdict\*{0,2}\s*:\s*needs[-_]fix",
            text,
            re.IGNORECASE,
        ):
            m = re.search(r"^#\s*Review:\s*(.+)$", text, re.MULTILINE)
            title = m.group(1).strip() if m else rf.stem
            return title, rf.name
    return None, None


# ─── 명령 핸들러 ───────────────────────────────────────────

async def _check_usage_alerts_once() -> None:
    """usage_alerts 설정에 따라 daily / monthly 임계 체크 + Discord 알림 (하루/한달 1회)."""
    cfg = BOT_CFG.get("usage_alerts") or {}
    if not cfg.get("enabled"):
        return
    alert_channel_id = cfg.get("alert_channel_id")
    if not alert_channel_id:
        return
    try:
        channel = bot.get_channel(int(alert_channel_id))
    except (TypeError, ValueError):
        return
    if channel is None:
        return

    state = _load_alert_state()
    changed = False
    now_utc = datetime.now(timezone.utc)

    # --- Daily ---
    daily_thresh = float(cfg.get("daily_usd_threshold", 0) or 0)
    if daily_thresh > 0:
        today_str = now_utc.strftime("%Y%m%d")
        ok, result = await _run_ccusage(["daily", "--since", today_str])
        if ok and isinstance(result, dict):
            rows = result.get("daily", [])
            today_cost = sum(r.get("totalCost", 0) for r in rows if r.get("date", "").replace("-", "") == today_str)
            today_date_iso = now_utc.strftime("%Y-%m-%d")
            if today_cost > daily_thresh and state.get("last_daily_alert_date") != today_date_iso:
                embed = discord.Embed(
                    title="⚠ 오늘 Claude CLI 사용량 임계 초과",
                    description=(
                        f"오늘 누적: **${today_cost:.2f}**  (임계: ${daily_thresh:.2f})\n"
                        f"상세: `@bot usage 1`"
                    ),
                    color=discord.Color.orange(),
                )
                embed.set_footer(text=f"ccusage daily · {today_date_iso}")
                try:
                    await channel.send(embed=embed)
                    state["last_daily_alert_date"] = today_date_iso
                    state["last_daily_alert_amount"] = round(today_cost, 4)
                    changed = True
                except Exception as e:
                    print(f"[usage-alert] daily 전송 실패: {e}", flush=True)

    # --- Monthly ---
    monthly_thresh = float(cfg.get("monthly_usd_threshold", 0) or 0)
    if monthly_thresh > 0:
        month_ym = now_utc.strftime("%Y-%m")
        month_start = now_utc.replace(day=1).strftime("%Y%m%d")
        ok, result = await _run_ccusage(["daily", "--since", month_start])
        if ok and isinstance(result, dict):
            rows = result.get("daily", [])
            month_cost = sum(r.get("totalCost", 0) for r in rows)
            if month_cost > monthly_thresh and state.get("last_monthly_alert_month") != month_ym:
                embed = discord.Embed(
                    title="⚠ 이번 달 Claude CLI 사용량 임계 초과",
                    description=(
                        f"{month_ym} 누적: **${month_cost:.2f}**  (임계: ${monthly_thresh:.2f})\n"
                        f"상세: `@bot usage 30`"
                    ),
                    color=discord.Color.red(),
                )
                embed.set_footer(text=f"ccusage monthly · {month_ym}")
                try:
                    await channel.send(embed=embed)
                    state["last_monthly_alert_month"] = month_ym
                    state["last_monthly_alert_amount"] = round(month_cost, 4)
                    changed = True
                except Exception as e:
                    print(f"[usage-alert] monthly 전송 실패: {e}", flush=True)

    if changed:
        try:
            _save_alert_state(state)
        except Exception as e:
            print(f"[usage-alert] state 저장 실패: {e}", flush=True)


async def _usage_alert_loop() -> None:
    cfg = BOT_CFG.get("usage_alerts") or {}
    if not cfg.get("enabled"):
        return
    interval_min = int(cfg.get("check_interval_minutes", 60) or 60)
    interval_sec = max(60, interval_min * 60)   # 최소 1분
    print(f"[usage-alert] 루프 시작 — 주기 {interval_min}분", flush=True)
    while True:
        try:
            await _check_usage_alerts_once()
        except Exception as e:
            print(f"[usage-alert] 체크 실패: {e}", flush=True)
        await asyncio.sleep(interval_sec)


@bot.event
async def on_ready() -> None:
    global _usage_alert_task
    print(f"✅ Logged in as {bot.user} (id: {bot.user.id})", flush=True)
    print(f"   Projects: {list(PROJECTS.keys())}", flush=True)
    print(f"   Admins:   {sorted(ADMIN_IDS) if ADMIN_IDS else '(open)'}", flush=True)
    print(f"   Channels: {sorted(ALLOWED_CHANNELS) if ALLOWED_CHANNELS else '(any mention)'}", flush=True)
    # Usage 자동 경고 백그라운드 태스크
    if _usage_alert_task is None or _usage_alert_task.done():
        cfg = BOT_CFG.get("usage_alerts") or {}
        if cfg.get("enabled"):
            _usage_alert_task = asyncio.create_task(_usage_alert_loop())


@bot.event
async def on_message(message: discord.Message) -> None:
    if message.author.bot:
        return
    if bot.user not in message.mentions:
        return
    # command_prefix=when_mentioned 로 처리되므로 process_commands 로 위임
    await bot.process_commands(message)


@bot.command(name="projects")
async def cmd_projects(ctx: commands.Context) -> None:
    if not _authorized(ctx.author.id):
        await ctx.reply("⛔ 권한 없음 (admin_user_ids 확인)")
        return
    if not PROJECTS:
        await ctx.reply("⚠ 등록된 프로젝트 없음 (projects.yml 확인)")
        return
    lines = ["**등록 프로젝트**"]
    for pid, p in PROJECTS.items():
        path = Path(p["path"])
        ok = "✓" if path.is_dir() else "✗ (path 없음)"
        mark = " *(default)*" if pid == DEFAULT_PROJECT else ""
        lines.append(f"- `{pid}`{mark} — `{p['path']}` {ok}")
    await ctx.reply("\n".join(lines))


@bot.command(name="status")
async def cmd_status(ctx: commands.Context, project_id: str | None = None) -> None:
    if not _authorized(ctx.author.id):
        await ctx.reply("⛔ 권한 없음")
        return
    targets = (
        [(project_id, PROJECTS[project_id])]
        if project_id and project_id in PROJECTS
        else list(PROJECTS.items())
    )
    if not targets:
        msg = f"⚠ 프로젝트 `{project_id}` 없음" if project_id else "⚠ 등록 프로젝트 없음"
        await ctx.reply(msg)
        return
    bch = _binding_channel_id(ctx.channel)
    lines = []
    for pid, p in targets:
        if not _channel_ok(bch, p):
            continue
        lines.append(_summarize_status(pid, Path(p["path"])))
    await ctx.reply("\n".join(lines) if lines else "⚠ 이 채널에서 표시 가능한 프로젝트 없음")


@bot.command(name="active")
async def cmd_active(ctx: commands.Context) -> None:
    """v0.23: 등록된 모든 프로젝트의 head 작업 상태 한눈에 확인.

    채널 바인딩 필터 무시 — 어느 채널에서 실행해도 전체 프로젝트 조회.
    사용 예: "지금 어디서 뭐 돌고 있지?" 빠른 스캔.
    """
    if not _authorized(ctx.author.id):
        await ctx.reply("⛔ 권한 없음")
        return
    if not PROJECTS:
        await ctx.reply("⚠ 등록 프로젝트 없음")
        return

    busy: list[tuple[str, str, int]] = []   # (pid, started_str, elapsed_min)
    idle: list[str] = []
    pending_map: dict[str, int] = {}

    for pid, p in PROJECTS.items():
        path = Path(p["path"])
        active, started, elapsed_min = _head_lock_info(pid, path)
        if active:
            busy.append((pid, started or "?", elapsed_min))
        else:
            idle.append(pid)

        # pending inbox 개수
        coord = _coord_root(path)
        inbox_dir = coord / "coordination" / "inbox"
        count = 0
        if inbox_dir.exists():
            for f in inbox_dir.glob("*.md"):
                if f.name.startswith(("_", ".")):
                    continue
                try:
                    txt = f.read_text(encoding="utf-8", errors="replace")
                except Exception:
                    continue
                if "status: pending" in txt or "**status**: pending" in txt:
                    count += 1
        pending_map[pid] = count

    lines = ["**작업 현황 전체**"]
    if busy:
        lines.append("")
        lines.append("🔒 **작업 중**")
        for pid, started, elapsed in sorted(busy, key=lambda x: -x[2]):
            elapsed_str = f"{elapsed}분 경과" if elapsed > 0 else "방금 시작"
            lines.append(f"  • `{pid}` — {elapsed_str} (시작 {started})")
    else:
        lines.append("")
        lines.append("⚪ 전원 대기 (head 실행 중인 프로젝트 없음)")

    if idle:
        lines.append("")
        lines.append("⚪ **대기 중**: " + ", ".join(f"`{p}`" for p in idle))

    # pending 이 있는 프로젝트만
    pending_projects = [
        (pid, n) for pid, n in pending_map.items() if n > 0
    ]
    if pending_projects:
        lines.append("")
        lines.append("📬 **pending inbox**")
        for pid, n in sorted(pending_projects, key=lambda x: -x[1]):
            lines.append(f"  • `{pid}`: {n} 건")

    await ctx.reply("\n".join(lines))


async def _run_git(
    cwd: Path, *args: str, timeout: int = 120
) -> tuple[int, str, str]:
    """v0.27: git 명령 실행 → (returncode, stdout, stderr). 타임아웃 기본 2분."""
    try:
        proc = await asyncio.create_subprocess_exec(
            "git", *args,
            cwd=str(cwd),
            stdout=asyncio.subprocess.PIPE,
            stderr=asyncio.subprocess.PIPE,
        )
        stdout_b, stderr_b = await asyncio.wait_for(proc.communicate(), timeout=timeout)
        return (
            proc.returncode or 0,
            stdout_b.decode("utf-8", errors="replace"),
            stderr_b.decode("utf-8", errors="replace"),
        )
    except asyncio.TimeoutError:
        try:
            proc.kill()
        except Exception:
            pass
        return -1, "", f"timeout ({timeout}s)"
    except Exception as e:
        return -1, "", f"git 실행 실패: {e}"


async def _drop_coord_from_stable(
    project_path: Path, remote: str, stable_branch: str,
) -> tuple[bool, int, str]:
    """v0.46: stable_branch 에 tracked 된 .coord/* 을 일괄 untrack + commit + push.

    DEALOS 처럼 이미 main 에 .coord/ 이력이 있는 경우 rename/delete 충돌 원천 제거용.
    호출 전제: 이미 stable_branch 가 체크아웃된 상태.
    반환: (성공 여부, 삭제된 파일 수, 메시지).
    """
    code, out, err = await _run_git(
        project_path, "ls-files", "--", ".coord/",
    )
    if code != 0:
        return False, 0, f"ls-files 실패: {err[:200]}"
    files = [f for f in out.strip().split("\n") if f]
    if not files:
        return True, 0, "stable 에 tracked 된 .coord/* 없음 — 청소 불필요"

    code, _, err = await _run_git(
        project_path, "rm", "-r", "--cached", ".coord/", timeout=60,
    )
    if code != 0:
        return False, 0, f"git rm --cached 실패: {err[:200]}"

    commit_msg = (
        f"chore: drop .coord/ from {stable_branch} (coord v0.46 merge hygiene)\n\n"
        f"{len(files)} files untracked. `.gitattributes merge=ours` handles future merges."
    )
    code, _, err = await _run_git(
        project_path, "commit", "-m", commit_msg, timeout=60,
    )
    if code != 0:
        return False, 0, f"commit 실패: {err[:200]}"

    code, _, err = await _run_git(
        project_path, "push", remote, stable_branch, timeout=60,
    )
    if code != 0:
        return False, len(files), f"push 실패 (commit 은 local 에 있음): {err[:200]}"

    return True, len(files), f"{len(files)}개 파일 untrack + push 완료"


async def _back_merge_main_to_work(
    project_path: Path, remote: str, work_branch: str, stable_branch: str,
) -> tuple[bool, str, str]:
    """v0.46: main → work_branch 역머지. work_branch 의 .coord/ 는 .gitattributes
    merge=ours 덕분에 자동 보존.
    반환: (성공 여부, 머지 commit 해시 또는 '', 상세 메시지).
    """
    # work_branch 체크아웃 + pull
    code, _, err = await _run_git(project_path, "checkout", work_branch)
    if code != 0:
        return False, "", f"checkout {work_branch} 실패: {err[:200]}"

    code, _, err = await _run_git(
        project_path, "pull", "--ff-only", remote, work_branch, timeout=60,
    )
    if code != 0:
        return False, "", f"pull {work_branch} 실패: {err[:200]}"

    # main 머지 — merge=ours (.gitattributes) 가 .coord/ 자동 보존
    merge_msg = (
        f"chore: back-merge {stable_branch} into {work_branch} "
        f"(via @bot main-merge auto back-merge)"
    )
    code, out, err = await _run_git(
        project_path, "merge", "--no-ff", f"{remote}/{stable_branch}",
        "-m", merge_msg, timeout=90,
    )
    if code != 0:
        # 충돌 — abort + 실패 반환 (main 은 이미 성공 머지됐으니 치명 아님)
        await _run_git(project_path, "merge", "--abort")
        return False, "", (
            f"{stable_branch} → {work_branch} 역머지 충돌:\n"
            f"```{(err or out)[:400]}```\n"
            f".gitattributes 에 `.coord/** merge=ours` 있는지 확인 필요."
        )

    # push
    code, _, err = await _run_git(
        project_path, "push", remote, work_branch, timeout=60,
    )
    if code != 0:
        return False, "", f"{work_branch} push 실패: {err[:200]}"

    # commit 해시
    _, commit_out, _ = await _run_git(project_path, "rev-parse", work_branch)
    return True, commit_out.strip()[:8], f"{stable_branch} → {work_branch} 역머지 완료"


@bot.command(name="main-merge")
async def cmd_main_merge(ctx: commands.Context, *args: str) -> None:
    """v0.27: work_branch → stable_branch (main) 머지.
    v0.46+: `--drop-coord` 옵션 + 성공 시 자동 back-merge (main → work_branch).

    안전장치:
    1. Dry-run (temp 브랜치) 로 충돌 사전 감지
    2. 충돌 없으면 → 실제 머지 + push + rich embed + 자동 back-merge
    3. 충돌 있으면 → 파일 목록 embed 포스트 + 중단 (stable 안 건드림)

    사용법:
      @bot main-merge                       # 현재 채널 프로젝트
      @bot main-merge <project-id>
      @bot main-merge --drop-coord          # 사전 청소: main 의 과거 .coord/* 이력 untrack
      @bot main-merge <project-id> --drop-coord
    """
    # 인자 파싱 — project_id (positional) + --drop-coord (flag)
    project_id: str | None = None
    drop_coord = False
    for arg in args:
        if arg == "--drop-coord":
            drop_coord = True
        elif not arg.startswith("--"):
            project_id = arg

    if not _authorized(ctx.author.id):
        await ctx.reply("⛔ 권한 없음")
        return

    bch = _binding_channel_id(ctx.channel)
    project_id, project = _resolve_project(channel_id=bch, token=project_id)
    if project is None:
        await ctx.reply("⚠ 프로젝트 resolve 실패 — `@bot projects` 확인")
        return
    if not _channel_ok(bch, project):
        await ctx.reply("⛔ 이 채널에선 해당 프로젝트 조작 불가")
        return

    project_path = Path(project["path"])
    remote, work_branch, stable_branch = _git_branches(project_path)

    if work_branch == stable_branch:
        await ctx.reply(
            f"ℹ `{project_id}` work_branch 와 stable_branch 가 같음 (`{work_branch}`) — "
            f"main 머지 불필요 (단일 브랜치 운영)"
        )
        return

    await ctx.reply(
        f"🔍 `{project_id}` `{work_branch}` → `{stable_branch}` 머지 사전 점검 중…"
    )

    # Step 1: clean state 확인
    code, out, err = await _run_git(project_path, "status", "--porcelain")
    if code != 0:
        await ctx.reply(f"❌ git status 실패: ```{err[:500]}```")
        return
    if out.strip():
        await ctx.reply(
            f"⚠ `{project_path.name}` 에 uncommitted 변경 있음 — 먼저 정리 후 재시도\n"
            f"```{out[:500]}```"
        )
        return

    # Step 2: fetch 양쪽 브랜치
    code, _, err = await _run_git(
        project_path, "fetch", remote, work_branch, stable_branch, timeout=60,
    )
    if code != 0:
        await ctx.reply(f"❌ fetch 실패: ```{err[:500]}```")
        return

    # Step 3: stable 브랜치 최신으로 이동
    code, _, err = await _run_git(project_path, "checkout", stable_branch)
    if code != 0:
        await ctx.reply(f"❌ checkout {stable_branch} 실패: ```{err[:500]}```")
        return
    code, _, err = await _run_git(
        project_path, "pull", "--ff-only", remote, stable_branch,
    )
    if code != 0:
        await ctx.reply(
            f"❌ {stable_branch} pull --ff-only 실패 (로컬 ahead 상태일 수 있음): "
            f"```{err[:500]}```"
        )
        return

    # Step 3.5 (v0.46): --drop-coord — 과거 .coord/ 이력 청소 (일회성)
    if drop_coord:
        await ctx.reply(
            f"🧹 `{project_id}` `{stable_branch}` 의 .coord/* 사전 청소 중…"
        )
        ok_drop, n_dropped, msg = await _drop_coord_from_stable(
            project_path, remote, stable_branch,
        )
        if not ok_drop:
            await ctx.reply(f"❌ --drop-coord 실패: {msg}")
            return
        if n_dropped > 0:
            await ctx.reply(
                f"✅ `{stable_branch}` 에서 `.coord/*` **{n_dropped}개 파일** untrack + push\n"
                f"이후 `.gitattributes merge=ours` 로 재유입 방지됨."
            )

    # Step 4: dry-run 충돌 감지 — temp 브랜치로
    temp_branch = f"tmp-main-merge-check-{datetime.now().strftime('%H%M%S')}"
    code, _, err = await _run_git(
        project_path, "checkout", "-b", temp_branch, stable_branch,
    )
    if code != 0:
        await ctx.reply(f"❌ temp 브랜치 생성 실패: ```{err[:500]}```")
        return

    code, out, err = await _run_git(
        project_path, "merge", "--no-commit", "--no-ff",
        f"{remote}/{work_branch}",
    )

    if code == 0:
        # 충돌 없음 — temp abort + 실제 머지
        await _run_git(project_path, "merge", "--abort")
        await _run_git(project_path, "checkout", stable_branch)
        await _run_git(project_path, "branch", "-D", temp_branch)

        await ctx.reply(
            f"✅ `{project_id}` 충돌 없음 — `{stable_branch}` 에 `{work_branch}` 머지 진행"
        )

        # 실제 머지
        merge_msg = f"merge {work_branch} into {stable_branch} (via @bot main-merge)"
        code, _, err = await _run_git(
            project_path, "merge", "--no-ff", f"{remote}/{work_branch}",
            "-m", merge_msg, timeout=60,
        )
        if code != 0:
            await ctx.reply(
                f"❌ 실제 머지 실패 (dry-run 은 성공했는데 본 머지에서 문제): "
                f"```{err[:500]}```\n"
                f"수동 확인 필요"
            )
            return

        # push
        code, _, err = await _run_git(
            project_path, "push", remote, stable_branch, timeout=60,
        )
        if code != 0:
            await ctx.reply(
                f"⚠ 머지는 됐지만 push 실패 (원격 선행 등): ```{err[:500]}```\n"
                f"수동 push 필요"
            )
            return

        # 최종 commit 해시
        _, commit_out, _ = await _run_git(
            project_path, "rev-parse", stable_branch,
        )
        new_commit = commit_out.strip()[:8]

        embed = discord.Embed(
            title=f"🎉 `{work_branch}` → `{stable_branch}` 머지 완료",
            description=(
                f"**project**: `{project_id}`\n"
                f"**trigger**: reaction by <@{ctx.author.id}>"
            ),
            color=discord.Color.gold(),
        )
        embed.add_field(
            name="📍 머지 커밋",
            value=f"`{stable_branch}`: `{new_commit}`",
            inline=False,
        )
        embed.add_field(
            name="💡 다음 단계",
            value=(
                "• 필요 시 `wt/*` 브랜치 rebase (자동 orchestration 에서 자동 처리됨)\n"
                "• 협업자에게 `{stable}` 업데이트 공지\n"
                "• `@bot active` — 전체 프로젝트 작업 현황 확인"
            ).format(stable=stable_branch),
            inline=False,
        )
        try:
            await ctx.send(embed=embed)
        except Exception as e:
            await ctx.reply(f"머지 완료 (embed 전송 실패: {e})")

        # Step 5 (v0.46+): 자동 back-merge (main → work_branch) — work_branch 최신화.
        # .gitattributes merge=ours 덕분에 work_branch 의 .coord/ 는 자동 보존.
        await ctx.reply(
            f"🔄 `{project_id}` `{stable_branch}` → `{work_branch}` 역머지 진행 중 (work_branch 최신화)…"
        )
        ok_back, back_commit, back_msg = await _back_merge_main_to_work(
            project_path, remote, work_branch, stable_branch,
        )
        if ok_back:
            back_embed = discord.Embed(
                title=f"🔄 `{stable_branch}` → `{work_branch}` 역머지 완료",
                description=(
                    f"**project**: `{project_id}`\n"
                    f"**목적**: `{work_branch}` 를 `{stable_branch}` 최신 상태로 맞춤 "
                    f"(`.coord/` 는 `.gitattributes merge=ours` 로 자동 보존)"
                ),
                color=discord.Color.blue(),
            )
            back_embed.add_field(
                name="📍 역머지 커밋",
                value=f"`{work_branch}`: `{back_commit}`",
                inline=False,
            )
            try:
                await ctx.send(embed=back_embed)
            except Exception:
                pass
        else:
            await ctx.reply(
                f"⚠ 자동 역머지 실패 (main 머지는 성공): {back_msg}\n"
                f"수동 실행: `@bot back-merge {project_id}`"
            )
        return

    # 충돌 발생 — 파일 목록 수집 + abort
    code_files, conflict_files, _ = await _run_git(
        project_path, "diff", "--name-only", "--diff-filter=U",
    )
    conflict_list = [line for line in conflict_files.strip().split("\n") if line]

    await _run_git(project_path, "merge", "--abort")
    await _run_git(project_path, "checkout", stable_branch)
    await _run_git(project_path, "branch", "-D", temp_branch)

    embed = discord.Embed(
        title=f"⚠ `{work_branch}` → `{stable_branch}` 머지 충돌 감지",
        description=(
            f"**project**: `{project_id}`\n"
            f"**stable 안 건드림** — 자동 머지 중단, 사용자 수동 해결 필요"
        ),
        color=discord.Color.red(),
    )
    if conflict_list:
        files_str = "\n".join(f"• `{f}`" for f in conflict_list[:15])
        more = f"\n... 외 {len(conflict_list) - 15}개" if len(conflict_list) > 15 else ""
        embed.add_field(
            name=f"🔥 충돌 파일 ({len(conflict_list)}개)",
            value=files_str + more,
            inline=False,
        )
    embed.add_field(
        name="🔧 수동 해결 가이드",
        value=(
            "```bash\n"
            f"cd {project_path.name}\n"
            f"git checkout {stable_branch}\n"
            f"git merge {work_branch}\n"
            "# 충돌 파일 수정 후:\n"
            "git add <files>\n"
            "git commit\n"
            f"git push {remote} {stable_branch}\n"
            "```"
        ),
        inline=False,
    )
    try:
        await ctx.send(embed=embed)
    except Exception as e:
        await ctx.reply(f"충돌 {len(conflict_list)}개 감지 (embed 전송 실패: {e})")


@bot.command(name="back-merge")
async def cmd_back_merge(ctx: commands.Context, project_id: str | None = None) -> None:
    """v0.46: stable_branch (main) → work_branch 역머지.

    main 에 직접 hotfix 가 커밋된 경우 work_branch 최신화용. `.gitattributes
    merge=ours` 가 work_branch 의 .coord/ 를 자동 보존.

    @bot main-merge 는 자동으로 이 역머지를 실행. 이 명령은 **독립 호출용**
    (예: 사용자가 main 에 직접 패치 후 work_branch 맞추기).

    사용법:
      @bot back-merge                # 현재 채널 프로젝트
      @bot back-merge <project-id>
    """
    if not _authorized(ctx.author.id):
        await ctx.reply("⛔ 권한 없음")
        return

    bch = _binding_channel_id(ctx.channel)
    project_id, project = _resolve_project(channel_id=bch, token=project_id)
    if project is None:
        await ctx.reply("⚠ 프로젝트 resolve 실패 — `@bot projects` 확인")
        return
    if not _channel_ok(bch, project):
        await ctx.reply("⛔ 이 채널에선 해당 프로젝트 조작 불가")
        return

    project_path = Path(project["path"])
    remote, work_branch, stable_branch = _git_branches(project_path)

    if work_branch == stable_branch:
        await ctx.reply(
            f"ℹ `{project_id}` work_branch 와 stable_branch 가 같음 — 역머지 불필요"
        )
        return

    # clean state 확인
    code, out, _ = await _run_git(project_path, "status", "--porcelain")
    if code != 0 or out.strip():
        await ctx.reply(
            f"⚠ `{project_path.name}` uncommitted 변경 있음 — 정리 후 재시도"
        )
        return

    # fetch
    code, _, err = await _run_git(
        project_path, "fetch", remote, work_branch, stable_branch, timeout=60,
    )
    if code != 0:
        await ctx.reply(f"❌ fetch 실패: ```{err[:500]}```")
        return

    await ctx.reply(
        f"🔄 `{project_id}` `{stable_branch}` → `{work_branch}` 역머지 시작…"
    )

    ok, commit, msg = await _back_merge_main_to_work(
        project_path, remote, work_branch, stable_branch,
    )
    if not ok:
        await ctx.reply(f"❌ {msg}")
        return

    embed = discord.Embed(
        title=f"🔄 `{stable_branch}` → `{work_branch}` 역머지 완료",
        description=(
            f"**project**: `{project_id}`\n"
            f"**trigger**: manual `@bot back-merge` by <@{ctx.author.id}>\n"
            f"`.coord/` 는 `.gitattributes merge=ours` 로 자동 보존됨."
        ),
        color=discord.Color.blue(),
    )
    embed.add_field(
        name="📍 역머지 커밋",
        value=f"`{work_branch}`: `{commit}`",
        inline=False,
    )
    try:
        await ctx.send(embed=embed)
    except Exception:
        await ctx.reply(f"✅ 역머지 완료 (commit `{commit}`)")


def _list_sub_names(project_path: Path) -> list[str]:
    """v0.32: config.yml 에서 sub 이름 목록 추출."""
    cfg = _read_project_config(project_path)
    subs = cfg.get("subs") or []
    return [s.get("name", "").strip() for s in subs if s.get("name")]


def _list_project_worktrees(project_path: Path, subs: list[str]) -> list[tuple[str, Path]]:
    """v0.32: project 에 속한 worktree 목록 [(name, path), ...].
    head + 각 sub 의 sibling worktree 경로."""
    parent = project_path.parent
    project_dir_name = project_path.name
    # head worktree 는 <project>-wt-head (사용자의 flat 규칙 따름)
    # 하지만 디렉토리명이 `Sena_Calculator` 처럼 project.name 과 다를 수도 있음 — 모든 sibling 에서 sub 이름 접미사 매칭
    wts: list[tuple[str, Path]] = []
    # project.name 추출 (config.yml)
    base_name = _extract_project_name_from_config(project_path) or project_dir_name.lower()
    # head + subs
    candidates = ["head"] + list(subs)
    for name in candidates:
        wt_path = parent / f"{base_name}-wt-{name}"
        if wt_path.is_dir():
            wts.append((name, wt_path))
        else:
            # 다른 접미 패턴 시도 (프로젝트 디렉토리명 기반)
            wt_path = parent / f"{project_dir_name}-wt-{name}"
            if wt_path.is_dir():
                wts.append((name, wt_path))
    return wts


async def _branch_has_commits_ahead(
    worktree_path: Path, remote: str, work_branch: str
) -> tuple[int, str]:
    """해당 worktree 브랜치가 origin/<work_branch> 보다 몇 커밋 앞서있는지 + 현재 tip hash."""
    code, out, _ = await _run_git(
        worktree_path, "rev-list", "--count",
        f"{remote}/{work_branch}..HEAD",
    )
    ahead = 0
    try:
        ahead = int(out.strip())
    except Exception:
        pass
    code2, tip_out, _ = await _run_git(worktree_path, "rev-parse", "HEAD")
    tip = tip_out.strip()[:8] if code2 == 0 else ""
    return ahead, tip


async def _rollback_worktree_to_work_branch(
    worktree_path: Path, branch_name: str, remote: str, work_branch: str, ts: str,
) -> tuple[bool, str]:
    """worktree 의 현재 branch 를 failed/<branch>-<ts> 로 이동 + work_branch 최신으로 재생성.

    반환: (성공, 메시지)
    """
    # 1. failed branch 이름
    failed_branch = f"failed/{branch_name}-{ts}"

    # 2. fetch 최신
    code, _, err = await _run_git(worktree_path, "fetch", remote, work_branch, timeout=60)
    if code != 0:
        return False, f"fetch 실패: {err[:200]}"

    # 3. 현재 브랜치 failed/ 로 rename
    code, _, err = await _run_git(worktree_path, "branch", "-m", branch_name, failed_branch)
    if code != 0:
        return False, f"branch rename 실패: {err[:200]}"

    # 4. wt/<name> 을 work_branch tip 으로 재생성 + checkout
    code, _, err = await _run_git(
        worktree_path, "checkout", "-B", branch_name, f"{remote}/{work_branch}",
    )
    if code != 0:
        # 롤백 — rename 원복 시도
        await _run_git(worktree_path, "branch", "-m", failed_branch, branch_name)
        return False, f"checkout -B 실패 ({err[:200]}) — rename 원복"

    # 5. failed branch 원격 push (보존)
    code, _, err = await _run_git(
        worktree_path, "push", remote, failed_branch, timeout=60,
    )
    if code != 0:
        print(f"[rollback] {failed_branch} push 실패 (로컬엔 남음): {err[:200]}", flush=True)

    # 6. wt/<name> force push (원격도 reset)
    code, _, err = await _run_git(
        worktree_path, "push", "-f", remote, branch_name, timeout=60,
    )
    if code != 0:
        return True, f"로컬 reset OK, 원격 force-push 실패 ({err[:100]})"

    return True, f"{branch_name} → {failed_branch} 이동 + 원격 reset"


@bot.command(name="stop")
async def cmd_stop(ctx: commands.Context, project_id: str | None = None) -> None:
    """v0.32 재설계: STOP 시그널 + 작업 전 상태로 rollback.

    단계:
    1. project 식별 (project_id 또는 현재 채널)
    2. HEAD_LOCK active 한 inbox 감지
    3. STOP 시그널 작성 (coordination/STOP)
    4. HEAD_LOCK clear 대기 (최대 5분)
    5. 롤백:
       - plan / tasks / reports / pending review-inbox 파일 삭제
       - inbox status → cancelled
       - 각 worktree: 현재 브랜치를 failed/<branch>-<ts> 로 이동, work_branch 최신으로 재생성
    6. 결과 embed

    인자:
      @bot stop                 # 현재 채널 프로젝트
      @bot stop <project-id>    # 명시적 프로젝트
    """
    if not _authorized(ctx.author.id):
        await ctx.reply("⛔ 권한 없음")
        return

    bch = _binding_channel_id(ctx.channel)
    if project_id and project_id in PROJECTS:
        project = PROJECTS[project_id]
    else:
        project_id, project = _resolve_project(channel_id=bch, token=None)

    if project is None or project_id is None:
        await ctx.reply("⚠ 프로젝트 resolve 실패 — `@bot projects` 확인")
        return
    if not _channel_ok(bch, project):
        await ctx.reply("⛔ 이 채널에선 해당 프로젝트 조작 불가")
        return

    project_path = Path(project["path"])
    coord = _coord_root(project_path)
    remote, work_branch, _ = _git_branches(project_path)

    # Step 2: HEAD_LOCK 체크 + 활성 inbox 감지
    active, started, elapsed_min = _head_lock_info(project_id, project_path)
    target_inbox_id = _latest_unreviewed_inbox(project_id, project_path)

    await ctx.reply(
        f"🛑 `{project_id}` STOP 처리 시작\n"
        f"- HEAD_LOCK: {'🔒 active (' + str(elapsed_min) + '분 경과)' if active else '⚪ idle'}\n"
        f"- 대상 inbox: `{target_inbox_id or '(없음)'}`\n"
        f"→ STOP 시그널 작성 + head 종료 대기 + 롤백 진행…"
    )

    # Step 3: STOP 시그널 작성
    stop_path = coord / "coordination" / "STOP"
    try:
        stop_path.write_text(
            f"STOPPED at {datetime.now(timezone.utc).isoformat()} via Discord bot\n"
            f"project: {project_id}\n"
            f"inbox: {target_inbox_id or '-'}\n",
            encoding="utf-8",
        )
    except Exception as e:
        await ctx.reply(f"❌ STOP 파일 작성 실패: {e}")
        return

    # Step 4: HEAD_LOCK clear 대기 (최대 5분)
    if active:
        head_lock = _head_lock_path(project_id, project_path)
        cleared = await _wait_for_file_state(head_lock, want_exists=False, timeout=300, poll=10)
        if not cleared:
            await ctx.reply(
                f"⚠ `{project_id}` HEAD_LOCK 5분 내 clear 안 됨 — 롤백 skip\n"
                f"head 수동 종료 후 재시도: `@bot stop {project_id}`"
            )
            return

    # Step 5: 롤백 수행
    ts = datetime.now().strftime("%Y%m%d-%H%M%S")
    removed_files: list[str] = []
    skipped_files: list[str] = []

    # 5-a: plan / tasks / reports / pending review-inbox 파일 삭제 (head worktree + work_branch 양쪽)
    # v0.43: base 별로 _coord_root 적용 — subdir 모드에선 base/.coord/coordination
    base_name = _extract_project_name_from_config(project_path) or project_path.name.lower()
    head_wt = project_path.parent / f"{base_name}-wt-head"
    search_dirs = [project_path, head_wt]

    for base in search_dirs:
        coord_sub = _coord_root(base) / "coordination"
        if not coord_sub.is_dir():
            continue
        # plan
        if target_inbox_id:
            plan_file = coord_sub / "plans" / f"{target_inbox_id}.md"
            if plan_file.exists():
                try:
                    plan_file.unlink()
                    removed_files.append(str(plan_file.relative_to(project_path.parent)))
                except Exception:
                    skipped_files.append(f"{plan_file} (삭제 실패)")
        # tasks — inbox id 언급된 것만
        tasks_dir = coord_sub / "tasks"
        if tasks_dir.is_dir() and target_inbox_id:
            for tf in tasks_dir.glob("wt-*.md"):
                try:
                    if target_inbox_id in tf.read_text(encoding="utf-8", errors="ignore"):
                        tf.unlink()
                        removed_files.append(str(tf.relative_to(project_path.parent)))
                except Exception:
                    pass
        # reports — inbox id 언급된 것만
        reports_dir = coord_sub / "reports"
        if reports_dir.is_dir() and target_inbox_id:
            for rf in reports_dir.glob("wt-*.md"):
                try:
                    if target_inbox_id in rf.read_text(encoding="utf-8", errors="ignore"):
                        rf.unlink()
                        removed_files.append(str(rf.relative_to(project_path.parent)))
                except Exception:
                    pass
        # review-inbox — inbox id 포함 + verdict 없을 때만
        review_dir = coord_sub / "review-inbox"
        if review_dir.is_dir() and target_inbox_id:
            for rf in review_dir.glob("*.md"):
                if rf.name.startswith(("_", ".")):
                    continue
                try:
                    text = rf.read_text(encoding="utf-8", errors="ignore")
                    if target_inbox_id in text:
                        v = _VERDICT_RE.search(text)
                        if v and v.group(1).lower() in ("go", "needs-fix", "block"):
                            skipped_files.append(
                                f"{rf.name} (verdict: {v.group(1)} — 보존)"
                            )
                        else:
                            rf.unlink()
                            removed_files.append(str(rf.relative_to(project_path.parent)))
                except Exception:
                    pass

    # 5-b: inbox status → cancelled
    # v0.43: subdir 모드 대응 — _coord_root(project_path) 사용
    inbox_status_note = ""
    if target_inbox_id:
        inbox_file = _coord_root(project_path) / "coordination" / "inbox" / f"{target_inbox_id}.md"
        if inbox_file.exists():
            try:
                text = inbox_file.read_text(encoding="utf-8", errors="replace")
                new_text = re.sub(
                    r"(^-\s*\*?\*?status\*?\*?:\s*)pending",
                    r"\1cancelled",
                    text,
                    count=1,
                    flags=re.IGNORECASE | re.MULTILINE,
                )
                if new_text != text:
                    inbox_file.write_text(new_text, encoding="utf-8")
                    inbox_status_note = f"`{target_inbox_id}.md` → `status: cancelled`"
            except Exception:
                pass

    # 5-c: 각 worktree 브랜치 failed/ 로 이동
    subs = _list_sub_names(project_path)
    worktrees = _list_project_worktrees(project_path, subs)
    moved: list[str] = []
    wt_skipped: list[str] = []

    for name, wt_path in worktrees:
        branch = f"wt/{name}"
        ahead, tip = await _branch_has_commits_ahead(wt_path, remote, work_branch)
        if ahead == 0:
            wt_skipped.append(f"`{branch}` (ahead 0)")
            continue
        ok, msg = await _rollback_worktree_to_work_branch(
            wt_path, branch, remote, work_branch, ts,
        )
        if ok:
            moved.append(f"`{branch}` (+{ahead}, tip `{tip}`) → `failed/{branch}-{ts}`")
        else:
            wt_skipped.append(f"`{branch}`: {msg}")

    # Step 6: 결과 embed
    embed = discord.Embed(
        title=f"🛑 `{project_id}` STOP + 롤백 완료",
        description=(
            f"**inbox**: `{target_inbox_id or '(없음)'}`\n"
            f"**work_branch**: `{work_branch}`\n"
            f"**trigger**: <@{ctx.author.id}>"
        ),
        color=discord.Color.red(),
    )
    if inbox_status_note:
        embed.add_field(name="📬 inbox 상태", value=inbox_status_note, inline=False)
    if removed_files:
        lines = "\n".join(f"• `{f}`" for f in removed_files[:15])
        more = f"\n... 외 {len(removed_files) - 15}개" if len(removed_files) > 15 else ""
        embed.add_field(
            name=f"🗑 삭제된 파일 ({len(removed_files)}개)",
            value=lines + more,
            inline=False,
        )
    if moved:
        embed.add_field(
            name=f"🔀 브랜치 이동 ({len(moved)}개)",
            value="\n".join(f"• {m}" for m in moved)[:1000],
            inline=False,
        )
    if wt_skipped:
        embed.add_field(
            name="⏭ worktree skip",
            value="\n".join(f"• {s}" for s in wt_skipped)[:1000],
            inline=False,
        )
    if skipped_files:
        embed.add_field(
            name="🔒 보존 (verdict 확정)",
            value="\n".join(f"• {f}" for f in skipped_files[:10])[:1000],
            inline=False,
        )
    embed.add_field(
        name="💡 STOP 해제",
        value=f"수동: `rm {stop_path}` → commit + push",
        inline=False,
    )
    try:
        await ctx.send(embed=embed)
    except Exception as e:
        await ctx.reply(f"STOP + 롤백 완료 (embed 전송 실패: {e})")


def _extract_project_name_from_config(project_path: Path) -> str | None:
    """v0.20: <path>/coordination/config.yml 또는 <path>/.coord/coordination/config.yml 에서
    project.name 을 추출. 찾지 못하면 None."""
    for rel in ("coordination/config.yml", ".coord/coordination/config.yml"):
        cfg_path = project_path / rel
        if not cfg_path.exists():
            continue
        try:
            with cfg_path.open(encoding="utf-8") as f:
                data = yaml.safe_load(f) or {}
            name = ((data.get("project") or {}).get("name") or "").strip()
            if name:
                return name
        except Exception as e:
            print(f"[register] config.yml 파싱 실패 ({cfg_path}): {e}", flush=True)
    return None


def _read_project_config(project_path: Path) -> dict[str, Any]:
    """v0.27: 프로젝트의 coordination/config.yml 전체를 dict 로 반환. 없으면 빈 dict."""
    for rel in ("coordination/config.yml", ".coord/coordination/config.yml"):
        cfg_path = project_path / rel
        if not cfg_path.exists():
            continue
        try:
            with cfg_path.open(encoding="utf-8") as f:
                return yaml.safe_load(f) or {}
        except Exception as e:
            print(f"[config] 파싱 실패 ({cfg_path}): {e}", flush=True)
    return {}


def _git_branches(project_path: Path) -> tuple[str, str, str]:
    """v0.27: config.yml 의 git.{remote, work_branch, stable_branch} 반환.
    기본값: (origin, main, main)."""
    cfg = _read_project_config(project_path)
    git_cfg = cfg.get("git") or {}
    remote = git_cfg.get("remote", "origin").strip()
    work = git_cfg.get("work_branch", "main").strip()
    stable = git_cfg.get("stable_branch", "main").strip()
    return remote, work, stable


def _project_coord_dir(project_path: Path) -> Path | None:
    """v0.22: 프로젝트의 coordination/ 디렉토리 경로 (flat 또는 subdir 모드)."""
    for rel in ("coordination", ".coord/coordination"):
        candidate = project_path / rel
        if candidate.is_dir():
            return candidate
    return None


def _write_project_bot_yml(
    project_path: Path,
    discord_channel_id: str | None = None,
    discord_alert_channel_id: str | None = None,
    clear_channel: bool = False,
    clear_alert: bool = False,
) -> tuple[bool, str]:
    """v0.22: <project>/coordination/bot.yml 생성/갱신.

    봇 바인딩 정보를 프로젝트 자체에도 기록 — 이식성 (git clone 시 채널 정보 동반) +
    명시성 (프로젝트 파일 보고 "어느 채널에 바인딩됐는지" 확인 가능).

    중앙 `bot/projects.yml` 이 여전히 런타임 source of truth — bot.yml 은 사본/기록.

    Returns: (성공 여부, 메시지)
    """
    coord_dir = _project_coord_dir(project_path)
    if coord_dir is None:
        return False, "coordination/ 디렉토리 없음"

    bot_yml = coord_dir / "bot.yml"

    # 기존 내용 로드
    data: dict[str, Any] = {}
    if bot_yml.exists():
        try:
            with bot_yml.open(encoding="utf-8") as f:
                data = yaml.safe_load(f) or {}
        except Exception as e:
            print(f"[bot-sync] bot.yml 파싱 실패 ({bot_yml}): {e}", flush=True)
            data = {}

    # 필드 업데이트
    if clear_channel:
        data.pop("discord_channel_id", None)
    elif discord_channel_id is not None:
        data["discord_channel_id"] = str(discord_channel_id)

    if clear_alert:
        data.pop("discord_alert_channel_id", None)
    elif discord_alert_channel_id is not None:
        data["discord_alert_channel_id"] = str(discord_alert_channel_id)

    # 업데이트 시각
    data["updated_at"] = datetime.now(timezone.utc).astimezone().strftime("%Y-%m-%d %H:%M %Z")

    # 빈 dict 면 파일 삭제 (channel 없고 alert 없음)
    has_bindings = "discord_channel_id" in data or "discord_alert_channel_id" in data
    if not has_bindings:
        if bot_yml.exists():
            try:
                bot_yml.unlink()
                return True, f"bot.yml 삭제 (바인딩 없음)"
            except Exception as e:
                return False, f"bot.yml 삭제 실패: {e}"
        return True, "변경 없음"

    # 헤더 주석 포함해 저장
    try:
        header = (
            "# coord bot 바인딩 (v0.22+ 자동 생성)\n"
            "# 이 파일은 봇이 `@bot register` / `@bot bind` 시 자동 갱신.\n"
            "# 중앙 source of truth 는 `bot/projects.yml` — 이 파일은 프로젝트-side 사본.\n"
            "# git 에 커밋하면 clone 시 바인딩 정보 이식됨.\n\n"
        )
        body = yaml.dump(data, allow_unicode=True, sort_keys=False, indent=2)
        bot_yml.write_text(header + body, encoding="utf-8")
        return True, f"bot.yml 갱신"
    except Exception as e:
        return False, f"bot.yml 저장 실패: {e}"


@bot.command(name="register")
async def cmd_register(
    ctx: commands.Context, first: str, second: str = ""
) -> None:
    """레지스트리에 프로젝트 추가 + 현재 채널 자동 바인딩.

    사용법:
      @bot register <path>           # v0.20: config.yml 의 project.name 자동 추출
      @bot register <id> <path>      # 명시적 id
    """
    if not _authorized(ctx.author.id):
        await ctx.reply("⛔ 권한 없음")
        return

    # 1-arg vs 2-arg 판별
    if not second:
        # 1-arg 형식: first = path, id 는 config.yml 에서 추출
        try:
            path = Path(first).expanduser().resolve()
        except Exception as e:
            await ctx.reply(
                f"⚠ 경로 해석 실패: {e}\n"
                f"사용: `@bot register <path>` 또는 `@bot register <id> <path>`"
            )
            return
        if not path.is_dir():
            await ctx.reply(
                f"⚠ 경로 없음/디렉토리 아님: `{first}`\n"
                f"사용: `@bot register <path>` 또는 `@bot register <id> <path>`"
            )
            return
        project_id = _extract_project_name_from_config(path)
        if not project_id:
            await ctx.reply(
                f"⚠ `{path}/coordination/config.yml` 에서 `project.name` 을 찾지 못함.\n"
                f"id 명시 필요: `@bot register <id> {path}`"
            )
            return
    else:
        # 2-arg 형식: first = id, second = path
        project_id = first
        try:
            path = Path(second).expanduser().resolve()
        except Exception as e:
            await ctx.reply(f"⚠ 경로 해석 실패: {e}")
            return
        if not path.is_dir():
            await ctx.reply(f"⚠ 경로 없음/디렉토리 아님: `{path}`")
            return

    if not _coord_installed(path):
        await ctx.reply(
            f"⚠ coord 미설치 — `{path}` 에서 먼저 `bash scripts/init.sh` 또는 "
            f"`bash .coord/scripts/init.sh` 실행 후 재시도"
        )
        return

    # 중복 id 체크 (enabled 여부 무관)
    existing = {p.get("id") for p in _RAW_CFG.get("projects", [])}
    if project_id in existing:
        await ctx.reply(
            f"⚠ `{project_id}` 이미 등록됨 — `@bot bind {project_id}` 로 채널만 갱신하거나 "
            f"`@bot unregister {project_id}` 후 재등록"
        )
        return

    entry = {
        "id": project_id,
        "path": str(path),
        "enabled": True,
        "discord_channel_id": str(_binding_channel_id(ctx.channel)),
    }
    _RAW_CFG.setdefault("projects", []).append(entry)
    try:
        _save_registry()
    except Exception as e:
        # rollback in-memory
        _RAW_CFG["projects"].pop()
        await ctx.reply(f"❌ projects.yml 저장 실패: {e}")
        return
    _load_registry()
    auto_note = " (config.yml 자동 감지)" if not second else ""

    # v0.22: 프로젝트 측 bot.yml 에도 기록 (이식성)
    bot_yml_ok, bot_yml_msg = _write_project_bot_yml(
        path, discord_channel_id=str(_binding_channel_id(ctx.channel)),
    )
    bot_yml_note = ""
    if bot_yml_ok:
        bot_yml_note = "\n- 프로젝트 `coordination/bot.yml` 에도 기록됨 (git commit 권장)"
    else:
        bot_yml_note = f"\n- ⚠ 프로젝트 bot.yml 쓰기 실패: {bot_yml_msg}"

    await ctx.reply(
        f"✅ `{project_id}` 등록 완료{auto_note}\n"
        f"- path: `{path}`\n"
        f"- channel: `{_binding_channel_id(ctx.channel)}` (이 채널{' — parent' if isinstance(ctx.channel, discord.Thread) else ''})"
        f"{bot_yml_note}\n"
        f"이제 이 채널에서 `@bot <text>` 만 쳐도 자동 라우팅됨"
    )


@bot.command(name="unregister")
async def cmd_unregister(ctx: commands.Context, project_id: str) -> None:
    if not _authorized(ctx.author.id):
        await ctx.reply("⛔ 권한 없음")
        return

    projects_list = _RAW_CFG.get("projects", [])
    idx = next((i for i, p in enumerate(projects_list) if p.get("id") == project_id), -1)
    if idx < 0:
        await ctx.reply(f"⚠ `{project_id}` 레지스트리에 없음")
        return

    removed = projects_list.pop(idx)
    try:
        _save_registry()
    except Exception as e:
        projects_list.insert(idx, removed)
        await ctx.reply(f"❌ 저장 실패: {e}")
        return
    _load_registry()
    await ctx.reply(
        f"🗑 `{project_id}` 레지스트리 해제 — 프로젝트 코드/데이터는 삭제 안 됨"
    )


@bot.command(name="bind")
async def cmd_bind(ctx: commands.Context, project_id: str) -> None:
    """이미 등록된 프로젝트를 현재 채널에 (재)바인딩."""
    if not _authorized(ctx.author.id):
        await ctx.reply("⛔ 권한 없음")
        return

    target = next(
        (p for p in _RAW_CFG.get("projects", []) if p.get("id") == project_id), None
    )
    if not target:
        await ctx.reply(
            f"⚠ `{project_id}` 없음 — `@bot register {project_id} <path>` 로 먼저 등록"
        )
        return

    bind_id = _binding_channel_id(ctx.channel)
    target["discord_channel_id"] = str(bind_id)
    try:
        _save_registry()
    except Exception as e:
        await ctx.reply(f"❌ 저장 실패: {e}")
        return
    _load_registry()

    # v0.22: 프로젝트 bot.yml 도 갱신
    bot_yml_note = ""
    try:
        path = Path(target["path"])
        ok, msg = _write_project_bot_yml(path, discord_channel_id=str(bind_id))
        if ok:
            bot_yml_note = "\n프로젝트 `coordination/bot.yml` 갱신됨"
    except Exception:
        pass

    await ctx.reply(
        f"🔗 `{project_id}` ↔ 이 채널 (`{bind_id}`{' — parent' if isinstance(ctx.channel, discord.Thread) else ''}) 바인딩"
        f"{bot_yml_note}"
    )


@bot.command(name="unbind")
async def cmd_unbind(ctx: commands.Context, project_id: str) -> None:
    """프로젝트의 채널 바인딩 해제 (프로젝트는 레지스트리에 남음)."""
    if not _authorized(ctx.author.id):
        await ctx.reply("⛔ 권한 없음")
        return

    target = next(
        (p for p in _RAW_CFG.get("projects", []) if p.get("id") == project_id), None
    )
    if not target:
        await ctx.reply(f"⚠ `{project_id}` 없음")
        return

    if "discord_channel_id" not in target or not str(target.get("discord_channel_id", "")).strip():
        await ctx.reply(f"⚠ `{project_id}` 에 바인딩된 채널 없음")
        return

    target.pop("discord_channel_id", None)
    try:
        _save_registry()
    except Exception as e:
        await ctx.reply(f"❌ 저장 실패: {e}")
        return
    _load_registry()

    # v0.22: 프로젝트 bot.yml 에서도 channel 제거
    bot_yml_note = ""
    try:
        path = Path(target["path"])
        ok, msg = _write_project_bot_yml(path, clear_channel=True)
        if ok:
            bot_yml_note = f"\n프로젝트 `coordination/bot.yml` {msg}"
    except Exception:
        pass

    await ctx.reply(
        f"🔓 `{project_id}` 채널  바인딩 해제{bot_yml_note}"
    )


@bot.command(name="send")
async def cmd_send(ctx: commands.Context, *, text: str = "") -> None:
    """작업 지시를 `/inbox-send` 로 spawn. v0.11+ 명시적 명령 버전.

    사용법:
      @bot send <지시 내용>
      @bot send <project-id> <지시 내용>   (첫 단어가 project-id 면 그 프로젝트로)
    """
    if not _authorized(ctx.author.id):
        await ctx.reply("⛔ 권한 없음")
        return
    text = (text or "").strip()
    if not text:
        await ctx.reply("⚠ 지시 내용 없음 — `@bot send <text>`")
        return

    # 첫 토큰이 project id 면 그 뒤가 본문
    parts = text.split(maxsplit=1)
    first, rest = parts[0], (parts[1] if len(parts) > 1 else "")
    bch = _binding_channel_id(ctx.channel)
    if first in PROJECTS:
        project_id, project = first, PROJECTS[first]
        body = rest
    else:
        project_id, project = _resolve_project(channel_id=bch, token=None)
        body = text

    if project is None:
        await ctx.reply("⚠ 프로젝트 resolve 실패 — `@bot projects` 확인")
        return
    if not _channel_ok(bch, project):
        await ctx.reply("⛔ 이 채널에선 해당 프로젝트 조작 불가")
        return
    if not body:
        await ctx.reply(f"⚠ `{project_id}` 로 보낼 내용이 없음")
        return

    project_path = Path(project["path"])

    # v0.17: Discord 첨부 이미지 자동 다운로드 → coordination/inbox/attachments/
    attach_lines, saved_attachments = await _download_attachments(
        ctx.message, project_id, project_path
    )

    # 본문 + ## 첨부 섹션 결합
    final_body = body
    if attach_lines:
        final_body = body + "\n\n## 첨부\n" + "\n".join(attach_lines)

    job = _QueueJob(
        project_id=project_id,
        project_path=project_path,
        prompt=f"/inbox-send {final_body}",
        source_message=ctx.message,
        preview=body[:60],
    )
    pos = await _enqueue_job(project_id, job)

    if pos == 1:
        coord = _coord_root(project_path)
        busy = (coord / "coordination" / "HEAD_LOCK").exists()
        status = "즉시 처리" if not busy else "HEAD_LOCK 해제 대기 중"
        attach_note = f" (📎 {len(saved_attachments)})" if saved_attachments else ""
        await ctx.reply(f"⏳ `{project_id}` 큐 추가{attach_note} — {status}")
    else:
        attach_note = f" (📎 {len(saved_attachments)})" if saved_attachments else ""
        await ctx.reply(
            f"⏳ `{project_id}` 큐 **{pos}번째** 대기{attach_note} — 앞 job 완료 후 자동 spawn"
        )


@bot.command(name="review")
async def cmd_review(ctx: commands.Context, inbox_id: str | None = None) -> None:
    """head 처리 결과 검토. id 생략 시 가장 최근 미review inbox 자동 선택.

    v0.21: review 완료 대기 후 verdict prompt embed 자동 표시 — ✅/❌ 리액션으로
    머지/보류 선택 가능. 이전엔 spawn 만 하고 사용자가 수동 확인 필요했음.

    사용법:
      @bot review              # 최근 미review 자동
      @bot review <inbox-id>   # 특정 inbox
    """
    if not _authorized(ctx.author.id):
        await ctx.reply("⛔ 권한 없음")
        return

    bch = _binding_channel_id(ctx.channel)
    project_id, project = _resolve_project(channel_id=bch, token=None)
    if project is None:
        await ctx.reply("⚠ 프로젝트 resolve 실패 — `@bot projects` 확인")
        return
    if not _channel_ok(bch, project):
        await ctx.reply("⛔ 이 채널에선 해당 프로젝트 조작 불가")
        return

    project_path = Path(project["path"])
    if inbox_id is None:
        inbox_id = _latest_unreviewed_inbox(project_id, project_path)
        if inbox_id is None:
            await ctx.reply(f"✅ `{project_id}` 검토 대기 inbox 없음 (모두 review 됨)")
            return
        await ctx.reply(f"🔎 `{project_id}` 최근 미review inbox 자동 선택: `{inbox_id}`")

    target_channel = ctx.channel
    is_postable = isinstance(target_channel, (discord.TextChannel, discord.Thread))

    # v0.26: 이미 verdict prompt 가 채널에 있으면 사용자에게 jump URL 응답 + 종료
    if is_postable:
        existing_prompt = await _find_existing_verdict_prompt(target_channel, inbox_id)
        if existing_prompt is not None:
            await ctx.reply(
                f"⚠ `{project_id}` `{inbox_id}` verdict prompt 이미 존재\n"
                f"{existing_prompt.jump_url}\n"
                f"→ 기존 prompt 에 리액션 (✅/❌) 사용. 새 review 실행 안 함."
            )
            return

    # v0.26: verdict 이미 기록돼 있으면 spawn 생략, 즉시 prompt 표시
    # (_post_verdict_prompt 내부 dedup 이 최종 방어)
    existing_verdict, existing_fname = _read_verdict(project_id, project_path, inbox_id)
    if existing_verdict is not None:
        await ctx.reply(
            f"✨ `{project_id}` `{inbox_id}` verdict 이미 기록됨: `{existing_verdict}` — "
            f"review spawn 생략, prompt 표시 시도"
        )
        if is_postable:
            await _post_verdict_prompt(
                target_channel, project_id, inbox_id, existing_verdict, existing_fname,
                project_path=project_path,
            )
        return

    # v0.21: review 완료 대기 + verdict prompt
    # verdict-only 플래그로 head 의 Step 7 (머지) 은 건너뜀 — 머지는 리액션 ✅ 후에만.
    await ctx.reply(f"⏳ `{project_id}` `{inbox_id}` review 실행 중 (최대 10분)…")

    review_prompt = f"/review-inbox {inbox_id} --verdict-only"
    ok, out = await _spawn_and_wait(review_prompt, cwd=project_path, timeout=600)
    if not ok:
        await ctx.reply(
            f"❌ `{project_id}` review 실패: ```{(out or '')[:500]}```"
        )
        return

    verdict, review_fname = _read_verdict(project_id, project_path, inbox_id)
    if verdict is None:
        await ctx.reply(
            f"⚠ `{project_id}` review 완료 but verdict 파일에서 읽지 못함 — "
            f"review-inbox 파일 직접 확인 필요"
        )
        return

    # verdict prompt embed + ✅/❌ 리액션 (자동 orchestration 과 동일 경로)
    if is_postable:
        await _post_verdict_prompt(
            target_channel, project_id, inbox_id, verdict, review_fname,
            project_path=project_path,
        )
    else:
        await ctx.reply(f"✅ review 완료 — verdict: `{verdict}` (채널 타입 미지원, 수동 확인)")


@bot.command(name="fix")
async def cmd_fix(ctx: commands.Context, *, description: str = "") -> None:
    """긴급 수정 — /fix spawn. description 생략 시 review-inbox 의 needs-fix 항목 자동 탐색.

    사용법:
      @bot fix <설명>           # 직접 지시
      @bot fix                 # 최근 needs-fix review 자동 탐색
    """
    if not _authorized(ctx.author.id):
        await ctx.reply("⛔ 권한 없음")
        return

    bch = _binding_channel_id(ctx.channel)
    project_id, project = _resolve_project(channel_id=bch, token=None)
    if project is None:
        await ctx.reply("⚠ 프로젝트 resolve 실패")
        return
    if not _channel_ok(bch, project):
        await ctx.reply("⛔ 이 채널에선 해당 프로젝트 조작 불가")
        return

    project_path = Path(project["path"])
    description = (description or "").strip()

    if not description:
        # 자동 탐색: needs-fix review-inbox
        title, fname = _find_needs_fix_candidate(project_id, project_path)
        if title is None:
            await ctx.reply(f"✅ `{project_id}` needs-fix 대상 없음 — 지시 내용 필요하면 `@bot fix <설명>`")
            return
        description = f"review-inbox/{fname} needs-fix 처리: {title}"
        await ctx.reply(f"🔎 `{project_id}` 자동 탐색: `{fname}` → /fix 스폰")

    ok, msg = await _spawn_claude_command(
        project_id, project_path,
        f"/fix {description}",
        check_head_lock=False,
        label="fix",
    )
    await ctx.reply(msg)


@bot.command(name="resolve")
async def cmd_resolve(ctx: commands.Context, esc_id: str, *, reason: str = "") -> None:
    """escalation 승인 — 리액션 ✅ 대체 텍스트 버전.
    `@bot resolve <id> [reason]`
    """
    if not _authorized(ctx.author.id):
        await ctx.reply("⛔ 권한 없음")
        return
    bch = _binding_channel_id(ctx.channel)
    project_id, project = _resolve_project(channel_id=bch, token=None)
    if project is None:
        await ctx.reply("⚠ 프로젝트 resolve 실패")
        return
    ok, msg = await _spawn_escalation_response(
        project_id, Path(project["path"]), "resolve", esc_id,
        reason or f"Discord /resolve by <@{ctx.author.id}>",
    )
    await ctx.reply(msg)


@bot.command(name="reject")
async def cmd_reject(ctx: commands.Context, esc_id: str, *, reason: str = "") -> None:
    """escalation 거부 — 리액션 ❌ 대체 텍스트 버전.
    `@bot reject <id> [reason]`
    """
    if not _authorized(ctx.author.id):
        await ctx.reply("⛔ 권한 없음")
        return
    bch = _binding_channel_id(ctx.channel)
    project_id, project = _resolve_project(channel_id=bch, token=None)
    if project is None:
        await ctx.reply("⚠ 프로젝트 resolve 실패")
        return
    ok, msg = await _spawn_escalation_response(
        project_id, Path(project["path"]), "reject", esc_id,
        reason or f"Discord /reject by <@{ctx.author.id}>",
    )
    await ctx.reply(msg)


@bot.command(name="queue")
async def cmd_queue(ctx: commands.Context, project_id: str | None = None) -> None:
    """현재 in-memory 큐 상태 조회. v0.10+.
    `@bot queue` — 현재 채널 바인딩 프로젝트 또는 전체
    `@bot queue <project-id>` — 특정 프로젝트
    """
    if not _authorized(ctx.author.id):
        await ctx.reply("⛔ 권한 없음")
        return

    # 대상 프로젝트 선택
    bch = _binding_channel_id(ctx.channel)
    if project_id and project_id in PROJECTS:
        targets = [project_id]
    elif project_id:
        await ctx.reply(f"⚠ `{project_id}` 없음")
        return
    else:
        resolved_id, resolved = _resolve_project(channel_id=bch, token=None)
        if resolved and not project_id:
            targets = [resolved_id]
        else:
            targets = list(PROJECTS.keys())

    embed = discord.Embed(title="🗂 inbox 큐 상태", color=discord.Color.blurple())
    total_pending = 0
    for pid in targets:
        s = _queue_state_summary(pid)
        coord = _coord_root(Path(PROJECTS[pid]["path"]))
        lock_active = (coord / "coordination" / "HEAD_LOCK").exists()
        running = "🟢 실행 중" if lock_active else "⚪ 대기"
        worker = "🔄 worker live" if s["worker_running"] else "💤 worker idle"
        total_pending += s["queue_size"]
        embed.add_field(
            name=f"`{pid}`",
            value=f"{running} · {worker}\n큐 대기: **{s['queue_size']}** job",
            inline=False,
        )
    embed.set_footer(
        text=f"총 대기 job: {total_pending} · worker 는 10분 idle 시 자동 종료"
    )
    await ctx.reply(embed=embed)


@bot.command(name="retry")
async def cmd_retry(ctx: commands.Context, inbox_id: str, project_id: str | None = None) -> None:
    """실패한 plan 의 Step 재dispatch — head worktree 에서 /retry <inbox-id> spawn."""
    if not _authorized(ctx.author.id):
        await ctx.reply("⛔ 권한 없음")
        return

    # project: 명시 > 채널 바인딩 > default
    bch = _binding_channel_id(ctx.channel)
    resolved_id, project = _resolve_project(channel_id=bch, token=project_id)
    if not project:
        await ctx.reply("⚠ 프로젝트 resolve 실패 — `@bot projects` 확인 또는 `@bot retry <inbox-id> <project-id>`")
        return
    if not _channel_ok(bch, project):
        await ctx.reply("⛔ 이 채널에선 해당 프로젝트 조작 불가")
        return

    ok, msg = await _spawn_head_retry(resolved_id, Path(project["path"]), inbox_id)
    await ctx.reply(msg)


@bot.command(name="budget")
async def cmd_budget(ctx: commands.Context, project_id: str | None = None) -> None:
    """토큰 사용량 요약. project_id 생략 시 현재 채널 바인딩 또는 전체."""
    if not _authorized(ctx.author.id):
        await ctx.reply("⛔ 권한 없음")
        return

    # project 지정되면 그 하나만, 아니면 현재 채널 바인딩 or 전체
    bch = _binding_channel_id(ctx.channel)
    if project_id and project_id in PROJECTS:
        targets = [(project_id, PROJECTS[project_id])]
    else:
        resolved_id, resolved = _resolve_project(channel_id=bch, token=None)
        if resolved is not None and not project_id:
            # 채널 바인딩이 있으면 그것만
            targets = [(resolved_id, resolved)]
        else:
            targets = list(PROJECTS.items())

    if not targets:
        await ctx.reply("⚠ 등록 프로젝트 없음")
        return

    embed = discord.Embed(title="💰 토큰 예산 요약", color=discord.Color.gold())
    total_used = 0
    total_limit = 0
    for pid, p in targets:
        if not _channel_ok(bch, p):
            continue
        coord = _coord_root(Path(p["path"]))
        b = _parse_token_budget(coord)
        if b is None:
            embed.add_field(name=f"`{pid}`", value="⚠ token-budget.md 없음", inline=False)
            continue
        total_used += b["used"]
        total_limit += b["limit"]
        bar = _progress_bar(b["percent"])
        embed.add_field(
            name=f"`{pid}` — {b['percent']}%",
            value=(
                f"{bar}\n"
                f"used: **{b['used']:,}** / limit: **{b['limit']:,}**\n"
                f"week_start: `{b['week_start'] or 'n/a'}` · last_alert: {b['last_alert']}%"
            ),
            inline=False,
        )

    if len(targets) > 1 and total_limit > 0:
        agg_pct = round(total_used * 100 / total_limit, 2)
        embed.set_footer(text=f"합계: {total_used:,} / {total_limit:,} ({agg_pct}%)")

    await ctx.reply(embed=embed)


def _progress_bar(pct: float, width: int = 20) -> str:
    filled = min(width, max(0, int(round(pct * width / 100))))
    return "▰" * filled + "▱" * (width - filled)


@bot.command(name="usage")
async def cmd_usage(ctx: commands.Context, days: int = 7) -> None:
    """Claude CLI 실 토큰/비용 (ccusage 기반).
    사용법: `@bot usage [days=7]` — 최근 N일 daily 요약.
    """
    if not _authorized(ctx.author.id):
        await ctx.reply("⛔ 권한 없음")
        return

    if days < 1 or days > 90:
        await ctx.reply("⚠ `days` 는 1~90 범위")
        return

    # since = today - days (UTC 기준 YYYYMMDD)
    since_dt = datetime.now(timezone.utc) - timedelta(days=days - 1)
    since_str = since_dt.strftime("%Y%m%d")

    # 디버그 / 응답 지연 대비 typing
    async with ctx.typing():
        ok, result = await _run_ccusage(["daily", "--since", since_str, "--order", "desc"])
    if not ok:
        await ctx.reply(f"❌ {result}")
        return

    daily = result.get("daily", []) if isinstance(result, dict) else []
    if not daily:
        await ctx.reply(f"⚠ 최근 {days}일 사용 데이터 없음 (`~/.claude/projects/*.jsonl`)")
        return

    total_cost = sum(d.get("totalCost", 0) for d in daily)
    total_tokens = sum(d.get("totalTokens", 0) for d in daily)
    total_days = len(daily)
    avg_daily_cost = total_cost / total_days if total_days else 0

    embed = discord.Embed(
        title=f"📊 Claude CLI 사용량 — 최근 {days}일",
        description=(
            f"총 **${total_cost:.2f}** · {total_tokens:,} tokens\n"
            f"평균: **${avg_daily_cost:.2f}/day** ({total_days} 활성일)"
        ),
        color=discord.Color.teal(),
    )

    # 상위 5일 (이미 desc 정렬됨)
    shown = daily[:5]
    for d in shown:
        models = d.get("modelsUsed", [])
        # 모델명 축약 (claude-opus-4-6 → opus, claude-haiku-4-5-... → haiku)
        short_models = []
        for m in models:
            parts = m.split("-")
            short_models.append(parts[1] if len(parts) >= 2 else m)
        models_str = ", ".join(short_models) or "?"

        embed.add_field(
            name=f"📅 {d.get('date', '?')} · ${d.get('totalCost', 0):.2f}",
            value=f"{d.get('totalTokens', 0):,} tokens · models: {models_str}",
            inline=False,
        )

    if len(daily) > 5:
        embed.add_field(
            name="…",
            value=f"추가 {len(daily) - 5}일은 `npx ccusage daily --since {since_str}` 직접 실행",
            inline=False,
        )

    embed.set_footer(text="powered by ccusage · Claude CLI 전체 집계 (프로젝트 무관)")
    await ctx.reply(embed=embed)


def _short_models(models: list[str]) -> str:
    short: list[str] = []
    for m in models:
        parts = m.split("-")
        short.append(parts[1] if len(parts) >= 2 else m)
    return ", ".join(dict.fromkeys(short)) or "?"


def _fmt_duration_minutes(minutes: int) -> str:
    if minutes < 0:
        minutes = 0
    h, m = divmod(minutes, 60)
    if h and m:
        return f"{h}시간 {m}분"
    if h:
        return f"{h}시간"
    return f"{m}분"


@bot.command(name="blocks")
async def cmd_blocks(ctx: commands.Context) -> None:
    """Claude 5시간 과금 윈도우 현황 (ccusage blocks --active).
    사용법: `@bot blocks` — 현재 활성 블록의 사용량 · burn rate · 투영 · 남은 시간.
    """
    if not _authorized(ctx.author.id):
        await ctx.reply("⛔ 권한 없음")
        return

    async with ctx.typing():
        ok, result = await _run_ccusage(["blocks", "--active"])
    if not ok:
        await ctx.reply(f"❌ {result}")
        return

    blocks = result.get("blocks", []) if isinstance(result, dict) else []
    if not blocks:
        # 활성 블록 없음 — 최근 블록 fallback
        async with ctx.typing():
            ok2, result2 = await _run_ccusage(["blocks"])
        recent = (result2.get("blocks", []) if ok2 and isinstance(result2, dict) else [])
        # 마지막 non-gap 블록
        recent = [b for b in recent if not b.get("isGap")]
        if not recent:
            await ctx.reply("⚠ 사용 블록 데이터 없음 (`~/.claude/projects/*.jsonl`)")
            return
        last = recent[-1]
        end_dt = datetime.fromisoformat(last["endTime"].replace("Z", "+00:00"))
        embed = discord.Embed(
            title="💤 현재 활성 5시간 윈도우 없음",
            description=(
                f"마지막 윈도우: **${last.get('costUSD', 0):.2f}** · "
                f"{last.get('totalTokens', 0):,} tokens · {last.get('entries', 0)} msgs\n"
                f"종료: {end_dt.astimezone().strftime('%Y-%m-%d %H:%M %Z')}"
            ),
            color=discord.Color.greyple(),
        )
        embed.set_footer(text="powered by ccusage · 새 메시지 전송 시 새 윈도우 시작")
        await ctx.reply(embed=embed)
        return

    b = blocks[0]
    now = datetime.now(timezone.utc)
    start_dt = datetime.fromisoformat(b["startTime"].replace("Z", "+00:00"))
    end_dt = datetime.fromisoformat(b["endTime"].replace("Z", "+00:00"))
    remaining_min = int((end_dt - now).total_seconds() // 60)
    elapsed_min = int((now - start_dt).total_seconds() // 60)

    cost = b.get("costUSD", 0)
    tokens = b.get("totalTokens", 0)
    entries = b.get("entries", 0)
    burn = b.get("burnRate") or {}
    proj = b.get("projection") or {}

    burn_cost_h = burn.get("costPerHour", 0)
    burn_tok_min = burn.get("tokensPerMinute", 0)
    proj_cost = proj.get("totalCost", 0)
    proj_tok = proj.get("totalTokens", 0)

    models_str = _short_models(b.get("models", []))

    embed = discord.Embed(
        title="⚡ 현재 5시간 윈도우",
        description=(
            f"💵 **${cost:.2f}** · {tokens:,} tokens · {entries} msgs\n"
            f"🤖 models: {models_str}"
        ),
        color=discord.Color.orange(),
    )

    embed.add_field(
        name="⏰ 윈도우",
        value=(
            f"{start_dt.astimezone().strftime('%H:%M')} ~ "
            f"{end_dt.astimezone().strftime('%H:%M')}\n"
            f"경과 {_fmt_duration_minutes(elapsed_min)} · "
            f"남은 {_fmt_duration_minutes(remaining_min)}"
        ),
        inline=True,
    )

    embed.add_field(
        name="🔥 burn rate",
        value=(
            f"${burn_cost_h:.2f}/h\n"
            f"{burn_tok_min / 1000:,.0f}k tok/min"
        ),
        inline=True,
    )

    if proj_cost or proj_tok:
        embed.add_field(
            name="📈 이 속도 유지 시 (윈도우 끝)",
            value=f"${proj_cost:.2f} · {proj_tok:,} tokens",
            inline=False,
        )

    embed.set_footer(
        text="powered by ccusage · 5시간 윈도우는 첫 메시지 기준 · "
             "Anthropic 공식 rate limit 수치는 비공개 (burn rate 로 페이스 판단)"
    )
    await ctx.reply(embed=embed)


# ─── ccusage 연동 (Claude CLI 실 토큰/비용) ────────────────

# npx ccusage@latest — 로컬 ~/.claude/projects/*.jsonl 파싱. 사용자 단위 집계.
# Windows: npx.cmd / Linux+macOS: npx
_NPX_BIN = shutil.which("npx") or shutil.which("npx.cmd")


# ─── usage 자동 경고 (v0.9) ────────────────────────────────

ALERT_STATE_PATH = BOT_DIR / "usage-alerts-state.json"
_usage_alert_task: asyncio.Task | None = None


def _load_alert_state() -> dict[str, Any]:
    if not ALERT_STATE_PATH.exists():
        return {}
    try:
        return json.loads(ALERT_STATE_PATH.read_text(encoding="utf-8"))
    except Exception:
        return {}


def _save_alert_state(state: dict[str, Any]) -> None:
    tmp = ALERT_STATE_PATH.with_suffix(".json.tmp")
    tmp.write_text(json.dumps(state, indent=2, sort_keys=True), encoding="utf-8")
    tmp.replace(ALERT_STATE_PATH)


async def _run_ccusage(args: list[str], timeout_sec: int = 90) -> tuple[bool, dict | str]:
    """npx ccusage@latest <args> 실행. 성공 시 (True, parsed_json), 실패 시 (False, error_msg)."""
    if not _NPX_BIN:
        return False, "`npx` 찾을 수 없음 — Node.js 설치 확인"
    cmd = [_NPX_BIN, "ccusage@latest", *args, "--json"]
    try:
        proc = await asyncio.to_thread(
            subprocess.run,
            cmd,
            capture_output=True,
            text=True,
            timeout=timeout_sec,
            encoding="utf-8",
            errors="replace",
        )
    except subprocess.TimeoutExpired:
        return False, f"ccusage 타임아웃 ({timeout_sec}s) — 첫 실행은 패키지 다운로드라 오래 걸릴 수 있음"
    except Exception as e:
        return False, f"ccusage 실행 실패: {e}"

    if proc.returncode != 0:
        err = (proc.stderr or proc.stdout or "").strip()[:500]
        return False, f"ccusage exit {proc.returncode}: {err}"

    try:
        return True, json.loads(proc.stdout)
    except json.JSONDecodeError as e:
        return False, f"ccusage JSON 파싱 실패: {e}"


@bot.command(name="reload")
async def cmd_reload(ctx: commands.Context) -> None:
    """projects.yml 을 디스크에서 재읽기 (수동 편집 후)."""
    if not _authorized(ctx.author.id):
        await ctx.reply("⛔ 권한 없음")
        return
    try:
        _load_registry()
    except Exception as e:
        await ctx.reply(f"❌ reload 실패: {e}")
        return
    await ctx.reply(
        f"🔄 reload 완료 — projects: {len(PROJECTS)}, admins: {len(ADMIN_IDS)}, "
        f"default: `{DEFAULT_PROJECT or '(없음)'}`"
    )


def _build_help_embed() -> discord.Embed:
    """`@bot help` + on_guild_join 환영 메시지에서 공용."""
    embed = discord.Embed(
        title="coord Discord Bot — 명령 가이드",
        description=(
            "Claude 세션 오케스트레이션 시스템을 Discord 로 원격 조작. "
            "메시지에 봇을 **@mention** 하면 반응."
        ),
        color=discord.Color.blurple(),
    )

    embed.add_field(
        name="🎯 작업 지시 (v0.11+)",
        value=(
            "**`@bot send <text>`** — 현재 채널 프로젝트로 `/inbox-send`\n"
            "**`@bot send <project-id> <text>`** — 명시적 프로젝트 지정\n"
            "**`@bot review [inbox-id]`** — v0.21: /review-inbox 완료 대기 + verdict prompt 자동 (id 생략 시 최근 미review)\n"
            "**`@bot fix [description]`** — /fix (생략 시 needs-fix review 자동 탐색)\n"
            "**`@bot main-merge [project-id] [--drop-coord]`** — v0.27: work_branch → stable_branch 머지 "
            "(v0.46: 성공 시 자동 back-merge, `--drop-coord` 로 과거 `.coord/*` 이력 청소)\n"
            "**`@bot back-merge [project-id]`** — v0.46: stable_branch → work_branch 역머지 "
            "(`.coord/` 는 `merge=ours` 로 자동 보존)\n"
            "**`@bot resolve <id> [reason]`** — escalation 승인 (리액션 대체)\n"
            "**`@bot reject <id> [reason]`** — escalation 거부 (리액션 대체)\n"
            "*📎 Discord 이미지 첨부 지원 (v0.17) — inbox/attachments/ 자동 저장*"
        ),
        inline=False,
    )

    # v0.16: Option C 자연어 대화
    chat_enabled = (
        _ANTHROPIC_CLIENT is not None
        and (BOT_CFG.get("chat", {}) or {}).get("enabled", True)
    )
    chat_status = "✅ 활성" if chat_enabled else "❌ 비활성 (ANTHROPIC_API_KEY / bot.chat.enabled 확인)"
    chat_model = (BOT_CFG.get("chat", {}) or {}).get("model", "claude-sonnet-4-6")
    embed.add_field(
        name="💬 자연어 대화 (v0.16 Option C)",
        value=(
            f"**`@bot <text>`** (send 없이) — 현재 상태: {chat_status}\n"
            f"모델: `{chat_model}` · 컨텍스트: HEAD_LOCK + 최근 inbox + STATUS.md + review 요약\n"
            "v0.16.1: 어느 채널에서 물어도 응답은 **대화방** (`discord_channel_id`) 으로 라우팅 — "
            "작업장은 작업 알림만, 대화방은 대화만 유지"
        ),
        inline=False,
    )

    embed.add_field(
        name="🔍 조회",
        value=(
            "**`@bot projects`** — 등록 목록 + path 존재 확인\n"
            "**`@bot active`** — v0.23: 전체 프로젝트 head 작업 상태 + 경과 시간 (채널 필터 무시)\n"
            "**`@bot status [project-id]`** — 상세 상태 (id 생략 시 현재 채널의 프로젝트만)\n"
            "**`@bot queue [project-id]`** — inbox 큐 상태 (v0.10+)"
        ),
        inline=False,
    )

    embed.add_field(
        name="🛑 제어 / 조회",
        value=(
            "**`@bot stop <project-id>`** — `coordination/STOP` 시그널 작성\n"
            "**`@bot retry <inbox-id> [project]`** — `/retry` spawn\n"
            "**`@bot budget [project]`** — 프로젝트 토큰 예산 (progress bar)\n"
            "**`@bot usage [days=7]`** — Claude CLI 일별 비용 (ccusage daily)\n"
            "**`@bot blocks`** — 현재 5시간 윈도우 · burn rate · 투영 (v0.33+)"
        ),
        inline=False,
    )

    embed.add_field(
        name="🔁 자동 orchestration (v0.12+)",
        value=(
            "`@bot send` 한 번이면 전체 흐름 자동 진행:\n"
            "1. /inbox-send spawn → thread 생성\n"
            "2. ⏱ head 스폰 대기 → 📍 시작 감지 → 🕐 10/30/60분 마일스톤 (v0.16.7)\n"
            "3. ✅ head 완료 감지 → v0.13 self-review 있으면 Phase D spawn 생략\n"
            "4. verdict 기반 Discord 프롬프트 (✅/❌)\n"
            "5. ✅ → /review-inbox `--merge-only --auto-yes` (go) 또는 /fix (needs-fix)\n"
            "6. ❌ 리액션 → 보류"
        ),
        inline=False,
    )

    embed.add_field(
        name="🔔 에스컬레이션 리액션 (v0.8+)",
        value=(
            "head 판사 거부 알림(`🔔 head 판사 거부 — 승인 요청`)에 리액션:\n"
            "**✅** → `/resolve-escalation <id>` 자동 spawn (승인)\n"
            "**❌** → `/reject-escalation <id>` 자동 spawn (거부)\n"
            "*admin_user_ids 에 등록된 사용자의 리액션만 처리*"
        ),
        inline=False,
    )

    embed.add_field(
        name="🧵 Thread 격리 (v0.9+)",
        value=(
            "`@bot <text>` spawn 성공 시 **inbox-<project>-<ts>** thread 자동 생성. "
            "thread 내부에서도 `@bot <text>` 명령 정상 작동 (parent 채널 바인딩 상속). "
            "24h 후 auto-archive."
        ),
        inline=False,
    )

    embed.add_field(
        name="🗂 큐 (v0.10+)",
        value=(
            "`@bot <text>` 는 자동으로 프로젝트별 큐 경유 → HEAD_LOCK 직렬화를 봇이 관리.\n"
            "동시에 여러 요청 와도 순서대로 자동 spawn (busy 거부 없음).\n"
            "`@bot queue` 로 현재 큐 상태 확인."
        ),
        inline=False,
    )

    embed.add_field(
        name="⚠ 사용량 자동 경고 (v0.9+)",
        value=(
            "`projects.yml` 의 `bot.usage_alerts` 에 임계값 설정 시 봇이 주기적으로 "
            "`ccusage` 체크 → 지정 채널에 자동 알림 (하루/한달 1회).\n"
            "설정: `daily_usd_threshold`, `monthly_usd_threshold`, `alert_channel_id`"
        ),
        inline=False,
    )

    embed.add_field(
        name="📋 레지스트리 관리 (v0.7+)",
        value=(
            "**`@bot register <path>`** — v0.20: config.yml 의 `project.name` 자동 추출\n"
            "**`@bot register <id> <path>`** — 명시적 id 로 등록 (수동 override)\n"
            "**`@bot unregister <id>`** — 레지스트리 제거\n"
            "**`@bot bind <id>`** — 기존 프로젝트를 현재 채널에 바인딩\n"
            "**`@bot unbind <id>`** — 채널 바인딩 해제\n"
            "**`@bot reload`** — `projects.yml` 재읽기 (수동 편집 후)"
        ),
        inline=False,
    )

    embed.add_field(
        name="❓ help",
        value="**`@bot help`** — 이 안내 다시 보기",
        inline=False,
    )

    embed.set_footer(text="coord-template · /inbox-send 는 백그라운드 spawn — 완료 알림은 webhook 경로")
    return embed


def _build_welcome_embed() -> discord.Embed:
    """봇이 새 서버에 추가됐을 때 시스템 채널에 한 번 보내는 환영 메시지."""
    embed = discord.Embed(
        title="👋 coord Discord Bot 이 추가됐습니다",
        description=(
            "Claude 세션 오케스트레이션 봇 — Discord 에서 원격으로 프로젝트의 "
            "`/inbox-send` spawn, 상태 확인, 레지스트리 관리 가능."
        ),
        color=discord.Color.green(),
    )

    embed.add_field(
        name="⚙️ 먼저 해야 할 것 (봇 관리자)",
        value=(
            "1. 봇 호스트에서 `bot/projects.yml` 의 `admin_user_ids` 에 본인 Discord user ID 추가 "
            "(비어있으면 전원 허용 — 공개 서버에선 위험)\n"
            "2. 프로젝트별로 `coord-template` 설치 (터미널, 1회): "
            "`bash scripts/init.sh`\n"
            "3. Discord 에서 `@bot register <path>` 로 등록 (v0.20: id 자동 감지)"
        ),
        inline=False,
    )

    embed.add_field(
        name="🚀 빠른 시작 (채널별)",
        value=(
            "**이 채널에서**:\n"
            "• `@bot register /path/to/myapp` — v0.20: id 자동 감지 + 이 채널 자동 바인딩\n"
            "• `@bot register myapp /path/to/myapp` — 수동 id 지정\n"
            "• `@bot projects` — 등록된 전체 목록 확인\n"
            "• `@bot help` — 전체 명령 가이드\n"
            "\n등록 후엔 `@bot <text>` 만 쳐도 이 채널 프로젝트로 자동 라우팅."
        ),
        inline=False,
    )

    embed.add_field(
        name="📚 더 알아보기",
        value=(
            "• [GitHub](https://github.com/gone7729/coord-template)\n"
            "• [봇 README](https://github.com/gone7729/coord-template/blob/main/bot/README.md)\n"
            "• `@bot help` 로 명령 목록 즉시 확인"
        ),
        inline=False,
    )

    embed.set_footer(text="이 메시지는 봇이 서버에 처음 추가될 때 1회만 표시됩니다.")
    return embed


@bot.command(name="help")
async def cmd_help(ctx: commands.Context) -> None:
    await ctx.reply(embed=_build_help_embed())


@bot.event
async def on_guild_join(guild: discord.Guild) -> None:
    """봇이 새 서버에 추가됐을 때 환영 메시지 자동 전송.
    시스템 채널 우선 → 없으면 봇이 쓸 수 있는 첫 텍스트 채널.
    """
    # 시스템 채널 (서버 설정에서 지정됨) 우선
    candidates: list[discord.TextChannel] = []
    if guild.system_channel is not None:
        candidates.append(guild.system_channel)
    # fallback: 봇이 보낼 수 있는 첫 텍스트 채널
    for ch in guild.text_channels:
        if ch not in candidates and ch.permissions_for(guild.me).send_messages:
            candidates.append(ch)

    for ch in candidates:
        try:
            await ch.send(embed=_build_welcome_embed())
            print(f"[welcome] sent to #{ch.name} in {guild.name}", flush=True)
            return
        except discord.Forbidden:
            continue
        except Exception as e:
            print(f"[welcome] {guild.name} #{ch.name} 실패: {e}", flush=True)
            continue
    print(f"[welcome] {guild.name}: 보낼 채널 없음 (권한 부족)", flush=True)


@bot.event
async def on_raw_reaction_add(payload: discord.RawReactionActionEvent) -> None:
    """✅/❌ 리액션 → escalation 응답 자동 spawn.
    메시지 내용에서 escalation-id 를 찾아 현재 채널 바인딩 프로젝트로 라우팅.
    """
    # 봇 자신이 단 리액션은 무시
    if payload.user_id == bot.user.id:
        return
    # admin 만
    if not _authorized(payload.user_id):
        return

    emoji = str(payload.emoji)
    if emoji not in ("✅", "❌"):
        return

    channel = bot.get_channel(payload.channel_id)
    # TextChannel 또는 Thread 허용
    if channel is None or not isinstance(channel, (discord.TextChannel, discord.Thread)):
        return

    try:
        msg = await channel.fetch_message(payload.message_id)
    except Exception:
        return

    bch = _binding_channel_id(channel)

    # 메시지 전체 텍스트 수집 (content + embed 모든 필드)
    parts: list[str] = []
    if msg.content:
        parts.append(msg.content)
    for e in msg.embeds:
        if e.title: parts.append(e.title)
        if e.description: parts.append(e.description)
        for f in e.fields:
            parts.append(f.name or "")
            parts.append(f.value or "")
        if e.footer and e.footer.text:
            parts.append(e.footer.text)
    full_text = "\n".join(parts)

    # 1) verdict-prompt 감지 (embed footer 에 marker 있음)
    footer_text = ""
    for e in msg.embeds:
        if e.footer and e.footer.text and e.footer.text.startswith(_VERDICT_MARKER_FOOTER_PREFIX):
            footer_text = e.footer.text
            break
    if footer_text:
        # 형식: verdict-prompt|<inbox-id>|<verdict>
        parts = footer_text.split("|")
        if len(parts) >= 3:
            inbox_id = parts[1]
            verdict = parts[2]
            project_id, project = _resolve_project(channel_id=bch, token=None)
            if not project:
                await channel.send(f"⚠ verdict 리액션 감지했으나 프로젝트 resolve 실패")
                return
            if not _channel_ok(bch, project):
                return
            await _handle_verdict_reaction(
                channel, project_id, Path(project["path"]),
                inbox_id, verdict, emoji, payload.user_id,
            )
            return

    # 2) escalation-id 감지 (기존 경로)
    esc_id = _extract_escalation_id(full_text)
    if not esc_id:
        return   # escalation 알림 아님

    project_id, project = _resolve_project(channel_id=bch, token=None)
    if not project:
        await channel.send(f"⚠ escalation `{esc_id}` 리액션 감지했으나 프로젝트 resolve 실패")
        return
    if not _channel_ok(bch, project):
        return

    action = "resolve" if emoji == "✅" else "reject"
    reason = f"Discord reaction by <@{payload.user_id}>"

    ok, result = await _spawn_escalation_response(
        project_id, Path(project["path"]), action, esc_id, reason
    )
    await channel.send(result)


async def _handle_verdict_reaction(
    channel: discord.TextChannel,
    project_id: str,
    project_path: Path,
    inbox_id: str,
    verdict: str,
    emoji: str,
    user_id: int,
) -> None:
    """verdict prompt 에 달린 리액션 처리.
    go + ✅ → 머지 단계만 spawn
    needs-fix + ✅ → /fix spawn
    ❌ → 보류
    """
    if emoji == "❌":
        await channel.send(
            f"⏸ `{project_id}` `{inbox_id}` 보류됨 (reaction by <@{user_id}>) — 수동 처리"
        )
        return

    if emoji != "✅":
        return

    if verdict == "go":
        # v0.24: 머지 완료 대기 + rich summary 포스트 (이전엔 fire-and-forget 이라
        # webhook 의 최소 "완료" 텍스트만 보였음)
        await channel.send(
            f"🚀 `{project_id}` `{inbox_id}` 머지 진행 중 (reaction by <@{user_id}>) — 최대 30분 대기"
        )

        # verdict prompt embed 의 footer 에서 review_filename 추출
        # (parent orchestration 에서 이미 알고 있지만 reaction handler 는 별도 경로)
        # 간단히 inbox_id 로 review 파일 찾기 — _find_review_path 에 None 넣으면 실패
        # 대신 head worktree / work_branch 양쪽 스캔해서 inbox_id 포함 최신 파일 찾음
        verdict_check, review_fname = _read_verdict(project_id, project_path, inbox_id)
        # (review_fname 은 참고용 — 머지 후 파일 위치 탐색에도 사용)

        prompt = f"/review-inbox {inbox_id} --merge-only --auto-yes"
        ok, out = await _spawn_and_wait(prompt, cwd=project_path, timeout=1800)
        if not ok:
            await channel.send(
                f"❌ `{project_id}` 머지 실패: ```{(out or '')[:500]}```\n"
                f"review-inbox 파일 직접 확인 필요"
            )
            return

        # 머지 후 review-inbox 재조회 (merge_commits 필드 포함된 상태)
        _, post_merge_fname = _read_verdict(project_id, project_path, inbox_id)
        await _post_merge_summary(
            channel, project_id, project_path, inbox_id,
            post_merge_fname or review_fname,
        )
    elif verdict == "needs-fix":
        # /fix spawn — 자동 탐색으로 needs-fix 대상 발견해 처리
        _, rfname = _find_needs_fix_candidate(project_id, project_path)
        description = f"review-inbox/{rfname or inbox_id} needs-fix 처리 (Discord 리액션 승인)"
        ok, msg_text = await _spawn_claude_command(
            project_id, project_path,
            f"/fix {description}",
            check_head_lock=False,
            label="fix",
        )
        await channel.send(
            f"🔧 `{project_id}` fix spawn (reaction by <@{user_id}>)\n{msg_text}"
        )
    else:
        await channel.send(
            f"⚠ verdict `{verdict}` 는 자동 action 없음 — 수동 결정"
        )


@bot.event
async def on_command_error(ctx: commands.Context, error: Exception) -> None:
    # 미등록 명령 → 기본 라우팅으로 fallback (@bot [project?] <text>)
    if isinstance(error, commands.CommandNotFound):
        await _handle_freeform(ctx.message)
        return
    if isinstance(error, commands.MissingRequiredArgument):
        await ctx.reply(f"⚠ 인자 부족: `{error.param.name}`")
        return
    print(f"[bot error] {error!r}", file=sys.stderr)


# ─── v0.16: Option C — 자연어 대화 (Anthropic SDK) ──────────

_CHAT_SYSTEM_BASE = (
    "너는 coord-template 기반 프로젝트의 Discord 봇 어시스턴트다. "
    "coord 시스템(3-tier op/head/sub, 파일 기반 coordination, head self-review 자동 orchestration)을 숙지하고 있다. "
    "사용자 질문에 짧고 명확하게 답한다 (Discord 메시지 2000자 제한 — 반드시 1800자 이내).\n\n"
    "규칙:\n"
    "- 작업 지시 요청이 들어오면 직접 수행하지 말고 `@bot send <내용>` 사용법 안내\n"
    "- 상태 조회는 바로 답 (`@bot status`, `@bot queue` 등 명령도 대안으로 제시)\n"
    "- 추측 금지 — 제공된 context 에 없는 수치나 파일은 '알 수 없음' 이라고 말함\n"
    "- 한국어로 응답 (사용자 언어 영어면 영어)\n"
    "- 코드/명령어는 백틱으로 감싸기. 모호한 대답 금지."
)


def _gather_project_state(
    project_id: str, project: dict[str, Any], chat_cfg: dict[str, Any]
) -> str:
    """v0.16: 프로젝트 상태를 텍스트로 요약해 chat context 에 붙일 블록 생성.
    include_status_md / include_recent_inbox 설정에 따라 포함 여부 조절."""
    project_path = Path(project.get("path", ""))
    coord_root = _coord_root(project_path)
    coord_dir = coord_root / "coordination"

    lines: list[str] = [f"# 프로젝트 상태: {project_id}", f"- path: `{project_path}`"]

    # HEAD_LOCK (head 작업 중 여부)
    head_lock = _head_lock_path(project_id, project_path)
    if head_lock.exists():
        try:
            ts = head_lock.read_text(encoding="utf-8", errors="ignore").strip()
        except Exception:
            ts = "?"
        lines.append(f"- HEAD_LOCK: 🔒 active (시작 {ts})")
    else:
        lines.append("- HEAD_LOCK: ⚪ idle")

    # 최근 inbox
    n_recent = int(chat_cfg.get("include_recent_inbox", 3) or 0)
    if n_recent > 0:
        inbox_dir = coord_dir / "inbox"
        if inbox_dir.exists():
            inbox_files = sorted(
                [p for p in inbox_dir.glob("*.md") if p.name != ".gitkeep"],
                key=lambda p: p.stat().st_mtime,
                reverse=True,
            )[:n_recent]
            if inbox_files:
                lines.append(f"\n## 최근 inbox {len(inbox_files)}건")
                for p in inbox_files:
                    try:
                        text = p.read_text(encoding="utf-8", errors="ignore")[:400]
                    except Exception:
                        text = ""
                    # 제목 추출
                    title_match = re.search(r"^#\s+(.+)$", text, flags=re.M)
                    status_match = re.search(r"^-\s*\*?\*?status\*?\*?:\s*(\S+)", text, flags=re.M | re.I)
                    title = title_match.group(1).strip() if title_match else p.stem
                    status = status_match.group(1).strip() if status_match else "?"
                    lines.append(f"- `{p.stem}` — {title} (status: {status})")

    # STATUS.md (요약)
    if chat_cfg.get("include_status_md", True):
        status_md = coord_dir / "STATUS.md"
        if status_md.exists():
            try:
                content = status_md.read_text(encoding="utf-8", errors="ignore")
                # 너무 길면 앞 4KB 만
                if len(content) > 4096:
                    content = content[:4096] + "\n...(truncated)"
                lines.append(f"\n## STATUS.md\n```markdown\n{content}\n```")
            except Exception:
                pass

    # 최근 review-inbox verdict (v0.16.8: head worktree 우선 — 신규 verdict 는 거기에 있음)
    # v0.43: subdir 모드 대응 — _head_coord_dir 사용
    head_review_dir = _head_coord_dir(project_id, project_path) / "review-inbox"
    review_dir = head_review_dir if head_review_dir.exists() else (coord_dir / "review-inbox")
    if review_dir.exists():
        review_files = sorted(
            [p for p in review_dir.glob("*.md") if "_TEMPLATE" not in p.name and p.name != ".gitkeep"],
            key=lambda p: p.stat().st_mtime,
            reverse=True,
        )[:2]
        if review_files:
            lines.append("\n## 최근 review-inbox")
            for p in review_files:
                try:
                    text = p.read_text(encoding="utf-8", errors="ignore")[:800]
                except Exception:
                    text = ""
                v_match = re.search(r"^verdict:\s*(\S+)", text, flags=re.M | re.I)
                merged_match = re.search(r"^merged:\s*true", text, flags=re.M | re.I)
                verdict = v_match.group(1).strip() if v_match else "pending"
                merged = "✅ merged" if merged_match else "⏳ not merged"
                lines.append(f"- `{p.name}` — verdict: `{verdict}`, {merged}")

    return "\n".join(lines)


async def _resolve_chat_channel(
    project: dict[str, Any], source_message: discord.Message
) -> tuple[discord.abc.Messageable, bool]:
    """v0.16.1: 자연어 대화 응답을 보낼 채널 결정.
    우선순위: discord_channel_id (대화방) → source 채널 (fallback).
    두 번째 반환값: 'redirected' (대화방이 source 와 다르면 True)."""
    chat_id = project.get("discord_channel_id")
    src_channel_id = _binding_channel_id(source_message.channel)
    if chat_id and str(chat_id).strip():
        try:
            ch = bot.get_channel(int(chat_id)) or await bot.fetch_channel(int(chat_id))
        except Exception as e:
            print(f"[chat] 대화방 조회 실패 ({chat_id}): {e}", flush=True)
            ch = None
        if isinstance(ch, (discord.TextChannel, discord.Thread)):
            redirected = str(chat_id) != str(src_channel_id)
            return ch, redirected
    # fallback: source 채널에 그대로
    return source_message.channel, False


async def _chat_with_claude(
    message: discord.Message,
    project_id: str,
    project: dict[str, Any],
    user_text: str,
) -> None:
    """v0.16: Option C — Anthropic SDK 로 user_text 에 자연어 응답.
    v0.16.1: 응답을 대화방(discord_channel_id)으로 강제 라우팅. 다른 채널에서 질의 시
    간단 꼬리표만 그 채널에 남기고 실제 답은 대화방."""
    if _ANTHROPIC_CLIENT is None:
        await message.reply(
            "⚠ Anthropic SDK 비활성 — `.env.local` 의 `ANTHROPIC_API_KEY` 확인 또는 `pip install anthropic` 설치 필요"
        )
        return

    chat_cfg = BOT_CFG.get("chat", {}) or {}
    if not chat_cfg.get("enabled", True):
        await message.reply("⚠ 자연어 대화 기능이 비활성화됨 (`projects.yml` 의 `bot.chat.enabled` 확인)")
        return

    model = chat_cfg.get("model", "claude-sonnet-4-6")
    max_tokens = int(chat_cfg.get("max_tokens", 1024))

    # 출력 대상 채널 결정 (대화방 우선)
    reply_channel, redirected = await _resolve_chat_channel(project, message)

    # 다른 채널에서 질의 시 원래 채널에 간단 안내 + 실제 답은 대화방으로
    if redirected:
        try:
            chat_id = project.get("discord_channel_id")
            await message.reply(
                f"💬 자연어 대화는 <#{chat_id}> 대화방에서 응답합니다…"
            )
        except Exception:
            pass

    # 상태 번들
    try:
        state_block = _gather_project_state(project_id, project, chat_cfg)
    except Exception as e:
        state_block = f"(상태 수집 실패: {e})"

    # prompt caching: system 을 2개 블록으로 분리 (base 는 static, state 는 매 call 갱신)
    system_blocks = [
        {"type": "text", "text": _CHAT_SYSTEM_BASE, "cache_control": {"type": "ephemeral"}},
        {"type": "text", "text": state_block},
    ]

    # 타이핑 표시 (대화방 기준)
    try:
        typing_cm = reply_channel.typing()
    except Exception:
        typing_cm = None

    try:
        if typing_cm is not None:
            async with typing_cm:
                response = await asyncio.to_thread(
                    _ANTHROPIC_CLIENT.messages.create,
                    model=model,
                    max_tokens=max_tokens,
                    system=system_blocks,
                    messages=[{"role": "user", "content": user_text}],
                )
        else:
            response = await asyncio.to_thread(
                _ANTHROPIC_CLIENT.messages.create,
                model=model,
                max_tokens=max_tokens,
                system=system_blocks,
                messages=[{"role": "user", "content": user_text}],
            )
    except Exception as e:
        err = f"❌ Anthropic API 호출 실패: `{type(e).__name__}: {e}`"
        try:
            await reply_channel.send(err)
        except Exception:
            await message.reply(err)
        return

    # 응답 조립
    try:
        chunks = [
            block.text for block in response.content if getattr(block, "type", "") == "text"
        ]
        reply_text = "".join(chunks).strip() or "(빈 응답)"
    except Exception:
        reply_text = "(응답 파싱 실패)"

    # Discord 2000 자 제한
    if len(reply_text) > 1900:
        reply_text = reply_text[:1900] + "\n…(truncated)"

    # usage 정보 footer
    try:
        u = response.usage
        in_tok = getattr(u, "input_tokens", 0)
        out_tok = getattr(u, "output_tokens", 0)
        cache_read = getattr(u, "cache_read_input_tokens", 0) or 0
        cache_create = getattr(u, "cache_creation_input_tokens", 0) or 0
        footer = f"\n\n_{model} · in:{in_tok} out:{out_tok} cache_r:{cache_read} cache_w:{cache_create}_"
    except Exception:
        footer = ""

    # 다른 채널에서 온 질의면 원문 인용 (대화방 사용자가 문맥 파악)
    prefix = ""
    if redirected:
        snippet = user_text[:200] + ("…" if len(user_text) > 200 else "")
        prefix = f"<@{message.author.id}> 질문 (from <#{_binding_channel_id(message.channel)}>):\n> {snippet}\n\n"

    final_text = prefix + reply_text + footer
    if len(final_text) > 1990:
        final_text = final_text[:1990] + "…"

    try:
        await reply_channel.send(final_text)
    except Exception as e:
        await message.reply(f"❌ 응답 전송 실패: {e}")


async def _handle_freeform(message: discord.Message) -> None:
    """@bot <text> (등록된 명령 없이). v0.16: 대화방이면 Anthropic 채팅, 작업장이면 fallback 안내.
    """
    # mention 제거
    content = message.content
    for m in message.mentions:
        content = content.replace(f"<@{m.id}>", "").replace(f"<@!{m.id}>", "")
    content = content.strip()

    if not content:
        # @bot 만 멘션 → help 띄움
        await message.reply(embed=_build_help_embed())
        return

    if not _authorized(message.author.id):
        await message.reply("⛔ 권한 없음 (admin_user_ids 확인)")
        return

    # 슬래시 명령 실수 감지
    if content.startswith("/"):
        await message.reply(
            f"⚠ 슬래시 명령은 별도 봇 명령 사용:\n"
            f"- `/inbox-send` → `@bot send <text>`\n"
            f"- `/review-inbox` → `@bot review [id]`\n"
            f"- `/fix` → `@bot fix <desc>`\n"
            f"- `/resolve-escalation` / `/reject-escalation` → `@bot resolve/reject <id>`\n"
            f"전체: `@bot help`"
        )
        return

    # v0.16: Option C 자연어 대화 (어느 채널에서든 질의 가능, 응답은 대화방으로 라우팅)
    channel_id = _binding_channel_id(message.channel)
    project_id, project = _resolve_project(channel_id=channel_id)

    chat_cfg = BOT_CFG.get("chat", {}) or {}
    chat_available = (
        _ANTHROPIC_CLIENT is not None
        and chat_cfg.get("enabled", True)
        and project is not None
    )

    if chat_available:
        await _chat_with_claude(message, project_id or "unknown", project or {}, content)
        return

    # fallback: 기존 안내
    reasons: list[str] = []
    if _ANTHROPIC_CLIENT is None:
        reasons.append("ANTHROPIC_API_KEY 미설정")
    if not chat_cfg.get("enabled", True):
        reasons.append("`bot.chat.enabled: false`")
    if project is None:
        reasons.append("채널 바인딩된 프로젝트 없음")

    reason_text = " / ".join(reasons) if reasons else "—"
    await message.reply(
        "💬 **자연어 대화 비활성**: " + reason_text + "\n\n"
        "대안:\n"
        "- 작업 지시: `@bot send <text>`\n"
        "- 검토: `@bot review [inbox-id]`\n"
        "- 전체 명령: `@bot help`"
    )


# ─── main ──────────────────────────────────────────────────

def main() -> None:
    if not PROJECTS:
        print("⚠ enabled 프로젝트 없음 — projects.yml 확인", file=sys.stderr)
    try:
        bot.run(TOKEN)
    except discord.LoginFailure:
        print("❌ Discord 로그인 실패 — 토큰 확인 (Reset Token 후 .env.local 갱신)", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
