// Genera la presentación "Báscula N9 · Refinería Ventanas" con pptxgenjs.
// Uso, desde esta carpeta:  corepack pnpm install   y luego   node generar.js
// Las imágenes salen de img/ (capturas.py las vuelve a sacar) y del diagrama en ../../diagramas.
'use strict';
const fs = require('fs');
const path = require('path');
const pptxgen = require('pptxgenjs');

const AQUI = __dirname;
const IMG = f => path.join(AQUI, 'img', f);
const SALIDA = path.join(AQUI, '..', 'Bascula-N9-Ventanas.pptx');
const DIAGRAMA = path.join(AQUI, '..', '..', 'diagramas', 'flujo-bascula-n9.png');

/* ---------- Tema: el mismo cobre y pizarra de la simulación y la maqueta ---------- */
const TEMA = {
  name: 'Bascula N9 Ventanas',
  headFontFace: 'Bahnschrift SemiBold',
  bodyFontFace: 'Calibri',
  colors: {
    dk1: '1A2126', lt1: 'FFFFFF', dk2: '0E181E', lt2: 'F2F5F6',
    accent1: 'A4561F', accent2: 'DA8B4F', accent3: '2F7D4F', accent4: 'B8860B', accent5: 'B23A2E', accent6: '1E4F86',
    hlink: 'A4561F', folHlink: '7A4219',
  },
};
// Tonos que no son ranuras del tema (texto secundario, líneas y fondos de tarjeta)
const P = { mut: '5B6870', linea: 'D3DBDF', tarjeta: 'F2F5F6', cobreSuave: 'FBF1E8', claro: 'D6E5EB', claroMut: '9DB3BD', panel: '16242C' };

/* ---------- Datos reales del 22-10-2026, grupo 21 (Sistema de Cátodos y cuaderno de restos) ---------- */
const PESO = {1:4796,2:4855,3:4722,4:4687,5:4860,6:4815,7:4818,8:4843,9:4742,10:4831,11:4869,12:4724,13:4610,14:4725,15:4714,16:4680,17:4800,18:4789,19:4790,20:4644,21:4775,
  42:4310,41:4646,40:4714,39:4445,38:4365,37:4791,36:4756,35:4796,34:4641,33:4612,32:4651,31:4640,30:4626,29:4316};
const MUESTRA = 854;
const RESTOS = [3413,3878,3488,3718,3868,3464,3506,3274,3394,3912,2046];
const LADO_T = Array.from({length: 21}, (_, i) => i + 1);
const LADO_P = [42,41,40,39,38,37,36,35,34,33,32,31,30,29];
const suma = a => a.reduce((x, y) => x + y, 0);
const totT = suma(LADO_T.map(n => PESO[n])), totP = suma(LADO_P.map(n => PESO[n])), totRestos = suma(RESTOS);
const total = totT + totP + MUESTRA;
const todos = LADO_T.concat(LADO_P).map(n => PESO[n]);
// Los totales tienen que cuadrar con el Resumen del sistema; si no, no se arma la presentación
const esperado = { totT: 100089, totP: 64309, total: 165252, totRestos: 37961 };
for (const [k, v] of Object.entries(esperado)) {
  const real = { totT, totP, total, totRestos }[k];
  if (real !== v) throw new Error(`El dato ${k} da ${real} y el sistema dice ${v}`);
}
const kg = n => n.toLocaleString('es-CL');

/* ---------- Ayudas ---------- */
function tamPng(f) { const b = fs.readFileSync(f); return { w: b.readUInt32BE(16), h: b.readUInt32BE(20) }; }
function imagen(s, f, x, y, opt) {
  const t = tamPng(f), r = t.h / t.w;
  let w = opt.w, h = opt.h;
  if (w && !h) h = w * r; else if (h && !w) w = h / r;
  s.addImage({ path: f, x, y, w, h, altText: opt.alt, objectName: opt.nombre || path.basename(f) });
  return { w, h };
}

const pres = new pptxgen();
pres.layout = 'LAYOUT_WIDE'; // 13,333 x 7,5 pulgadas
pres.theme = { headFontFace: TEMA.headFontFace, bodyFontFace: TEMA.bodyFontFace };
pres.title = 'Báscula N9 · Identificación y pesaje automático';
pres.subject = 'Propuesta para la Báscula N9 de la Refinería Ventanas';
pres.author = 'Ingmétrica';
pres.company = 'Ingmétrica';
const C = pres.SchemeColor;
const SH = pres.ShapeType;
const CABEZA = '+mj-lt'; // fuente de títulos del tema

