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
// Dos juegos de capturas: img/ (programas en el tema oscuro, el original) e img_claro/ (en el tema claro).
// Se elige con IMG_DIR; SALIDA_PPT cambia el archivo que sale. Sin variables: oscuro, a presentacion/Bascula-N9-Ventanas.pptx.
const CARPETA_IMG = process.env.IMG_DIR || 'img';
const IMG = f => path.join(AQUI, CARPETA_IMG, f);
const SALIDA = process.env.SALIDA_PPT || path.join(AQUI, '..', 'Bascula-N9-Ventanas.pptx');

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

/* ---------- Diagrama de flujo con formas de PowerPoint (editable) ----------
   Es el mismo dibujo de diagramas/generar_diagrama.py (lienzo de 1600 x 1000 px), pero hecho con cajas, rombos,
   líneas y textos de PowerPoint: cada palabra se corrige con doble clic y cada caja se puede mover o recolorear.
   Las coordenadas se copian tal cual del script del SVG y se llevan a pulgadas con DG (no cambiarlas a mano en un
   solo lado: si cambia el flujo, cambiar los dos). */
const DG = { x0: 1.31, y0: 0.27, k: 0.00671 }; // 1000 px de alto = 6,71 pulgadas, centrado en la lámina
const gx = v => DG.x0 + v * DG.k, gy = v => DG.y0 + v * DG.k, gl = v => v * DG.k, gpt = v => +(v * DG.k * 72).toFixed(1);
const GC = { ink: '1A2126', txt: '46545C', mut: '6B7A82', line: 'C9D3D8', flecha: '5A6B73', cobre: 'A4561F', cobre2: 'DA8B4F',
  carril: 'F3F6F7', carril2: 'FAFBFB', ambBg: 'FFF6DA', amb: 'B8860B', rojBg: 'FDECEA', roj: 'B23A2E', verBg: 'E8F6EE', ver: '2F7D4F' };
