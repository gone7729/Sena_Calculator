# coding: utf-8
import csv,re,bisect,json
rows=list(csv.DictReader(open('events.csv',encoding='utf-8')))
# 1) 턴 시작: 부분 판독 허용 — 값에 '/'가 있고 숫자 끝자리가 N%10 인 첫 이벤트 (직전 턴 이후)
turn={}; last=0; lastt=24.0
tv=[(float(r['t']),r['value']) for r in rows if r['region']=='turn' and float(r['t'])>=25.0]
for n in range(1,60):
    cand=None
    for t,v in tv:
        if t<=lastt+0.6: continue
        m=re.match(r'^(\d+)/',v) or re.match(r'^(\d+)$',v)
        if not m: continue
        num=m.group(1)
        if num.endswith(str(n%10)) and (len(num)<=len(str(n))):
            cand=t; break
    if cand is None or cand-lastt>15: break
    turn[n]=cand; lastt=cand; last=n
# 2) 무쿨감 시계: 비스킷 장비강화(R3 t0, 120) · 리나 울림(t12, 120) 체인 → 게임 경과 G(t)
def chain(reg,t0,v0,tend):
    pts=[(t0,v0)]
    for r in rows:
        if r['region']!=reg: continue
        t=float(r['t'])
        if t<=t0 or t>tend or not re.fullmatch(r'\d{1,3}',r['value']): continue
        v=int(r['value']); pt,pv=pts[-1]; d=pv-v
        if 1<=d<=5 and d<=(t-pt)*1.3+4.6: pts.append((t,v))
    return pts
bis=chain('비스킷_bot',24.38,119,103.0)
lin=chain('리나_top',67.46,119,190.0)
print("비스킷 체인",bis[0],bis[-1],len(bis),"/ 리나 체인",lin[0],lin[-1],len(lin))
def remf(pts):
    P=[(t,v+1) for t,v in pts]; ts=[p[0] for p in P]
    def f(t):
        i=bisect.bisect_right(ts,t)-1
        if i<0 or i>=len(P)-1: return None
        (ta,ra),(tb,rb)=P[i],P[i+1]; return max(rb,ra-(t-ta))
    return f
fb,fl=remf(bis),remf(lin)
# G(t): 비스킷 기준(장비강화 완료 = 0). 리나 구간은 겹침(67.5~102)에서 오프셋 정렬
offs=[ (120-fb(t)) - (120-fl(t)) for t in [x/10 for x in range(690,1000,5)] if fb(t) is not None and fl(t) is not None]
off=sorted(offs)[len(offs)//2]
print(f"리나→비스킷 시계 오프셋 {off:.2f}s (겹침 {len(offs)}점, 범위 {min(offs):.2f}~{max(offs):.2f})")
def G(t):
    if fb(t) is not None and t<100: return 120-fb(t)
    if fl(t) is not None: return 120-fl(t)+off
    return None
out={n:(turn[n],G(turn[n])) for n in turn}
json.dump(out,open('turn_clock.json','w'))
ks=sorted(out)
print("턴  영상시각   게임경과(R3 장비강화 후)  Δ게임(다음 턴까지)")
for i,n in enumerate(ks[:-1]):
    t,g=out[n]; g2=out[ks[i+1]][1]
    print(f"{n:3d}  {t:7.2f}   {g if g is None else round(g,2)!s:>8}      {'' if g is None or g2 is None else f'{g2-g:5.2f}'}")