function texto(s, t, o) { s.addText(t, Object.assign({ isTextBox: true, margin: 0, color: C.text1, fontSize: 14, valign: 'top' }, o)); }
function tarjeta(s, x, y, w, h, o) {
  s.addShape(SH.roundRect, Object.assign({ x, y, w, h, rectRadius: 0.12, fill: { color: P.tarjeta }, line: { color: P.tarjeta, width: 0.75 } }, o || {}));
}
function numero(s, n, x, y, d, o) {
  s.addText(String(n), Object.assign({ shape: SH.ellipse, x, y, w: d, h: d, fill: { color: C.accent1 }, line: { color: C.accent1 },
    color: C.background1, fontFace: CABEZA, fontSize: Math.round(d * 34), align: 'center', valign: 'middle', margin: 0 }, o || {}));
}

/* ---------- Diseños (layouts) ---------- */
pres.defineSlideMaster({
  title: 'PORTADA',
  background: { color: TEMA.colors.dk2 },
  objects: [
    { placeholder: { options: { name: 'kicker', type: 'body', x: 0.6, y: 1.05, w: 6.2, h: 0.4, fontSize: 13, bold: true, color: C.accent2, charSpacing: 3, margin: 0 }, text: '' } },
    { placeholder: { options: { name: 'title', type: 'title', x: 0.6, y: 1.5, w: 6.2, h: 2.3, fontSize: 40, color: C.background1, align: 'left', valign: 'top', margin: 0 }, text: '' } },
  ],
});
pres.defineSlideMaster({
  title: 'CONTENIDO',
  background: { color: TEMA.colors.lt1 },
  objects: [
    { placeholder: { options: { name: 'kicker', type: 'body', x: 0.6, y: 0.34, w: 12.1, h: 0.3, fontSize: 12, bold: true, color: C.accent1, charSpacing: 3, margin: 0 }, text: '' } },
    { placeholder: { options: { name: 'title', type: 'title', x: 0.6, y: 0.66, w: 12.1, h: 0.7, fontSize: 30, color: C.text1, align: 'left', valign: 'top', margin: 0 }, text: '' } },
    { text: { text: 'Báscula N9 · Refinería Ventanas · Propuesta Ingmétrica', options: { x: 0.6, y: 7.05, w: 8, h: 0.25, fontSize: 10, color: P.mut, margin: 0 } } },
  ],
  slideNumber: { x: 12.2, y: 7.05, w: 0.5, h: 0.25, fontSize: 10, color: P.mut, align: 'right' },
});
pres.defineSlideMaster({
  title: 'LIENZO',
  background: { color: TEMA.colors.lt1 },
  objects: [{ text: { text: 'Báscula N9 · Refinería Ventanas · Propuesta Ingmétrica', options: { x: 0.6, y: 7.05, w: 8, h: 0.25, fontSize: 10, color: P.mut, margin: 0 } } }],
  slideNumber: { x: 12.2, y: 7.05, w: 0.5, h: 0.25, fontSize: 10, color: P.mut, align: 'right' },
});
pres.defineSlideMaster({
  title: 'CIERRE',
  background: { color: TEMA.colors.dk2 },
  objects: [
    { placeholder: { options: { name: 'kicker', type: 'body', x: 0.6, y: 0.8, w: 12.1, h: 0.35, fontSize: 13, bold: true, color: C.accent2, charSpacing: 3, margin: 0 }, text: '' } },
    { placeholder: { options: { name: 'title', type: 'title', x: 0.6, y: 1.2, w: 12.1, h: 0.9, fontSize: 36, color: C.background1, align: 'left', valign: 'top', margin: 0 }, text: '' } },
  ],
});
function lamina(seccion, kicker, titulo, master) {
  const s = pres.addSlide({ masterName: master || 'CONTENIDO', sectionTitle: seccion });
  if (kicker) s.addText(kicker, { placeholder: 'kicker' });
  if (titulo) s.addText(titulo, { placeholder: 'title' });
  return s;
}

/* ===================== 1. Portada ===================== */
pres.addSection({ title: 'Inicio' });
{
  const s = lamina('Inicio', 'CODELCO DIVISIÓN VENTANAS · REFINERÍA', 'Identificación y pesaje automático en la Báscula N9', 'PORTADA');
  texto(s, 'Una etiqueta con QR desde la nave, una báscula que controla el peso y un solo registro, listo en Excel.',
    { x: 0.6, y: 3.95, w: 5.9, h: 1.0, fontSize: 18, color: P.claro });
  texto(s, 'Propuesta Ingmétrica · noviembre 2026', { x: 0.6, y: 6.35, w: 5.9, h: 0.35, fontSize: 14, color: P.claroMut });
  imagen(s, IMG('vt_cat_pesando.png'), 6.85, 1.2, { w: 5.9, alt: 'Pantalla de la VT1200: etiqueta 5T21 que sigue y la Báscula N9 estabilizando el paquete 4T21' });
  texto(s, 'La VT1200 en la nave: la etiqueta que sigue y la Báscula N9 en vivo.', { x: 6.85, y: 5.75, w: 5.9, h: 0.3, fontSize: 11, color: P.claroMut });
  s.addNotes('Propuesta para identificar y pesar las cargas de la Báscula N9: cátodos comerciales, láminas y restos de ánodos.');
}

