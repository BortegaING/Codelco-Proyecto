// Genera la presentación "Báscula N9 · Refinería Ventanas" con pptxgenjs.
// Uso, desde esta carpeta:  corepack pnpm install   y luego   node generar.js
// Las imágenes salen de img/ y del diagrama en ../../diagramas. Las pantallas, la etiqueta de cátodo, la báscula y el puntero (vt_*, etq_cat_vt,
// bal_*, app_impresa, cursor) son de la app nativa: capturas_app.py las copia desde la carpeta de la prueba. Las etiquetas de láminas
// y restos y la simulación las saca capturas.py.
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
// Ancho y alto de un PNG o un JPG, para mantener la proporción de cada imagen
function tamPng(f) {
  const b = fs.readFileSync(f);
  if (b[0] === 0x89) return { w: b.readUInt32BE(16), h: b.readUInt32BE(20) };
  for (let i = 2; i < b.length;) {
    const m = b[i + 1], len = b.readUInt16BE(i + 2);
    if (m >= 0xC0 && m <= 0xC3) return { w: b.readUInt16BE(i + 7), h: b.readUInt16BE(i + 5) };
    i += 2 + len;
  }
  throw new Error('No se pudo leer el tamaño de ' + f);
}
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
pres.title = 'Báscula N9 · Sistema de trazabilidad y pesaje';
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

/* Puntero sobre lo que se toca en una captura de la app (coordenadas de la captura de 2048 x 1536) */
function puntero(s, x0, y0, k, px, py, marco) {
  if (marco) {
    const [a, b, c, d] = marco;
    s.addShape(SH.roundRect, { x: x0 + a * k - 0.05, y: y0 + b * k - 0.05, w: (c - a) * k + 0.1, h: (d - b) * k + 0.1, rectRadius: 0.06,
      fill: { type: 'none' }, line: { color: 'F2B33D', width: 3 }, objectName: 'Marca de selección' });
  }
  imagen(s, IMG('cursor.png'), x0 + px * k - 0.04, y0 + py * k - 0.04, { w: 0.5, alt: 'Puntero: aquí toca el operador', nombre: 'Puntero' });
}
function items(s, lista, x, y, paso, w) {
  lista.forEach(([h, b], i) => {
    const yy = y + i * paso;
    numero(s, i + 1, x, yy, 0.4);
    texto(s, h, { x: x + 0.6, y: yy - 0.02, w, h: 0.36, fontFace: CABEZA, fontSize: 17 });
    texto(s, b, { x: x + 0.6, y: yy + 0.36, w, h: paso - 0.4, fontSize: 14, color: P.mut });
  });
}

/* Números sobre las capturas (coordenadas de la captura de 2048 x 1536) */
const MARCAS_ORIGEN = [[16, 60], [30, 236], [30, 690], [2010, 690], [30, 1400]];
const MARCAS_CASETA = [[30, 230], [840, 230], [1190, 230], [30, 720], [1630, 720], [30, 1400]];

/* ===================== 1. Portada ===================== */
pres.addSection({ title: 'Inicio' });
{
  const s = lamina('Inicio', 'CODELCO DIVISIÓN VENTANAS · REFINERÍA', 'Sistema de trazabilidad y pesaje · Báscula N9', 'PORTADA');
  texto(s, 'Etiquetado estandarizado de cátodos en origen, lectura por visión artificial y captura automática del peso en un registro único y auditable.',
    { x: 0.6, y: 3.95, w: 5.9, h: 1.3, fontSize: 18, color: P.claro });
  texto(s, 'Propuesta Ingmétrica · noviembre 2026', { x: 0.6, y: 6.35, w: 5.9, h: 0.35, fontSize: 14, color: P.claroMut });
  imagen(s, IMG('vt_principal.png'), 6.85, 1.2, { w: 5.9, alt: 'Pantalla de la VT1200 en origen: etiqueta 5T21 que sigue y el grupo 21 con sus celdas' });
  texto(s, 'VT1200 en origen: etiqueta que sigue y grupo de producción.', { x: 6.85, y: 5.75, w: 5.9, h: 0.3, fontSize: 11, color: P.claroMut });
  s.addNotes('Sistema de trazabilidad y pesaje para la Báscula N9. En esta etapa la VT1200 de origen rotula cátodos comerciales; la báscula identifica y pesa con visión artificial.');
}