const GF = 'Segoe UI';
function diagramaNativo(s) {
  let n = 0;
  const nombre = t => `Diagrama · ${String(++n).padStart(2, '0')} ${t}`;
  // Texto con la línea base del SVG en (x, y): la caja empieza un poco más arriba (el alto de las mayúsculas)
  function txt(x, y, t, size, o = {}) {
    const ancho = o.w || 900, alin = o.align || 'left';
    const left = alin === 'center' ? x - ancho / 2 : alin === 'right' ? x - ancho : x;
    s.addText(t, Object.assign({ x: gx(left), y: gy(y - size * 0.98), w: gl(ancho), h: gl(size * 1.32), fontFace: GF, fontSize: gpt(size), color: GC.txt,
      margin: 0, valign: 'top', align: alin, fit: 'none', objectName: nombre(String(t).slice(0, 30)) }, o.opt || {}));
  }
  function caja(x, y, w, h, titulo, cuerpo, num, bg, borde, colorT) {
    s.addShape(SH.roundRect, { x: gx(x), y: gy(y), w: gl(w), h: gl(h), rectRadius: gl(12), fill: { color: bg || 'FFFFFF' },
      line: { color: borde || GC.line, width: 1 }, objectName: nombre('caja ' + titulo) });
    let tx = x + 18;
    if (num) {
      s.addText(num, { shape: SH.ellipse, x: gx(x + 13), y: gy(y + 14), w: gl(26), h: gl(26), fill: { color: GC.cobre }, line: { color: GC.cobre },
        fontFace: GF, fontSize: gpt(14), bold: true, color: 'FFFFFF', align: 'center', valign: 'middle', margin: 0, objectName: nombre('número ' + num) });
      tx = x + 48;
    }
    txt(tx, y + 32, titulo, 17, { w: x + w - 6 - tx, opt: { bold: true, color: colorT || GC.ink, wrap: false } });
    // El cuerpo: un solo cuadro que PowerPoint ajusta al ancho (interlineado de 19 px como en el SVG)
    s.addText(cuerpo, { x: gx(x + 18), y: gy(y + 58 - 14), w: gl(w - 36), h: gl(h - 52), fontFace: GF, fontSize: gpt(14), color: GC.txt,
      margin: 0, valign: 'top', lineSpacing: gpt(19), fit: 'none', objectName: nombre('texto ' + titulo) });
  }
  // El rombo y su texto van separados: dentro de un rombo PowerPoint deja poco ancho y la pregunta se partía en tres líneas
  function rombo(cx, cy, w, h, l1, l2) {
    s.addShape(SH.diamond, { x: gx(cx - w / 2), y: gy(cy - h / 2), w: gl(w), h: gl(h), fill: { color: 'FFF4E8' },
      line: { color: GC.cobre2, width: 1.25 }, objectName: nombre('rombo ' + l1) });
    s.addText(l2 ? `${l1}\n${l2}` : l1, { x: gx(cx - w * 0.36), y: gy(cy - 22), w: gl(w * 0.72), h: gl(44), fontFace: GF, fontSize: gpt(14.5), bold: true,
      color: GC.ink, align: 'center', valign: 'middle', margin: 0, lineSpacing: gpt(19), fit: 'none', objectName: nombre('pregunta ' + l1) });
  }
  // Flecha por tramos rectos (horizontales o verticales); la punta va en el último tramo
  function flecha(pts, etiqueta, pos, color, guion) {
    const c = color || GC.flecha;
    for (let i = 0; i < pts.length - 1; i++) {
      const [x1, y1] = pts[i], [x2, y2] = pts[i + 1];
      const ultimo = i === pts.length - 2;
      s.addShape(SH.line, { x: gx(Math.min(x1, x2)), y: gy(Math.min(y1, y2)), w: gl(Math.abs(x2 - x1)), h: gl(Math.abs(y2 - y1)),
        flipH: x2 < x1, flipV: y2 < y1,
        line: Object.assign({ color: c, width: 1.25 }, guion ? { dashType: 'dash' } : {}, ultimo ? { endArrowType: 'triangle' } : {}),
        objectName: nombre('flecha') });
    }
    if (etiqueta) {
      const [ex, ey] = pos;
      s.addText(etiqueta, { shape: SH.roundRect, x: gx(ex - 22), y: gy(ey - 14), w: gl(44), h: gl(20), rectRadius: gl(10), fill: { color: 'FFFFFF' },
        line: { color: 'FFFFFF', width: 0 }, fontFace: GF, fontSize: gpt(13), bold: true, color: c, align: 'center', valign: 'middle', margin: 0,
        objectName: nombre('rótulo ' + etiqueta) });
    }
  }
  function baliza(x, y, color, t) {
    s.addShape(SH.ellipse, { x: gx(x - 7), y: gy(y - 7), w: gl(14), h: gl(14), fill: { color }, line: { color }, objectName: nombre('luz de la baliza') });
    txt(x + 14, y + 5, t, 13.5, { w: 380, opt: { bold: true } });
  }

  // ---------- Título y carriles ----------
  txt(40, 52, 'Flujo del sistema de trazabilidad y pesaje · Báscula N9', 28, { w: 1500, opt: { bold: true, color: GC.ink } });
  txt(40, 82, 'Identificación estandarizada en origen, lectura por visión artificial y captura automática del peso en un registro único, con la hora real.', 16, { w: 1500, opt: { color: GC.mut } });
  [[30, 305, '1 · Origen', 'Cosecha de cátodos en la nave'], [345, 255, '2 · Traslado', 'Horquilla o carro de arrastre'],
   [615, 640, '3 · Báscula N9', 'Visor, 4 celdas de carga, 2 cámaras y baliza'], [1270, 300, '4 · Registro', 'Registro único y seguimiento']]
    .forEach(([x, w, t, sub], i) => {
      s.addShape(SH.roundRect, { x: gx(x), y: gy(110), w: gl(w), h: gl(820), rectRadius: gl(16), fill: { color: i % 2 === 0 ? GC.carril : GC.carril2 },
        line: { color: 'E3E9EC', width: 0.75 }, objectName: nombre('carril ' + t) });
      txt(x + 20, 142, t.toUpperCase(), 15, { w: w - 30, opt: { bold: true, color: GC.cobre } });
      txt(x + 20, 163, sub, 13.5, { w: w - 30, opt: { color: GC.mut } });
    });

  // ---------- 1 · Origen ----------
  caja(50, 190, 270, 170, 'Selecciona grupo y celda', 'Paso 1: grupo en el layout de circuitos. Paso 2: el sistema propone la celda que sigue (celda + lado + grupo).', '1');
  caja(50, 400, 270, 130, 'Emite la etiqueta', 'Rótulo estandarizado (5T21) con QR y 39 cátodos. Queda pendiente de pesaje.', '2');
  caja(50, 570, 270, 130, 'Fija la etiqueta', 'Sobre el paquete, visible para la cámara superior.', '3');
  flecha([[185, 360], [185, 398]]);
  flecha([[185, 530], [185, 568]]);

  // ---------- 2 · Traslado ----------
  caja(362, 400, 220, 130, 'Horquilla', 'Paquetes de cátodos rotulados, directo a la plataforma.');
  caja(362, 570, 220, 130, 'Carro de arrastre', 'Rack con restos de ánodos.');
  flecha([[320, 635], [360, 635]]);
  flecha([[320, 635], [340, 635], [340, 465], [360, 465]]);

  // ---------- 3 · Báscula N9 ----------
  const cx = 845;
  caja(705, 190, 280, 112, 'Ingreso a la plataforma', 'Baliza verde. La cámara superior lee el QR; la lateral, la posición.', '4');
  flecha([[582, 465], [650, 465], [650, 246], [703, 246]]);
  flecha([[582, 635], [650, 635], [650, 465]]);
  rombo(cx, 372, 250, 92, '¿Etiqueta leída?');
  flecha([[cx, 302], [cx, 324]]);
  caja(1030, 336, 205, 98, 'Alerta: lectura manual', 'El pesador identifica la carga a mano.', null, GC.ambBg, GC.amb, '7A5A00');
  flecha([[cx + 125, 372], [1028, 372]], 'No', [998, 362]);
  flecha([[1132, 434], [1132, 452], [cx + 30, 452], [cx + 30, 470]], null, null, null, true);
  rombo(cx, 520, 300, 104, '¿Carga en el área, sin', 'vehículo y visor en cero?');
  flecha([[cx, 418], [cx, 466]], 'Sí', [cx - 26, 444]);
  caja(1030, 478, 205, 112, 'Bloqueo', 'No captura el peso hasta corregir. Baliza amarilla.', null, GC.rojBg, GC.roj, GC.roj);
  flecha([[cx + 150, 520], [1028, 520]], 'No', [1000, 510]);
  flecha([[1080, 590], [1080, 612], [cx + 60, 612], [cx + 60, 560]], null, null, GC.roj, true);
  caja(705, 640, 280, 96, 'Captura del peso', 'Baliza roja: mantener la carga. El visor entrega el peso estable.', '5');
  flecha([[cx, 572], [cx, 638]], 'Sí', [cx - 26, 600]);
  caja(705, 760, 280, 112, 'Peso neto', 'Descuenta las taras validadas (carro + rack, leídas por la cámara lateral).', '6');
  flecha([[cx, 736], [cx, 758]]);
  rombo(1110, 816, 230, 92, '¿Dentro del', 'rango esperado?');
  flecha([[985, 816], [993, 816]]);

  // ---------- 4 · Registro ----------
  caja(1290, 190, 260, 150, 'Registro único', 'Identificación, peso bruto y neto, hora real y validaciones. Sin digitación. Baliza verde: retirar.', '7');
  caja(1290, 380, 260, 150, 'Excel por turno', 'Hoja de cátodos y Resumen con las columnas Lado-P, Lado-T, Muestra y totales.', '8');
  caja(1290, 570, 260, 150, 'Pendientes de pesaje', 'Etiquetas emitidas sin peso asociado: alerta para seguimiento y cierre del grupo.', '9');
  flecha([[1420, 340], [1420, 378]]);
  flecha([[1420, 530], [1420, 568]]);
  flecha([[1225, 816], [1250, 816], [1250, 265], [1288, 265]], 'Sí', [1250, 300], GC.ver);
  caja(1290, 760, 260, 112, 'Registro con observación', 'Peso fuera de rango: queda marcado para revisión del pesador.', null, GC.rojBg, GC.roj, GC.roj);
  flecha([[1110, 862], [1110, 900], [1420, 900], [1420, 874]], 'No', [1180, 900], GC.roj);
  flecha([[1550, 816], [1575, 816], [1575, 265], [1552, 265]], null, null, GC.roj, true);

  // ---------- Baliza ----------
  txt(40, 965, 'Baliza:', 13.5, { w: 60, opt: { bold: true, color: GC.ink } });
  baliza(100, 960, '2F9E5F', 'Verde: plataforma disponible, ingresar o retirar');
  baliza(500, 960, 'D9A21B', 'Amarillo: posicionar la carga o lectura manual');
  baliza(870, 960, 'C9473B', 'Rojo: mantener la carga mientras se captura el peso');
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
  imagen(s, IMG('vt_principal.png'), 6.85, 1.2, { w: 5.9, alt: 'Pantalla de etiquetado en origen: etiqueta 5T21 que sigue y el grupo 21 con sus celdas' });
  texto(s, 'Etiquetado en origen: etiqueta que sigue y grupo de producción.', { x: 6.85, y: 5.75, w: 5.9, h: 0.3, fontSize: 11, color: P.claroMut });
  s.addNotes('Sistema de trazabilidad y pesaje para la Báscula N9. En esta etapa el programa de etiquetado en origen rotula cátodos comerciales; la báscula identifica y pesa con visión artificial.');
}