/* ===================== 2. Resumen ===================== */
pres.addSection({ title: 'Situación actual' });
{
  const s = lamina('Situación actual', 'RESUMEN', 'Hoy cada peso se anota tres veces; con la propuesta, ninguna');
  const cols = [
    ['3 veces', 'Hoy', 'El rótulo se escribe con plumón y algunos paquetes llegan sin él. El pesador anota cada peso en el cuaderno, en Excel y en el sistema web. Las taras de carro y los pesajes por diferencia se calculan a mano.'],
    ['1 toque', 'Con la propuesta', 'En la nave, la VT1200 imprime la etiqueta con QR. En la báscula, la cámara la lee y el sistema espera el peso estable, lo controla y lo registra.'],
    ['1 registro', 'Resultado', 'Cada carga queda con su peso, la hora real del pesaje y lo que dijo el control. Sale a Excel con las columnas del Sistema de Cátodos.'],
  ];
  cols.forEach(([big, h, body], i) => {
    const x = 0.6 + i * 4.15;
    tarjeta(s, x, 1.75, 3.85, 4.15, { fill: { color: i === 1 ? P.cobreSuave : P.tarjeta }, line: { color: i === 1 ? TEMA.colors.accent2 : P.tarjeta, width: 1 } });
    texto(s, big, { x: x + 0.35, y: 2.1, w: 3.2, h: 0.8, fontFace: CABEZA, fontSize: 40, color: C.accent1 });
    texto(s, h, { x: x + 0.35, y: 3.05, w: 3.2, h: 0.45, fontFace: CABEZA, fontSize: 20 });
    texto(s, body, { x: x + 0.35, y: 3.6, w: 3.15, h: 2.7, fontSize: 15, color: P.mut, lineSpacingMultiple: 1.1 });
  });
}

/* ===================== 3. La visita ===================== */
{
  const s = lamina('Situación actual', 'SITUACIÓN ACTUAL', 'Lo que vimos en la Báscula N9');
  imagen(s, IMG('sim_escena.png'), 0.6, 1.85, { w: 6.3, alt: 'Escena isométrica: nave, grúa con el paquete, báscula a ras de piso, caseta del pesador y balizas' });
  texto(s, 'Escena de la simulación: (A) nave, (B) cámara cenital, (C) balizas, (D) báscula, (E) caseta del pesador.',
    { x: 0.6, y: 5.75, w: 6.3, h: 0.5, fontSize: 12, color: P.mut });
  const hechos = [
    ['Báscula de piso', 'A ras de suelo, al aire libre, con 4 celdas de carga.'],
    ['Pesador en la caseta', 'Con el terminal de pesaje, un PC, cuaderno y calculadora.'],
    ['Tres registros', 'Cada peso se anota en el cuaderno, en Excel y en el sistema web.'],
    ['Rótulo a plumón', 'Se escribe sobre la carga; algunos paquetes llegan sin rótulo.'],
    ['Nódulos en la junta', 'Caen trozos de cobre en el borde de la plataforma y la báscula pierde el cero.'],
  ];
  hechos.forEach(([h, b], i) => {
    const y = 1.6 + i * 1.0;
    numero(s, i + 1, 7.2, y, 0.42);
    texto(s, h, { x: 7.85, y: y - 0.02, w: 4.85, h: 0.36, fontFace: CABEZA, fontSize: 17 });
    texto(s, b, { x: 7.85, y: y + 0.36, w: 4.85, h: 0.55, fontSize: 14, color: P.mut });
  });
}

