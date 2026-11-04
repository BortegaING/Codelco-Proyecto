/* Prueba libre: se mueven las variables y se ve cómo reacciona la Báscula N9.
   Usa window.SIM (sim.js) y los datos de casos.js. Valores de taras de carros: ejemplo. */
(function(){
'use strict';
const M=window.SIM;
const $=s=>document.querySelector(s);
const ctrl={prod:$('#pProd'),etq:$('#pEtq'),neto:$('#pNeto'),veh:$('#pVeh'),nod:$('#pNod'),orden:$('#pOrden'),carro:$('#pCarro'),tara:$('#pTara')};
if(!M||!ctrl.prod) return;
const VT=M.VENT, f0=VT.f0, sig0=VT.sig0, V=VT.V;
const TIPICO={catodos:4860,anodos:3413,laminas:3458};   // celda 5T21 del 22-10, carro 1 del grupo 21, lámina Nº 263 del 21-10
const TARA_REG={catodos:0,anodos:2150,laminas:12};
const VEHICULO_KG=1850, NODULOS_KG=6;

function leer(){
  return {prod:ctrl.prod.value,etq:ctrl.etq.value==='si',neto:+ctrl.neto.value,vehEncima:ctrl.veh.checked,nod:ctrl.nod.checked,
    fueraOrden:ctrl.orden.checked,carroReg:ctrl.carro.value==='si',dTara:+ctrl.tara.value};
}
/* Qué marcaría la báscula y qué decidiría el sistema */
function evaluar(v){
  const P=VT.PROD[v.prod], anod=v.prod==='anodos';
  const taraReg=TARA_REG[v.prod], taraReal=taraReg+(anod?v.dTara:0);
  const bruto=v.neto+taraReal+(v.vehEncima?VEHICULO_KG:0)+(v.nod?NODULOS_KG:0);
  const taraConocida=!anod||v.carroReg;
  const captura=!v.nod&&!v.vehEncima;
  const identificado=v.etq&&!v.fueraOrden&&taraConocida;
  const netoCalc=bruto-taraReg;
  const enRango=netoCalc>=P.rango[0]&&netoCalc<=P.rango[1];
  return {P,anod,taraReg,taraReal,bruto,taraConocida,captura,identificado,netoCalc,enRango,registraSolo:captura&&identificado&&enRango};
}
function estado(v,r){
  const c={grupo:'21',lado:'T',cuba:'05',muestra:'N',carro:'22',n:1,tara:r.taraReg,tipo:'Láminas aprobadas N.E.',rotulo:'21-10 · Nº 263'};
  const campos=v.prod==='catodos'?VT.camposCat(c,v.fueraOrden||!v.etq?'cuba':null):v.prod==='anodos'?VT.camposAno(c):VT.camposLam(c);
  const campo=l=>campos.find(o=>o.l===l)||{};
  if(!v.etq) Object.assign(campo(v.prod==='catodos'?'Cuba':v.prod==='anodos'?'Grupo':'Rótulo'),{v:'?',a:0});
  if(r.anod&&!v.carroReg) Object.assign(campo('Tara carro + rack'),{v:'sin registro',e:true,a:0});
  let b2='verde', pst='estable', jit=0, cam={txt:'Carga bien ubicada; nadie sobre la báscula',ok:true}, b1='off';
  if(v.nod){b2='rojo-blink';pst='bloqueado';cam={txt:'Nódulos de cobre en el borde de la plataforma',ok:false};}
  else if(v.vehEncima){b2='rojo';pst='bloqueado';cam={txt:r.anod?'El tractor está sobre la báscula':'Ruedas de la grúa sobre la báscula',ok:false};}
  if(r.captura) b1=r.identificado?(r.enRango?'verde':'rojo'):'ambar-blink';
  else if(!r.identificado) b1='ambar-blink';
  let x;
  if(v.nod) x={t:'No vuelve a cero',focus:'bascula',quien:'Pesador',ope:'Pide retirar los nódulos del borde de la plataforma.',
    sis:`Los nódulos suman ${NODULOS_KG} kg y la báscula vacía no marca cero. No deja pesar.`,banner:{k:'err',txt:'Báscula sin cero: retire los nódulos.'}};
  else if(v.vehEncima) x={t:r.anod?'Tractor sobre la báscula':'Grúa sobre la báscula',focus:'bascula',quien:r.anod?'Operador de tractor':'Operador de grúa',
    ope:r.anod?'Avanza el tractor hasta dejarlo fuera de la báscula.':'Retrocede hasta dejar la báscula libre.',
    sis:`La cámara ve el vehículo sobre la plataforma: sumaría unos ${f0(VEHICULO_KG)} kg. B2 en rojo y no pesa.`,banner:{k:'err',txt:'Vehículo sobre la báscula. Sáquelo para pesar.'}};
  else if(!v.etq) x={t:'Llega sin etiqueta',focus:'caseta',quien:'Pesador',ope:'Identifica la carga en su pantalla con un toque.',
    sis:'La cámara no encuentra etiqueta. Propone lo que sigue en el orden y espera la confirmación.',banner:{k:'warn',txt:'Sin etiqueta: confirme a mano.'}};
  else if(v.fueraOrden) x={t:'Fuera de orden',focus:'caseta',quien:'Pesador',ope:'Avisa a la nave o registra con nota.',
    sis:'La etiqueta no es la que sigue en el orden. B1 parpadea en ámbar.',banner:{k:'err',txt:'Fuera de orden: falta la carga anterior.'}};
  else if(!r.taraConocida) x={t:'Carro sin tara registrada',focus:'caseta',quien:'Pesador',ope:'Pide pesar el carro vacío, con su rack, para registrar la tara.',
    sis:'Leyó el número del carro, pero no tiene la tara de ese carro con su rack. No puede calcular el neto.',banner:{k:'warn',txt:`Carro 22 sin tara: pesarlo vacío, con su rack.`}};
  else if(!r.enRango) x={t:'Peso fuera de lo esperado',focus:'balizas',quien:'Pesador',ope:'Revisa la carga con el operador o registra con nota.',
    sis:`Neto ${f0(r.netoCalc)} kg; lo normal es ${f0(r.P.rango[0])} a ${f0(r.P.rango[1])} kg. B1 en rojo.`,banner:{k:'err',txt:`Neto ${f0(r.netoCalc)} kg: fuera de lo esperado.`}};
  else x={t:'Se registra solo',focus:'caseta',quien:'Pesador',ope:'No anota nada: revisa y sigue.',
    sis:`Bruto ${f0(r.bruto)} − tara ${f0(r.taraReg)} = neto ${f0(r.netoCalc)} kg. Queda en el sistema de Codelco.`,banner:{k:'ok',txt:`Registrado: neto ${f0(r.netoCalc)} kg.`}};
  const grua=!r.anod;
  const paso={t:x.t,focus:x.focus,quien:x.quien,ope:x.ope,sis:x.sis,veh:grua?'grua':'tractor',carga:v.prod,
    vx:grua?(v.vehEncima?200:V.atras):(v.vehEncima?600:640),lift:grua&&v.vehEncima?2:0,enHorquilla:grua&&v.vehEncima,cx:v.vehEncima&&grua?378:V.cxBas,cz:0,
    etiqueta:v.etq,plumon:v.etq?'':(v.prod==='catodos'?'5T21':v.prod==='laminas'?'263':''),piso:v.nod,persona:null,imprime:false,b1:b1,b2:b2,cam:cam,
    scr:{sis:r.anod?VT.PANT.carro:VT.PANT.cat,titulo:x.t,peso:r.bruto,pst:pst,jit:jit,campos:campos,tara:r.taraConocida?r.taraReg:null,neto:r.captura&&r.taraConocida?r.netoCalc:null,
      rango:r.P.rango,banner:x.banner,acc:[]}};
  return {paso:paso,c:{carro:'22'},codigo:grua?(v.prod==='catodos'?'5T21':'Nº 263'):'G21·C22'};
}
function resultados(v,r){
  const it=[[!v.nod,'Báscula vacía en cero'],[!v.vehEncima,'Vehículo fuera de la báscula'],[v.etq,'Etiqueta leída por la cámara'],
    [!v.fueraOrden,'Sigue el orden (la celda o el número que corresponde)'],[r.taraConocida,r.anod?'Tara de carro + rack registrada':'Tara conocida'],
    [r.captura&&r.taraConocida?r.enRango:null,`Neto dentro de ${f0(r.P.rango[0])} a ${f0(r.P.rango[1])} kg`]];
  let h=it.map(([ok,txt])=>`<li class="${ok===null?'na':ok?'ok':'no'}"><b>${ok===null?'—':ok?'OK':'Falla'}</b><span>${txt}</span></li>`).join('');
  h+=`<li class="fin"><b>${r.registraSolo?'Sí':'No'}</b><span>${r.registraSolo?'Se registra solo, sin cuaderno':'Necesita al pesador'}</span></li>`;
  if(r.anod&&v.dTara&&r.registraSolo) h+=`<li class="no"><b>Ojo</b><span>El carro con su rack pesa ${sig0(v.dTara)} kg distinto a la tara registrada: el neto queda corrido en esa cantidad y nadie lo nota. Conviene repesar carros y racks vacíos cada cierto tiempo.</span></li>`;
  $('#pRes').innerHTML=h;
}
function salidas(v){
  $('#oNeto').textContent=f0(v.neto)+' kg';
  $('#oTara').textContent=v.dTara===0?'igual':`${sig0(v.dTara)} kg`;
  document.querySelectorAll('[data-solo="anodos"]').forEach(el=>{el.hidden=v.prod!=='anodos';});
}

const sc={id:'libre',grupo:'Probar',nombre:'Prueba libre',libre:true,c:{carro:'22'},codigo:'',steps:[],states:[]};
M.SC.push(sc);
M.tabs();
let prodAnt=ctrl.prod.value;
function recalcular(){
  if(ctrl.prod.value!==prodAnt){prodAnt=ctrl.prod.value;ctrl.neto.value=TIPICO[prodAnt];}
  const v=leer(), r=evaluar(v), e=estado(v,r);
  sc.c=e.c; sc.codigo=e.codigo; sc.states=M.build([e.paso]);
  salidas(v); resultados(v,r);
  if(M.SC[M.cur.s]===sc){M.cur.i=0;M.render();}
}
Object.keys(ctrl).forEach(k=>{ctrl[k].addEventListener('input',recalcular);ctrl[k].addEventListener('change',recalcular);});
recalcular();
})();
