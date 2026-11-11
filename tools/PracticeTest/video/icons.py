# coding: utf-8
"""녹화 영상에서 버프/디버프 아이콘을 전부 잘라 종류별로 묶는다 (아이콘 사전 만들기 1단계).

  python icons.py <영상.mp4> [출력폴더=icons] [프레임간격=20]

출력: <출력폴더>/tmpl/<id>.png (27x27 대표 아이콘), icons.csv (id·종류·횟수·처음/마지막 시각·위치),
      sheet.png (확대 목록 — 사람이 보고 이름을 붙이는 용도).
좌표·크기는 1920x1080 녹화 기준. 종류 = P(상시 버프, 파란 바탕) / T(턴제 버프, 어두운 바탕+숫자) /
      DP(상시 디버프, 빨간 바탕) / DT(턴제 디버프).
"""
import cv2, numpy as np, sys, os, csv

S = 27                    # 유닛 머리 위·적 HP바 옆 아이콘 한 변
PX, PY = 30, 31           # 격자 간격
BOSS = (1020, 95, 1320, 150, 31, 35)   # 보스 상단 줄: x0,y0,x1,y1, 한 변, 간격
P = 40                    # 적분영상 패딩

def imwrite(p, im): cv2.imencode('.png', im)[1].tofile(p)

def color_masks(im):
    b, g, r = [im[:, :, i].astype(np.int16) for i in range(3)]
    mx = np.maximum(np.maximum(r, g), b)
    m = {}
    m['blue'] = (np.abs(r-45) < 34) & (np.abs(g-117) < 34) & (np.abs(b-213) < 42)   # 상시 버프 바탕
    m['red'] = (r > 165) & (g < 100) & (b < 100)                                    # 상시 디버프 바탕
    m['dark'] = mx < 55                                                             # 턴제 바탕
    m['glyph_b'] = (b > 150) & (b-r > 60)                                           # 턴제 버프 문양
    m['glyph_r'] = (r > 150) & (r-g > 55) & (r-b > 40) & (g < 120)                  # 턴제 디버프 문양(주황 글자 제외)
    m['dim'] = mx < 95                                                              # 상시 디버프의 어두운 문양
    m['bluefam'] = (b-g > 50) & (b > 90)                                            # 파랑 계열(바탕+남색 문양)
    m['white'] = mx > 215
    return m

class Integral:
    def __init__(self, mask):
        self.I = cv2.integral(np.pad(mask.astype(np.float32), P))
        self.H, self.W = mask.shape
    def all(self, x0, y0, x1, y1):
        """모든 좌상단 (x,y)에 대한 [x+x0,x+x1)x[y+y0,y+y1) 합."""
        I, H, W = self.I, self.H, self.W
        return (I[P+y1:P+y1+H, P+x1:P+x1+W] - I[P+y0:P+y0+H, P+x1:P+x1+W]
                - I[P+y1:P+y1+H, P+x0:P+x0+W] + I[P+y0:P+y0+H, P+x0:P+x0+W])
    def at(self, x, y, w, h):
        I = self.I
        return float(I[P+y+h, P+x+w] - I[P+y, P+x+w] - I[P+y+h, P+x] + I[P+y, P+x])

def ring_detect(ints, s, region=None):
    """테두리가 한 색(파랑/빨강/어두움)이고 바깥은 그 색이 아닌 정사각형을 찾는다."""
    out = []
    for kind in ('blue', 'red', 'dark'):
        I = ints[kind]
        box, inner, outer = I.all(0, 0, s, s), I.all(3, 3, s-3, s-3), I.all(-3, -3, s+3, s+3)
        ring = (box-inner) / (s*s-(s-6)**2)
        outr = (outer-box) / ((s+6)**2-s*s)
        fill = inner / ((s-6)**2)
        e = 5; th = 3*(s-2*e)
        sides = np.minimum(np.minimum(I.all(e, 0, s-e, 3), I.all(e, s-3, s-e, s)),
                           np.minimum(I.all(0, e, 3, s-e), I.all(s-3, e, s, s-e))) / th
        ok = (ring >= 0.60) & (outr <= 0.42) & (fill <= 0.93) & (fill >= 0.15) & (sides >= 0.55)
        if kind == 'dark':
            gl = (ints['glyph_b'].all(2, 2, s-2, s-2) + ints['glyph_r'].all(2, 2, s-2, s-2)) / ((s-4)**2)
            ok &= (gl >= 0.10) & (gl <= 0.75)
        if region is not None:
            rm = np.zeros_like(ok); x0, y0, x1, y1 = region; rm[y0:y1, x0:x1] = True; ok &= rm
        ys, xs = np.nonzero(ok)
        sc = ring - outr
        out += [(float(sc[y, x]), int(x), int(y), s) for y, x in zip(ys, xs)]
    out.sort(reverse=True); keep = []
    for c in out:
        if all(abs(c[1]-k[1]) >= s-7 or abs(c[2]-k[2]) >= s-7 for k in keep): keep.append(c)
    return [(x, y, s) for _, x, y, s in keep]