/* ===================== 4. Tres productos ===================== */
{
  const s = lamina('Situación actual', 'TRES PRODUCTOS', 'Cada producto llega distinto a la báscula');
  const prods = [
    ['etq_cat.png', 'Cátodos comerciales', ['Un paquete por celda, de 4,3 a 4,9 t.', 'Llega con grúa horquilla y no lleva tara.', 'Rótulo: celda + lado + grupo (12P31).']],
    ['etq_lam.png', 'Láminas', ['Pallet con número correlativo del mes.', 'Tara fija de 12 kg por pallet.', 'Dos pallets montados se pesan por diferencia.']],
    ['etq_rs.png', 'Restos de ánodos', ['Carro + rack que arrastra un tractor.', 'Unos 11 carros por grupo, de 3 a 3,9 t netos.', 'Hoy la tara se busca en una lista impresa.']],
  ];
  prods.forEach(([f, h, items], i) => {
    const x = 0.6 + i * 4.15;
    tarjeta(s, x, 1.6, 3.85, 5.15);
    imagen(s, IMG(f), x + 0.42, 1.85, { w: 3.0, alt: `Etiqueta de ${h.toLowerCase()} con rótulo y código QR` });
    texto(s, h, { x: x + 0.35, y: 4.05, w: 3.2, h: 0.45, fontFace: CABEZA, fontSize: 20 });
    texto(s, items.map((t, k) => ({ text: t, options: { bullet: true, breakLine: k < items.length - 1 } })),
      { x: x + 0.35, y: 4.6, w: 3.2, h: 2.0, fontSize: 14, color: P.mut, paraSpaceAfter: 6 });
  });
  s.addNotes('Las etiquetas son las mismas que imprime la maqueta de la VT1200: el rótulo de siempre más un QR.');
}

/* ===================== 5. Un día real ===================== */
pres.addSection({ title: 'Datos' });
{
  const s = lamina('Datos', 'UN DÍA REAL · 22-10-2026', 'Grupo 21: 35 paquetes, todos en un rango estrecho');
  const kpi = [[`${kg(total)} kg`, 'cátodos del grupo 21: lado total, lado parcial y muestra'],
    ['35 paquetes', '21 del lado total y 14 del lado parcial'],
    [`${kg(totRestos)} kg`, 'restos de ánodos del mismo grupo, en 11 carros']];
  kpi.forEach(([n, l], i) => {
    const y = 1.65 + i * 1.6;
    texto(s, n, { x: 0.6, y, w: 3.5, h: 0.65, fontFace: CABEZA, fontSize: 32, color: C.accent1 });
    texto(s, l, { x: 0.6, y: y + 0.68, w: 3.4, h: 0.6, fontSize: 14, color: P.mut });
  });
  const etiquetas = LADO_T.concat(LADO_P).map(String);
  s.addChart([
    { type: pres.ChartType.line, data: [{ name: 'Peso del paquete (kg)', labels: etiquetas, values: todos }],
      options: { chartColors: ['C2692A'], lineSize: 2, lineDataSymbol: 'circle', lineDataSymbolSize: 7 } },
    { type: pres.ChartType.line, data: [
        { name: 'Máximo esperado (5.000 kg)', labels: etiquetas, values: etiquetas.map(() => 5000) },
        { name: 'Mínimo esperado (4.200 kg)', labels: etiquetas, values: etiquetas.map(() => 4200) }],
      options: { chartColors: ['7E8D95', '7E8D95'], lineSize: 1.5, lineDash: 'dash', lineDataSymbol: 'none' } },
  ], {
    x: 4.3, y: 1.55, w: 8.4, h: 4.85,
    showTitle: true, title: 'Peso de cada paquete, en el orden en que se pesó', titleFontSize: 14, titleColor: '1A2126', titleFontFace: '+mn-lt',
    valAxisMinVal: 4000, valAxisMaxVal: 5200, valAxisMajorUnit: 200, valAxisLabelFormatCode: '#,##0',
    catAxisLabelFontSize: 10, valAxisLabelFontSize: 11, catAxisLabelFontFace: '+mn-lt', valAxisLabelFontFace: '+mn-lt',
    catAxisLabelColor: '5B6870', valAxisLabelColor: '5B6870', valGridLine: { color: 'E3E9EC', size: 0.75 }, catGridLine: { style: 'none' },
    showCatAxisTitle: true, catAxisTitle: 'Celda: lado total 1 a 21, después lado parcial 42 a 29', catAxisTitleFontSize: 11, catAxisTitleColor: '5B6870', catAxisTitleFontFace: '+mn-lt',
    showLegend: true, legendPos: 'b', legendFontSize: 11, legendFontFace: '+mn-lt', legendColor: '5B6870',
  });
  texto(s, `Pesaron entre ${kg(Math.min(...todos))} y ${kg(Math.max(...todos))} kg. Fuente: Sistema de Cátodos (Detalle pesaje de producción) y cuaderno de restos.`,
    { x: 4.3, y: 6.5, w: 8.4, h: 0.3, fontSize: 11, color: P.mut });
  s.addNotes(`Lado total ${kg(totT)} kg, lado parcial ${kg(totP)} kg, muestra ${kg(MUESTRA)} kg. Con un rango tan estrecho, un peso raro salta a la vista y el sistema lo puede avisar solo.`);
}

