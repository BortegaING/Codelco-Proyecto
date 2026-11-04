/* Motor de la simulación · Báscula N9, Codelco Ventanas (propuesta Ingmétrica).
   Toma los casos de casos.js, pinta el paso, las señales y la pantalla del pesador. La escena la dibuja iso.js. */
(function(){
'use strict';
const VT=window.VENT; if(!VT) return;
const SUB_MS=1700;
const $=s=>document.querySelector(s);
const reduce=!!(window.matchMedia&&window.matchMedia('(prefers-reduced-motion: reduce)').matches);
const f0=VT.f0, sig0=VT.sig0;
const LUGAR={origen:['A','Etiquetado en la nave'],camara:['B','Cámara cenital'],balizas:['C','Balizas B1 y B2'],bascula:['D','Báscula N9'],caseta:['E','Caseta del pesador'],salida:['F','Salida']};
const PST={cero:'CERO',espera:'EN ESPERA',inestable:'ESTABILIZANDO',estable:'ESTABLE',bloqueado:'BLOQUEADO'};
const B1T={off:'Apagada',verde:'Verde: peso dentro de lo esperado',rojo:'Roja: peso fuera de lo esperado','ambar-blink':'Ámbar: falta identificar o fuera de orden'};
const B2T={off:'Apagada: báscula libre',azul:'Azul: carga llegando','amarillo-blink':'Amarilla: estabilizando',verde:'Verde: peso capturado',
  rojo:'Roja: vehículo o carga mal ubicados','rojo-blink':'Roja intermitente: sin cero o algo roza'};

/* Cada paso hereda del anterior; título, aviso y botones no se heredan. "luego" es un segundo momento del mismo paso. */
const DEF={b1:'off',b2:'off',veh:'',vx:-420,lift:0,enHorquilla:false,carga:'',cx:310,cz:0,apilado:false,queda:false,persona:null,imprime:false,etiqueta:false,plumon:'',piso:false,cam:null,focus:'origen',quien:'Operador',
  scr:{sis:VT.PANT.cat,titulo:'',peso:0,pst:'cero',jit:0,campos:null,tara:null,neto:null,rango:null,banner:null,acc:[]}};
const merge=(st,s)=>Object.assign({},st,s,{scr:Object.assign({},st.scr,{banner:null,acc:[],titulo:''},s.scr||{})});
const patch=(st,s)=>Object.assign({},st,s,{scr:Object.assign({},st.scr,s.scr||{})});
function build(steps){
  const out=[]; let st=DEF;
  for(const s of steps){const a=merge(st,s); delete a.luego; delete a.fin;
    const fin=s.luego?patch(a,s.luego):null; if(fin) a.fin=fin; out.push(a); st=fin||a;}
  return out;
}
const SC=VT.CASOS.slice();
SC.forEach(s=>{s.states=build(s.steps);});
const cur={s:0,i:0};
let jitT=null, subT=null, playT=null;

function dot(el,state){const p=String(state||'off').split('-');el.className=p[0]+(p[1]?' blink':'');}
function pantalla(s){
  $('#scrSis').textContent=s.sis||VT.PANT.cat;
  $('#scrTitulo').textContent=s.titulo;
  const pst=$('#scrPst'); pst.textContent=PST[s.pst]; pst.className='chip '+s.pst;
  clearInterval(jitT); const pe=$('#scrPeso'); pe.textContent=f0(s.peso);
  if(s.jit>0){ if(reduce) pe.textContent='~'+f0(s.peso); else jitT=setInterval(()=>{pe.textContent=f0(s.peso+(Math.random()*2-1)*s.jit);},260); }
  $('#scrForm').innerHTML=s.campos?s.campos.map(o=>`<div class="fld s${o.s||1}${o.e?' err':''}${o.a?' auto':''}"><span>${o.l}</span><b>${o.v}</b></div>`).join('')
    :'<div class="fld-empty">Sin carga identificada</div>';
  const fuera=s.neto!=null&&s.rango&&(s.neto<s.rango[0]||s.neto>s.rango[1]);
  $('#scrCalc').innerHTML=`<div class="calc"><span>Peso tara</span><b>${s.tara!=null?f0(s.tara):'—'}</b></div>`+
    `<div class="calc"><span>Peso producción</span><b class="${s.neto==null?'':(fuera?'err':'ok')}">${s.neto!=null?f0(s.neto):'—'}</b></div>`+
    `<div class="calc"><span>Esperado</span><b>${s.rango?f0(s.rango[0])+'–'+f0(s.rango[1]):'—'}</b></div>`;
  const bn=$('#scrBanner'); bn.hidden=!s.banner; if(s.banner){bn.className='tb-banner '+s.banner.k;bn.textContent=s.banner.txt;}
  $('#scrAcc').innerHTML=(s.acc||[]).map(a=>`<span class="${a.on?'on':''}">${a.l}</span>`).join('');
}
function mostrar(st,sc){
  if(window.ISO) window.ISO.update(st,{c:sc.c,codigo:sc.codigo});
  const L=LUGAR[st.focus]||['',''];
  $('#lugar').innerHTML=`<i>${L[0]}</i><span>${L[1]}</span>`;
  document.querySelectorAll('#leyenda li').forEach(li=>li.classList.toggle('on',li.dataset.f===st.focus));
  dot($('#eB1'),st.b1); dot($('#eB2'),st.b2);
  $('#tB1').textContent=B1T[st.b1]||''; $('#tB2').textContent=B2T[st.b2]||'';
  $('#eCam').className=st.cam?(st.cam.ok?'cam-ok':'cam-err'):''; $('#tCam').textContent=st.cam?st.cam.txt:'—';
  pantalla(st.scr);
}
function render(){
  clearTimeout(subT);
  const sc=SC[cur.s], st=sc.states[cur.i], n=sc.states.length;
  mostrar(st,sc);
  if(st.fin) subT=setTimeout(()=>mostrar(st.fin,sc),SUB_MS);
  $('#pasoN').textContent=sc.libre?'Prueba libre':`${sc.nombre} · paso ${cur.i+1} de ${n}`;
  $('#pasoT').textContent=st.t; $('#pQuien').textContent=st.quien||'Operador'; $('#pOpe').textContent=st.ope; $('#pSis').textContent=st.sis;
  $('#paso').classList.toggle('libre',!!sc.libre); $('#prueba').hidden=!sc.libre; $('#pasos').hidden=!!sc.libre;
  $('#bAnt').disabled=cur.i===0; $('#bSig').disabled=cur.i===n-1;
  $('#pasos').innerHTML=sc.states.map((x,j)=>`<li class="${j<cur.i?'hecho':''}${j===cur.i?' on':''}"><button type="button" data-i="${j}"${j===cur.i?' aria-current="step"':''}><b>${j+1}</b>${x.t}</button></li>`).join('');
  document.querySelectorAll('.caso').forEach(b=>b.setAttribute('aria-pressed',+b.dataset.s===cur.s?'true':'false'));
}
function tabs(){
  const grupos=[];
  SC.forEach((s,j)=>{const g=s.grupo||'Prueba';let gr=grupos.find(x=>x.g===g);if(!gr){gr={g,items:[]};grupos.push(gr);}gr.items.push([s,j]);});
  $('#casos').innerHTML=grupos.map(gr=>`<div class="casos-g"><span class="casos-h">${gr.g}</span>`+
    gr.items.map(([s,j])=>`<button type="button" class="caso${s.libre?' libre':''}" data-s="${j}" aria-pressed="${j===cur.s}">${s.nombre}</button>`).join('')+`</div>`).join('');
}

/* ---------- Controles ---------- */
function stop(){clearInterval(playT);playT=null;$('#bPlay').textContent='Reproducir';}
const go=d=>{const n=SC[cur.s].states.length;cur.i=Math.max(0,Math.min(n-1,cur.i+d));render();};
function abrir(id,paso){
  const j=SC.findIndex(s=>s.id===id); if(j<0) return;
  stop(); cur.s=j; cur.i=Math.min(paso||0,SC[j].states.length-1); render();
  $('#simulacion').scrollIntoView({behavior:reduce?'auto':'smooth',block:'start'});
}
$('#casos').addEventListener('click',e=>{const b=e.target.closest('.caso');if(!b)return;stop();cur.s=+b.dataset.s;cur.i=0;render();});
$('#pasos').addEventListener('click',e=>{const b=e.target.closest('button');if(!b)return;stop();cur.i=+b.dataset.i;render();});
$('#bAnt').onclick=()=>{stop();go(-1);};
$('#bSig').onclick=()=>{stop();go(1);};
$('#bPlay').onclick=()=>{
  if(playT){stop();return;}
  if(cur.i===SC[cur.s].states.length-1){cur.i=0;render();}
  $('#bPlay').textContent='Pausar';
  playT=setInterval(()=>{if(cur.i>=SC[cur.s].states.length-1){stop();return;}go(1);},4300);
};
document.addEventListener('click',e=>{const b=e.target.closest('[data-caso]');if(!b)return;abrir(b.dataset.caso,+(b.dataset.paso||0));});
document.addEventListener('keydown',e=>{
  if(e.target.closest&&e.target.closest('input,textarea,select'))return;
  if(e.key==='ArrowRight'){stop();go(1);} else if(e.key==='ArrowLeft'){stop();go(-1);}
});

$('#leyenda').innerHTML=Object.keys(LUGAR).map(k=>`<li data-f="${k}"><b>${LUGAR[k][0]}</b>${LUGAR[k][1]}</li>`).join('');
tabs();
window.SIM={SC,cur,render,tabs,build,VENT:VT};
render();

/* ---------- Etiquetas (se imprimen al armar la carga, antes de la báscula) ---------- */
function qrSvg(txt){
  try{const q=qrcode(0,'M');q.addData(txt);q.make();return q.createSvgTag(4,0);}
  catch(e){return '<svg viewBox="0 0 10 10"><rect width="10" height="10" fill="none" stroke="#111" stroke-width=".5"/><text x="5" y="6" font-size="2.4" text-anchor="middle" fill="#111">QR</text></svg>';}
}
function etiqueta(el,d){
  if(!el) return;
  el.innerHTML=`<div class="lb-top"><span class="lb-tipo">${d.tipo}</span><span class="lb-id">${d.id}</span></div>
  <div class="lb-main"><div class="lb-qr">${qrSvg(d.qr)}</div>
  <div class="lb-loc">${d.cajas.map(([a,b])=>`<div><small>${a}</small><b>${b}</b></div>`).join('')}</div></div>
  <div class="lb-foot">${d.pie}</div>`;
}
/* Cada etiqueta repite el rótulo que hoy se escribe a mano: celda + lado + grupo, número de carro, o fecha + correlativo. */
etiqueta($('#lbl1'),{tipo:'CÁTODO',id:'5T21',qr:'VEN|CAT|C05|LT|G21|M-N',cajas:[['CELDA','5'],['LADO','T'],['GRUPO','21']],
  pie:'Cátodo comercial · Lado T: total · Muestra: N · Nave electrolítica<br>22-10-2026 10:02 · Etiquetó: operador 1043'});
etiqueta($('#lbl2'),{tipo:'RESTOS ÁNODOS',id:'G21 · C22',qr:'VEN|RAN|G21|CARRO22|N1',cajas:[['GRUPO','21'],['CARRO','22'],['Nº','1']],
  pie:'Tarjeta del carro · La tara de carro + rack la trae el sistema según el número del carro<br>22-10-2026 11:58 · Etiquetó: operador 1043'});
etiqueta($('#lbl3'),{tipo:'LÁMINAS',id:'21-10 · Nº 263',qr:'VEN|OTP|LAPNE|20261021|N263|T12',cajas:[['DÍA','21'],['MES','10'],['Nº','263']],
  pie:'Otros pesajes · Láminas aprobadas N.E. · Correlativo del mes · Tara del pallet: 12 kg<br>21-10-2026 08:15 · Etiquetó: operador 1043'});

/* ---------- Registro: detalle real del 22-10-2026, producto 18 cátodo comercial, grupo 21 (consulta "Detalle pesaje de producción") ---------- */
const DET=[['T','01',4796,'10:06:40'],['T','02',4855,'10:06:46'],['T','03',4722,'10:06:52'],['T','04',4687,'10:07:05'],['T','05',4860,'10:07:10'],
  ['T','06',4815,'10:07:17'],['T','07',4818,'10:07:24'],['T','08',4843,'10:07:31'],['T','09',4742,'10:07:37'],['T','10',4831,'10:07:42'],
  ['T','11',4869,'10:19:11'],['T','12',4724,'10:19:19'],['','00',854,'10:37:45','S'],['T','13',4610,'10:37:54'],['T','14',4725,'10:38:01'],
  ['T','15',4714,'10:38:06'],['T','16',4680,'10:38:18'],['T','17',4800,'10:38:24'],['T','18',4789,'10:38:30'],['T','19',4790,'10:45:47'],
  ['T','20',4644,'10:53:07'],['T','21',4775,'11:34:27'],['P','42',4310,'11:34:42'],['P','41',4646,'11:34:48'],['P','40',4714,'11:37:52'],
  ['P','39',4445,'11:38:03'],['P','38',4365,'11:39:54'],['P','37',4791,'11:41:38'],['P','36',4756,'11:43:05'],['P','35',4796,'11:43:14'],
  ['P','34',4641,'11:45:57'],['P','33',4612,'11:46:20'],['P','32',4651,'11:46:28'],['P','31',4640,'11:47:28'],['P','30',4626,'11:49:26'],['P','29',4316,'11:52:58']];
const tb=$('#tReg');
if(tb) tb.innerHTML=DET.map((r,i)=>`<tr${i<10?' class="tanda"':''}><td>21</td><td>${r[4]||'N'}</td><td>${r[0]}</td><td>${r[1]}</td><td class="num">${f0(r[2])}</td><td class="num">${r[3]}</td></tr>`).join('');
const suma=l=>DET.filter(r=>r[0]===l).reduce((a,r)=>a+r[2],0), nP=DET.filter(r=>r[0]==='P').length, nT=DET.filter(r=>r[0]==='T').length, mu=DET.filter(r=>r[4]==='S').reduce((a,r)=>a+r[2],0);
const tr=$('#tRes');
if(tr) tr.innerHTML=`<tr><td>22/10/2026</td><td>18</td><td>Cátodo comercial</td><td>21</td><td class="num">${f0(suma('P'))}</td><td class="num">${f0(suma('T'))}</td>`+
  `<td class="num">${f0(mu)}</td><td class="num">${f0(suma('P')+suma('T')+mu)}</td><td class="num">${nP}</td><td class="num">${nT}</td><td class="num">${nP+nT}</td></tr>`;
window.SIM.DET=DET;

/* ---------- Registro: restos de ánodos del mismo día (cuaderno y Excel del grupo 21, y "Ver datos" del Sistema de Ánodos) ---------- */
const RG=VT.RESTOS, totRG=RG.carros.reduce((a,x)=>a+x,0);
const tA=$('#tAno');
if(tA) tA.innerHTML=RG.carros.map((p,i)=>`<tr><td>${i+1}${i===RG.carros.length-1?' <small>parcial</small>':''}</td><td class="num">${f0(p)}</td></tr>`).join('')+
  `<tr class="tot"><td>Total grupo ${RG.grupo}</td><td class="num">${f0(totRG)}</td></tr>`;
const tH=$('#tHor');
if(tH) tH.innerHTML=`<tr><td>${RG.grupo}</td><td>${RG.hornada}</td><td class="num">${f0(RG.unidades)}</td><td class="num">${f0(RG.pesoSistema)}</td><td>${RG.lado}</td></tr>`;
const cA=$('#chkAno');
if(cA) cA.textContent=totRG===RG.pesoSistema?`Los ${RG.carros.length} carros suman ${f0(totRG)} kg: lo mismo que guardó el sistema.`:`Ojo: los carros suman ${f0(totRG)} kg y el sistema dice ${f0(RG.pesoSistema)} kg.`;
})();
