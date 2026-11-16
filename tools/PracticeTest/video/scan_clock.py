# coding: utf-8
"""녹화 영상 → 게임 시계용 판독 (0.1초 간격). 턴 카운터 + 보스 스킬 쿨 2칸 + 지정 아군 쿨 칸.

  python scan_clock.py <영상> <출력csv> [시작초=80] [끝초=끝] [아군칸=비스킷_top,비스킷_bot,리나_top,리나_bot]

창 모드 녹화(2026-11 수요일, 상단 제목줄) 좌표 기준. 보스 쿨 칸 = 보스 초상 오른쪽 1·2번째 칸(1스킬·2스킬).
아군 칸 이름은 scan.py와 같은 "영웅_top/bot" — 진형번호 순서 NAMES로 열을 찾는다.
값이 바뀐 시점만 기록(t, region, value). 판독 잡음은 clock_fit.py에서 거른다.
"""
import cv2, subprocess, csv, sys
TESS = r"C:\Program Files\Tesseract-OCR\tesseract.exe"
V, OUT = sys.argv[1], sys.argv[2]
T0 = float(sys.argv[3]) if len(sys.argv) > 3 else 80.0
T1 = float(sys.argv[4]) if len(sys.argv) > 4 else 1e9
NAMES = "라이언,리나,비스킷,미호,나타".split(',')
SLOT_X = [(815, 910), (1060, 1155), (1305, 1395), (1545, 1640), (1790, 1880)]
want = (sys.argv[5] if len(sys.argv) > 5 else "비스킷_top,비스킷_bot,리나_top,리나_bot").split(',')
R = {'turn': (15, 230, 125, 262), 'boss1': (768, 112, 834, 178), 'boss2': (858, 112, 924, 178)}
for n, (x1, x2) in zip(NAMES, SLOT_X):
    for k, (y1, y2) in (('top', (795, 885)), ('bot', (895, 985))):
        if f'{n}_{k}' in want: R[f'{n}_{k}'] = (x1, y1, x2, y2)

def ocr(crop, wl):
    g = cv2.cvtColor(crop, cv2.COLOR_BGR2GRAY)
    _, b = cv2.threshold(g, 200, 255, cv2.THRESH_BINARY_INV)
    b = cv2.resize(b, None, fx=3, fy=3, interpolation=cv2.INTER_NEAREST)
    ok, buf = cv2.imencode('.png', b)
    r = subprocess.run([TESS, 'stdin', 'stdout', '--psm', '7', '-c', f'tessedit_char_whitelist={wl}'],
                       input=buf.tobytes(), capture_output=True)
    return r.stdout.decode().strip(), b

cap = cv2.VideoCapture(V); fps = cap.get(cv2.CAP_PROP_FPS)
cap.set(cv2.CAP_PROP_POS_MSEC, T0 * 1000)
step = max(1, round(fps / 10))
last_img = {k: None for k in R}; last_val = {k: None for k in R}
out = open(OUT, 'w', newline='', encoding='utf-8'); w = csv.writer(out); w.writerow(['t', 'region', 'value'])
f = 0
while True:
    ok, fr = cap.read()
    if not ok: break
    t = cap.get(cv2.CAP_PROP_POS_MSEC) / 1000.0
    if t > T1: break
    f += 1
    if f % step: continue
    for k, (x1, y1, x2, y2) in R.items():
        g = cv2.cvtColor(fr[y1:y2, x1:x2], cv2.COLOR_BGR2GRAY)
        li = last_img[k]
        if li is not None and cv2.absdiff(g, li).mean() < 3: continue
        last_img[k] = g
        v, _ = ocr(fr[y1:y2, x1:x2], '0123456789/' if k == 'turn' else '0123456789')
        if v != last_val[k]:
            last_val[k] = v; w.writerow([f'{t:.3f}', k, v])
    if f % 1200 == 0: out.flush(); print(f'{t:.0f}s', flush=True)
out.close(); print('done')