/* ===================== 2. Paso 1: selección de cuba en el layout ===================== */
pres.addSection({ title: 'Propuesta' });
{
  const s = lamina('Propuesta', 'PASO 1 · SELECCIÓN DE CUBA', 'El operador selecciona el grupo en el layout de circuitos');
  const x0 = 0.6, y0 = 1.5, im = imagen(s, IMG('vt_paso1.png'), x0, y0, { w: 7.3, alt: 'Paso 1: layout de la nave con los circuitos I a VI, sus grupos y EW; el grupo 21 seleccionado' });
  const k = im.w / 2048;
  puntero(s, x0, y0, k, 700, 668, [586, 614, 781, 697]);
  items(s, [
    ['Layout de la nave', 'Circuitos I a VI con sus grupos, en la misma disposición del plano de la refinería.'],
    ['Primera pantalla', 'Al ingresar, el operador toca el grupo a cosechar (en el ejemplo, grupo 21) y pasa a la pantalla de producción.'],
    ['Restricciones', 'Solo se rotulan cátodos: los grupos 1 y 2 quedan sin rotulado. EW queda por confirmar.'],
    ['Cierre del grupo', 'Desde "Cambiar grupo", "Terminar grupo" informa a la báscula que no vienen más paquetes de ese grupo.'],
  ], 8.3, 1.6, 1.25, 4.1);
  s.addNotes('Es parte del programa de etiquetado: la primera ventana al ingresar y al cambiar de grupo. Se toca el grupo en el layout; el puntero marca la selección.');
}