def iconlike(ints, x, y, s):
    a = float(s*s)
    cov = (ints['bluefam'].at(x, y, s, s) + ints['dark'].at(x, y, s, s) + ints['red'].at(x, y, s, s)
           + ints['glyph_r'].at(x, y, s, s) + ints['white'].at(x, y, s, s)) / a
    dark = ints['dark'].at(x, y, s, s) / a
    return cov if dark <= 0.88 else 0.0

def extend(ints, cells, s, px, py, W, H):
    """검출된 칸의 격자 이웃 중 아이콘처럼 보이는 칸을 보완(문양이 테두리에 닿아 놓친 아이콘)."""
    cells = list(cells)
    for _ in range(4):
        new = []
        for (x, y, _s) in cells:
            for dx, dy in ((px, 0), (-px, 0), (0, py), (0, -py)):
                cx, cy = x+dx, y+dy
                if cx < 2 or cy < 2 or cx+s+2 >= W or cy+s+2 >= H: continue
                if any(abs(cx-k[0]) < s-7 and abs(cy-k[1]) < s-7 for k in cells+new): continue
                best = max(((iconlike(ints, cx+ax, cy+ay, s), cx+ax, cy+ay) for ax in (-2, -1, 0, 1, 2) for ay in (-2, -1, 0, 1, 2)))
                if best[0] >= 0.90: new.append((best[1], best[2], s))
        if not new: break
        cells += new
    return cells

BG = {'P': 'blue', 'DP': 'red', 'T': 'dark', 'DT': 'dark'}

def classify(ints, x, y, s):
    a = float(s*s)
    blue, red = ints['blue'].at(x, y, s, s)/a, ints['red'].at(x, y, s, s)/a
    if blue > 0.25: return 'P'
    if red > 0.25: return 'DP'
    return 'DT' if ints['glyph_r'].at(x, y, s, s) > ints['glyph_b'].at(x, y, s, s) else 'T'

def snap(ints, x, y, s, kind, W, H):
    """바탕색 정사각형 테두리에 맞춰 ±4px 위치 보정. (점수, x, y)"""
    I = ints[BG[kind]]; ring_a = s*s-(s-6)**2; out_a = (s+6)**2-s*s; best = (-9.0, x, y)
    for ax in range(-4, 5):
        for ay in range(-4, 5):
            cx, cy = x+ax, y+ay
            if cx < 4 or cy < 4 or cx+s+4 >= W or cy+s+4 >= H: continue
            box = I.at(cx, cy, s, s)
            sc = (box-I.at(cx+3, cy+3, s-6, s-6))/ring_a - (I.at(cx-3, cy-3, s+6, s+6)-box)/out_a
            if sc > best[0]: best = (sc, cx, cy)
    return best

def valid(ints, x, y, s, kind):
    """바탕 + 문양 구조가 있는 칸만 (단색 이펙트·글자·HP바 제외)."""
    a = float(s*s); f = lambda k: ints[k].at(x, y, s, s)/a
    if kind == 'P': return 0.30 <= f('blue') <= 0.90 and f('bluefam')-f('blue') >= 0.06
    if kind == 'DP': return 0.30 <= f('red') <= 0.90 and f('dim') >= 0.05
    if kind == 'T': return 0.30 <= f('dark') <= 0.88 and f('glyph_b') >= 0.07
    return 0.30 <= f('dark') <= 0.88 and f('glyph_r') >= 0.07

def finalize(ints, cells, W, H):
    out = []
    for x, y, s in cells:
        kind = classify(ints, x, y, s)
        sc, x, y = snap(ints, x, y, s, kind, W, H)
        if sc < 0.25 or not valid(ints, x, y, s, kind): continue
        if any(abs(x-o[0]) < s-7 and abs(y-o[1]) < s-7 for o in out): continue
        out.append((x, y, s, kind))
    return out

ROI_Y = (90, 660)          # 버프/디버프 아이콘이 뜨는 세로 범위 (하단 스킬 UI·상단 점수 제외)

