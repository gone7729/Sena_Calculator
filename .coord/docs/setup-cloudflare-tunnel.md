# Cloudflare Tunnel 셋업 (Coordination Dashboard 외부 노출)

> 프로젝트 이름을 `<project>` 로 표기 — 실제 사용 시 `coordination/config.yml` 의 `project.name` 값으로 치환.

## 사전 조건
- Cloudflare 계정 (Free, 카드 등록 완료)
- 도메인 (보유 또는 Cloudflare 등록)
- Windows: `winget install cloudflare.cloudflared`
- macOS: `brew install cloudflared`

## 단계 1: cloudflared 설치 + 인증

```bash
# Windows
winget install cloudflare.cloudflared

# 인증 (브라우저로 Cloudflare 계정 연결)
cloudflared tunnel login
# → 브라우저 열리면 도메인 선택 + 인증
# → ~/.cloudflared/cert.pem 생성됨
```

## 단계 2: Tunnel 생성

```bash
# <project> 자리에 프로젝트 이름 대입 (예: myapp-dashboard)
cloudflared tunnel create <project>-dashboard
# → tunnel ID + ~/.cloudflared/<id>.json (credentials) 생성

cloudflared tunnel list
# → 확인
```

## 단계 3: DNS 라우팅

```bash
# dashboard.your-domain.com → tunnel
cloudflared tunnel route dns <project>-dashboard dashboard.your-domain.com
```

도메인 없으면:
- 옵션 A: Cloudflare 에서 도메인 등록 (~$10/년)
- 옵션 B: TryCloudflare 임시 URL (`cloudflared tunnel --url http://localhost:8888`)

## 단계 4: 설정 파일

`~/.cloudflared/config.yml`:
```yaml
tunnel: <project>-dashboard
credentials-file: /path/to/.cloudflared/<tunnel-id>.json   # Windows: C:\Users\<user>\.cloudflared\<tunnel-id>.json

ingress:
  - hostname: dashboard.your-domain.com
    service: http://localhost:8888     # config.yml 의 dashboard.port 와 일치
  - service: http_status:404
```

## 단계 5: Tunnel 실행

```bash
cloudflared tunnel run <project>-dashboard
# 백그라운드:
# nohup cloudflared tunnel run <project>-dashboard > /tmp/cf-tunnel.log 2>&1 &
```

## 단계 6: Zero Trust Access (인증 추가)

Cloudflare 대시보드 → Zero Trust → Access → Applications → Add an application:

1. **Self-hosted** 선택
2. Application name: `<project> Dashboard`
3. Application domain: `dashboard.your-domain.com`
4. Identity providers: 기본 One-time PIN (이메일 OTP) 사용
5. Policy: `Allow` + Include `Emails` → `your@email.com`
6. Save

이제 `https://dashboard.your-domain.com` 접근 시 이메일 OTP 인증 후 진입.

## 단계 7: Windows 서비스로 등록 (선택, 항상 켜진 PC 용)

```powershell
# 관리자 PowerShell
cloudflared service install
```

→ Windows 부팅 시 자동 시작.

## 트러블슈팅

### "tunnel not found"
- `cloudflared tunnel list` 로 ID 재확인
- credentials 파일 경로 확인

### 502 Bad Gateway
- `localhost:<port>` 서버 안 떠있음. `bash scripts/start-dashboard.sh` 먼저 실행

### 인증 안 됨
- Zero Trust 정책에 본인 이메일 정확히 입력 확인
- 정책 순서: Allow 가 위에 있어야

### 빠른 시작 (도메인/인증 없이)
```bash
cloudflared tunnel --url http://localhost:8888
# → 임시 URL 즉시 발급 (https://random.trycloudflare.com)
# 단점: 매번 URL 변경, 인증 없음
```

## 비용

| 항목 | 비용 |
|------|------|
| Cloudflare Free 계정 | $0 |
| Cloudflare Tunnel | $0 (무제한) |
| Zero Trust (50 user) | $0 |
| 도메인 (선택) | ~$10/년 |
| **합계** | **$0~10/년** |

## 대안 (카드 등록 거부 시)

- **Tailscale**: 본인 디바이스만, 카드 X (사적 VPN)
- **ngrok Free**: 임시 URL, 카드 X
- **TryCloudflare**: 계정도 X, 임시 URL (coord-template 의 `scripts/update-dashboard-url.sh` 가 자동 감지)