/* ===================== 3. Resumen ===================== */
{
  const s = lamina('Propuesta', 'RESUMEN', 'Un registro único por paquete, sin digitación manual del peso');
  const cols = [
    ['1 etiqueta', 'En origen', 'El programa de etiquetado emite una etiqueta estandarizada por paquete (celda, lado, grupo, 39 cátodos y QR) y la deja registrada como pendiente de pesaje.'],
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
  const s = lamina('Propuesta', 'PASO 2 · SELECCIÓN GRUPO DE PRODUCCIÓN', 'El programa propone la celda que sigue y emite la etiqueta');
  const x0 = 0.6, y0 = 1.5, im = imagen(s, IMG('vt_paso2.png'), x0, y0, { w: 7.3, alt: 'Paso 2: grupo 22 con las celdas 1 a 21 arriba y 22 a 42 abajo; celda 1 seleccionada y botón Imprimir celda 1' });
  const k = im.w / 2048;
  puntero(s, x0, y0, k, 128, 960);
  items(s, [
    ['Un grupo a la vista', 'El grupo seleccionado en grande: arriba la fila de celdas 1 a 21 y abajo la de 22 a 42.'],
    ['Celda que sigue', 'Marcada en cobre según el orden de cosecha. Un toque cambia de celda; otro grupo, con Cambiar grupo.'],
    ['Lado total o parcial', 'Se marca con el botón sobre cada fila (Lado total / Lado parcial). La letra T o P entra en el rótulo.'],
    ['Emisión', 'Imprimir envía la etiqueta (1T22 o 1P22) a la Zebra y la deja pendiente de pesaje.'],
  ], 8.3, 1.6, 1.25, 4.1);
}

/* ===================== 11. La etiqueta ===================== */
{
  const s = lamina('Propuesta', 'LA ETIQUETA', 'Rótulo estandarizado: legible a distancia y por la cámara');
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

/* ===================== 5. Tres productos ===================== */
// Benjamin la borró del PPT el 16-11: queda apagada. NO volver a subirla.
const INCLUIR_PRODUCTOS = false;
pres.addSection({ title: 'Situación actual' });
if (INCLUIR_PRODUCTOS) {
  const s = lamina('Situación actual', 'PRODUCTOS DE LA BÁSCULA N9', 'Tres productos con identificación y tara distintas');
  const prods = [
    ['etq_cat_vt.png', 'Cátodos comerciales', ['Paquete de 39 cátodos por celda, de 4,3 a 4,9 t.', 'Ingresa con horquilla; sin tara.', 'Rótulo: celda + lado + grupo (12P31).']],
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
  texto(s, 'Alcance de esta etapa: el programa de etiquetado rotula cátodos comerciales. Láminas y restos de ánodos se incorporan en la ampliación.',
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
  s.addNotes('Son los datos que la pantalla de la báscula tiene que dejar ingresar y corregir. Fotos de la visita, recortadas sin nombres.');
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
  diagramaNativo(s);
  s.addNotes('1 a 3 en origen con el programa de etiquetado; traslado en horquilla o carro de arrastre; 4 a 6 en la báscula: lectura del QR, validación de posición y cero, captura del peso y neto; 7 a 9 registro único, Excel y pendientes de pesaje. Si la etiqueta no se lee, el sistema alerta y el pesador identifica la carga a mano.');
}

/* ===================== 8. La pantalla en origen ===================== */
{
  const s = lamina('Solución', 'PROGRAMA 1 · ETIQUETADO EN ORIGEN', 'Rotulado de cátodos: solo emite la etiqueta');
  const im = imagen(s, IMG('vt_principal.png'), 0.6, 1.5, { w: 7.2, alt: 'Pantalla principal del programa de etiquetado: etiqueta que sigue, grupo 21 con las celdas 1 a 21 y 22 a 42, y botón Imprimir' });
  const k = im.w / 2048;
  MARCAS_ORIGEN.forEach(([px, py], i) => numero(s, i + 1, 0.6 + px * k - 0.18, 1.5 + py * k - 0.18, 0.36));
  items(s, [
    ['Origen', 'Rotulado de cátodos comerciales; el operador queda identificado por su RUT.'],
    ['Etiqueta que sigue', 'Vista previa exacta de lo que imprime la Zebra.'],
    ['Grupo de producción', 'Celdas 1 a 21 arriba y 22 a 42 abajo; la que sigue va marcada en cobre.'],
    ['Cambiar grupo', 'Abre el layout de circuitos (paso 1) para pasar a otro grupo.'],
    ['Imprimir', 'Un toque por paquete. Registro y Excel siempre disponibles.'],
  ], 8.15, 1.55, 1.02, 3.95);
}

/* ===================== 12. Registro y Excel ===================== */
{
  const s = lamina('Solución', 'REGISTRO Y EXCEL', 'Registro único y trazable, exportable a Excel por turno');
  imagen(s, IMG('vt_registro.png'), 0.6, 1.5, { w: 6.3, alt: 'Registro del día en el programa de etiquetado: etiquetas emitidas, estado de pesaje, hora y neto' });
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
/* ===================== 8a. Introducción al sistema de pesaje ===================== */
{
  const s = lamina('Pesaje', 'PROGRAMA 2 · SISTEMA DE PESAJE', 'Un segundo sistema en la caseta de la Báscula N9');
  texto(s, 'El programa de etiquetado deja cada paquete rotulado en origen. El sistema de pesaje la recibe en la Báscula N9: la identifica, captura su peso y cierra el registro, sin digitación manual.',
    { x: 0.6, y: 1.5, w: 12.1, h: 0.7, fontSize: 16, color: P.mut });
  const pasos = [
    ['Ingreso', 'La horquilla deja el paquete en la plataforma. La baliza indica cuándo ingresar.'],
    ['Identificación', 'La cámara superior lee el QR y lo cruza con las etiquetas anunciadas desde origen.'],
    ['Pesaje', 'Con la carga quieta y la posición validada por la cámara lateral, toma el peso estable del visor.'],
    ['Registro', 'Guarda identificación, peso, hora real y validación. La baliza autoriza el retiro.'],
  ];
  const w = 2.75, g = 0.37;
  pasos.forEach(([h, b], i) => {
    const x = 0.6 + i * (w + g), y = 2.45;
    tarjeta(s, x, y, w, 2.45, { objectName: `Paso ${i + 1}: ${h}` });
    numero(s, i + 1, x + 0.25, y + 0.25, 0.45);
    texto(s, h, { x: x + 0.25, y: y + 0.85, w: w - 0.5, h: 0.4, fontFace: CABEZA, fontSize: 18 });
    texto(s, b, { x: x + 0.25, y: y + 1.3, w: w - 0.45, h: 1.05, fontSize: 13, color: P.mut });
    if (i < pasos.length - 1) s.addShape(SH.rightArrow, { x: x + w + 0.06, y: y + 1.05, w: g - 0.12, h: 0.32, fill: { color: TEMA.colors.accent2 }, line: { color: TEMA.colors.accent2 }, objectName: 'Flecha' });
  });
  texto(s, 'Lo componen', { x: 0.6, y: 5.2, w: 12.1, h: 0.35, fontFace: CABEZA, fontSize: 16, color: C.accent1 });
  const partes = ['Visor de la báscula', 'Cámara superior', 'Cámara lateral', 'Baliza', 'PC de la caseta · BasculaCaseta.exe'];
  const anchos = [2.1, 1.9, 1.9, 1.3, 3.7]; let x = 0.6;
  partes.forEach((t, i) => {
    s.addText(t, { shape: SH.roundRect, rectRadius: 0.08, x, y: 5.62, w: anchos[i], h: 0.48, fontSize: 13, bold: true, color: TEMA.colors.dk1, align: 'center', valign: 'middle',
      fill: { color: P.cobreSuave }, line: { color: TEMA.colors.accent2, width: 0.75 }, margin: 0, isTextBox: true, objectName: `Componente: ${t}` });
    x += anchos[i] + 0.2;
  });
  texto(s, 'Si la etiqueta no se lee, el sistema alerta y el pesador identifica la carga a mano.', { x: 0.6, y: 6.35, w: 12.1, h: 0.35, fontSize: 13, color: P.mut });
  s.addNotes('Introduce el segundo programa: es aparte del de etiquetado y se instala en el PC de la caseta. En esta etapa las cámaras y el visor están simulados.');
}
/* ===================== 8b. Programa 2: la caseta ===================== */
{
  const s = lamina('Pesaje', 'PROGRAMA 2 · PESAJE EN LA CASETA DE LA BÁSCULA N9', 'Identificación por cámaras, captura del peso y registro');
  const im = imagen(s, IMG('cs_principal.png'), 0.6, 1.5, { w: 7.2, alt: 'Pantalla de la caseta: lectura de la Báscula N9, baliza, cámaras superior y lateral, registro de lo identificado, cargas anunciadas e identificación manual' });
  const k = im.w / 2048;
  MARCAS_CASETA.forEach(([px, py], i) => numero(s, i + 1, 0.6 + px * k - 0.18, 1.5 + py * k - 0.18, 0.36));
  items(s, [
    ['Báscula N9', 'Peso del visor y estado: disponible, registrando, registrado o bloqueado.'],
    ['Baliza', 'La misma luz que ve el operador de la horquilla o del carro.'],
    ['Cámaras', 'Superior: producto y etiqueta. Lateral: posición, paquetes y tara del carro.'],
    ['Registro', 'Grilla con lo identificado: lectura por cámara o manual, bruto, tara, neto y validación.'],
    ['Anunciadas', 'Etiquetas emitidas en origen que todavía no se pesan.'],
    ['Identificación manual', 'Si la cámara no lee la etiqueta, el pesador escribe el rótulo y asocia el peso.'],
  ], 8.15, 1.45, 0.88, 3.95);
  s.addNotes('Es un programa aparte del de etiquetado: se instala en el PC de la caseta. En esta etapa las cámaras y el visor están simulados.');
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
  s.addNotes('Es la lectura del programa de la caseta, no la del etiquetado. Rangos: cátodos 4.200 a 5.000 kg; restos 2.800 a 4.000 kg netos. Los valores de error son los de la simulación.');
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
    ['Cámara lateral', 'Verifica la posición de la carga o del carro, distingue si vienen 1 o 2 paquetes y lee las marcas de tara de carro y rack. Alerta si detecta un ánodo en la grilla abierta.'],
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

/* ===================== 10b. Cómo funcionan las cámaras ===================== */
// Cuatro láminas: dónde va cada cámara (esquema con formas de PowerPoint, editable), qué hace cada una en cada momento (tabla editable)
// y cómo lo muestra el programa de la caseta (recortes de la prueba: cam_*.png, los deja preparar_capturas.py).
// Lo que dicen sale de la minuta (dos vistas, baliza, taras de carro y rack) y de lo que el programa ya muestra
// (Caseta.cs: CamSuperior, CamLateral y EstadoBaliza). No se inventan modelos ni distancias: la ubicación se valida en terreno.
const CAM = { piso: '8A979E', plataforma: '35505C', cobre: 'C9733A', cobreOsc: '8E4A1C', cuerpo: '26343B', lente: '0E181E', cono: 'DA8B4F',
  cable: '8A979E', rojo: 'C9473B', amarillo: 'D9A21B', verde: '2F9E5F' };
{
  const s = lamina('Pesaje', 'CÁMARAS · DÓNDE VAN Y QUÉ MIRA CADA UNA', 'Dos vistas sobre la plataforma: desde arriba y desde el costado');
  const N = t => 'Esquema cámaras · ' + t;
  const rect = (x, y, w, h, color, t, forma) => s.addShape(forma || SH.rect, { x, y, w, h, rectRadius: 0.05, fill: { color }, line: { color }, objectName: N(t) });
  const linea = (x1, y1, x2, y2, color, t, o) => s.addShape(SH.line, { x: Math.min(x1, x2), y: Math.min(y1, y2), w: Math.abs(x2 - x1), h: Math.abs(y2 - y1),
    flipH: x2 < x1, flipV: y2 < y1, line: Object.assign({ color, width: 1.25 }, o || {}), objectName: N(t) });
  const rotulo = (t, x, y, w, h, o) => texto(s, t, Object.assign({ x, y, w, h, fontSize: 11, color: P.mut, objectName: N(t.slice(0, 28)) }, o || {}));
  const cono = (x, y, w, h, puntos, t) => s.addShape(SH.custGeom, { x, y, w, h, points: puntos.map(([px, py]) => ({ x: px, y: py })).concat([{ close: true }]),
    fill: { color: CAM.cono, transparency: 72 }, line: { color: CAM.cono, width: 1, dashType: 'dash' }, objectName: N(t) });

  // Piso, plataforma y el paquete con su etiqueta arriba, en la esquina (lo que pide la VT1200 al imprimir)
  linea(0.7, 5.7, 7.55, 5.7, CAM.piso, 'piso', { width: 1.5 });
  rect(2.6, 5.45, 3.0, 0.25, CAM.plataforma, 'plataforma');
  rect(3.2, 4.35, 1.8, 1.1, CAM.cobre, 'paquete de cátodos');
  [4.57, 4.79, 5.01, 5.23].forEach(y => linea(3.2, y, 5.0, y, CAM.cobreOsc, 'cátodo', { width: 0.75 }));
  rotulo('Paquete de cátodos', 3.2, 4.78, 1.8, 0.25, { align: 'center', bold: true, color: 'FFFFFF', fontSize: 12 });
  s.addShape(SH.rect, { x: 3.28, y: 4.24, w: 0.42, h: 0.11, fill: { color: 'FFFFFF' }, line: { color: CAM.cuerpo, width: 0.75 }, objectName: N('etiqueta') });
  rotulo('Etiqueta con QR', 1.8, 4.45, 1.25, 0.24, { align: 'right', bold: true, color: TEMA.colors.dk1 });
  linea(3.08, 4.57, 3.36, 4.35, CAM.cuerpo, 'guía etiqueta', { width: 0.75 });
  rotulo('Plataforma de la báscula · 4 celdas de carga', 2.6, 5.78, 3.0, 0.25, { align: 'center' });
  linea(0.8, 6.2, 2.4, 6.2, TEMA.colors.accent1, 'ingreso', { width: 2, endArrowType: 'triangle' });
  rotulo('Ingresa la horquilla o el carro', 2.5, 6.08, 3.0, 0.25, { color: TEMA.colors.accent1, bold: true });

  // Cámara superior: en un brazo sobre la plataforma, mirando hacia abajo a la cara de arriba de la carga
  rect(1.0, 1.85, 0.08, 3.85, CAM.plataforma, 'poste cámara superior');
  rect(1.0, 1.85, 3.3, 0.08, CAM.plataforma, 'brazo cámara superior');
  rect(3.82, 1.93, 0.56, 0.28, CAM.cuerpo, 'cámara superior', SH.roundRect);
  rect(4.02, 2.21, 0.16, 0.08, CAM.lente, 'lente cámara superior');
  cono(3.1, 2.29, 2.0, 2.0, [[1.0, 0], [2.0, 2.0], [0, 2.0]], 'vista de la cámara superior');
  rotulo('Cámara superior', 1.25, 2.0, 2.45, 0.28, { align: 'right', bold: true, fontSize: 14, color: TEMA.colors.dk1, fontFace: CABEZA });
  rotulo('Mira hacia abajo: la etiqueta y su QR', 1.25, 2.28, 2.45, 0.25, { align: 'right' });

  // Cámara lateral: a la altura de la carga, mirando el costado (posición, horquilla, paquetes y marcas de tara)
  rect(6.78, 4.72, 0.52, 0.26, CAM.cuerpo, 'cámara lateral', SH.roundRect);
  rect(6.7, 4.79, 0.08, 0.12, CAM.lente, 'lente cámara lateral');
  rect(7.0, 4.98, 0.08, 0.72, CAM.plataforma, 'poste cámara lateral');
  cono(5.0, 4.15, 1.7, 1.4, [[1.7, 0.7], [0, 0], [0, 1.4]], 'vista de la cámara lateral');
  rotulo('Cámara lateral', 5.6, 3.35, 1.75, 0.28, { align: 'right', bold: true, fontSize: 14, color: TEMA.colors.dk1, fontFace: CABEZA });
  rotulo('Mira el costado: posición, horquilla y marcas de tara', 5.6, 3.63, 1.75, 0.42, { align: 'right' });

  // Baliza a la vista del operador
  s.addShape(SH.roundRect, { x: 1.47, y: 3.25, w: 0.26, h: 0.07, rectRadius: 0.03, fill: { color: CAM.plataforma }, line: { color: CAM.plataforma }, objectName: N('tapa baliza') });
  [[CAM.rojo, 'roja'], [CAM.amarillo, 'amarilla'], [CAM.verde, 'verde']].forEach(([c, t], i) => rect(1.45, 3.33 + i * 0.25, 0.3, 0.24, c, 'luz ' + t));
  rect(1.57, 4.08, 0.06, 1.62, CAM.plataforma, 'poste baliza');
  rotulo('Baliza', 1.85, 3.38, 1.5, 0.25, { bold: true, fontSize: 13, color: TEMA.colors.dk1, fontFace: CABEZA });
  rotulo('A la vista del operador', 1.85, 3.63, 1.5, 0.25);

  // Las dos lecturas van al programa de la caseta
  s.addShape(SH.roundRect, { x: 5.55, y: 1.5, w: 2.0, h: 0.72, rectRadius: 0.08, fill: { color: P.tarjeta }, line: { color: P.linea, width: 1 }, objectName: N('caseta') });
  rotulo('Caseta · programa de pesaje', 5.65, 1.56, 1.85, 0.24, { bold: true, color: TEMA.colors.dk1 });
  rotulo('Recibe las dos lecturas y el peso del visor', 5.65, 1.8, 1.85, 0.4, { fontSize: 10 });
  linea(4.38, 2.07, 5.55, 2.07, CAM.cable, 'cable cámara superior', { dashType: 'dash', width: 1 });
  linea(7.3, 4.85, 7.45, 4.85, CAM.cable, 'cable cámara lateral', { dashType: 'dash', width: 1 });
  linea(7.45, 4.85, 7.45, 2.22, CAM.cable, 'cable cámara lateral', { dashType: 'dash', width: 1 });

  // Qué hace cada una
  [['Cámara superior · mira hacia abajo', [
      'Detecta cuándo llega cobre a la plataforma y cuándo queda vacía.',
      'Lee el QR de la etiqueta: celda, lado y grupo (por ejemplo, 4T21).',
      'Si no puede leerlo, avisa "etiqueta no legible" y el pesador identifica la carga a mano.']],
   ['Cámara lateral · mira el costado', [
      'Verifica que la carga o el carro quede dentro del área de pesaje.',
      'Confirma que la horquilla salió antes de tomar el peso y distingue 1 o 2 paquetes.',
      'En restos de ánodos lee las marcas de tara del carro y del rack: neto = bruto − tara de carro − tara de rack.']]]
    .forEach(([h, lista], i) => {
      const y = 1.5 + i * 2.6;
      tarjeta(s, 7.95, y, 4.75, 2.45);
      numero(s, i + 1, 8.2, y + 0.22, 0.4);
      texto(s, h, { x: 8.75, y: y + 0.24, w: 3.8, h: 0.36, fontFace: CABEZA, fontSize: 16 });
      texto(s, lista.map((t, j) => ({ text: t, options: { bullet: true, breakLine: j < lista.length - 1 } })),
        { x: 8.2, y: y + 0.75, w: 4.3, h: 1.6, fontSize: 13, color: P.mut, paraSpaceAfter: 5 });
    });
  texto(s, 'Esquema conceptual. La ubicación, cantidad y óptica de las cámaras se definen con revisión técnica y pruebas en terreno, de día y de noche.',
    { x: 0.6, y: 6.62, w: 12.1, h: 0.3, fontSize: 12, color: P.mut });
  s.addNotes('Las dos vistas de la minuta: la superior lee la etiqueta (va arriba del paquete, en la esquina, y no refleja como el cobre); la lateral controla la posición y las taras. Todo el esquema es editable: cada pieza tiene nombre en el panel de selección.');
}
{
  const s = lamina('Pesaje', 'CÁMARAS · PASO A PASO', 'Qué hace cada cámara en cada momento del pesaje');
  const H = t => ({ text: t, options: { bold: true, color: 'FFFFFF', fill: { color: P.panel }, fontFace: CABEZA, fontSize: 13 } });
  const luz = (c, t, sub) => ({ text: [
    { text: '● ', options: { color: c, fontSize: 16 } }, { text: t, options: { bold: true, breakLine: true } }, { text: sub, options: { color: P.mut } }] });
  const filas = [
    ['1 · Plataforma libre', 'Ve la plataforma vacía.', 'Sin carga en el área.', luz(CAM.verde, 'Verde · Disponible', 'Puede ingresar'),
      'Visor en cero: espera la próxima carga.'],
    ['2 · Llega la carga', 'Detecta el cobre y lee el QR de la etiqueta.', 'Ve la horquilla depositando la carga.', luz(CAM.amarillo, 'Amarillo · Posicionar', 'Deje la carga y retírese'),
      'Muestra la etiqueta que está leyendo.'],
    ['3 · Captura del peso', 'Cátodo identificado: etiqueta 4T21.', '1 paquete y horquilla fuera (o carro dentro del área).', luz(CAM.rojo, 'Rojo · Capturando', 'No se mueva'),
      'Espera el peso estable del visor y lo toma solo.'],
    ['4 · Registro', 'Sigue viendo el cátodo 4T21.', 'Sigue viendo 1 paquete, horquilla fuera.', luz(CAM.verde, 'Verde · Retirar', 'Peso registrado'),
      'Guarda rótulo, bruto, tara, neto, hora real y validación. La etiqueta deja de estar pendiente.'],
    ['5 · Salida', 'Ve la carga saliendo y la plataforma vacía.', 'Ve la carga saliendo.', luz(CAM.verde, 'Verde · Disponible', 'Puede ingresar la siguiente'),
      'Si el visor no vuelve a cero con la plataforma vacía, alerta: residuos o nódulos.'],
  ];
  const cuerpo = filas.map((f, i) => f.map((c, j) => {
    const celda = typeof c === 'string' ? { text: c } : c;
    celda.options = Object.assign({ fill: { color: i % 2 ? P.tarjeta : 'FFFFFF' } }, j === 0 ? { bold: true, fontFace: CABEZA, color: C.accent1 } : {}, celda.options || {});
    return celda;
  }));
  s.addTable([[H('Momento'), H('Cámara superior'), H('Cámara lateral'), H('Baliza'), H('Programa de la caseta')]].concat(cuerpo), {
    x: 0.6, y: 1.5, w: 12.1, colW: [2.0, 2.55, 2.55, 2.3, 2.7], rowH: [0.42, 0.82, 0.82, 0.82, 0.82, 0.82],
    fontFace: TEMA.bodyFontFace, fontSize: 12.5, color: TEMA.colors.dk1, valign: 'middle', margin: [0.04, 0.1, 0.04, 0.1],
    border: { type: 'solid', pt: 0.75, color: P.linea }, objectName: 'Cámaras paso a paso' });
  texto(s, 'Nadie digita el peso: el programa lo toma cuando la cámara superior identificó la carga, la lateral confirma la posición y el visor está estable.',
    { x: 0.6, y: 6.2, w: 12.1, h: 0.5, fontSize: 14, bold: true, color: C.accent1 });
  s.addNotes('Es la secuencia que ya corre en el programa de la caseta (simulada): mismos textos de las tarjetas Cámaras y Baliza. La tabla es editable.');
}
function camaras(kicker, titulo, lista, nota) {
  const s = lamina('Pesaje', kicker, titulo);
  lista.forEach(([f, cap], i) => {
    const x = 0.6 + (i % 2) * 6.25, y = 1.42 + Math.floor(i / 2) * 2.78;
    numero(s, i + 1, x, y + 0.02, 0.36);
    texto(s, cap, { x: x + 0.5, y, w: 5.3, h: 0.46, fontSize: 13, color: P.mut });
    imagen(s, IMG(f), x, y + 0.48, { w: 5.2, alt: cap });
  });
  if (nota) s.addNotes(nota);
}
camaras('CÁMARAS · EN EL PROGRAMA DE LA CASETA', 'Así lo ve el pesador: baliza y lectura de cada cámara', [
  ['cam_disponible.png', 'Plataforma libre: ninguna cámara ve carga. Baliza verde: puede ingresar.'],
  ['cam_llega.png', 'Llega la carga: la superior lee la etiqueta 4T21 y la lateral ve la horquilla. Baliza amarilla.'],
  ['cam_capturando.png', 'Cátodo identificado, 1 paquete y horquilla fuera. Baliza roja mientras se toma el peso.'],
  ['cam_registrado.png', 'Peso registrado con su etiqueta. Baliza verde: retirar la carga.'],
], 'Recortes de la pantalla de la caseta (BasculaCaseta.exe) en la prueba de punta a punta: tarjetas Baliza y Cámaras.');
camaras('CÁMARAS · ALERTAS', 'Alertas de las cámaras y lectura de la tara del carro', [
  ['cam_sin_etiqueta.png', 'Superior · etiqueta no legible: el peso queda retenido y el pesador identifica la carga a mano.'],
  ['cam_horquilla.png', 'Lateral · horquilla sobre la plataforma: no se toma el peso hasta que salga. Baliza amarilla.'],
  ['cam_carro_fuera.png', 'Lateral · carro fuera del área: baliza amarilla hasta que el carro quede bien ubicado.'],
  ['cam_carro.png', 'Restos de ánodos: la lateral ve el carro dentro del área y lee su marca de tara (2.150 kg).'],
], 'Recortes de la pantalla de la caseta en la prueba de punta a punta. La baliza le indica al operador qué corregir.');

/* ===================== 12b. Pantallas del sistema (avance) ===================== */
// Todas las pantallas de los dos programas, sacadas de la prueba de punta a punta de cada uno (construir.py):
// img/pantallas/etiquetado (TerminalVentanas.exe en la VT1200 de origen), img/pantallas/pesaje (BasculaCaseta.exe)
// e img/pantallas/gs1 (la versión de etiquetado con DataMatrix GS1). Las copia y las pasa a JPG el paso de capturas.
pres.addSection({ title: 'Pantallas del sistema' });
const PANT = (carpeta, f) => path.join(AQUI, CARPETA_IMG, 'pantallas', carpeta, f + '.jpg');
const ETIQUETADO = [
  ['01-ingreso', 'Ingreso con el RUT del operador'],
  ['01b-salir', 'Salir: pide confirmación antes de cerrar el programa'],
  ['04-paso1-grupos', 'Paso 1: el operador elige el grupo en el layout de circuitos'],
  ['02-principal', 'Paso 2: la etiqueta que sigue y las 42 celdas del grupo'],
  ['05-imprimiendo', 'Imprimiendo la etiqueta de la celda 5'],
  ['06-impresa', 'Etiqueta 5T21 impresa: la celda queda marcada'],
  ['11-final', 'Sigue la celda 6: el programa propone siempre la que viene'],
  ['07-grupo22', 'Cambio de grupo: el grupo 22 parte en la celda 1'],
  ['07b-lado', 'Lado total o parcial: se elige con el botón sobre cada fila'],
  ['08-impresas', 'Etiqueta parcial 1P22 impresa; sigue la 2P22'],
  ['09-registro', 'Registro de etiquetas emitidas, con reimpresión y descarga a Excel'],
  ['25-colores', 'Colores: original, claro, alto contraste o como Windows'],
  ['21-claro', 'Pantalla principal · tema claro'],
  ['22-claro-grupos', 'Layout de circuitos · tema claro'],
  ['22b-claro-registro', 'Registro · tema claro'],
  ['01c-ingreso-claro', 'Ingreso · tema claro'],
  ['23-contraste', 'Pantalla principal · alto contraste'],
  ['24-contraste-grupos', 'Layout de circuitos · alto contraste'],
  ['01d-ingreso-contraste', 'Ingreso · alto contraste'],
];
const PESAJE = [
  ['c01-ingreso', 'Ingreso del pesador con su RUT'],
  ['c02-llega', 'Llega la carga 4T21: la cámara superior lee la etiqueta'],
  ['c03-capturando', 'Capturando el peso: baliza roja, la carga no se mueve'],
  ['c04-registrado', 'Registrado: 4.687 kg, dentro de rango'],
  ['c05-horquilla', 'Alerta: la horquilla está sobre la báscula, el peso no se registra'],
  ['c06-sin-etiqueta', 'Carga sin etiqueta legible: queda a la espera de identificación'],
  ['c07-manual', 'Identificación manual: el pesador asocia el peso a la 9T21'],
  ['c08-carro-fuera', 'Alerta: el carro quedó fuera del área de la plataforma'],
  ['c09-carro', 'Carro de restos C22: neto con la tara de carro + rack'],
  ['c10-disponible', 'Plataforma disponible para la próxima carga'],
  ['c12-prueba-4750', 'Peso de prueba (panel de pesaje, F3) 4.750 kg: dentro del rango'],
  ['c13-prueba-3900', 'Peso de prueba 3.900 kg: bajo el rango, faltan 300 kg'],
  ['c14-prueba-5300', 'Peso de prueba 5.300 kg: sobre el rango, se pasa por 300 kg'],
  ['c15-panel-pesaje', 'Panel de pesaje (F3) al costado: se coloca el peso sin tapar el programa'],
  ['c16-panel-pesaje-compacto', 'Panel de pesaje compacto en una esquina, con el peso en vivo'],
  ['c21-claro', 'Caseta · tema claro'],
  ['c22-contraste', 'Caseta · alto contraste'],
];
const BASCULA = [
  ['bal-1-disponible', 'Disponible'], ['bal-2-leyendo', 'Llega la carga'],
  ['bal-3-registrando', 'Registrando'], ['bal-4-registrado', 'Registrado dentro de rango'],
  ['bal-5-horquilla', 'Horquilla sobre la báscula'], ['bal-6-sincero', 'Sin cero: la báscula vacía marca 6 kg'],
  ['bal-7-carro-fuera', 'Carro fuera del área'], ['bal-8-sin-tara', 'Sin tara: falta pesar el carro vacío'],
];
// Dos capturas por lámina (4:3), cada una con su pie
function pantallas(seccion, kicker, titulo, carpeta, lista, nota) {
  for (let i = 0; i < lista.length; i += 2) {
    const par = lista.slice(i, i + 2);
    const pagina = `${Math.floor(i / 2) + 1} de ${Math.ceil(lista.length / 2)}`;
    const s = lamina(seccion, `${kicker} · ${pagina}`, titulo);
    par.forEach(([f, pie], j) => {
      const x = 0.6 + j * 6.15, y = 1.55, w = 5.95;
      const r = imagen(s, PANT(carpeta, f), x, y, { w, alt: `${titulo}: ${pie}`, nombre: f });
      s.addShape(SH.rect, { x, y, w, h: r.h, fill: { type: 'none' }, line: { color: P.linea, width: 0.75 } });
      numero(s, i + j + 1, x, y + r.h + 0.14, 0.34, { fontSize: 11 });
      texto(s, pie, { x: x + 0.45, y: y + r.h + 0.12, w: w - 0.45, h: 0.45, fontSize: 13, valign: 'middle' });
    });
    if (nota) texto(s, nota, { x: 0.6, y: 6.62, w: 12.1, h: 0.3, fontSize: 11, color: P.mut });
  }
}
{
  const s = lamina('Pantallas del sistema', 'AVANCE · 18-11-2026', 'Pantallas de los dos programas, tal como corren hoy');
  const bloques = [
    ['1', 'Etiquetado en origen', 'TerminalVentanas.exe en el equipo de la nave. Solo rotula cátodos: el operador elige el grupo, la celda y el lado, y el programa imprime la etiqueta.', `${ETIQUETADO.length} pantallas`],
    ['2', 'Pesaje en la caseta', 'BasculaCaseta.exe en la caseta de la Báscula N9. Lee la etiqueta con las cámaras, captura el peso, lo valida y avisa con la baliza.', `${PESAJE.length} pantallas + ${BASCULA.length} estados de la báscula`],
    ['3', 'Variante DataMatrix GS1', 'El mismo programa de etiquetado con un código DataMatrix GS1 en vez del QR, por si Codelco lo prefiere.', '1 lámina'],
  ];
  bloques.forEach(([n, t, d, c], i) => {
    const y = 1.75 + i * 1.6;
    tarjeta(s, 0.6, y, 12.1, 1.35);
    numero(s, n, 0.9, y + 0.33, 0.6);
    texto(s, t, { x: 1.8, y: y + 0.22, w: 7.6, h: 0.4, fontFace: CABEZA, fontSize: 20 });
    texto(s, d, { x: 1.8, y: y + 0.68, w: 7.6, h: 0.6, fontSize: 13, color: P.mut });
    texto(s, c, { x: 9.6, y: y + 0.45, w: 2.9, h: 0.45, fontSize: 16, bold: true, color: C.accent1, align: 'right', valign: 'middle' });
  });
  texto(s, 'Las capturas salen de la prueba de punta a punta de cada programa (versión 6.2.0, compilada el 18-11-2026). Los datos son de ejemplo: grupo 21 del 22-10-2026.',
    { x: 0.6, y: 6.6, w: 12.1, h: 0.3, fontSize: 11, color: P.mut });
}
pantallas('Pantallas del sistema', 'PROGRAMA 1 · ETIQUETADO EN ORIGEN', 'Rotulado de cátodos en origen', 'etiquetado', ETIQUETADO,
  'TerminalVentanas.exe · pantalla de 1024 x 768 del equipo de origen · datos de ejemplo del grupo 21');
pantallas('Pantallas del sistema', 'PROGRAMA 2 · PESAJE EN LA CASETA', 'Identificación, captura del peso y registro', 'pesaje', PESAJE,
  'BasculaCaseta.exe · PC de la caseta de la Báscula N9 · pesos de ejemplo');
// Los estados de la tarjeta de la báscula: 4 por lámina
for (let i = 0; i < BASCULA.length; i += 4) {
  const s = lamina('Pantallas del sistema', `PROGRAMA 2 · ESTADOS DE LA BÁSCULA · ${i / 4 + 1} de 2`,
    i === 0 ? 'Estados normales de la tarjeta de la báscula' : 'Desviaciones y alertas de la báscula');
  BASCULA.slice(i, i + 4).forEach(([f, pie], j) => {
    const x = 0.6 + (j % 2) * 6.15, y = 1.5 + Math.floor(j / 2) * 2.62, w = 3.1;
    const r = imagen(s, PANT('pesaje', f), x, y, { w, alt: `Báscula N9: ${pie}`, nombre: f });
    s.addShape(SH.rect, { x, y, w, h: r.h, fill: { type: 'none' }, line: { color: P.linea, width: 0.75 } });
    numero(s, i + j + 1, x + w + 0.25, y + 0.2, 0.42, { fontSize: 13 });
    texto(s, pie, { x: x + w + 0.8, y: y + 0.18, w: 2.0, h: 1.2, fontSize: 15 });
  });
}
{
  const s = lamina('Pantallas del sistema', 'PROGRAMA 1 · VARIANTE DATAMATRIX GS1', 'La misma pantalla con un código DataMatrix GS1 en la etiqueta');
  [['02-principal', 'La etiqueta que sigue, con el DataMatrix'], ['06-impresa', 'Etiqueta impresa']].forEach(([f, pie], j) => {
    const x = 0.6 + j * 6.15, y = 1.55, w = 5.95;
    const r = imagen(s, PANT('gs1', f), x, y, { w, alt: `Variante DataMatrix GS1: ${pie}`, nombre: 'gs1-' + f });
    s.addShape(SH.rect, { x, y, w, h: r.h, fill: { type: 'none' }, line: { color: P.linea, width: 0.75 } });
    numero(s, j + 1, x, y + r.h + 0.14, 0.34, { fontSize: 11 });
    texto(s, pie, { x: x + 0.45, y: y + r.h + 0.12, w: w - 0.45, h: 0.45, fontSize: 13, valign: 'middle' });
  });
  texto(s, 'El DataMatrix lleva los datos con identificadores GS1: (11) fecha de producción, (30) cantidad de cátodos, (10) lote = rótulo y (91) código interno. La prueba lo vuelve a leer para comprobarlo.',
    { x: 0.6, y: 6.55, w: 12.1, h: 0.4, fontSize: 11, color: P.mut });
}

/* ===================== 13. Demo ===================== */
pres.addSection({ title: 'Cierre' });
{
  const s = lamina('Cierre', 'PRUÉBELO', 'Dos programas: etiquetado y pesaje');
  imagen(s, IMG('app_impresa.jpg'), 0.6, 1.45, { w: 6.9, alt: 'Captura de TerminalVentanas.exe: etiqueta 5T21 impresa y el grupo 21' });
  texto(s, 'Captura de TerminalVentanas.exe, el programa de etiquetado en origen.', { x: 0.6, y: 6.7, w: 6.9, h: 0.25, fontSize: 11, color: P.mut });
  const bloques = [['Programa 1 · Etiquetado en origen', 'TerminalVentanas.exe: selección de grupo en el layout, emisión de la etiqueta en la Zebra, registro y Excel.'],
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
      'Etiqueta emitida en origen por paquete de 39 cátodos; no lleva peso.',
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