/* ===================== 6. Problemas ===================== */
{
  const s = lamina('Datos', 'DÓNDE SE PIERDE', 'Seis puntos donde hoy se pierde tiempo o precisión');
  const p = [
    ['Rótulo a mano', 'Plumón sobre la lámina de arriba; algunos paquetes llegan sin rótulo.'],
    ['Triple digitación', 'Cuaderno, Excel y sistema web: tres veces el mismo peso.'],
    ['Hora que no es real', 'Las cubas 01 a 10 del 22-10 quedaron con 6 segundos de diferencia: se digitan en tanda.'],
    ['Tara con calculadora', 'La tara de cada carro + rack se busca en una lista impresa y se resta a mano.'],
    ['Diferencias a mano', 'Con dos pallets montados, el peso de cada uno se calcula restando.'],
    ['La báscula se engaña', 'Grúa encima, carro fuera del área o nódulos en la junta cambian el peso.'],
  ];
  p.forEach(([h, b], i) => {
    const x = 0.6 + (i % 3) * 4.15, y = 1.65 + Math.floor(i / 3) * 2.55;
    tarjeta(s, x, y, 3.85, 2.3);
    numero(s, i + 1, x + 0.3, y + 0.3, 0.42);
    texto(s, h, { x: x + 0.9, y: y + 0.33, w: 2.75, h: 0.4, fontFace: CABEZA, fontSize: 18 });
    texto(s, b, { x: x + 0.3, y: y + 0.95, w: 3.3, h: 1.2, fontSize: 14, color: P.mut });
  });
}

/* ===================== 7. Flujo propuesto (diagrama) ===================== */
pres.addSection({ title: 'Propuesta' });
{
  const s = pres.addSlide({ masterName: 'LIENZO', sectionTitle: 'Propuesta' });
  const t = tamPng(DIAGRAMA), h = 6.55, w = h * t.w / t.h;
  s.addImage({ path: DIAGRAMA, x: (13.333 - w) / 2, y: 0.32, w, h, altText: 'Diagrama del flujo propuesto: nave, traslado, báscula N9 con sus controles y registro', objectName: 'Diagrama de flujo' });
  s.addNotes('Flujo propuesto. 1 a 3 en la nave con la VT1200; traslado en grúa o tractor; 4 a 6 en la báscula: lectura del QR, controles (grúa, área, cero), peso estable y neto; 7 a 9 el registro, el Excel y los sistemas de Codelco. Las balizas avisan en terreno.');
}

/* ===================== 8. La pantalla de la VT1200 ===================== */
{
  const s = lamina('Propuesta', 'LA PANTALLA EN LA NAVE', 'La VT1200: un toque por paquete y la báscula a la vista');
  const im = imagen(s, IMG('vt_cat_pesando.png'), 0.6, 1.5, { w: 7.2, alt: 'Pantalla de cátodos de la VT1200 con el mapa del grupo 21 y la báscula estabilizando' });
  const k = im.w / 2124;
  [[60, 53], [62, 190], [1255, 175], [62, 647], [56, 1351]].forEach(([px, py], i) => numero(s, i + 1, 0.6 + px * k - 0.18, 1.5 + py * k - 0.18, 0.36));
  const items = [
    ['Producto', 'Tres pestañas grandes: cátodos, láminas y restos de ánodos.'],
    ['Etiqueta que sigue', 'El terminal ya la propone y se ve tal como sale de la Zebra.'],
    ['Báscula N9 en vivo', 'Peso, estado y control de la carga que está encima.'],
    ['Mapa del grupo', 'Verde pesada, ámbar en la báscula, azul en camino. Se toca para cambiar de celda.'],
    ['Un botón', '"Imprimir 5T21". El registro y el Excel siempre a la vista.'],
  ];
  items.forEach(([h, b], i) => {
    const y = 1.55 + i * 1.02;
    numero(s, i + 1, 8.15, y, 0.4);
    texto(s, h, { x: 8.75, y: y - 0.02, w: 3.95, h: 0.36, fontFace: CABEZA, fontSize: 17 });
    texto(s, b, { x: 8.75, y: y + 0.35, w: 3.95, h: 0.6, fontSize: 14, color: P.mut });
  });
}

