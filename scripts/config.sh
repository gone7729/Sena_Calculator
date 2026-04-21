#!/usr/bin/env bash
# coordination/config.yml 에서 값 읽기 헬퍼
# 사용법:
#   source scripts/config.sh
#   echo "$WORK_BRANCH"  # 3dview
#   sub_models                # db:sonnet, backend:opus, ...
#
# v0.4+: ROOT 변수도 export:
#   COORD_ROOT   — template 루트 (flat: project-root; subdir: <project>/.coord)
#   PROJECT_ROOT — 사용자 프로젝트 루트 (.env.local / .claude / worktree 기준)
#   INSTALL_MODE — flat | subdir
#
# 스크립트는 리소스별로 base 를 선택:
#   coordination/, scripts/, templates/, docs/  → COORD_ROOT
#   .env.local, .claude/, .githooks path, ../${name}-wt-*/  → PROJECT_ROOT

set -euo pipefail

# === ROOT 검출 (1회만) ===
if [[ -z "${_COORD_ROOT_DETECTED:-}" ]]; then
  _CONFIG_SH_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
  COORD_ROOT="$(cd "$_CONFIG_SH_DIR/.." && pwd)"
  if [[ "$(basename "$COORD_ROOT")" == ".coord" ]]; then
    PROJECT_ROOT="$(cd "$COORD_ROOT/.." && pwd)"
    INSTALL_MODE="subdir"
  else
    PROJECT_ROOT="$COORD_ROOT"
    INSTALL_MODE="flat"
  fi
  export COORD_ROOT PROJECT_ROOT INSTALL_MODE
  _COORD_ROOT_DETECTED=1
  unset _CONFIG_SH_DIR
fi

CONFIG_FILE="${CONFIG_FILE:-$COORD_ROOT/coordination/config.yml}"

[[ ! -f "$CONFIG_FILE" ]] && { echo "❌ $CONFIG_FILE 없음"; return 1 2>/dev/null || exit 1; }

# Git Bash(Windows) 에서 /c/... 경로를 Windows Python 이 못 읽으므로 변환
_coord_to_native() {
  if command -v cygpath >/dev/null 2>&1; then
    cygpath -w "$1" 2>/dev/null || echo "$1"
  else
    echo "$1"
  fi
}

_CONFIG_FILE_NATIVE="$(_coord_to_native "$CONFIG_FILE")"
export _CONFIG_FILE_NATIVE

# === 단순 단일 값 추출 (yq 없이) ===
_yaml_value() {
  local key="$1"
  python3 -c "
import sys, yaml
with open(r'$_CONFIG_FILE_NATIVE', encoding='utf-8') as f:
    d = yaml.safe_load(f)
keys = '$key'.split('.')
val = d
for k in keys:
    val = val.get(k) if isinstance(val, dict) else None
    if val is None: break
print(val if val is not None else '')
" 2>/dev/null
}

# === 자주 쓰는 값 export ===
PROJECT_NAME=$(_yaml_value "project.name")
WORK_BRANCH=$(_yaml_value "git.work_branch")
STABLE_BRANCH=$(_yaml_value "git.stable_branch")
REMOTE=$(_yaml_value "git.remote")
HEAD_BRANCH=$(_yaml_value "head.branch")
HEAD_MODEL=$(_yaml_value "head.model")
JUDGE_MODEL=$(_yaml_value "judiciary.judge_model")
APPROVAL_TTL_MIN=$(_yaml_value "judiciary.approval_ttl_minutes")
WEEKLY_LIMIT=$(_yaml_value "token_budget.weekly_limit")
DASHBOARD_PORT=$(_yaml_value "dashboard.port")

export PROJECT_NAME WORK_BRANCH STABLE_BRANCH REMOTE
export HEAD_BRANCH HEAD_MODEL JUDGE_MODEL APPROVAL_TTL_MIN
export WEEKLY_LIMIT DASHBOARD_PORT

# === Sub 정보 (배열 처리는 python 으로) ===
sub_names() {
  python3 -c "
import yaml
with open(r'$_CONFIG_FILE_NATIVE', encoding='utf-8') as f:
    d = yaml.safe_load(f)
for s in d.get('subs', []):
    print(s['name'])
"
}

sub_model() {
  local name="$1"
  python3 -c "
import yaml
with open(r'$_CONFIG_FILE_NATIVE', encoding='utf-8') as f:
    d = yaml.safe_load(f)
for s in d.get('subs', []):
    if s['name'] == '$name':
        print(s.get('model', 'claude-sonnet-4-6'))
        break
"
}

sub_validation() {
  local name="$1"
  python3 -c "
import yaml
with open(r'$_CONFIG_FILE_NATIVE', encoding='utf-8') as f:
    d = yaml.safe_load(f)
for s in d.get('subs', []):
    if s['name'] == '$name':
        print(s.get('validation', ''))
        break
"
}

sub_scope() {
  local name="$1"
  python3 -c "
import yaml
with open(r'$_CONFIG_FILE_NATIVE', encoding='utf-8') as f:
    d = yaml.safe_load(f)
for s in d.get('subs', []):
    if s['name'] == '$name':
        for p in s.get('scope_allowed', []):
            print(p)
        break
"
}

# === .githooks path (PROJECT_ROOT 기준, 모드별 상대경로) ===
# flat:   .githooks
# subdir: .coord/.githooks
coord_hooks_path() {
  if [[ "$INSTALL_MODE" == "subdir" ]]; then
    echo ".coord/.githooks"
  else
    echo ".githooks"
  fi
}

# === 외부 호출 가능 함수 export ===
export -f sub_names sub_model sub_validation sub_scope _yaml_value coord_hooks_path 2>/dev/null || true
