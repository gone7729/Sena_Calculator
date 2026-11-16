# coding: utf-8
"""scan_clock.py 판독 → 게임 시계 G(t) + 턴 시작 시각별 게임 경과 → 시뮬 시간 장부와 턴별 대조.

  python clock_fit.py <clock.csv> <시뮬 ledger.csv> [출력 csv]

게임 시계: 쿨 카운터(보스 1·2스킬, 비스킷·리나 — 평타 쿨감을 받지 않는 칸)는 게임 시간 1초당 1 감소, 표시 = 내림.
  카운터가 v+1 → v로 바뀐 순간 = 남은 쿨이 정수 v+1을 지난 순간 → G = C_run − (v+1). 런(시전~0)마다 상수 C가 달라
  겹치는 구간의 중앙값으로 이어 붙인다. 판독 잡음: 한 런 안에서 값이 1~5씩 줄고 시간 간격과 맞는 것만 채택.
턴 시작 = 턴 카운터가 처음 "n/70"으로 읽힌 시각. 시뮬 ledger(turn,sec,elapsed,why)의 턴 시작 경과와 비교.
"""
import csv, re, sys, bisect, statistics as st

rows = list(csv.DictReader(open(sys.argv[1], encoding='utf-8')))
ledger = list(csv.DictReader(open(sys.argv[2], encoding='utf-8')))
OUT = sys.argv[3] if len(sys.argv) > 3 else None
CNT = ['boss1', 'boss2', '비스킷_top', '비스킷_bot', '리나_top', '리나_bot']

# ---- 1) 카운터 런 → 감소 이벤트
def runs_of(reg):
    raw = [(float(r['t']), int(r['value'])) for r in rows if r['region'] == reg and re.fullmatch(r'\d{1,3}', r['value'])]
    # 고립 오독 제거: 2.5초 안에 앞에 v+1 또는 뒤에 v−1이 있어야 채택 (1씩 세는 카운터의 연속성)
    pts = [(t, v) for t, v in raw
           if any(0 < t2 - t <= 2.5 and v2 == v - 1 for t2, v2 in raw)
           or any(0 < t - t2 <= 2.5 and v2 == v + 1 for t2, v2 in raw)]
    runs, cur = [], []
    for t, v in pts:
        if cur:
            pt, pv = cur[-1]
            d = pv - v
            if d == 0: continue
            # 1씩 / 컷신 일괄(≈4) / 판독 공백(컷신·메뉴)을 건넌 감소 — 감소폭은 경과 실시간+5.5 이내
            if 1 <= d <= (t - pt) + 5.5 and (d == 1 or d >= 3 or t - pt > 1.5):
                cur.append((t, v)); continue
            if d < 0 and v - pv >= 10:            # 리셋(재시전)
                runs.append(cur); cur = [(t, v)]; continue
            continue                              # 잡음
        cur = [(t, v)]
    if cur: runs.append(cur)
    return [r for r in runs if len(r) >= 4]

events = []   # (reg, run_id, t, v_new)
for reg in CNT:
    for k, run in enumerate(runs_of(reg)):
        for (t1, v1), (t2, v2) in zip(run, run[1:]):
            if v1 - v2 == 1: events.append((reg, k, t2, v2))
            # 일괄 감소(컷신)는 시각이 불명확 → 시계 앵커로 쓰지 않음

# ---- 2) 런별 상수 C 이어 붙이기 (G = C − (v+1))
#   런 안에서 1초 간격 감소 이벤트가 이어지는 구간(세그먼트)은 g(t) = −(v+1) 을 선형 보간해 쓸 수 있다.
#   시간이 겹치는 두 런의 세그먼트로 상대 오프셋(C_A − C_B)을 추정 → 첫 런부터 BFS로 C 확정.
groups = {}
for reg, k, t, v in events: groups.setdefault((reg, k), []).append((t, v))
def segments(pts):
    segs, cur = [], [pts[0]]
    for p in pts[1:]:
        if p[0] - cur[-1][0] <= 1.6: cur.append(p)
        else:
            if len(cur) >= 2: segs.append(cur)
            cur = [p]
    if len(cur) >= 2: segs.append(cur)
    return segs