/* ===================== 9. La báscula controla el peso ===================== */
{
  const s = lamina('Propuesta', 'CONTROL DEL PESO', 'La báscula registra sola y frena lo que no cuadra');
  const filas = [
    ['Lo normal: llega, se estabiliza, se controla y se registra', 1.45, [
      ['bal_1llegando.png', 'Llega: la cámara lee la etiqueta'], ['bal_2estabilizando.png', 'Estabilizando: nadie toca la carga'],
      ['bal_3registrado.png', 'Estable: dentro de rango y registrado'], ['bal_6lam.png', 'Láminas: descuenta 12 kg por pallet']]],
    ['Lo que frena o avisa', 4.1, [
      ['bal_4grua.png', 'Grúa encima: marca 1.850 kg de más'], ['bal_7fuera.png', 'Carro fuera del área: pierde peso'],
      ['bal_5sincero.png', 'Sin cero: nódulos en la junta'], ['bal_9sintara.png', 'Sin tara: falta pesar el carro vacío']]],
  ];
  filas.forEach(([rot, y, imgs]) => {
    texto(s, rot, { x: 0.6, y, w: 12.1, h: 0.32, fontFace: CABEZA, fontSize: 15, color: C.accent1 });
    imgs.forEach(([f, cap], i) => {
      const x = 0.6 + i * 3.07;
      const im = imagen(s, IMG(f), x, y + 0.4, { w: 2.9, alt: cap });
      texto(s, cap, { x, y: y + 0.48 + im.h, w: 2.9, h: 0.3, fontSize: 12, color: P.mut });
    });
  });
  s.addNotes('Rangos usados: cátodos 4.200 a 5.000 kg, láminas aprobadas 3.200 a 3.900 kg netos, restos 2.800 a 4.000 kg netos. Salen de los pesajes de la visita.');
}

/* ===================== 10. Láminas y restos ===================== */
{
  const s = lamina('Propuesta', 'LÁMINAS Y RESTOS', 'La misma pantalla sirve para láminas y restos de ánodos');
  const a = imagen(s, IMG('vt_lam.png'), 0.6, 1.5, { w: 5.95, alt: 'Pantalla de láminas: tipo de lámina, número del mes y báscula con el neto' });
  imagen(s, IMG('vt_rs.png'), 6.75, 1.5, { w: 5.95, alt: 'Pantalla de restos de ánodos: teclado del número de carro, carros del grupo y báscula' });
  texto(s, 'Láminas: se elige el tipo y el terminal propone el número del mes. La báscula descuenta 12 kg por pallet.',
    { x: 0.6, y: 1.65 + a.h, w: 5.95, h: 0.6, fontSize: 14, color: P.mut });
  texto(s, 'Restos: se escribe el número pintado del carro. El sistema pone la tara de carro + rack y, si no la tiene, avisa.',
    { x: 6.75, y: 1.65 + a.h, w: 5.95, h: 0.6, fontSize: 14, color: P.mut });
}

/* ===================== 11. La etiqueta ===================== */
{
  const s = lamina('Propuesta', 'LA ETIQUETA', 'El rótulo de siempre, impreso y con un código QR');
  texto(s, '5T21', { x: 0.6, y: 1.55, w: 6.2, h: 1.3, fontFace: CABEZA, fontSize: 80, color: C.text1 });
  const partes = [['5', 'Celda', 'de 1 a 42'], ['T', 'Lado total', 'P es lado parcial'], ['21', 'Grupo', 'de la cosecha']];
  partes.forEach(([v, h, b], i) => {
    const x = 0.6 + i * 2.1;
    tarjeta(s, x, 3.0, 1.95, 1.35);
    texto(s, v, { x: x + 0.2, y: 3.1, w: 1.6, h: 0.5, fontFace: CABEZA, fontSize: 26, color: C.accent1 });
    texto(s, h, { x: x + 0.2, y: 3.6, w: 1.6, h: 0.3, fontSize: 14, bold: true });
    texto(s, b, { x: x + 0.2, y: 3.9, w: 1.6, h: 0.3, fontSize: 12, color: P.mut });
  });
  texto(s, [
    { text: 'El QR repite el dato para que la cámara lo lea sin errores.', options: { bullet: true, breakLine: true } },
    { text: 'Láminas: día, mes y número del mes (21-10 · Nº 263).', options: { bullet: true, breakLine: true } },
    { text: 'Restos: grupo y número pintado del carro.', options: { bullet: true, breakLine: true } },
    { text: 'Si la cámara no la lee, el pesador identifica la carga desde la caseta.', options: { bullet: true } },
  ], { x: 0.6, y: 4.65, w: 6.2, h: 2.1, fontSize: 15, color: P.mut, paraSpaceAfter: 6 });
  imagen(s, IMG('etq_cat.png'), 7.45, 1.55, { w: 4.6, alt: 'Etiqueta de cátodo 5T21 con QR y cajas de celda, lado y grupo' });
  imagen(s, IMG('etq_lam.png'), 7.45, 4.85, { w: 2.2, alt: 'Etiqueta de láminas' });
  imagen(s, IMG('etq_rs.png'), 9.85, 4.85, { w: 2.2, alt: 'Etiqueta de restos de ánodos' });
}

