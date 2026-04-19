---
description: 토큰 사용량 + 임계 + 리셋 상태 한눈 확인 (수동 체크 트리거)
---
> v0.5.1+: 경로는 `coordination/config.yml` 참조. 수동 편집 불필요.

너는 op (메인 세션). 사용자가 "토큰 얼마 썼나?" 물을 때.

## 절차

1. **현재 상태 표시**
   ```bash
   grep -E "weekly_limit:|^- \*\*(week_start_ts|used_tokens|last_alerted_threshold|last_update)\*\*:" \
     coordination/token-budget.md
   ```

2. **% 계산 + 표시**
   ```bash
   USED=$(grep -E "^- \*\*used_tokens\*\*:" coordination/token-budget.md | grep -oE '[0-9]+' | head -1)
   LIMIT=$(grep -E "weekly_limit:" coordination/token-budget.md | grep -oE '[0-9]+' | head -1)
   PCT=$(python3 -c "print(round($USED * 100 / $LIMIT, 2))")
   echo "사용: $USED / $LIMIT (${PCT}%)"
   ```

3. **임계/리셋 자동 체크 트리거** (이 호출 자체로 리셋 감지 작동)
   ```bash
   bash scripts/check-token-threshold.sh
   ```

4. **최근 사용 내역 (token-log.md 마지막 5건)**
   ```bash
   tail -5 coordination/token-log.md 2>/dev/null
   ```

5. **다음 임계까지 남은 양**
   - 다음 임계 % 계산 (10/20/.../99 중 다음)
   - 거기까지 몇 토큰 더 쓰면 알림 발송될지 표시

## 사용자 친화적 출력 예시

```markdown
## 📊 Token Status

- **이번 주차**: 2026-04-13 ~ 2026-04-19 (KST 월~일)
- **사용**: 1,234,567 / 5,000,000 (24.7%)
- **남은**: 3,765,433
- **마지막 알림 임계**: 20%
- **다음 임계**: 30% (까지 263,433 토큰 여유)

### 최근 사용
| 시각 | 컨텍스트 | 토큰 |
|------|---------|------|
| ... | wt-backend (164231) | 87,234 |
| ... | wt-frontend (155702) | 23,456 |

### 리셋 정보
- 다음 리셋: 2026-04-20 (월) 00:00 KST
- 남은 시간: 4일 6시간
```

## 금지
- 추정 정확도 과대평가 금지 — interactive 세션은 미추적임을 명시

## 지금 할 것
위 절차로 현재 상태 요약 + check-token-threshold.sh 트리거 (리셋 감지 + 임계 알림 자동).