/* ===================== 2. Paso 1: selección de cuba en el layout ===================== */
pres.addSection({ title: 'Propuesta' });
{
  const s = lamina('Propuesta', 'PASO 1 · SELECCIÓN DE CUBA', 'El operador selecciona el grupo en el layout de circuitos');
  const x0 = 0.6, y0 = 1.5, im = imagen(s, IMG('vt_paso1.png'), x0, y0, { w: 7.3, alt: 'VT1200, paso 1: layout de la nave con los circuitos I a VI, sus grupos y EW; el grupo 21 seleccionado' });
  const k = im.w / 2048;
  puntero(s, x0, y0, k, 700, 668, [586, 614, 781, 697]);
  items(s, [
    ['Layout de la nave', 'Circuitos I a VI con sus grupos, en la misma disposición del plano de la refinería.'],
    ['Primera pantalla', 'Al ingresar, el operador toca el grupo a cosechar (en el ejemplo, grupo 21) y pasa a la pantalla de producción.'],
    ['Restricciones', 'Solo se rotulan cátodos: los grupos 1 y 2 quedan sin rotulado. EW queda por confirmar.'],
    ['Cierre del grupo', 'Desde "Cambiar grupo", "Terminar grupo" informa a la báscula que no vienen más lingadas de ese grupo.'],
  ], 8.3, 1.6, 1.25, 4.1);
  s.addNotes('Es parte del formulario de la VT1200: la primera ventana al ingresar y al cambiar de grupo. Se toca el grupo en el layout; el puntero marca la selección.');
}

/* ===================== 3. Resumen ===================== */
{
  const s = lamina('Propuesta', 'RESUMEN', 'Un registro único por lingada, sin digitación manual del peso');
  const cols = [
    ['1 etiqueta', 'En origen', 'La VT1200 emite una etiqueta estandarizada por lingada (celda, lado, grupo, 39 cátodos y QR) y la deja registrada como pendiente de pesaje.'],
    ['1 registro', 'En la Báscula N9', 'La cámara superior lee el QR, el visor entrega el peso estable y el sistema consolida identificación, peso, hora real y validaciones.'],
  ];
  cols.forEach(([big, h, body], i) => {
    const x = 0.6 + i * 6.15;
    tarjeta(s, x, 1.75, 5.95, 4.15, { fill: { color: i === 0 ? P.cobreSuave : P.tarjeta }, line: { color: i === 0 ? TEMA.colors.accent2 : P.tarjeta, width: 1 } });
    texto(s, big, { x: x + 0.45, y: 2.1, w: 5.0, h: 0.8, fontFace: CABEZA, fontSize: 40, color: C.accent1 });
    texto(s, h, { x: x + 0.45, y: 3.05, w: 5.0, h: 0.45, fontFace: CABEZA, fontSize: 20 });
    texto(s, body, { x: x + 0.45, y: 3.6, w: 5.0, h: 2.1, fontSize: 16, color: P.mut, lineSpacingMultiple: 1.1 });
  });
  texto(s, 'Hoy cada peso se anota tres veces (cuaderno, Excel y sistema web) y la identificación depende de un rótulo a plumón.',
    { x: 0.6, y: 6.15, w: 12.1, h: 0.4, fontSize: 14, color: P.mut });
}

/* ===================== 4. Paso 2: grupo de producción y celda ===================== */
{
  const s = lamina('Propuesta', 'PASO 2 · SELECCIÓN GRUPO DE PRODUCCIÓN', 'La VT1200 propone la celda que sigue y emite la etiqueta');
  const x0 = 0.6, y0 = 1.5, im = imagen(s, IMG('vt_paso2.png'), x0, y0, { w: 7.3, alt: 'VT1200, paso 2: grupo 22 con las celdas 1 a 21 arriba y 22 a 42 abajo; celda 1 seleccionada y etiqueta 1T22 lista para imprimir' });
  const k = im.w / 2048;
  puntero(s, x0, y0, k, 128, 968);
  items(s, [
    ['Un grupo a la vista', 'El grupo seleccionado en grande: arriba la fila de celdas 1 a 21 y abajo la de 22 a 42.'],
    ['Celda que sigue', 'Marcada en cobre según el orden de cosecha. Un toque cambia de celda; otro grupo, con Cambiar grupo.'],
    ['Lado total o parcial', 'Se define una vez por lado (T o P) y entra en el rótulo.'],
    ['Emisión', '"Imprimir 1T22" envía la etiqueta a la Zebra y la deja pendiente de pesaje.'],
  ], 8.3, 1.6, 1.25, 4.1);
}