/* ===================== 12. Registro y Excel ===================== */
{
  const s = lamina('Propuesta', 'REGISTRO Y EXCEL', 'Todo queda en un registro y sale a Excel por turno');
  imagen(s, IMG('vt_registro.png'), 0.6, 1.5, { w: 6.3, alt: 'Registro del día en la VT1200: etiquetas, estado en la báscula, hora de pesaje y neto' });
  texto(s, 'Hojas del Excel', { x: 7.3, y: 1.5, w: 5.4, h: 0.4, fontFace: CABEZA, fontSize: 18 });
  const hojas = [['Cátodos', 'grupo, cuba, lado, muestra, peso, hora de pesaje y control.'],
    ['Láminas', 'número, bruto, tara de 12 kg y neto, como la hoja de láminas.'],
    ['Restos de ánodos', 'carro, bruto, tara de carro + rack, neto y total del grupo.'],
    ['Resumen', 'las columnas del Resumen del Sistema de Cátodos.']];
  texto(s, hojas.map(([h, b], i) => [{ text: h + ': ', options: { bold: true, color: TEMA.colors.dk1 } }, { text: b, options: { breakLine: i < hojas.length - 1 } }]).flat(),
    { x: 7.3, y: 2.0, w: 5.4, h: 2.2, fontSize: 14, color: P.mut, paraSpaceAfter: 8 });
  texto(s, 'Resumen del grupo 21 del 22-10, en el formato del Excel', { x: 7.3, y: 4.45, w: 5.4, h: 0.3, fontSize: 13, bold: true });
  const cab = ['Lado-P', 'Lado-T', 'Muestra', 'Tot-peso', 'Tot-coli'].map(t => ({ text: t, options: { bold: true, color: 'FFFFFF', fill: { color: TEMA.colors.dk2 }, align: 'center' } }));
  const fila = [kg(totP), kg(totT), kg(MUESTRA), kg(total), '35'].map(t => ({ text: t, options: { align: 'center' } }));
  s.addTable([cab, fila], { x: 7.3, y: 4.85, w: 5.4, colW: [1.08, 1.08, 1.08, 1.08, 1.08], fontSize: 13, color: TEMA.colors.dk1,
    border: { type: 'solid', pt: 0.75, color: P.linea }, rowH: [0.4, 0.42], valign: 'middle' });
  texto(s, 'Cada fila trae además la hora real en que la báscula tomó el peso.', { x: 7.3, y: 5.85, w: 5.4, h: 0.5, fontSize: 13, color: P.mut });
}

/* ===================== 13. Demo ===================== */
{
  const s = lamina('Propuesta', 'PRUÉBELO', 'Una simulación de la báscula y una maqueta de la VT1200');
  imagen(s, IMG('sim_simulacion.png'), 0.6, 1.5, { w: 7.5, alt: 'Simulación de la báscula: escena, pasos del caso y pantalla del pesador' });
  const bloques = [['Simulación de la báscula', 'Tres productos, dos pallets montados y siete situaciones: sin etiqueta, grúa encima, carro fuera del área, nódulos, fuera de orden, fuera de rango y lado parcial.'],
    ['Maqueta de la VT1200', 'Imprime etiquetas, muestra la báscula en vivo, prueba grúa encima, carro fuera y sin cero, y baja el Excel del día.'],
    ['Sin instalar nada', 'Las dos son páginas que se abren en el navegador. Los datos son simulados con los pesos de la visita.']];
  bloques.forEach(([h, b], i) => {
    const y = 1.55 + i * 1.65;
    texto(s, h, { x: 8.5, y, w: 4.2, h: 0.4, fontFace: CABEZA, fontSize: 18 });
    texto(s, b, { x: 8.5, y: y + 0.45, w: 4.2, h: 1.1, fontSize: 14, color: P.mut });
  });
}

