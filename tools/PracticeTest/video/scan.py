# coding: utf-8
import cv2, subprocess, csv, sys
TESS=r"C:\Program Files\Tesseract-OCR\tesseract.exe"
# 사용법: python scan.py [영상] [진형번호1~5 이름(쉼표)] [출력 csv]   — 인자 생략 시 2026-10 월요일 녹화 기준.
V=sys.argv[1] if len(sys.argv)>1 else r"C:\Users\rhkde\Desktop\김광동\개인\월요일 공성전 시간소요 테스트.mp4"
NAMES=(sys.argv[2] if len(sys.argv)>2 else "리나,지크,비스킷,미호,나타").split(',')
OUT=sys.argv[3] if len(sys.argv)>3 else 'events.csv'
SLOT_X=[(815,910),(1060,1155),(1305,1395),(1545,1640),(1790,1880)]   # 하단 쿨 아이콘 열(진형번호 1~5)
R={'turn':(30,232,120,262)}
for n,(x1,x2) in zip(NAMES,SLOT_X):
    R[f'{n}_top']=(x1,795,x2,885); R[f'{n}_bot']=(x1,895,x2,985)
R['score']=(40,0,330,40)
# 창 모드 녹화(상단 제목줄 있음, 2026-11 수요일): 점수가 아래로 밀림 → 4번째 인자 "window"
if len(sys.argv)>4 and sys.argv[4]=='window':
    R['score']=(60,40,330,85); R['turn']=(15,230,125,262)
def prep(crop):
    g=cv2.cvtColor(crop,cv2.COLOR_BGR2GRAY)
    _,b=cv2.threshold(g,200,255,cv2.THRESH_BINARY_INV)
    return b
def ocr(b, wl):
    b=cv2.resize(b,None,fx=3,fy=3,interpolation=cv2.INTER_NEAREST)
    ok,buf=cv2.imencode('.png',b)
    r=subprocess.run([TESS,'stdin','stdout','--psm','7','-c',f'tessedit_char_whitelist={wl}'],input=buf.tobytes(),capture_output=True)
    return r.stdout.decode().strip()
cap=cv2.VideoCapture(V)
last_img={k:None for k in R}; last_val={k:None for k in R}
out=open(OUT,'w',newline='',encoding='utf-8'); w=csv.writer(out); w.writerow(['t','frame','region','value'])
f=0
while True:
    ok,fr=cap.read()
    if not ok: break
    t=cap.get(cv2.CAP_PROP_POS_MSEC)/1000.0
    for k,(x1,y1,x2,y2) in R.items():
        b=prep(fr[y1:y2,x1:x2])
        li=last_img[k]
        if li is not None and cv2.absdiff(b,li).mean()<4: continue
        last_img[k]=b
        v=ocr(b,'0123456789/,' if k in ('turn','score') else '0123456789')
        if v!=last_val[k]:
            last_val[k]=v; w.writerow([f'{t:.3f}',f,k,v])
    f+=1
    if f%600==0: out.flush(); print(f, flush=True)
out.close(); print('done',f)