/* ===================== 5. Tres productos ===================== */
pres.addSection({ title: 'Situación actual' });
{
  const s = lamina('Situación actual', 'PRODUCTOS DE LA BÁSCULA N9', 'Tres productos con identificación y tara distintas');
  const prods = [
    ['etq_cat_vt.png', 'Cátodos comerciales', ['Lingada de 39 cátodos por celda, de 4,3 a 4,9 t.', 'Ingresa con horquilla; sin tara.', 'Rótulo: celda + lado + grupo (12P31).']],
    ['etq_lam.png', 'Láminas', ['Pallet con correlativo mensual.', 'Tara de pallet: 12 kg.', 'Dos pallets montados: pesaje por diferencia.']],
    ['etq_rs.png', 'Restos de ánodos', ['Carro de arrastre con rack.', 'Neto = bruto − tara de carro − tara de rack.', 'Tara leída por la cámara lateral.']],
  ];
  prods.forEach(([f, h, it], i) => {
    const x = 0.6 + i * 4.15;
    tarjeta(s, x, 1.6, 3.85, 4.75);
    imagen(s, IMG(f), x + 0.42, 1.85, { w: 3.0, alt: `Etiqueta de ${h.toLowerCase()} con rótulo y código QR` });
    texto(s, h, { x: x + 0.35, y: 4.05, w: 3.2, h: 0.45, fontFace: CABEZA, fontSize: 20 });
    texto(s, it.map((t, j) => ({ text: t, options: { bullet: true, breakLine: j < it.length - 1 } })),
      { x: x + 0.35, y: 4.55, w: 3.2, h: 1.7, fontSize: 14, color: P.mut, paraSpaceAfter: 6 });
  });
  texto(s, 'Alcance de esta etapa: la VT1200 rotula cátodos comerciales. Láminas y restos de ánodos se incorporan en la ampliación.',
    { x: 0.6, y: 6.5, w: 12.1, h: 0.35, fontSize: 13, bold: true, color: C.accent1 });
}

/* Láminas que se sacaron del deck el 04-11 a pedido de Benjamin (las 5 a 8): las tres de "Cómo se registra hoy"
   y la del gráfico del grupo 21. Quedan aquí apagadas; con true vuelven a salir. */
const INCLUIR = { sistema: false, diaReal: false };

/* ===================== 4b. Cómo se registra hoy: imágenes del sistema y de las planillas (visita del 22-10) ===================== */
// Recortes de las fotos de la visita, sin barra de direcciones, IP ni nombres de personas
function encaja(s, f, x, y, w, h, alt) {
  const t = tamPng(f), r = Math.min(w / t.w, h / t.h), iw = t.w * r, ih = t.h * r;
  s.addImage({ path: f, x: x + (w - iw) / 2, y, w: iw, h: ih, altText: alt, objectName: path.basename(f) });
  return ih;
}
if (INCLUIR.sistema) {
  const s = lamina('Situación actual', 'CÓMO SE REGISTRA HOY · SISTEMA DE CÁTODOS', 'El pesador digita cada pesaje en el sistema');
  const h = encaja(s, IMG('sis_form_catodos.jpg'), 0.6, 1.5, 7.0, 3.4, 'Pantalla Ingreso pesaje producción del Sistema de Cátodos: tipo de movimiento, producto, sub-producto, fecha, peso automático, peso tara y peso producción');
  texto(s, [
    { text: 'Lo que llena por cada carga: ', options: { bold: true, color: TEMA.colors.dk1 } },
    { text: 'tipo de movimiento, producto, sub-producto, fecha de pesaje, peso automático (sí o no), peso tara y peso producción. Para cátodos, además grupo, cuba, lado y muestra, y "F" para finalizar el grupo.' },
  ], { x: 0.6, y: 1.65 + h, w: 7.0, h: 1.3, fontSize: 14, color: P.mut });
  const h1 = encaja(s, IMG('sis_productos.jpg'), 7.95, 1.5, 2.3, 2.1, 'Lista de productos del sistema: cátodos, despuntes y láminas, otros pesajes, restos de ánodos y ánodos');
  const h2 = encaja(s, IMG('sis_subproductos.jpg'), 10.4, 1.5, 2.3, 2.1, 'Lista de sub-productos de despuntes y láminas');
  texto(s, 'Producto', { x: 7.95, y: 1.58 + h1, w: 2.3, h: 0.3, fontSize: 12, color: P.mut });
  texto(s, 'Sub-producto (despuntes y láminas)', { x: 10.4, y: 1.58 + h2, w: 2.3, h: 0.5, fontSize: 12, color: P.mut });
  tarjeta(s, 7.95, 4.3, 4.75, 2.35, { fill: { color: P.cobreSuave }, line: { color: TEMA.colors.accent2, width: 1 } });
  texto(s, 'En la propuesta', { x: 8.25, y: 4.5, w: 4.2, h: 0.4, fontFace: CABEZA, fontSize: 17, color: C.accent1 });
  texto(s, 'La pantalla Báscula trae los mismos datos: los llena la etiqueta y el peso estable, y el pesador los puede corregir antes o después de grabar.',
    { x: 8.25, y: 4.95, w: 4.2, h: 1.6, fontSize: 14, color: P.mut });
  s.addNotes('Fotos de la visita del 22-10-2026. Se recortaron la barra de direcciones, la IP y los nombres de usuario.');
}
if (INCLUIR.sistema) {
  const s = lamina('Situación actual', 'CÓMO SE REGISTRA HOY · CONSULTAS', 'Lo que queda en el sistema: detalle, resumen y restos por grupo');
  const h = encaja(s, IMG('sis_detalle.jpg'), 0.6, 1.5, 4.4, 4.75, 'Detalle pesaje de producción del 22-10-2026: grupo, muestra, lado, cuba, peso y hora pesada');
  texto(s, 'Detalle del día: las cubas 01 a 10 quedaron entre las 10:06:40 y las 10:07:42. Se digitan en tanda, así que la hora no es la del pesaje.',
    { x: 0.6, y: 1.6 + h, w: 4.4, h: 0.8, fontSize: 12, color: P.mut });
  const h3 = encaja(s, IMG('sis_resumen.jpg'), 5.35, 1.5, 7.35, 2.75, 'Resumen de pesaje de producción: Lado-P 64.309, Lado-T 100.089, muestra 854, total 165.252 y totales del mes por producto');
  texto(s, 'Resumen: Lado-P 64.309, Lado-T 100.089, muestra 854 y total 165.252 kg, más los totales del mes por producto (18, 48 y 66).',
    { x: 5.35, y: 1.58 + h3, w: 7.35, h: 0.5, fontSize: 12, color: P.mut });
  const y4 = 2.25 + h3;
  encaja(s, IMG('sis_anodos.jpg'), 5.35, y4, 4.3, 6.75 - y4, 'Sistema de Ánodos: producción de restos ánodos corriente, con fecha, grupo, lado y unidades y peso por tipo de ánodo');
  texto(s, [
    { text: 'Sistema de Ánodos', options: { bold: true, color: TEMA.colors.dk1, breakLine: true } },
    { text: 'Producción de restos por grupo: fecha y hora, grupo, lado, unidades y peso por tipo de ánodo, y "Generar hornada". El grupo 21 cerró con 37.961 kg.' },
  ], { x: 9.9, y: y4, w: 2.8, h: 2.3, fontSize: 12, color: P.mut });
}
if (INCLUIR.sistema) {
  const s = lamina('Situación actual', 'CÓMO SE REGISTRA HOY · PLANILLAS', 'Y en paralelo, cuatro planillas del pesador');
  const celdas = [
    ['pla_cuaderno.jpg', 0.6, 1.5, 5.6, 2.75, 'Cuaderno de cátodos: grupo, celdas 1 a 21 (T) y 42 a 22 (P), muestra, totales T, P y M, hora y control de cero.'],
    ['pla_subproductos.jpg', 0.6, 4.92, 5.6, 1.42, 'Subproductos en PM (Excel): D.G, D.O, láminas de rechazo y venta, Cu sucio; cajas y peso por turno A y C.'],
    ['pla_laminas.jpg', 6.55, 1.5, 3.0, 3.6, 'Hoja de láminas despegue (Excel): número, bruto, tara 12, neto y Nº de paquete, en bloques H. INIC., C.O, venta y rechazo S/O.'],
    ['pla_restos.jpg', 9.7, 1.5, 3.0, 3.6, 'Restos de ánodos (Excel): un bloque por grupo, carros 1 a 12 con su neto y el total del grupo.'],
  ];
  celdas.forEach(([f, x, y, w, hm, cap]) => {
    const h = encaja(s, IMG(f), x, y, w, hm, cap);
    texto(s, cap, { x, y: y + h + 0.06, w, h: 0.8, fontSize: 12, color: P.mut });
  });
  s.addNotes('Son los datos que la pantalla Báscula de la VT1200 tiene que dejar ingresar y corregir. Fotos de la visita, recortadas sin nombres.');
}