/* ===================== 14. Supuestos ===================== */
pres.addSection({ title: 'Cierre' });
{
  const s = lamina('Cierre', 'SUPUESTOS', 'Lo que asumimos y lo que falta confirmar');
  const listas = [
    ['Supuestos de esta propuesta', [
      'La etiqueta se imprime y se pega en la nave, donde hoy se rotula con plumón; no lleva peso.',
      'El pesador sigue en la caseta y ve todo en su PC.',
      'Rangos de peso tomados de la visita: cátodos 4.200 a 5.000 kg, restos 2.800 a 4.000 kg, láminas aprobadas 3.200 a 3.900 kg.',
      'Taras de carro + rack de ejemplo, hasta tener la lista real.']],
    ['Por confirmar con Codelco', [
      'Cátodos EW: hoy llegan a la báscula sin rótulo.',
      'Láminas rechazo C/O y los rangos de C.O y rechazo S/O.',
      'Si el terminal de pesaje entrega la lectura de cada celda de carga.',
      'Qué es el lado "M" del Sistema de Ánodos.',
      'Otros productos poco frecuentes que pasan por la báscula.']],
  ];
  listas.forEach(([h, items], i) => {
    const x = 0.6 + i * 6.15;
    tarjeta(s, x, 1.6, 5.95, 5.1, i === 1 ? { fill: { color: P.cobreSuave }, line: { color: TEMA.colors.accent2, width: 1 } } : undefined);
    texto(s, h, { x: x + 0.4, y: 1.9, w: 5.2, h: 0.45, fontFace: CABEZA, fontSize: 20 });
    texto(s, items.map((t, k) => ({ text: t, options: { bullet: true, breakLine: k < items.length - 1 } })),
      { x: x + 0.4, y: 2.5, w: 5.2, h: 4.0, fontSize: 15, color: P.mut, paraSpaceAfter: 10 });
  });
}

/* ===================== 15. Próximos pasos ===================== */
{
  const s = lamina('Cierre', 'PRÓXIMOS PASOS', 'Cómo seguimos', 'CIERRE');
  const pasos = [
    ['Validar', 'Revisar el flujo y las pantallas con la nave, el pesador y supervisión.'],
    ['Datos', 'Recibir la lista de taras de carro + rack y confirmar los rangos por producto.'],
    ['Terreno', 'Confirmar la salida de datos del terminal de pesaje y ubicar cámara y balizas.'],
    ['Piloto', 'Partir en la Báscula N9 con cátodos comerciales.'],
    ['Ampliar', 'Sumar láminas y restos, y conectar el registro con los sistemas de Codelco.'],
  ];
  s.addShape(SH.line, { x: 1.01, y: 3.11, w: 10.0, h: 0, line: { color: '35505C', width: 2 } });
  pasos.forEach(([h, b], i) => {
    const x = 0.6 + i * 2.5;
    numero(s, i + 1, x + 0.1, 2.8, 0.62, { fill: { color: C.accent2 }, line: { color: C.accent2 }, color: TEMA.colors.dk2 });
    texto(s, h, { x, y: 3.65, w: 2.25, h: 0.45, fontFace: CABEZA, fontSize: 20, color: C.background1 });
    texto(s, b, { x, y: 4.15, w: 2.25, h: 1.6, fontSize: 14, color: P.claro });
  });
  texto(s, 'Ingmétrica · noviembre 2026', { x: 0.6, y: 6.6, w: 6, h: 0.35, fontSize: 13, color: P.claroMut });
}

/* ---------- Escribir y poner los colores del tema ---------- */
async function aplicarTema(file, T) {
  const JSZip = require(require.resolve('jszip', { paths: [require.resolve('pptxgenjs')] }));
  const zip = await JSZip.loadAsync(fs.readFileSync(file));
  const ranuras = ['dk1', 'lt1', 'dk2', 'lt2', 'accent1', 'accent2', 'accent3', 'accent4', 'accent5', 'accent6', 'hlink', 'folHlink'];
  const esquema = `<a:clrScheme name="${T.name}">` + ranuras.map(k => `<a:${k}><a:srgbClr val="${T.colors[k]}"/></a:${k}>`).join('') + '</a:clrScheme>';
  for (const n of Object.keys(zip.files).filter(n => /^ppt\/theme\/theme\d+\.xml$/.test(n))) {
    const xml = await zip.file(n).async('string');
    zip.file(n, xml.replace(/<a:clrScheme[\s\S]*?<\/a:clrScheme>/, esquema).replace(/(<a:theme[^>]*\sname=")[^"]*"/, `$1${T.name}"`));
  }
  fs.writeFileSync(file, await zip.generateAsync({ type: 'nodebuffer', compression: 'DEFLATE' }));
}
(async () => {
  await pres.writeFile({ fileName: SALIDA });
  await aplicarTema(SALIDA, TEMA);
  console.log('Listo:', SALIDA);
  console.log(`Lado T ${kg(totT)} · Lado P ${kg(totP)} · Muestra ${kg(MUESTRA)} · Total ${kg(total)} · Restos ${kg(totRestos)}`);
})().catch(e => { console.error(e); process.exit(1); });
