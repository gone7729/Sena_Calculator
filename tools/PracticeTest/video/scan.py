# coding: utf-8
import cv2, subprocess, csv, sys
TESS=r"C:\Program Files\Tesseract-OCR\tesseract.exe"
V=r"C:\Users\rhkde\Desktop\김광동\개인\월요일 공성전 시간소요 테스트.mp4"
R={
 'turn':(30,232,120,262),
 '리나_top':(815,795,910,885),'리나_bot':(815,895,910,985),
 '지크_top':(1060,795,1155,885),'지크_bot':(1060,895,1155,985),
 '비스킷_top':(1305,795,1395,885),'비스킷_bot':(1305,895,1395,985),
 '미호_top':(1545,795,1640,885),'미호_bot':(1545,895,1640,985),
 '나타_top':(1790,795,1880,885),'나타_bot':(1790,895,1880,985),
 'score':(40,0,330,40),
}
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
out=open('events.csv','w',newline='',encoding='utf-8'); w=csv.writer(out); w.writerow(['t','frame','region','value'])
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
