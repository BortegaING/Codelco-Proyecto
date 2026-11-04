/* Vista isométrica · Báscula N9, refinería Codelco Ventanas (propuesta Ingmétrica).
   Mundo en cm: X = sentido del camino (nave -> salida), Y = profundidad, Z = altura.
   window.ISO.update(estado, ctx) dibuja el momento; vehículos y carga se animan entre pasos. */
(function(){
'use strict';
const svg=document.getElementById('iso');
if(!svg) return;
const reduce=!!(window.matchMedia&&window.matchMedia('(prefers-reduced-motion: reduce)').matches);
const C=0.866, S=0.5;
const r1=v=>Math.round(v*10)/10;
const P=(x,y,z)=>[r1((x-y)*C),r1((x+y)*S-(z||0))];
const pts=a=>a.map(p=>P(p[0],p[1],p[2]).join(',')).join(' ');
const poly=(a,c)=>`<polygon points="${pts(a)}" class="${c}"/>`;
const num=(v,d)=>typeof v==='number'?v:d;

/* Caja: cara +Y (izquierda) en tono medio, cara +X (derecha) oscura, techo claro */
function box(x,y,z,w,d,h,pal){
  const l=[[x,y+d,z],[x+w,y+d,z],[x+w,y+d,z+h],[x,y+d,z+h]], r=[[x+w,y,z],[x+w,y+d,z],[x+w,y+d,z+h],[x+w,y,z+h]], t=[[x,y,z+h],[x+w,y,z+h],[x+w,y+d,z+h],[x,y+d,z+h]];
  return poly(l,pal)+poly(l,'sh1')+poly(r,pal)+poly(r,'sh2')+poly(t,pal);
}
/* Contenido en coordenadas de mundo pintado sobre un plano: techo (z fijo), cara +Y o cara +X */
const mTop=(x,y,z)=>{const [e,f]=P(x,y,z);return `matrix(${C} ${S} ${-C} ${S} ${e} ${f})`;};
const mFront=(x,y,z)=>{const [e,f]=P(x,y,z);return `matrix(${C} ${S} 0 1 ${e} ${f})`;};
const mSide=(x,y,z)=>{const [e,f]=P(x,y,z);return `matrix(${C} ${-S} 0 1 ${e} ${f})`;};

const BAS={x:300,y:105,w:340,d:130};          // báscula N9, al ras del piso
const CAS={x:380,y:0,w:180,d:70,h:250};       // caseta del pesador, ventana hacia la báscula
const LENTE=[470,169,272];                    // cámara sobre el centro de la báscula
const CARRIL=110;                              // borde trasero de vehículos y carga
const QUEDA_X=BAS.x+10;                        // pallet que queda en la báscula al sacar el de arriba (pesaje por diferencia)
const FOCO={
  origen:{r:[24,10,166,246],mk:'A',txt:'ent'},
  bascula:{r:[BAS.x-8,BAS.y-8,BAS.w+16,BAS.d+16],mk:'D'},
  camara:{r:[BAS.x,BAS.y,BAS.w,BAS.d],mk:'B'},
  balizas:{r:[640,16,72,44],mk:'C'},
  caseta:{r:[370,0,200,92],mk:'E'},
  salida:{r:[660,84,236,182],mk:'F',txt:'sal'}
};
const MK={A:[[82,30,170],0,0],B:[[482,170,282],18,-4],C:[[677,37,236],0,0],D:[[640,238,0],12,8],E:[[380,70,250],-14,0],F:[[830,96,0],0,0]};

function base(){
  return `<g transform="${mTop(0,0,0)}"><rect x="0" y="0" width="900" height="340" class="fl0"/><rect x="0" y="82" width="900" height="186" class="fl1"/>`+
    `<rect x="30" y="100" width="150" height="150" rx="4" class="libre"/><text x="40" y="264" class="ftxt sm">ZONA DE ARMADO</text>`+
    `<text x="14" y="314" class="ftxt" id="isoEnt">DESDE LA NAVE →</text><text x="742" y="314" class="ftxt" id="isoSal">SALIDA →</text>`+
    `<rect x="${BAS.x}" y="${BAS.y}" width="${BAS.w}" height="${BAS.d}" class="plat"/><rect x="${BAS.x}" y="${BAS.y}" width="${BAS.w}" height="${BAS.d}" class="junta"/>`+
    `<text x="${BAS.x+14}" y="${BAS.y+BAS.d-10}" class="ftxt sm">BÁSCULA N9 · AL RAS DEL PISO</text></g>`;
}
function cono(x,y){const [sx,sy]=P(x,y,0);return `<polygon points="${sx-6},${sy} ${sx+6},${sy} ${sx},${sy-24}" class="cono"/><line x1="${sx-3.5}" y1="${sy-9}" x2="${sx+3.5}" y2="${sy-9}" class="cono-b"/>`;}
function caseta(){
  const {x,y,w,d,h}=CAS;
  return box(x,y,0,w,d,h,'mu')+box(x-6,y-6,h,w+12,d+12,8,'dk')+
    `<g transform="${mFront(x,y+d,h)}"><rect x="18" y="62" width="144" height="84" class="glass"/>`+
    `<circle cx="70" cy="98" r="9" class="skin"/><rect x="56" y="108" width="28" height="38" rx="6" class="vest"/><rect x="100" y="122" width="34" height="22" class="pc"/>`+
    `<rect x="18" y="62" width="144" height="84" class="glass-f"/>`+
    `<rect x="36" y="14" width="50" height="30" rx="2" class="cartel"/><text x="61" y="27" text-anchor="middle" class="cartel-t">BÁSCULA</text><text x="61" y="39" text-anchor="middle" class="cartel-t">Nº 9</text>`+
    `<rect x="122" y="160" width="28" height="90" class="puerta"/></g>`;
}
function kiosko(st,ctx){
  return box(66,22,0,22,14,4,'st')+box(74,26,4,6,6,90,'st')+box(56,26,94,44,8,58,'dk')+
    `<g transform="${mFront(56,34,152)}"><rect x="3" y="3" width="38" height="50" rx="1.5" class="scr-bg"/><rect x="3" y="3" width="38" height="7" class="band ok"/>`+
    `<text x="22" y="27" text-anchor="middle" class="scr-n">${ctx.codigo||''}</text><text x="22" y="42" text-anchor="middle" class="scr-s">ETIQUETA</text></g>`+
    box(108,24,0,30,20,70,'st')+box(110,26,70,26,16,16,'dk')+(st.imprime?box(116,42,74,12,9,1,'wh'):'');
}
function lamp(x,y,z,w,d,h,col){
  const l=[[x,y+d,z],[x+w,y+d,z],[x+w,y+d,z+h],[x,y+d,z+h]], r=[[x+w,y,z],[x+w,y+d,z],[x+w,y+d,z+h],[x+w,y,z+h]], t=[[x,y,z+h],[x+w,y,z+h],[x+w,y+d,z+h],[x,y+d,z+h]];
  return poly(l,'lz '+col)+poly(l,'sh1')+poly(r,'lz '+col)+poly(r,'sh2')+poly(t,'lz '+col);
}
function baliza(x,y,state){
  const p=String(state||'off').split('-'), col=p[0], bl=p[1]==='blink', c=P(x+7.5,y+7.5,165);
  return box(x+5,y+5,0,5,5,146,'st')+box(x-1,y-1,146,17,17,4,'dk')+
    `<g${bl?' class="blink"':''}>`+(col!=='off'?`<circle cx="${c[0]}" cy="${c[1]}" r="26" class="halo ${col}"/>`:'')+lamp(x,y,150,15,15,30,col)+`</g>`+
    box(x-1,y-1,180,17,17,4,'dk');
}
function persona(x,y){
  const [sx,sy]=P(x,y,0), R=(a,b,w,h,rx,c)=>`<rect x="${r1(sx+a)}" y="${r1(sy+b)}" width="${w}" height="${h}" rx="${rx}" class="${c}"/>`;
  return `<ellipse cx="${sx}" cy="${sy}" rx="17" ry="8" class="shadow"/>`+R(-9,-80,8,80,2,'pants')+R(1,-80,8,80,2,'pants')+R(-20,-136,7,54,3.5,'vest')+R(13,-136,7,54,3.5,'vest')+
    R(-14,-142,28,68,7,'vest')+R(-14,-118,28,4,0,'refl')+R(-14,-104,28,4,0,'refl')+`<circle cx="${sx}" cy="${r1(sy-153)}" r="11" class="skin"/>`+
    `<path d="M${r1(sx-13)} ${r1(sy-154)} a13 12 0 0 1 26 0 z" class="helmet"/>`+R(-16,-156,32,3.5,1.5,'helmet');
}
function conductor(x,y,z){
  const [sx,sy]=P(x,y,z);
  return `<rect x="${r1(sx-10)}" y="${r1(sy-52)}" width="20" height="40" rx="5" class="vest"/><circle cx="${sx}" cy="${r1(sy-62)}" r="9" class="skin"/>`+
    `<path d="M${r1(sx-10.5)} ${r1(sy-63)} a10.5 10 0 0 1 21 0 z" class="helmet"/>`;
}
/* Paquete de cátodos (con orejas) o de láminas (en pallet, tara fija). Etiqueta arriba o marca de plumón. */
function paquete(x,y,z,o){
  let s='';
  if(o.tipo==='laminas'){s+=box(x,y,z,110,110,12,'wd');z+=12;}
  const h=o.tipo==='laminas'?40:52;
  s+=box(x,y,z,110,110,h,'cu');
  for(let k=4;k<h;k+=4) s+=`<polyline points="${pts([[x,y+110,z+k],[x+110,y+110,z+k],[x+110,y,z+k]])}" class="cu-line"/>`;
  if(o.tipo==='laminas') s+=`<g transform="${mTop(x,y,z+h)}"><rect x="30" y="0" width="5" height="110" class="zuncho"/><rect x="75" y="0" width="5" height="110" class="zuncho"/></g>`;
  else for(let k=0;k<4;k++) s+=box(x+110,y+12+k*26,z+h-12,6,12,10,'cu');
  if(o.etq) s+=`<g transform="${mTop(x+70,y+14,z+h)}"><rect width="30" height="22" class="wh"/><rect x="3" y="3" width="11" height="11" class="qr"/><rect x="17" y="4" width="10" height="3" class="qr"/>`+
    `<rect x="17" y="10" width="7" height="3" class="qr"/><rect x="3" y="17" width="24" height="3" class="qr"/>${o.scan?'<rect x="-7" y="-7" width="44" height="36" class="scan"/>':''}</g>`;
  else if(o.plumon) s+=`<g transform="${mTop(x+52,y+30,z+h)}"><text x="0" y="12" class="plumon">${o.plumon}</text></g>`;
  return s;
}
function grua(x,y,lift,malas,carga){
  const rueda=(rx,ry,big,bad)=>box(rx,ry,0,big?40:34,10,big?40:34,bad?'bad':'dk');
  let s=rueda(x+18,y-2,false,false)+rueda(x+118,y-2,true,malas);
  s+=box(x,y,10,165,110,52,'am')+box(x,y,62,42,110,30,'dk');
  s+=box(x+50,y+4,62,5,5,128,'dk')+box(x+138,y+4,62,5,5,128,'dk')+conductor(x+92,y+56,62);
  s+=box(x+50,y+100,62,5,5,128,'dk')+box(x+138,y+100,62,5,5,128,'dk')+box(x+48,y+2,190,97,106,5,'dk');
  s+=box(x+165,y+16,0,8,8,232,'dk')+box(x+165,y+86,0,8,8,232,'dk')+box(x+165,y+16,lift+36,8,78,8,'dk');
  s+=box(x+173,y+22,lift,120,10,4,'dk')+box(x+173,y+78,lift,120,10,4,'dk');
  if(carga) s+=carga(x+178,y,lift+4);
  return s+rueda(x+18,y+102,false,false)+rueda(x+118,y+102,true,malas);
}
function tractor(x,y,malas){
  const rueda=(rx,ry,bad)=>box(rx,ry,0,36,10,36,bad?'bad':'dk');
  return rueda(x+14,y-2,malas)+rueda(x+96,y-2,false)+box(x,y,12,140,110,46,'tg')+box(x+12,y+6,58,74,98,80,'tg')+
    `<g transform="${mFront(x+12,y+104,138)}"><rect x="8" y="10" width="56" height="34" class="glass"/></g>`+
    `<g transform="${mFront(x,y+110,58)}"><rect x="0" y="8" width="140" height="7" class="lima"/></g>`+
    rueda(x+14,y+102,malas)+rueda(x+96,y+102,false);
}
function reticulado(w,h){
  const n=Math.max(2,Math.round(w/50));
  let d=`M0 0 H${w} M0 ${h} H${w}`;
  for(let i=0;i<=n;i++){const a=r1(i*w/n);d+=` M${a} 0 V${h}`;if(i<n) d+=` M${a} ${i%2?0:h} L${r1((i+1)*w/n)} ${i%2?h:0}`;}
  return `<path d="${d}" class="truss"/>`;
}
/* Carro de restos de ánodos: marco reticulado, restos colgados y orejas de cobre arriba */
function carro(x,y,o){
  const rueda=(rx,ry,bad)=>box(rx,ry,0,40,10,40,bad?'bad':'dk');
  let s=rueda(x+30,y-2,o.malIzq)+rueda(x+230,y-2,o.malDer)+box(x,y,28,300,120,16,'ru')+box(x+8,y+14,44,284,92,100,'dk');
  for(let k=0;k<20;k++) s+=box(x+14+k*14,y+18,144,8,84,7,'cu');
  s+=`<g transform="${mFront(x,y+120,150)}">${reticulado(300,106)}<text x="150" y="64" text-anchor="middle" class="num-carro">${o.num||''}</text></g>`;
  s+=`<g transform="${mSide(x+300,y+120,150)}">${reticulado(120,106)}</g>`;
  if(o.etq) s+=`<g transform="${mFront(x,y+120,150)}"><rect x="12" y="8" width="26" height="18" class="wh"/><rect x="15" y="11" width="9" height="9" class="qr"/>${o.scan?'<rect x="6" y="2" width="38" height="30" class="scan"/>':''}</g>`;
  return s+rueda(x+30,y+112,o.malIzq)+rueda(x+230,y+112,o.malDer);
}
function nodulos(){
  let s='';
  [[312,224],[326,229],[316,118],[628,214]].forEach(([x,y])=>{const [sx,sy]=P(x,y,0);s+=`<circle cx="${sx}" cy="${r1(sy-3)}" r="3.5" class="nod"/>`;});
  return s;
}
function camara(alerta){
  const L=P(LENTE[0],LENTE[1],LENTE[2]), cls=alerta?'fovl bad':'fovl';
  let s='';
  [[BAS.x,BAS.y],[BAS.x+BAS.w,BAS.y],[BAS.x+BAS.w,BAS.y+BAS.d],[BAS.x,BAS.y+BAS.d]].forEach(([x,y])=>{const q=P(x,y,0);s+=`<line x1="${L[0]}" y1="${L[1]}" x2="${q[0]}" y2="${q[1]}" class="${cls}"/>`;});
  return s+box(465,26,258,10,10,40,'st')+box(465,26,290,10,150,8,'st')+box(460,160,272,20,18,18,'dk')+`<ellipse cx="${L[0]}" cy="${L[1]}" rx="5" ry="3" class="lens"/>`;
}
function marcas(st){
  const f=FOCO[st.focus], alerta=st.cam&&!st.cam.ok;
  let s=`<g transform="${mTop(0,0,0)}"><rect x="${BAS.x}" y="${BAS.y}" width="${BAS.w}" height="${BAS.d}" class="fov${alerta?' bad':''}"/>`;
  if(f) s+=`<rect x="${f.r[0]}" y="${f.r[1]}" width="${f.r[2]}" height="${f.r[3]}" rx="4" class="foco-r"/>`;
  if(st.piso) s+=`<circle cx="318" cy="222" r="22" class="ring blink"/>`;
  return s+'</g>';
}
function marcador(id,on){
  const m=MK[id], p=P(m[0][0],m[0][1],m[0][2]), x=r1(p[0]+m[1]), y=r1(p[1]+m[2]);
  return (on?`<circle cx="${x}" cy="${y}" r="17" class="mk-ring"/>`:'')+
    `<circle cx="${x}" cy="${y}" r="${on?12:9}" class="mk${on?' on':''}"/><text x="${x}" y="${r1(y+(on?4.5:3.6))}" class="mk-t${on?' on':''}">${id}</text>`;
}
/* Orden de dibujo: lo que está más atrás (menor Y, luego menor X) va primero */
function antes(a,b){
  if(a.b[3]<=b.b[2]) return -1; if(b.b[3]<=a.b[2]) return 1;
  if(a.b[1]<=b.b[0]) return -1; if(b.b[1]<=a.b[0]) return 1;
  return (a.b[0]+a.b[2])-(b.b[0]+b.b[2]);
}

svg.setAttribute('viewBox','-300 -160 1090 648');
svg.innerHTML=`<g>${base()}</g><g id="isoMarks"></g><g id="isoObjs"></g><g id="isoOver"></g>`;
const g={};
['isoMarks','isoObjs','isoOver','isoEnt','isoSal'].forEach(id=>{g[id]=svg.querySelector('#'+id);});

let last=null, pos=null, raf=0, tipoAnt='';
function draw(){
  if(!last||!pos) return;
  const st=last.st, ctx=last.ctx||{}, f=FOCO[st.focus]||{}, y=CARRIL, c=ctx.c||{};
  const sobre=(a0,a1)=>a1>BAS.x&&a0<BAS.x+BAS.w;
  g.isoMarks.innerHTML=marcas(st);
  const it=[], add=(b,s)=>it.push({b,svg:s});
  add([56,140,20,60],kiosko(st,ctx));
  add([CAS.x-6,CAS.x+CAS.w+6,CAS.y,CAS.y+CAS.d+6],caseta());
  add([180,320,58,72],box(180,58,0,140,14,52,'am'));
  add([720,880,58,72],box(720,58,0,160,14,52,'am'));
  add([651,668,27,44],baliza(652,28,st.b1));
  add([681,698,27,44],baliza(682,28,st.b2));
  [[BAS.x-10,BAS.y-8],[BAS.x+BAS.w+10,BAS.y-8],[BAS.x-10,BAS.y+BAS.d+8],[BAS.x+BAS.w+10,BAS.y+BAS.d+8]].forEach(([x,yy])=>add([x-6,x+6,yy-6,yy+6],cono(x,yy)));
  if(st.persona==='origen') add([96,112,66,82],persona(104,74));
  /* vis: su etiqueta se ve desde arriba. En dos pallets montados solo se ve la de arriba; la de abajo aparece al sacar el de arriba. */
  const scanOn=st.focus==='camara';
  const pq=(x,yy,z,vis,scan)=>paquete(x,yy,z,{tipo:st.carga,etq:vis&&st.etiqueta,scan:vis&&scan,plumon:vis?st.plumon:''});
  const cargaSvg=(x,yy,z)=>st.apilado?pq(x,yy,z,false,false)+pq(x,yy,z+52,true,scanOn):pq(x,yy,z,true,scanOn&&!st.queda);
  if(st.veh==='grua'){
    const x=pos.vx;
    add([x,x+293,y,y+112],grua(x,y,pos.lift,sobre(x+118,x+158),st.enHorquilla?cargaSvg:null));
    if(!st.enHorquilla&&st.carga) add([pos.cx,pos.cx+110,y,y+110],cargaSvg(pos.cx,y,pos.cz));
    if(st.queda) add([QUEDA_X,QUEDA_X+110,y,y+110],pq(QUEDA_X,y,0,true,scanOn));   // el pallet de abajo sigue en la báscula
  }else if(st.veh==='tractor'){
    const x=pos.vx, cx=x-310, enB=sobre(cx,cx+300);
    add([x,x+140,y,y+112],tractor(x,y,sobre(x,x+50)));
    add([cx,cx+300,y,y+122],carro(cx,y,{num:c.carro,etq:st.etiqueta,scan:st.focus==='camara',malIzq:enB&&cx+30<BAS.x,malDer:enB&&cx+270>BAS.x+BAS.w}));
  }else if(st.carga){
    add([pos.cx,pos.cx+110,y,y+110],cargaSvg(pos.cx,y,pos.cz));
  }
  if(st.piso) add([BAS.x,BAS.x+BAS.w,BAS.y,BAS.y+BAS.d],nodulos());
  it.sort(antes);
  g.isoObjs.innerHTML=it.map(o=>o.svg).join('');
  g.isoEnt.setAttribute('class','ftxt'+(f.txt==='ent'?' on':''));
  g.isoSal.setAttribute('class','ftxt'+(f.txt==='sal'?' on':''));
  const b1=P(659.5,35.5,196), b2=P(689.5,35.5,196);
  let o=camara(st.cam&&!st.cam.ok)+`<text x="${b1[0]}" y="${b1[1]}" text-anchor="middle" class="lbl">B1</text><text x="${b2[0]}" y="${b2[1]}" text-anchor="middle" class="lbl">B2</text>`;
  Object.keys(MK).forEach(id=>{o+=marcador(id,id===f.mk);});
  g.isoOver.innerHTML=o;
}
function update(st,ctx){
  last={st:st,ctx:ctx};
  const t={vx:num(st.vx,-420),lift:num(st.lift,0),cx:num(st.cx,50),cz:num(st.cz,0)}, tipo=(st.veh||'')+'|'+(st.carga||'');
  cancelAnimationFrame(raf);
  if(!pos||reduce||tipo!==tipoAnt){pos=t;tipoAnt=tipo;draw();return;}
  const a=Object.assign({},pos), t0=performance.now(), ease=k=>k<.5?2*k*k:1-Math.pow(-2*k+2,2)/2;
  const step=now=>{const k=Math.min(1,(now-t0)/950), e=ease(k);
    pos={vx:a.vx+(t.vx-a.vx)*e,lift:a.lift+(t.lift-a.lift)*e,cx:a.cx+(t.cx-a.cx)*e,cz:a.cz+(t.cz-a.cz)*e};
    draw(); if(k<1) raf=requestAnimationFrame(step);};
  raf=requestAnimationFrame(step);
}
window.ISO={update:update};
})();
