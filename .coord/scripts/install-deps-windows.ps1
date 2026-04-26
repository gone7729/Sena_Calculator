# coord-template 의존성 자동 설치 (Windows, PowerShell)
# 사용: powershell -ExecutionPolicy Bypass -File scripts\install-deps-windows.ps1 [-SkipCloudflared] [-Yes]
#
# 설치 대상:
#   - winget 으로 Python 3.12 / Node.js / Git / uv / (옵션) cloudflared
#   - claude CLI (npm 글로벌)
#   - bot\requirements.txt (.venv 생성 + uv pip)
#
# 이미 설치된 항목은 skip. `-Yes` 면 확인 없이 진행.

param(
    [switch]$SkipCloudflared,
    [switch]$Yes
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot

Write-Host "📦 coord-template 의존성 설치 (Windows)" -ForegroundColor Cyan
Write-Host "   repo: $RepoRoot"
Write-Host ""

# ─── 1. winget 확인 ───────────────────────────────────────
if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
    Write-Host "❌ winget 이 없습니다." -ForegroundColor Red
    Write-Host "   Windows 10 1809+ / Windows 11 이면 'App Installer' 앱을 Microsoft Store 에서 설치하세요."
    Write-Host "   또는 Scoop/Chocolatey 로 수동 설치:"
    Write-Host "     - Python 3.12 / Node.js LTS / Git / uv / (옵션) cloudflared"
    exit 1
}
Write-Host "✅ winget 사용 가능"

# ─── 2. 설치할 패키지 목록 ────────────────────────────────
# 형식: @{ Id = 'winget-id'; Check = 'command-to-verify' }
$Packages = @(
    @{ Id = "Python.Python.3.12";      Check = "python" }
    @{ Id = "OpenJS.NodeJS.LTS";       Check = "node" }
    @{ Id = "Git.Git";                 Check = "git" }
    @{ Id = "astral-sh.uv";            Check = "uv" }
)

if (-not $SkipCloudflared) {
    $Packages += @{ Id = "Cloudflare.cloudflared"; Check = "cloudflared" }
}

# ─── 3. 설치 ──────────────────────────────────────────────
foreach ($pkg in $Packages) {
    if (Get-Command $pkg.Check -ErrorAction SilentlyContinue) {
        Write-Host "✅ 이미 설치됨: $($pkg.Check)"
        continue
    }

    if (-not $Yes) {
        $reply = Read-Host "📥 $($pkg.Id) 설치? [Y/n]"
        if ($reply -match '^[Nn]') { Write-Host "   skip"; continue }
    }

    Write-Host "📥 winget install $($pkg.Id)"
    winget install --id $pkg.Id --silent --accept-source-agreements --accept-package-agreements
}

# PATH 갱신 (방금 설치한 것 즉시 인식)
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" +
            [System.Environment]::GetEnvironmentVariable("Path","User")

# ─── 4. claude CLI (npm 글로벌) ───────────────────────────
if (Get-Command claude -ErrorAction SilentlyContinue) {
    Write-Host "✅ 이미 설치됨: claude"
} else {
    Write-Host "📥 claude CLI 설치: npm i -g @anthropic-ai/claude-code"
    npm install -g "@anthropic-ai/claude-code"
}

# ─── 5. Python venv + bot 의존성 ──────────────────────────
$ReqFile = Join-Path $RepoRoot "bot\requirements.txt"
if (-not (Test-Path $ReqFile)) {
    Write-Host "⚠ bot\requirements.txt 없음 — venv 생성 skip"
} else {
    $VenvDir = Join-Path $RepoRoot ".venv"
    Push-Location $RepoRoot
    try {
        if (Test-Path $VenvDir) {
            Write-Host "✅ .venv 이미 존재 — 의존성만 재설치"
        } else {
            Write-Host "📥 .venv 생성 (uv)"
            uv venv
        }

        Write-Host "📥 bot 의존성 설치 (uv pip)"
        # PowerShell 에서 venv activate 대신 uv 가 자동으로 .venv 감지
        uv pip install -r bot\requirements.txt
    } finally {
        Pop-Location
    }
}

# ─── 6. 확인 + 다음 단계 안내 ─────────────────────────────
Write-Host ""
Write-Host "✅ 설치 완료 — 설치된 도구:" -ForegroundColor Green
function Show-Version($cmd, $args) {
    try {
        $v = & $cmd $args 2>$null | Select-Object -First 1
        Write-Host "   ${cmd}: $v"
    } catch {
        Write-Host "   ${cmd}: (버전 조회 실패)"
    }
}
Show-Version "python" "--version"
Show-Version "node" "--version"
Show-Version "npm" "--version"
Show-Version "git" "--version"
Show-Version "uv" "--version"
Show-Version "claude" "--version"
if (-not $SkipCloudflared) { Show-Version "cloudflared" "--version" }

Write-Host ""
Write-Host "📋 다음 단계:" -ForegroundColor Cyan
Write-Host "   1. .\.venv\Scripts\Activate.ps1                      # Python 환경 활성화"
Write-Host "   2. copy .env.local.example .env.local                # 환경변수 채우기 (DISCORD_WEBHOOK_URL 등)"
Write-Host "   3. copy bot\projects.yml.example bot\projects.yml    # 봇 레지스트리 설정"
Write-Host "   4. bash scripts/init.sh                              # coord 구조 초기화 (Git Bash 필요)"
Write-Host "   5. bash scripts/start-bot.sh --bg                    # Discord 봇 실행"
Write-Host ""
Write-Host "📚 자세한 설정: README.md / bot\README.md"