def seg_val(seg, t):   # 세그먼트 안에서 −(v+1) 보간
    ts = [p[0] for p in seg]
    if t < ts[0] or t > ts[-1]: return None
    i = min(bisect.bisect_right(ts, t) - 1, len(seg) - 2)
    (ta, va), (tb, vb) = seg[i], seg[i + 1]
    return -(va + 1) + (-(vb + 1) + (va + 1)) * (t - ta) / (tb - ta)
SEG = {g: segments(groups[g]) for g in groups}
SEG = {g: s for g, s in SEG.items() if s}
keys = sorted(SEG, key=lambda g: SEG[g][0][0][0])
def rel(a, b):   # C_a − C_b 추정 (겹치는 시각에서 g_a − g_b의 중앙값의 부호 반대)
    d = []
    for sa in SEG[a]:
        for sb in SEG[b]:
            lo, hi = max(sa[0][0], sb[0][0]), min(sa[-1][0], sb[-1][0])
            t = lo
            while t <= hi:
                x, y = seg_val(sa, t), seg_val(sb, t)
                if x is not None and y is not None: d.append(y - x)
                t += 0.25
    return (st.median(d), len(d)) if len(d) >= 6 else (None, 0)
C = {keys[0]: 0.0}
changed = True
while changed:
    changed = False
    for b in keys:
        if b in C: continue
        best = None
        for a in list(C):
            r, n = rel(a, b)
            # G = C_a + g_a = C_b + g_b  →  C_b = C_a − (g_b − g_a)
            if r is not None and (best is None or n > best[1]): best = (C[a] - r, n)
        if best: C[b] = best[0]; changed = True
anchors = sorted((t, C[g] - (v + 1)) for g in C for seg in SEG[g] for t, v in seg)
print(f"런 {len(keys)}개 중 {len(C)}개 연결: " + ", ".join(f"{g[0]}#{g[1]}" for g in keys if g not in C) + " 미연결")
def Gi(t):
    ts = [a[0] for a in anchors]
    i = bisect.bisect_right(ts, t) - 1
    if i < 0 or i >= len(anchors) - 1: return None
    (ta, ga), (tb, gb) = anchors[i], anchors[i + 1]
    if tb - ta > 3: return None   # 컷신·일시정지 걸친 구간은 보간 안 함
    return ga + (gb - ga) * (t - ta) / (tb - ta) if tb > ta else ga

# ---- 3) 턴 시작 시각
tv = [(float(r['t']), r['value']) for r in rows if r['region'] == 'turn']
turn = {}; best = -1
for t, v in tv:
    m = re.fullmatch(r'(\d{1,2})/70', v)
    if not m: continue
    n = int(m.group(1))
    if n > best and n - best <= 3: turn[n] = t; best = n

# ---- 4) 시뮬 턴 시작 경과 (ledger: 행동 완료 경과 → 턴 n의 첫 행동 시작 = 직전 행 elapsed)
sim = {}; prev = 0.0
for r in ledger:
    n = int(r['turn']); e = float(r['elapsed']); s = float(r['sec'])
    if n not in sim and n >= 1: sim[n] = e - s
    prev = e

out = []
print(" 턴   영상t    게임G   ΔG(턴)   시뮬경과  Δ시뮬   누적차(시뮬−게임)")
g0 = s0 = None; lastg = lasts = None
for n in sorted(turn):
    g = Gi(turn[n]); s = sim.get(n)
    if g is None or s is None: continue
    if g0 is None: g0, s0 = g, s
    dg = '' if lastg is None else f'{g - lastg:6.2f}'
    ds = '' if lasts is None else f'{s - lasts:6.2f}'
    print(f"{n:3d}  {turn[n]:7.2f}  {g - g0:7.2f}  {dg:>6}   {s - s0:7.2f}  {ds:>6}   {(s - s0) - (g - g0):+6.2f}")
    out.append((n, turn[n], g - g0, s - s0))
    lastg, lasts = g, s
if OUT:
    with open(OUT, 'w', newline='', encoding='utf-8') as f:
        w = csv.writer(f); w.writerow(['turn', 'video_t', 'game_G', 'sim_elapsed']); w.writerows(out)
print(f"\n앵커 {len(anchors)}개, 턴 {len(turn)}개")