/* ===================== 5. Un día real ===================== */
if (INCLUIR.diaReal) {
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

/* ===================== 6. Brechas del proceso actual ===================== */
{
  const s = lamina('Situación actual', 'PROCESO ACTUAL', 'Seis brechas de trazabilidad en el pesaje actual');
  const p = [
    ['Identificación manual', 'Rótulo a plumón o tiza: caracteres ambiguos, ocultos o ausentes; reflejos del cobre.'],
    ['Triple digitación', 'El mismo peso se registra en cuaderno, Excel y sistema web.'],
    ['Hora no trazable', 'Las cubas 01 a 10 del 22-10 quedaron con 6 s de diferencia: se digitan en lote.'],
    ['Taras no validadas', 'Marcas de carro y rack ausentes o no auditadas; el descuento se hace con calculadora.'],
    ['Cálculo por diferencia', 'Con dos pallets montados, el neto de cada uno se calcula a mano.'],
    ['Lecturas alteradas', 'Horquilla sobre la plataforma, carro fuera del área o nódulos en la junta.'],
  ];
  p.forEach(([h, b], i) => {
    const x = 0.6 + (i % 3) * 4.15, y = 1.65 + Math.floor(i / 3) * 2.55;
    tarjeta(s, x, y, 3.85, 2.3);
    numero(s, i + 1, x + 0.3, y + 0.3, 0.42);
    texto(s, h, { x: x + 0.9, y: y + 0.33, w: 2.75, h: 0.4, fontFace: CABEZA, fontSize: 18 });
    texto(s, b, { x: x + 0.3, y: y + 0.95, w: 3.3, h: 1.2, fontSize: 14, color: P.mut });
  });
}

/* ===================== 7. Flujo del sistema (diagrama) ===================== */
pres.addSection({ title: 'Solución' });
{
  const s = pres.addSlide({ masterName: 'LIENZO', sectionTitle: 'Solución' });
  const t = tamPng(DIAGRAMA), h = 6.55, w = h * t.w / t.h;
  s.addImage({ path: DIAGRAMA, x: (13.333 - w) / 2, y: 0.32, w, h, altText: 'Diagrama del flujo: origen, traslado, Báscula N9 con visión artificial y baliza, y registro', objectName: 'Diagrama de flujo' });
  s.addNotes('1 a 3 en origen con la VT1200; traslado en horquilla o carro de arrastre; 4 a 6 en la báscula: lectura del QR, validación de posición y cero, captura del peso y neto; 7 a 9 registro único, Excel y pendientes de pesaje. Si la etiqueta no se lee, el sistema alerta y el pesador identifica la carga a mano.');
}

/* ===================== 8. La pantalla en origen ===================== */
{
  const s = lamina('Solución', 'PROGRAMA 1 · ETIQUETADO EN ORIGEN (VT1200)', 'Rotulado de cátodos: solo emite la etiqueta');
  const im = imagen(s, IMG('vt_principal.png'), 0.6, 1.5, { w: 7.2, alt: 'Pantalla principal de la VT1200: etiqueta que sigue, grupo 21 con las celdas 1 a 21 y 22 a 42, y botón Imprimir' });
  const k = im.w / 2048;
  MARCAS_ORIGEN.forEach(([px, py], i) => numero(s, i + 1, 0.6 + px * k - 0.18, 1.5 + py * k - 0.18, 0.36));
  items(s, [
    ['Origen', 'Rotulado de cátodos comerciales; el operador queda identificado por su RUT.'],
    ['Etiqueta que sigue', 'Vista previa exacta de lo que imprime la Zebra.'],
    ['Grupo de producción', 'Celdas 1 a 21 arriba y 22 a 42 abajo; la que sigue va marcada en cobre.'],
    ['Cambiar grupo', 'Abre el layout de circuitos (paso 1) para pasar a otro grupo.'],
    ['Imprimir', 'Un toque por lingada. Registro y Excel siempre disponibles.'],
  ], 8.15, 1.55, 1.02, 3.95);
}

/* ===================== 11. La etiqueta ===================== */
{
  const s = lamina('Solución', 'LA ETIQUETA', 'Rótulo estandarizado: legible a distancia y por la cámara');
  texto(s, '5T21', { x: 0.6, y: 1.5, w: 6.2, h: 1.3, fontFace: CABEZA, fontSize: 80, color: C.text1 });
  const partes = [['5', 'Celda', 'de 1 a 42'], ['T', 'Lado', 'T total · P parcial'], ['21', 'Grupo', 'de la cosecha'], ['39', 'Cátodos', 'por celda']];
  partes.forEach(([v, h, b], i) => {
    const x = 0.6 + i * 1.58;
    tarjeta(s, x, 2.95, 1.45, 1.35);
    texto(s, v, { x: x + 0.18, y: 3.05, w: 1.2, h: 0.5, fontFace: CABEZA, fontSize: 26, color: C.accent1 });
    texto(s, h, { x: x + 0.18, y: 3.55, w: 1.2, h: 0.3, fontSize: 14, bold: true });
    texto(s, b, { x: x + 0.18, y: 3.85, w: 1.25, h: 0.3, fontSize: 11, color: P.mut });
  });
  texto(s, [
    { text: 'Arriba: celda, lado y grupo en caracteres grandes, para lectura a distancia.', options: { bullet: true, breakLine: true } },
    { text: 'Abajo: QR de mayor tamaño para la cámara superior; no depende de reflejos del cobre.', options: { bullet: true, breakLine: true } },
    { text: 'Formato Zebra 100 × 70 mm, impresión térmica en origen.', options: { bullet: true, breakLine: true } },
    { text: 'Si la cámara no lee el QR, el sistema alerta y el pesador identifica la carga.', options: { bullet: true } },
  ], { x: 0.6, y: 4.6, w: 6.2, h: 2.1, fontSize: 15, color: P.mut, paraSpaceAfter: 6 });
  imagen(s, IMG('etq_cat_vt.png'), 7.3, 1.55, { w: 5.4, alt: 'Etiqueta de cátodo 5T21: arriba celda, lado y grupo; abajo el QR y 39 cátodos' });
}

/* ===================== 12. Registro y Excel ===================== */
{
  const s = lamina('Solución', 'REGISTRO Y EXCEL', 'Registro único y trazable, exportable a Excel por turno');
  imagen(s, IMG('vt_registro.png'), 0.6, 1.5, { w: 6.3, alt: 'Registro del día en la VT1200: etiquetas emitidas, estado de pesaje, hora y neto' });
  texto(s, 'Contenido del registro', { x: 7.3, y: 1.5, w: 5.4, h: 0.4, fontFace: CABEZA, fontSize: 18 });
  const hojas = [['Cátodos', 'grupo, cuba, lado, muestra, peso, hora real de pesaje y validación.'],
    ['Pendientes', 'etiquetas emitidas sin pesaje asociado, para seguimiento y cierre del grupo.'],
    ['Resumen', 'columnas Lado-P, Lado-T, Muestra, Tot-peso y Tot-coli.']];
  texto(s, hojas.map(([h, b], i) => [{ text: h + ': ', options: { bold: true, color: TEMA.colors.dk1 } }, { text: b, options: { breakLine: i < hojas.length - 1 } }]).flat(),
    { x: 7.3, y: 2.0, w: 5.4, h: 2.2, fontSize: 14, color: P.mut, paraSpaceAfter: 8 });
  texto(s, 'Resumen del grupo 21 del 22-10, en el formato del Excel', { x: 7.3, y: 4.45, w: 5.4, h: 0.3, fontSize: 13, bold: true });
  const cab = ['Lado-P', 'Lado-T', 'Muestra', 'Tot-peso', 'Tot-coli'].map(t => ({ text: t, options: { bold: true, color: 'FFFFFF', fill: { color: TEMA.colors.dk2 }, align: 'center' } }));
  const fila = [kg(totP), kg(totT), kg(MUESTRA), kg(total), '35'].map(t => ({ text: t, options: { align: 'center' } }));
  s.addTable([cab, fila], { x: 7.3, y: 4.85, w: 5.4, colW: [1.08, 1.08, 1.08, 1.08, 1.08], fontSize: 13, color: TEMA.colors.dk1,
    border: { type: 'solid', pt: 0.75, color: P.linea }, rowH: [0.4, 0.42], valign: 'middle' });
  texto(s, 'Historial auditable: cada fila guarda la hora real en que la báscula capturó el peso.', { x: 7.3, y: 5.85, w: 5.4, h: 0.5, fontSize: 13, color: P.mut });
}

pres.addSection({ title: 'Pesaje' });
/* ===================== 8b. Programa 2: la caseta ===================== */
{
  const s = lamina('Pesaje', 'PROGRAMA 2 · PESAJE EN LA CASETA DE LA BÁSCULA N9', 'Identificación por cámaras, captura del peso y registro');
  const im = imagen(s, IMG('cs_principal.png'), 0.6, 1.5, { w: 7.2, alt: 'Pantalla de la caseta: lectura de la Báscula N9, baliza, cámaras superior y lateral, registro de lo identificado, cargas anunciadas e identificación manual' });
  const k = im.w / 2048;
  MARCAS_CASETA.forEach(([px, py], i) => numero(s, i + 1, 0.6 + px * k - 0.18, 1.5 + py * k - 0.18, 0.36));
  items(s, [
    ['Báscula N9', 'Peso del visor y estado: disponible, registrando, registrado o bloqueado.'],
    ['Baliza', 'La misma luz que ve el operador de la horquilla o del carro.'],
    ['Cámaras', 'Superior: producto y etiqueta. Lateral: posición, lingadas y tara del carro.'],
    ['Registro', 'Grilla con lo identificado: lectura por cámara o manual, bruto, tara, neto y validación.'],
    ['Anunciadas', 'Etiquetas emitidas en origen que todavía no se pesan.'],
    ['Identificación manual', 'Si la cámara no lee la etiqueta, el pesador escribe el rótulo y asocia el peso.'],
  ], 8.15, 1.45, 0.88, 3.95);
  s.addNotes('Es un programa aparte del de la VT1200: se instala en el PC de la caseta. En esta etapa las cámaras y el visor están simulados.');
}

/* ===================== 8c. Programa 2: identificación manual ===================== */
{
  const s = lamina('Pesaje', 'PROGRAMA 2 · IDENTIFICACIÓN MANUAL', 'Si la cámara no lee la etiqueta, el pesador asocia el peso');
  [['cs_manual.png', 'Etiqueta no legible: la báscula retiene el peso, la baliza queda en amarillo y la grilla muestra "Sin identificar".'],
   ['cs_manual_ok.png', 'El pesador escribe el rótulo (9T21) y toca Asociar: queda registrado con lectura "Manual" y la baliza pasa a verde.']]
    .forEach(([f, cap], i) => {
      const x = 0.6 + i * 6.2;
      numero(s, i + 1, x, 1.55, 0.4);
      texto(s, cap, { x: x + 0.55, y: 1.47, w: 5.35, h: 0.75, fontSize: 14, color: P.mut });
      imagen(s, IMG(f), x, 2.35, { w: 5.9, alt: cap });
    });
  texto(s, 'Cada identificación manual queda marcada en el registro y en el Excel, para auditoría.', { x: 0.6, y: 6.85, w: 12.1, h: 0.3, fontSize: 13, bold: true, color: C.accent1 });
}

/* ===================== 9. Control del peso ===================== */
{
  const s = lamina('Pesaje', 'PROGRAMA 2 · CONTROL DEL PESO', 'Captura automática del peso con validación de cada carga');
  const filas = [
    ['Secuencia normal', 1.45, [
      ['bal_1-disponible.png', 'Disponible: plataforma libre, puede ingresar'], ['bal_2-leyendo.png', 'Leyendo etiqueta: cámara superior'],
      ['bal_3-registrando.png', 'Registrando: baliza roja, carga quieta'], ['bal_4-registrado.png', 'Registrado: dentro de rango']]],
    ['Desviaciones y alertas', 4.1, [
      ['bal_5-horquilla.png', 'Horquilla sobre la báscula: +1.850 kg'], ['bal_7-carro-fuera.png', 'Carro fuera del área: −960 kg'],
      ['bal_6-sincero.png', 'Sin cero: nódulos en la junta'], ['bal_8-sin-tara.png', 'Sin tara: carro sin tara validada']]],
  ];
  filas.forEach(([rot, y, imgs]) => {
    texto(s, rot, { x: 0.6, y, w: 12.1, h: 0.32, fontFace: CABEZA, fontSize: 15, color: C.accent1 });
    imgs.forEach(([f, cap], i) => {
      const x = 0.6 + i * 3.07;
      const im = imagen(s, IMG(f), x, y + 0.4, { w: 2.75, alt: cap });
      texto(s, cap, { x, y: y + 0.46 + im.h, w: 2.9, h: 0.3, fontSize: 12, color: P.mut });
    });
  });
  s.addNotes('Es la lectura del programa de la caseta, no la de la VT1200. Rangos: cátodos 4.200 a 5.000 kg; restos 2.800 a 4.000 kg netos. Los valores de error son los de la simulación.');
}

/* ===================== 10. Visión artificial y baliza ===================== */
{
  const s = lamina('Pesaje', 'PROGRAMA 2 · VISIÓN ARTIFICIAL Y BALIZA', 'Dos cámaras y una baliza coordinan la maniobra');
  // Baliza de torre: tapa, tres módulos de luz (rojo, amarillo, verde), poste y base
  const bx = 0.95, bw = 0.62;
  s.addShape(SH.roundRect, { x: bx + 0.06, y: 1.55, w: bw - 0.12, h: 0.16, rectRadius: 0.06, fill: { color: '35505C' }, line: { color: '35505C' }, objectName: 'Baliza: tapa' });
  const luces = [['C9473B', 'Rojo', 'Mantener la carga detenida mientras se captura el peso.'],
    ['D9A21B', 'Amarillo', 'Posicionar la carga o corregir la posición del carro.'],
    ['2F9E5F', 'Verde', 'Plataforma disponible: autoriza el ingreso. Al cerrar el registro, autoriza el retiro.']];
  luces.forEach(([c, h, b], i) => {
    const y = 1.73 + i * 0.86;
    s.addShape(SH.rect, { x: bx, y, w: bw, h: 0.82, fill: { color: c }, line: { color: c }, objectName: `Baliza: luz ${h.toLowerCase()}` });
    s.addShape(SH.rect, { x: bx, y: y + 0.8, w: bw, h: 0.04, fill: { color: '35505C' }, line: { color: '35505C' }, objectName: 'Baliza: separación' });
    texto(s, h, { x: 2.1, y: y + 0.02, w: 3.9, h: 0.34, fontFace: CABEZA, fontSize: 17 });
    texto(s, b, { x: 2.1, y: y + 0.36, w: 3.9, h: 0.5, fontSize: 13, color: P.mut });
  });
  s.addShape(SH.rect, { x: bx + bw / 2 - 0.07, y: 4.31, w: 0.14, h: 0.5, fill: { color: '35505C' }, line: { color: '35505C' }, objectName: 'Baliza: poste' });
  s.addShape(SH.roundRect, { x: bx - 0.08, y: 4.8, w: bw + 0.16, h: 0.12, rectRadius: 0.04, fill: { color: '35505C' }, line: { color: '35505C' }, objectName: 'Baliza: base' });
  texto(s, 'La baliza queda a la vista del operador de la horquilla o del carro.', { x: 0.6, y: 5.3, w: 5.4, h: 0.5, fontSize: 13, bold: true, color: C.accent1 });
  // Cámaras y visor
  const cards = [
    ['Cámara superior', 'Detecta la llegada de cobre, identifica el producto y lee el QR de la etiqueta.'],
    ['Cámara lateral', 'Verifica la posición de la carga o del carro, distingue si vienen 1 o 2 lingadas y lee las marcas de tara de carro y rack. Alerta si detecta un ánodo en la grilla abierta.'],
    ['Visor de la báscula', 'Entrega el peso estable al sistema. Con la plataforma vacía y lectura distinta de cero, alerta por nódulos.'],
  ];
  cards.forEach(([h, b], i) => {
    const y = 1.6 + i * 1.72;
    tarjeta(s, 6.5, y, 6.2, 1.55);
    texto(s, h, { x: 6.8, y: y + 0.18, w: 5.6, h: 0.4, fontFace: CABEZA, fontSize: 17 });
    texto(s, b, { x: 6.8, y: y + 0.6, w: 5.6, h: 0.9, fontSize: 13, color: P.mut });
  });
  texto(s, 'Ubicación de cámaras, iluminación (día y noche) y secuencia de la baliza se validan en terreno y con los procedimientos de seguridad de Codelco.',
    { x: 0.6, y: 6.55, w: 12.1, h: 0.35, fontSize: 12, color: P.mut });
}

/* ===================== 13. Demo ===================== */
pres.addSection({ title: 'Cierre' });
{
  const s = lamina('Cierre', 'PRUÉBELO', 'Dos programas: etiquetado y pesaje');
  imagen(s, IMG('app_impresa.jpg'), 0.6, 1.45, { w: 6.9, alt: 'Captura de TerminalVentanas.exe: etiqueta 5T21 impresa y el grupo 21' });
  texto(s, 'Captura de TerminalVentanas.exe, la app que se instala en la VT1200.', { x: 0.6, y: 6.7, w: 6.9, h: 0.25, fontSize: 11, color: P.mut });
  const bloques = [['Programa 1 · Etiquetado (VT1200)', 'TerminalVentanas.exe: selección de grupo en el layout, emisión de la etiqueta en la Zebra, registro y Excel.'],
    ['Programa 2 · Pesaje (caseta)', 'BasculaCaseta.exe: cámaras, baliza, captura del peso, registro de lo identificado e identificación manual.'],
    ['Instalación', 'Aplicaciones Windows autónomas (.NET 8): sin navegador ni internet; se instalan copiando una carpeta. Cámaras y visor simulados en esta etapa.']];
  bloques.forEach(([h, b], i) => {
    const y = 1.55 + i * 1.7;
    texto(s, h, { x: 7.95, y, w: 4.75, h: 0.4, fontFace: CABEZA, fontSize: 18 });
    texto(s, b, { x: 7.95, y: y + 0.45, w: 4.75, h: 1.15, fontSize: 14, color: P.mut });
  });
}

/* ===================== 14. Supuestos ===================== */
{
  const s = lamina('Cierre', 'SUPUESTOS', 'Supuestos y puntos por validar');
  const listas = [
    ['Supuestos de esta propuesta', [
      'Etiqueta emitida en origen por lingada de 39 cátodos; no lleva peso.',
      'El pesador supervisa desde la caseta, en su PC.',
      'Rangos de la visita: cátodos 4.200 a 5.000 kg, restos 2.800 a 4.000 kg, láminas aprobadas 3.200 a 3.900 kg.',
      'Taras de carro y rack de ejemplo, hasta contar con la base validada.']],
    ['Por validar con Codelco', [
      'Salida de datos del visor hacia el sistema.',
      'Ubicación, óptica e iluminación de las dos cámaras (día y noche).',
      'Lógica de la baliza según los procedimientos de seguridad.',
      'Ubicación estandarizada de las marcas de tara de carro y rack.',
      'Cátodos EW: hoy sin rótulo.',
      'Láminas rechazo C/O y rangos de C.O y rechazo S/O.']],
  ];
  listas.forEach(([h, it], i) => {
    const x = 0.6 + i * 6.15;
    tarjeta(s, x, 1.6, 5.95, 5.1, i === 1 ? { fill: { color: P.cobreSuave }, line: { color: TEMA.colors.accent2, width: 1 } } : undefined);
    texto(s, h, { x: x + 0.4, y: 1.9, w: 5.2, h: 0.45, fontFace: CABEZA, fontSize: 20 });
    texto(s, it.map((t, j) => ({ text: t, options: { bullet: true, breakLine: j < it.length - 1 } })),
      { x: x + 0.4, y: 2.5, w: 5.2, h: 4.0, fontSize: 15, color: P.mut, paraSpaceAfter: 10 });
  });
}

/* ===================== 15. Próximos pasos ===================== */
{
  const s = lamina('Cierre', 'PRÓXIMOS PASOS', 'Cómo seguimos', 'CIERRE');
  const pasos = [
    ['Validar', 'Revisar flujo y pantallas con operadores, pesador y supervisión.'],
    ['Taras', 'Base validada de carros, racks y pallets, con código único por estructura.'],
    ['Terreno', 'Salida de datos del visor, ubicación de cámaras y baliza, pruebas día y noche.'],
    ['PoC', 'Prueba de concepto en la Báscula N9 con cátodos comerciales.'],
    ['Ampliar', 'Láminas y restos de ánodos, e integración con los sistemas de Codelco.'],
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
