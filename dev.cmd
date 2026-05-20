@echo off
REM 세나리 웹 공략 사이트 개발 서버 실행 (web/ = Next.js)
cd /d "%~dp0web"

if not exist node_modules (
  echo [dev] node_modules 없음 - 의존성 설치 중...
  call npm install
  if errorlevel 1 (
    echo [dev] npm install 실패
    exit /b 1
  )
)

echo [dev] Next.js 개발 서버 시작 - http://localhost:3000
call npm run dev