def detect(im):
    x0, y0, x1, y1, bs, bp = BOSS
    sub = im[ROI_Y[0]:ROI_Y[1]]; H, W = sub.shape[:2]
    ints = {k: Integral(v) for k, v in color_masks(sub).items()}
    cells = extend(ints, ring_detect(ints, S), S, PX, PY, W, H)
    res = [(x, y+ROI_Y[0], s, k) for x, y, s, k in finalize(ints, cells, W, H)
           if not (x0-10 <= x <= x1 and y0-10 <= y+ROI_Y[0] <= y1)]
    m = 45; bx, by = x0-m, y0-m
    bsub = im[by:y1+m, bx:x1+m]; bH, bW = bsub.shape[:2]
    bints = {k: Integral(v) for k, v in color_masks(bsub).items()}
    boss = extend(bints, ring_detect(bints, bs, (m, m, bW-m, bH-m)), bs, bp, 10**6, bW, bH)
    res += [(x+bx, y+by, s, k) for x, y, s, k in finalize(bints, boss, bW, bH)]
    return res

def where(x, y, s):
    if s == BOSS[4]: return '보스상단'
    return '아군' if x < 860 else '적'

# ---- 군집화 ----
MASK_T = np.ones((S, S), bool); MASK_T[13:, 14:] = False     # 턴제: 우하단 남은 턴 숫자 제외
MASK_P = np.ones((S, S), bool)

def ncc(a, b, m):
    a, b = a[m], b[m]; a = a-a.mean(); b = b-b.mean()
    d = np.sqrt((a*a).sum()*(b*b).sum())
    return float((a*b).sum()/d) if d > 1e-6 else 0.0

def sncc(a, b, m, R=3):
    """±R px 어긋남을 허용한 유사도 (겹치는 영역만 비교)."""
    best = -1.0
    for dx in range(-R, R+1):
        for dy in range(-R, R+1):
            y0, y1, x0, x1 = max(0, dy), S+min(0, dy), max(0, dx), S+min(0, dx)
            A, B = a[y0:y1, x0:x1], b[y0-dy:y1-dy, x0-dx:x1-dx]
            M = m[y0:y1, x0:x1] & m[y0-dy:y1-dy, x0-dx:x1-dx]
            best = max(best, ncc(A, B, M))
    return best

def merge_shifted(cl, th=0.85):
    """같은 아이콘이 몇 픽셀 어긋나 따로 묶인 군집을 합친다 (횟수 많은 쪽이 대표)."""
    cl = sorted(cl, key=lambda c: -c['n']); out = []
    for c in cl:
        g = cv2.cvtColor(c['rep'], cv2.COLOR_BGR2GRAY).astype(np.float32); c['g'] = g
        m = MASK_T if c['kind'] in ('T', 'DT') else MASK_P
        hit = next((k for k in out if k['kind'] == c['kind'] and sncc(k['g'], g, m) >= th), None)
        if hit:
            hit['n'] += c['n']; hit['first'] = min(hit['first'], c['first']); hit['last'] = max(hit['last'], c['last'])
            for w, n in c['where'].items(): hit['where'][w] = hit['where'].get(w, 0)+n
        else: out.append(c)
    return out

