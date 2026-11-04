/* Casos de pesaje en la Báscula N9 · Codelco Ventanas (propuesta Ingmétrica).
   Pesos, grupos, celdas y lados vienen de la visita (planillas del 21-10 y consultas del 22-10-2026) y del correo de Codelco.
   Las taras de carro + rack son de ejemplo. */
(function(){
'use strict';
const f0=v=>Math.round(v).toLocaleString('es-CL');
const sig0=v=>(v>0.5?'+':v<-0.5?'−':'±')+f0(Math.abs(v));
const PROD={
  catodos:{nombre:'Cátodo comercial',rango:[4200,5000],tara:'Sin tara'},
  anodos:{nombre:'Restos de ánodos',rango:[2800,4000],tara:'Carro + rack'},
  laminas:{nombre:'Láminas aprobadas N.E.',rango:[3200,3900],tara:'12 kg por pallet'}
};
const V={armado:-128,deja:132,atras:0,salida:640,cxBas:310,lejos:-420};   // posiciones de la grúa y la carga (cm)
/* Pantallas que usa hoy el pesador (fotos de la visita): cada carga va a un sistema distinto. */
const PANT={
  cat:'Sistema de Cátodos · Ingreso pesaje producción',   // cátodos, despuntes y láminas, otros pesajes: carga por carga
  carro:'Recepción de restos de ánodos · por carro',      // hoy: cuaderno y Excel, un bloque por grupo
  ano:'Sistema de Ánodos · Producción restos ánodos Ctte'  // hoy: el total del grupo, que genera la hornada
};
/* Restos de ánodos del 22-10-2026, grupo 21: los 11 carros del cuaderno y del Excel, y lo que guardó el Sistema de Ánodos (Ver datos). */
const RESTOS={fecha:'22-10-2026',grupo:'21',lado:'M',unidades:819,hornada:'985812',pesoSistema:37961,
  carros:[3413,3878,3488,3718,3868,3464,3506,3274,3394,3912,2046]};
/* Rótulo de los cátodos, como se escribe hoy con plumón: celda + lado + grupo.
   12P31 = celda 12, lado P (parcial), grupo 31. Las celdas van de 1 a 21 y de 22 a 42. En el sistema la celda se llama "Cuba". */
const LADO={T:'total',P:'parcial'};
const rot=c=>`${+c.cuba}${c.lado}${c.grupo}`;
/* Campos de cada pantalla. l = etiqueta, v = valor, s = columnas que ocupa, a = lo llena la propuesta (etiqueta o báscula), e = con error */
const camposCat=(c,err)=>[{l:'Tipo movimiento',v:'Producción',s:2},{l:'Producto',v:'Cátodos',s:2},{l:'Sub-producto',v:'Cátodo comercial',s:4},
  {l:'Grupo',v:c.grupo,a:1},{l:'Cuba',v:c.cuba,a:1,e:err==='cuba'},{l:'Lado (P/T)',v:c.lado,a:1},{l:'Muestra (S/N)',v:c.muestra||'N',a:1}];
/* Por carro: lo que hoy va al cuaderno y al Excel (grupo y Nº del carro en el grupo), más el número pintado y la tara de carro + rack. */
const camposAno=c=>[{l:'Producto',v:'Restos ánodos',s:2},{l:'Grupo',v:c.grupo,a:1},{l:'Nº en el grupo',v:String(c.n),a:1},
  {l:'Carro (pintado)',v:c.carro,a:1,s:2},{l:'Tara carro + rack',v:f0(c.tara)+' kg',a:1,s:2}];
/* Al cerrar el grupo: los campos de "Producción restos ánodos Ctte" que se conocen. Lado y unidades los sigue poniendo el pesador. */
const camposAnoGrupo=g=>[{l:'Fecha producción',v:g.fecha,a:1,s:2},{l:'Grupo',v:'Nº '+g.grupo,a:1,s:2},
  {l:'Lado',v:g.lado,s:2},{l:'Total unidades Ctes',v:f0(g.unidades),s:2}];
/* Láminas: el rótulo es fecha + correlativo del mes, como en la hoja de láminas despegue. */
const camposLam=c=>[{l:'Tipo movimiento',v:'Producción',s:2},{l:'Producto',v:'Otros pesajes',s:2},{l:'Sub-producto',v:c.tipo,s:4,a:1},
  {l:'Rótulo',v:c.rotulo,a:1,s:2},{l:'Peso tara',v:'12',a:1,s:2}];
/* Dos pallets montados: la cámara lee primero la etiqueta de arriba; la de abajo aparece al sacar el de arriba. */
const camposDos=(d,abajoVisible)=>[{l:'Sub-producto',v:d.tipo,s:4,a:1},{l:'Arriba',v:`${d.fecha} · Nº ${d.arriba.n}`,a:1,s:2},
  {l:'Abajo',v:abajoVisible?`${d.fecha} · Nº ${d.abajo.n}`:'Tapada',a:abajoVisible?1:0,s:2}];
const camposDosCalc=(d,W1,W2)=>[{l:'Sub-producto',v:d.tipo,s:4,a:1},
  {l:`Arriba · Nº ${d.arriba.n}`,v:`${f0(W1)} − ${f0(W2)} − 12 = ${f0(W1-W2-12)}`,a:1,s:4},
  {l:`Abajo · Nº ${d.abajo.n}`,v:`${f0(W2)} − 12 = ${f0(W2-12)}`,a:1,s:4}];
const libre={titulo:'Báscula libre',peso:0,pst:'cero',jit:0,campos:null,tara:null,neto:null,rango:null};
const capturado=(bruto,tara,rango,txt)=>({titulo:'Peso capturado',peso:bruto,pst:'estable',jit:0,tara:tara,neto:bruto-tara,rango:rango,banner:{k:'ok',txt:txt}});
const R=PROD.catodos.rango;

/* ---------- Cátodos: flujo completo ---------- */
function catodos(c){
  const b=c.peso, r=rot(c);
  return [
    {t:'Etiqueta al armar el paquete',focus:'origen',quien:'Operador de la nave',veh:'grua',vx:V.armado,lift:0,enHorquilla:true,carga:'catodos',cx:V.cxBas,cz:0,
     persona:'origen',imprime:true,etiqueta:true,plumon:'',piso:false,b1:'off',b2:'off',cam:{txt:'Báscula libre y en cero',ok:true},
     ope:`Elige celda ${+c.cuba}, lado ${c.lado} y grupo ${c.grupo} en la pantalla, imprime la etiqueta y la pega arriba.`,
     sis:`Imprime ${r} con un código QR: el mismo rótulo que hoy se escribe con plumón.`,
     scr:Object.assign({},libre,{banner:{k:'info',txt:`Siguiente en orden: ${r} (celda ${+c.cuba}, lado ${LADO[c.lado]}, grupo ${c.grupo}).`}})},
    {t:'La grúa lo lleva a la báscula',focus:'bascula',quien:'Operador de grúa',vx:V.deja,lift:25,persona:null,imprime:false,b2:'azul',
     cam:{txt:'Grúa llegando con un paquete',ok:true},
     ope:'Deja el paquete cerca del borde de entrada, sin subir a la báscula.',
     sis:'Ve llegar la carga y enciende B2 en azul.',
     scr:{titulo:'Carga llegando',banner:{k:'info',txt:'Carga en camino.'}}},
    {t:'Lo deja y retrocede',focus:'camara',quien:'Operador de grúa',lift:2,
     ope:'Baja el paquete, saca la horquilla y retrocede.',
     sis:'Con la grúa fuera, la cámara lee la etiqueta desde arriba.',
     scr:{titulo:'Leyendo etiqueta',peso:b+9,pst:'espera'},
     luego:{enHorquilla:false,vx:V.atras,b2:'amarillo-blink',cam:{txt:`Etiqueta leída: ${r}`,ok:true},
       scr:{titulo:'Datos leídos',peso:b-3,pst:'inestable',jit:3,campos:camposCat(c),banner:{k:'ok',txt:`En orden: la celda ${+c.cuba} sigue a la ${+c.prev}.`}}}},
    {t:'Se pesa solo',focus:'balizas',quien:'Operador de grúa',
     ope:'Espera fuera de la báscula y mira las balizas.',
     sis:'Toma el peso estable del terminal y lo compara con lo esperado para un paquete de cátodos.',
     luego:{b1:'verde',b2:'verde',cam:{txt:'Peso estable, nadie sobre la báscula',ok:true},scr:capturado(b,0,R,`${f0(b)} kg: dentro de lo esperado (4.200 a 5.000 kg).`)}},
    {t:'Queda registrado',focus:'caseta',quien:'Pesador',
     ope:'Revisa el ingreso y toca Grabar. No anota en cuaderno ni usa calculadora.',
     sis:'Deja listo el ingreso en el Sistema de Cátodos: grupo, cuba (la celda), lado, muestra y peso, con la hora real.',
     scr:{titulo:'Listo para grabar',banner:{k:'ok',txt:`${r} · ${f0(b)} kg, listo para el Sistema de Cátodos.`},acc:[{l:'Grabar',on:true},{l:'Ver datos'},{l:'Limpiar'}]}},
    {t:'Sale y vuelve a cero',focus:'salida',quien:'Operador de grúa',vx:V.deja,
     ope:'Toma el paquete y lo lleva a la salida.',
     sis:'Revisa que la báscula vuelva a cero. Si quedan nódulos, avisa.',
     scr:{titulo:'Retirando',banner:{k:'info',txt:'Retirando la carga.'}},
     luego:{enHorquilla:true,vx:V.salida,lift:10,b1:'off',b2:'off',cam:{txt:'Báscula libre y en cero',ok:true},
       scr:Object.assign({},libre,{banner:{k:'info',txt:`Siguiente: ${rot(Object.assign({},c,{cuba:c.next}))}.`}})}}
  ];
}

/* ---------- Restos de ánodos: carro a carro y, al final, el total del grupo en el Sistema de Ánodos ----------
   Hoy: bruto en el terminal, tara de carro + rack de la lista impresa, neto con calculadora, cuaderno, Excel y el total a mano en el sistema. */
function anodos(c){
  const b=c.neto+c.tara, g=RESTOS, n=g.carros.length, tot=g.carros.reduce((a,x)=>a+x,0);
  return [
    {t:'Llega el carro',focus:'origen',quien:'Operador de tractor',veh:'tractor',vx:330,carga:'anodos',etiqueta:true,persona:null,piso:false,b1:'off',b2:'off',
     cam:{txt:'Báscula libre y en cero',ok:true},
     ope:'Arrastra el carro de restos de ánodos hacia la báscula.',
     sis:`El carro llega con su rack, la tarjeta del grupo ${c.grupo} y el número ${c.carro} pintado.`,
     scr:Object.assign({},libre,{sis:PANT.carro,banner:{k:'info',txt:`Siguiente: grupo ${c.grupo}, carro ${c.n}.`}})},
    {t:'Deja el carro en el área',focus:'camara',quien:'Operador de tractor',vx:640,b2:'amarillo-blink',
     cam:{txt:`Carro ${c.carro} leído, dentro del área; tractor afuera`,ok:true},
     ope:'Deja el carro dentro del área marcada en la báscula, con el tractor afuera.',
     sis:`La cámara revisa que el carro quede dentro del área y lee el número ${c.carro}: trae la tara de carro + rack, ${f0(c.tara)} kg.`,
     scr:{titulo:'Datos leídos',peso:b-6,pst:'inestable',jit:4,campos:camposAno(c),tara:c.tara,banner:{k:'ok',txt:`Carro ${c.carro}: tara de carro + rack ${f0(c.tara)} kg, del registro.`}}},
    {t:'Se pesa y descuenta la tara',focus:'balizas',quien:'Pesador',
     ope:'Ya no busca la tara en la lista ni resta con calculadora.',
     sis:`Bruto ${f0(b)} menos la tara de carro + rack, ${f0(c.tara)}: neto ${f0(c.neto)} kg.`,
     luego:{b1:'verde',b2:'verde',scr:capturado(b,c.tara,PROD.anodos.rango,`Neto ${f0(c.neto)} kg: dentro de lo esperado.`)}},
    {t:'Queda en la lista del grupo',focus:'salida',quien:'Operador de tractor',
     ope:'Saca el carro hacia la salida. El pesador no anota nada.',
     sis:`Suma el carro ${c.n} a la lista del grupo ${c.grupo}: lo que hoy se escribe en el cuaderno y se pasa al Excel.`,
     scr:{titulo:'En la lista',banner:{k:'ok',txt:`Grupo ${c.grupo} · carro ${c.n} (Nº ${c.carro}) · ${f0(c.neto)} kg.`}},
     luego:{vx:1080,b1:'off',b2:'off',cam:{txt:'Báscula libre y en cero',ok:true},scr:Object.assign({},libre,{banner:{k:'info',txt:`Siguiente: grupo ${c.grupo}, carro ${c.n+1}.`}})}},
    {t:'Cierra el grupo',focus:'caseta',quien:'Pesador',cam:{txt:'Báscula libre y en cero',ok:true},
     ope:`Pasado el último carro (el ${n}, parcial: ${f0(g.carros[n-1])} kg), revisa la pantalla de producción y toca Generar hornada.`,
     sis:`Suma los ${n} carros del grupo ${g.grupo}: ${f0(tot)} kg, lo mismo que el Excel del ${g.fecha}. Deja lista la pantalla del Sistema de Ánodos.`,
     scr:{sis:PANT.ano,titulo:'Grupo completo',peso:0,pst:'cero',jit:0,campos:camposAnoGrupo(g),tara:null,neto:tot,rango:null,
       banner:{k:'ok',txt:`Grupo ${g.grupo}: ${n} carros, ${f0(tot)} kg.`},acc:[{l:'Generar hornada',on:true},{l:'Ver datos'},{l:'Limpiar'}]},
     luego:{scr:{titulo:'Hornada generada',acc:[{l:'Ver datos',on:true}],
       banner:{k:'ok',txt:`Hornada ${g.hornada} · grupo ${g.grupo} · ${f0(g.unidades)} unidades · ${f0(tot)} kg, como aparece en Ver datos.`}}}}
  ];
}

/* ---------- Láminas: un pallet, tara de 12 kg y correlativo del mes ---------- */
function laminas(c){
  const b=c.neto+12;
  return [
    {t:'Llega el pallet etiquetado',focus:'bascula',quien:'Operador de grúa',veh:'grua',vx:V.deja,lift:25,enHorquilla:true,carga:'laminas',cx:V.cxBas,cz:0,
     etiqueta:true,persona:null,piso:false,b1:'off',b2:'azul',cam:{txt:'Grúa llegando con un pallet',ok:true},
     ope:'Trae el pallet de láminas y lo baja en la báscula.',
     sis:`La etiqueta dice: ${c.tipo}, ${c.fecha} Nº ${c.n}.`,
     scr:Object.assign({},libre,{titulo:'Carga llegando',banner:{k:'info',txt:`Siguiente: Nº ${c.n} del ${c.fecha}.`}}),
     luego:{lift:2,scr:{titulo:'Leyendo etiqueta',peso:b+9,pst:'espera'}}},
    {t:'Lee, pesa y descuenta 12 kg',focus:'camara',quien:'Operador de grúa',enHorquilla:false,vx:V.atras,b2:'amarillo-blink',
     cam:{txt:`Etiqueta leída: ${c.fecha} · Nº ${c.n}`,ok:true},
     ope:'Retrocede y espera fuera de la báscula.',
     sis:`Descuenta 12 kg del pallet y revisa que el ${c.n} siga al ${c.n-1}.`,
     scr:{titulo:'Datos leídos',peso:b-4,pst:'inestable',jit:3,campos:camposLam(c),tara:12,banner:{k:'ok',txt:`En orden: Nº ${c.n} después del ${c.n-1}.`}},
     luego:{b1:'verde',b2:'verde',scr:capturado(b,12,PROD.laminas.rango,`Neto ${f0(c.neto)} kg (bruto ${f0(b)} menos 12).`)}},
    {t:'Registro y salida',focus:'salida',quien:'Operador de grúa',vx:V.deja,
     ope:'Retira el pallet hacia la salida.',
     sis:`Registra el Nº ${c.n} con bruto, tara 12 y neto: la fila que hoy se escribe en la hoja de láminas. Sigue el ${c.n+1}.`,
     scr:{titulo:'Registrado',banner:{k:'ok',txt:`Registrado: Nº ${c.n} · neto ${f0(c.neto)} kg.`}},
     luego:{enHorquilla:true,vx:V.salida,lift:10,b1:'off',b2:'off',cam:{txt:'Báscula libre y en cero',ok:true},scr:Object.assign({},libre,{banner:{k:'info',txt:`Siguiente: Nº ${c.n+1}.`}})}}
  ];
}

/* ---------- Láminas: dos pallets montados, pesaje por diferencia ----------
   Hoy: se pesan los dos, se saca el de arriba, se pesa el de abajo solo y la diferencia se calcula a mano. */
function dosPallets(d){
  const W1=d.arriba.bruto+d.abajo.bruto, W2=d.abajo.bruto, nA=W1-W2-12, nB=W2-12;
  return [
    {t:'Llegan dos pallets montados',focus:'bascula',quien:'Operador de grúa',veh:'grua',vx:V.deja,lift:25,enHorquilla:true,carga:'laminas',apilado:true,queda:false,cx:V.cxBas,cz:0,
     etiqueta:true,persona:null,piso:false,b1:'off',b2:'azul',cam:{txt:'Grúa llegando con dos pallets, uno sobre otro',ok:true},
     ope:'Trae dos pallets de láminas montados y los baja juntos en la báscula.',
     sis:`Espera el Nº ${d.arriba.n}, que sigue en el correlativo del mes.`,
     scr:Object.assign({},libre,{titulo:'Carga llegando',banner:{k:'info',txt:`Siguiente: Nº ${d.arriba.n} del ${d.fecha}.`}}),
     luego:{lift:2,scr:{titulo:'Leyendo etiqueta',peso:W1+9,pst:'espera'}}},
    {t:'Pesa los dos juntos',focus:'camara',quien:'Operador de grúa',enHorquilla:false,vx:V.atras,b2:'amarillo-blink',
     cam:{txt:`Etiqueta de arriba leída: Nº ${d.arriba.n}. La de abajo queda tapada`,ok:true},
     ope:'Retrocede y espera fuera de la báscula.',
     sis:`Pesa ${f0(W1)} kg, el doble de un pallet: guarda esta lectura y espera que saquen el de arriba.`,
     scr:{titulo:'Lectura 1 de 2',peso:W1-5,pst:'inestable',jit:3,campos:camposDos(d,false),tara:null,neto:null,rango:null,
       banner:{k:'info',txt:'Dos pallets montados: se pesa por diferencia.'}},
     luego:{b2:'verde',scr:{titulo:'Lectura 1 guardada',peso:W1,pst:'estable',jit:0,banner:{k:'ok',txt:`Lectura 1: ${f0(W1)} kg. Saque el pallet de arriba.`}}}},
    {t:'Saca el pallet de arriba',focus:'bascula',quien:'Operador de grúa',vx:V.deja,lift:48,
     cam:{txt:'Grúa tomando el pallet de arriba',ok:true},
     ope:'Toma solo el pallet de arriba y retrocede con él. El de abajo queda en la báscula.',
     sis:'El peso baja, pero no llega a cero porque sigue un pallet encima. Así sabe que viene la segunda lectura.',
     scr:{titulo:'Sacando el de arriba',banner:{k:'info',txt:'Saque solo el pallet de arriba.'}},
     luego:{enHorquilla:true,apilado:false,queda:true,vx:V.atras,lift:56,b2:'amarillo-blink',cam:{txt:`Queda un pallet: etiqueta Nº ${d.abajo.n} a la vista`,ok:true},
       scr:{titulo:'Leyendo etiqueta',peso:W2+6,pst:'inestable',jit:3,campos:camposDos(d,true),banner:{k:'info',txt:'Lectura 2: el pallet de abajo.'}}}},
    {t:'Calcula los dos netos',focus:'balizas',quien:'Pesador',b1:'verde',b2:'verde',
     cam:{txt:`Etiqueta de abajo leída: Nº ${d.abajo.n}`,ok:true},
     ope:'Ya no resta a mano: revisa los dos netos en su pantalla.',
     sis:`Lectura 2: ${f0(W2)} kg. Abajo: ${f0(W2)} − 12 = ${f0(nB)} kg. Arriba: ${f0(W1)} − ${f0(W2)} − 12 = ${f0(nA)} kg.`,
     scr:{titulo:'Lectura 2 guardada',peso:W2,pst:'estable',jit:0,campos:camposDosCalc(d,W1,W2),tara:24,neto:nA+nB,rango:null,
       banner:{k:'ok',txt:`Nº ${d.arriba.n}: ${f0(nA)} kg · Nº ${d.abajo.n}: ${f0(nB)} kg. Los dos dentro de lo esperado.`}}},
    {t:'Graba los dos',focus:'caseta',quien:'Pesador',
     ope:'Toca Grabar una vez y quedan los dos pallets.',
     sis:`Deja dos ingresos en el Sistema de Cátodos, en Otros pesajes con tara 12: Nº ${d.arriba.n} con ${f0(nA)} kg y Nº ${d.abajo.n} con ${f0(nB)} kg. Guarda también las dos lecturas.`,
     scr:{titulo:'Listo para grabar',banner:{k:'ok',txt:`Dos ingresos: ${f0(nA)} + ${f0(nB)} = ${f0(nA+nB)} kg.`},acc:[{l:'Grabar',on:true},{l:'Ver datos'},{l:'Limpiar'}]},
     luego:{veh:'',carga:'',apilado:false,queda:false,enHorquilla:false,b1:'off',b2:'off',cam:{txt:'Báscula libre y en cero',ok:true},
       scr:Object.assign({},libre,{banner:{k:'info',txt:`La grúa retiró los dos pallets. Siguiente: Nº ${d.abajo.n+1}.`}})}}
  ];
}

/* ---------- Qué pasa si... (sobre cátodos) ---------- */
const enBascula={veh:'grua',vx:V.atras,lift:0,enHorquilla:false,carga:'catodos',cx:V.cxBas,cz:0,persona:null,imprime:false,piso:false,plumon:'',apilado:false,queda:false};
const pesaNormal=(c,txt)=>({t:'Pesa normal',focus:'balizas',quien:'Operador de grúa',persona:null,
  ope:'Espera fuera de la báscula y mira las balizas.',sis:'Peso estable y dentro de lo esperado: B1 y B2 en verde.',
  b1:'verde',b2:'verde',cam:{txt:'Peso estable, nadie sobre la báscula',ok:true},
  scr:Object.assign(capturado(c.peso,0,R,txt||`${f0(c.peso)} kg: dentro de lo esperado.`),{campos:camposCat(c)})});

function sinEtiqueta(c){
  const r=rot(c);
  return [
    Object.assign({},enBascula,{t:'Llega sin etiqueta',focus:'camara',quien:'Operador de grúa',etiqueta:false,plumon:r,b1:'ambar-blink',b2:'amarillo-blink',
      cam:{txt:`No hay etiqueta, solo la marca de plumón "${r}"`,ok:false},
      ope:'Deja el paquete como siempre.',
      sis:'La cámara no lee marcas escritas a mano: B1 parpadea en ámbar y avisa al pesador.',
      scr:{titulo:'Sin etiqueta',peso:c.peso-3,pst:'inestable',jit:3,campos:camposCat(Object.assign({},c,{cuba:'?'}),'cuba'),tara:null,neto:null,rango:null,
        banner:{k:'warn',txt:`Sin etiqueta. En el orden sigue ${r}.`},acc:[{l:`Usar ${r}`,on:true},{l:'Elegir otra'}]}}),
    {t:'El pesador lo identifica',focus:'caseta',quien:'Pesador',b1:'off',
     ope:`Mira la marca, confirma que dice ${r} y toca "Usar ${r}".`,
     sis:'Anota que este paquete se identificó a mano, para el reporte. Así se identificaría también el EW mientras siga llegando sin rótulo.',
     scr:{titulo:'Identificado a mano',campos:camposCat(c),banner:{k:'ok',txt:`Identificado a mano: ${r}.`}}},
    pesaNormal(c,`${f0(c.peso)} kg registrado con la nota "identificado a mano".`)
  ];
}
function gruaEncima(c){
  return [
    Object.assign({},enBascula,{t:'La grúa queda encima',focus:'bascula',quien:'Operador de grúa',vx:200,lift:2,enHorquilla:true,cx:378,etiqueta:true,b1:'off',b2:'rojo',
      cam:{txt:'Ruedas delanteras de la grúa sobre la báscula',ok:false},
      ope:'Baja el paquete pero se queda con las ruedas sobre la báscula.',
      sis:'La cámara ve la grúa sobre la plataforma: el peso incluye parte de la grúa. B2 en rojo y no pesa.',
      scr:{titulo:'Captura bloqueada',peso:c.peso+1850,pst:'bloqueado',jit:0,campos:camposCat(c),tara:null,neto:null,rango:null,banner:{k:'err',txt:'Grúa sobre la báscula. Retroceda para pesar.'}}}),
    {t:'Retrocede',focus:'bascula',enHorquilla:false,cx:378,vx:V.atras,b2:'amarillo-blink',cam:{txt:'Báscula despejada: solo el paquete',ok:true},
     ope:'Saca la horquilla y retrocede hasta salir de la báscula.',
     sis:'Con la grúa fuera, B2 pasa a amarillo y espera peso estable.',
     scr:{titulo:'Estabilizando',peso:c.peso-3,pst:'inestable',jit:3,banner:{k:'warn',txt:'Estabilizando. No toque la carga.'}}},
    pesaNormal(c)
  ];
}
/* El carro de restos se pasa de la báscula: la plataforma es corta y parte del peso queda en el piso. */
function carroFuera(c){
  const b=c.neto+c.tara;
  return [
    {t:'El carro se pasa',focus:'bascula',quien:'Operador de tractor',veh:'tractor',vx:700,carga:'anodos',etiqueta:true,persona:null,piso:false,b1:'off',b2:'rojo',
     cam:{txt:'Ruedas delanteras del carro fuera del área de pesaje',ok:false},
     ope:'Avanza de más y las ruedas delanteras del carro quedan fuera de la plataforma.',
     sis:'Parte del peso se apoya fuera de la báscula, así que marca de menos. La cámara ve el carro fuera del área: B2 en rojo y no captura.',
     scr:{sis:PANT.carro,titulo:'Captura bloqueada',peso:b-960,pst:'bloqueado',jit:0,campos:camposAno(c),tara:c.tara,neto:null,rango:null,
       banner:{k:'err',txt:'Carro fuera del área. Retroceda hasta la marca.'}}},
    {t:'Retrocede hasta el área',focus:'camara',quien:'Operador de tractor',vx:640,b2:'amarillo-blink',
     cam:{txt:`Carro ${c.carro} dentro del área; tractor afuera`,ok:true},
     ope:'Retrocede despacio hasta que el carro queda dentro de la marca.',
     sis:'Con el carro completo sobre la plataforma, B2 pasa a amarillo y espera peso estable.',
     scr:{titulo:'Estabilizando',peso:b-4,pst:'inestable',jit:4,banner:{k:'warn',txt:'Estabilizando. No mueva el carro.'}},
     luego:{b1:'verde',b2:'verde',scr:capturado(b,c.tara,PROD.anodos.rango,`Neto ${f0(c.neto)} kg: dentro de lo esperado.`)}},
    {t:'Queda en la lista del grupo',focus:'caseta',quien:'Pesador',
     ope:'No anota nada.',
     sis:`Suma el carro ${c.n} a la lista del grupo ${c.grupo}. El intento con el carro fuera del área queda en el registro de avisos.`,
     scr:{titulo:'En la lista',banner:{k:'ok',txt:`Grupo ${c.grupo} · carro ${c.n} (Nº ${c.carro}) · ${f0(c.neto)} kg.`}}}
  ];
}
function nodulosCaso(c){
  return [
    Object.assign({},enBascula,{t:'No vuelve a cero',focus:'bascula',quien:'Pesador',vx:V.lejos,enHorquilla:true,etiqueta:true,piso:true,b1:'off',b2:'rojo-blink',
      cam:{txt:'Nódulos de cobre en el borde de la plataforma',ok:false},
      ope:'Ve B2 en rojo intermitente y el aviso en su pantalla.',
      sis:'Vacía, la báscula marca 6 kg y la cámara ve nódulos en el borde. No deja pesar.',
      scr:{titulo:'Sin cero',peso:6,pst:'bloqueado',jit:0,campos:null,tara:null,neto:null,rango:null,banner:{k:'err',txt:'La báscula vacía marca 6 kg. Retire los nódulos.'}}}),
    {t:'Retiran los nódulos',focus:'bascula',quien:'Operador de grúa',piso:false,persona:[300,264],b2:'off',cam:{txt:'Plataforma limpia y en cero',ok:true},
     ope:'Barre los nódulos del borde de la plataforma.',
     sis:'La báscula vuelve a cero y B2 se apaga.',
     scr:Object.assign({},libre,{banner:{k:'ok',txt:'Báscula en cero. Puede pasar la carga.'}})},
    Object.assign(pesaNormal(c),{vx:V.atras,enHorquilla:false,cx:V.cxBas})
  ];
}
function fueraDeOrden(c){
  const c7=Object.assign({},c,{cuba:'07'}), r6=rot(c), r7=rot(c7);
  return [
    Object.assign({},enBascula,{t:'Llega otra celda',focus:'camara',quien:'Operador de grúa',etiqueta:true,b1:'ambar-blink',b2:'amarillo-blink',
      cam:{txt:`Etiqueta leída: ${r7}`,ok:true},
      ope:'Deja el paquete y retrocede.',
      sis:`La etiqueta dice ${r7}, pero falta ${r6}: B1 parpadea en ámbar.`,
      scr:{titulo:'Fuera de orden',peso:c.peso-3,pst:'inestable',jit:3,campos:camposCat(c7,'cuba'),tara:null,neto:null,rango:null,
        banner:{k:'err',txt:`Falta ${r6}. Este es ${r7}.`},acc:[{l:'Avisar a la nave',on:true},{l:'Pesar con nota'}]}}),
    {t:'Avisa y decide',focus:'caseta',quien:'Pesador',
     ope:`Avisa a la nave por radio. Si ${r6} viene atrás, pesa este con nota.`,
     sis:`Deja ${r6} pendiente y marca ${r7} como fuera de orden.`,
     scr:{titulo:`${r6} pendiente`,banner:{k:'warn',txt:`${r6} pendiente. ${r7} queda con nota.`},acc:[{l:'Pesar con nota',on:true}]}},
    pesaNormal(c7,`${r7} registrado con nota. Pendiente: ${r6}.`)
  ];
}
function fueraDeRango(c){
  const m=3910;
  return [
    Object.assign({},enBascula,{t:'El peso no calza',focus:'balizas',quien:'Operador de grúa',etiqueta:true,b1:'rojo',b2:'verde',
      cam:{txt:`Etiqueta leída: ${rot(c)}`,ok:true},
      ope:'Ve la baliza B1 en rojo.',
      sis:'Pesa 3.910 kg; un paquete de cátodos pesa entre 4.200 y 5.000. Puede venir incompleto.',
      scr:{titulo:'Fuera de rango',peso:m,pst:'estable',jit:0,campos:camposCat(c),tara:0,neto:m,rango:R,
        banner:{k:'err',txt:'3.910 kg: bajo lo esperado (4.200 a 5.000 kg).'},acc:[{l:'Revisar paquete',on:true},{l:'Registrar con nota'}]}}),
    {t:'Revisan el paquete',focus:'bascula',quien:'Operador de grúa',persona:[290,266],
     ope:'Revisa el paquete con el pesador: faltan cátodos.',
     sis:'Deja el pesaje en espera hasta que se decida.',
     scr:{titulo:'En revisión',banner:{k:'warn',txt:'Pesaje en espera de revisión.'}}},
    {t:'Registra con nota',focus:'caseta',quien:'Pesador',persona:null,b1:'off',
     ope:'Confirma que venía incompleto y toca "Registrar con nota".',
     sis:'Guarda el peso real con la nota y quién la autorizó.',
     scr:{titulo:'Registrado con nota',banner:{k:'ok',txt:'Registrado 3.910 kg con la nota "paquete incompleto".'}}}
  ];
}
/* Lado P = lado parcial: no entran las 21 celdas del lado. El 22-10 el grupo 21 tuvo el lado T con 21 paquetes y el lado P con 14 (de la 42 a la 29). */
function ladoParcial(c){
  const r=rot(c);
  return [
    Object.assign({},enBascula,{t:'Llega un lado parcial',focus:'camara',quien:'Operador de grúa',etiqueta:true,b1:'verde',b2:'verde',
      cam:{txt:`Etiqueta leída: ${r}`,ok:true},
      ope:'Deja el paquete y retrocede.',
      sis:`La P dice que este lado es parcial: no se esperan las 21 celdas. Va desde la 42 hacia abajo y la ${+c.cuba} es el paquete 14.`,
      scr:Object.assign(capturado(c.peso,0,R,`${r} registrado: paquete 14 del lado P.`),{campos:camposCat(c)})}),
    {t:'Finaliza el grupo',focus:'caseta',quien:'Pesador',
     ope:'Con el último paquete del lado, toca "Finaliza grupo", como la tecla F de hoy.',
     sis:'Cierra el grupo 21: lado T con 21 paquetes y lado P con 14. Son las columnas Col-total y Col-parc del resumen.',
     scr:{titulo:'Grupo 21 cerrado',banner:{k:'ok',txt:'Lado T: 21 paquetes, 100.089 kg. Lado P: 14 paquetes, 64.309 kg.'},acc:[{l:'Finaliza grupo',on:true}]}},
    {t:'Sale y queda el resumen',focus:'salida',quien:'Operador de grúa',vx:V.deja,
     ope:'Retira el paquete por la salida.',
     sis:'La báscula vuelve a cero. El resumen del día cuadra con la consulta del Sistema de Cátodos.',
     scr:{titulo:'Retirando',banner:{k:'info',txt:'Retirando la carga.'}},
     luego:{enHorquilla:true,vx:V.salida,lift:10,b1:'off',b2:'off',cam:{txt:'Báscula libre y en cero',ok:true},
       scr:Object.assign({},libre,{titulo:'Resumen del día',banner:{k:'ok',txt:'22-10, grupo 21: lado T 100.089 kg, lado P 64.309 kg y muestra 854 kg. Total 165.252 kg en 35 paquetes.'}})}}
  ];
}

/* ---------- Lista de casos ---------- */
const ct=(o)=>Object.assign({prod:'catodos',grupo:'21',lado:'T',cuba:'05',prev:'04',next:'06',muestra:'N',peso:4860,hora:'10:07'},o);
const AN={grupo:'21',carro:'22',n:1,tara:2150,neto:3413};
const LAM={tipo:'Láminas aprobadas N.E.',fecha:'21-10'};   // hoja de láminas despegue del 21-10-2026
const CASOS=[
  {id:'catodos',grupo:'Por producto',nombre:'Cátodos',c:ct(),codigo:rot(ct()),steps:catodos(ct())},
  {id:'anodos',grupo:'Por producto',nombre:'Restos de ánodos',c:Object.assign({prod:'anodos',hora:'12:05'},AN),codigo:'G21·C22',steps:anodos(AN)},
  {id:'laminas',grupo:'Por producto',nombre:'Láminas',c:{prod:'laminas',hora:'12:20'},codigo:'Nº 263',
   steps:laminas(Object.assign({n:263,neto:3458,rotulo:'21-10 · Nº 263'},LAM))},
  {id:'dospallets',grupo:'Por producto',nombre:'Dos pallets montados',c:{prod:'laminas',hora:'12:40'},codigo:'264/265',
   steps:dosPallets(Object.assign({arriba:{n:264,bruto:3755},abajo:{n:265,bruto:3713}},LAM))},
  {id:'sinetiqueta',grupo:'Qué pasa si…',nombre:'Sin etiqueta',c:ct({cuba:'06',prev:'05',next:'07',peso:4815}),codigo:'—',steps:sinEtiqueta(ct({cuba:'06',prev:'05',next:'07',peso:4815}))},
  {id:'grua',grupo:'Qué pasa si…',nombre:'Grúa encima',c:ct(),codigo:rot(ct()),steps:gruaEncima(ct())},
  {id:'carrofuera',grupo:'Qué pasa si…',nombre:'Carro fuera del área',c:Object.assign({prod:'anodos',hora:'12:05'},AN),codigo:'G21·C22',steps:carroFuera(AN)},
  {id:'nodulos',grupo:'Qué pasa si…',nombre:'Nódulos en la báscula',c:ct(),codigo:rot(ct()),steps:nodulosCaso(ct())},
  {id:'orden',grupo:'Qué pasa si…',nombre:'Fuera de orden',c:ct({cuba:'07',peso:4818}),codigo:'7T21',steps:fueraDeOrden(ct({cuba:'06',peso:4818}))},
  {id:'rango',grupo:'Qué pasa si…',nombre:'Peso fuera de rango',c:ct(),codigo:rot(ct()),steps:fueraDeRango(ct())},
  {id:'parcial',grupo:'Qué pasa si…',nombre:'Lado parcial',c:ct({lado:'P',cuba:'29',prev:'30',next:'28',peso:4316,hora:'11:52'}),codigo:'29P21',
   steps:ladoParcial(ct({lado:'P',cuba:'29',peso:4316}))}
];
window.VENT={CASOS,PROD,V,f0,sig0,PANT,RESTOS,LADO,rot,camposCat,camposAno,camposAnoGrupo,camposLam};
})();