def main():
    video = sys.argv[1]; out = sys.argv[2] if len(sys.argv) > 2 else 'icons'; step = int(sys.argv[3]) if len(sys.argv) > 3 else 20
    os.makedirs(os.path.join(out, 'tmpl'), exist_ok=True)
    cap = cv2.VideoCapture(video); clusters = []; f = 0
    mats = {}                                   # kind -> (군집 목록, 정규화 평균 행렬)
    def unit(v):
        v = v-v.mean(); n = np.linalg.norm(v); return v/n if n > 1e-6 else v*0
    while True:
        ok, fr = cap.read()
        if not ok: break
        if f % step == 0:
            t = cap.get(cv2.CAP_PROP_POS_MSEC)/1000.0
            for x, y, s, kind in detect(fr):
                big = fr[y-2:y+s+2, x-2:x+s+2]
                if big.shape[0] != s+4 or big.shape[1] != s+4: continue
                if s != S: big = cv2.resize(big, (S+4, S+4), interpolation=cv2.INTER_AREA)
                g = cv2.cvtColor(big, cv2.COLOR_BGR2GRAY).astype(np.float32)
                m = MASK_T if kind in ('T', 'DT') else MASK_P
                shifts = [(dx, dy) for dx in (1, 2, 3) for dy in (1, 2, 3)]
                V = np.stack([unit(g[dy:dy+S, dx:dx+S][m]) for dx, dy in shifts])
                lst, M = mats.get(kind, ([], None))
                v, c, dx, dy = 0.0, None, 2, 2
                if lst:
                    sim = V @ M[:len(lst)].T
                    i, j = np.unravel_index(int(sim.argmax()), sim.shape)
                    v, c, (dx, dy) = float(sim[i, j]), lst[j], shifts[i]
                crop = big[dy:dy+S, dx:dx+S]; gc = g[dy:dy+S, dx:dx+S]
                if c is None or v < 0.80:
                    c = dict(kind=kind, mean=gc.copy(), n=0, first=t, last=t, where={}, samples=[]); clusters.append(c)
                    if M is None: M = np.zeros((64, int(m.sum())), np.float32)
                    if len(lst) >= M.shape[0]: M = np.vstack([M, np.zeros_like(M)])
                    lst.append(c); c['row'] = len(lst)-1; mats[kind] = (lst, M)
                else:
                    c['mean'] = (c['mean']*c['n']+gc)/(c['n']+1)
                M[c['row']] = unit(c['mean'][m])
                c['n'] += 1; c['last'] = t
                w = where(x, y, s); c['where'][w] = c['where'].get(w, 0)+1
                if len(c['samples']) < 60: c['samples'].append((crop.copy(), gc))
            if (f//step) % 50 == 0: print(f"frame {f} t={t:.0f}s clusters={len(clusters)}", flush=True)
        f += 1
    # 비슷한 군집 병합 + 드문 것(가림·오검출) 제거
    clusters.sort(key=lambda c: -c['n']); merged = []
    for c in clusters:
        m = MASK_T if c['kind'] in ('T', 'DT') else MASK_P
        hit = next((k for k in merged if k['kind'] == c['kind'] and ncc(k['mean'], c['mean'], m) >= 0.86), None)
        if hit:
            hit['n'] += c['n']; hit['first'] = min(hit['first'], c['first']); hit['last'] = max(hit['last'], c['last'])
            for w, n in c['where'].items(): hit['where'][w] = hit['where'].get(w, 0)+n
        else: merged.append(c)
    keep = [c for c in merged if c['n'] >= 10]
    for c in keep:
        m = MASK_T if c['kind'] in ('T', 'DT') else MASK_P
        c['rep'] = max(c['samples'], key=lambda sgl: ncc(sgl[1], c['mean'], m))[0]     # 평균에 가장 가까운 실제 샘플
    keep = merge_shifted(keep)
    order = {'P': 0, 'T': 1, 'DP': 2, 'DT': 3}
    keep.sort(key=lambda c: (order[c['kind']], -c['n']))
    rows = []; cnt = {}
    for c in keep:
        cnt[c['kind']] = cnt.get(c['kind'], 0)+1; cid = f"{c['kind']}{cnt[c['kind']]:02d}"
        c['id'] = cid; rep = c['rep']
        imwrite(os.path.join(out, 'tmpl', cid+'.png'), rep)
        rows.append([cid, c['kind'], c['n'], f"{c['first']:.1f}", f"{c['last']:.1f}",
                     ' '.join(f"{w}:{n}" for w, n in sorted(c['where'].items(), key=lambda kv: -kv[1])), ''])
    with open(os.path.join(out, 'icons.csv'), 'w', newline='', encoding='utf-8-sig') as fp:
        w = csv.writer(fp); w.writerow(['id', 'kind', 'count', 'first_s', 'last_s', 'where', 'label']); w.writerows(rows)
    # 목록 이미지
    Z, CW, CH, COLS = 4, 190, 150, 8
    sheet = np.full((CH*((len(keep)+COLS-1)//COLS), CW*COLS, 3), 32, np.uint8)
    for i, c in enumerate(keep):
        ox, oy = (i % COLS)*CW, (i//COLS)*CH
        z = cv2.resize(c['rep'], (S*Z, S*Z), interpolation=cv2.INTER_CUBIC)
        sheet[oy+8:oy+8+S*Z, ox+8:ox+8+S*Z] = z
        cv2.putText(sheet, c['id'], (ox+124, oy+40), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255, 255, 255), 2, cv2.LINE_AA)
        cv2.putText(sheet, f"n={c['n']}", (ox+124, oy+66), cv2.FONT_HERSHEY_SIMPLEX, 0.45, (170, 170, 170), 1, cv2.LINE_AA)
        cv2.putText(sheet, f"{c['first']:.0f}s~", (ox+124, oy+86), cv2.FONT_HERSHEY_SIMPLEX, 0.45, (170, 170, 170), 1, cv2.LINE_AA)
    imwrite(os.path.join(out, 'sheet.png'), sheet)
    print(f"frames {f}, raw clusters {len(clusters)}, merged {len(merged)}, kept {len(keep)}:", {k: v for k, v in cnt.items()})

if __name__ == '__main__':
    main()
