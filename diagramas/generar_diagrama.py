"""Genera el diagrama del flujo propuesto para la Báscula N9 (SVG).

Uso:  python diagramas/generar_diagrama.py
Sale: diagramas/flujo-bascula-n9.svg  (GitHub lo muestra tal cual; el PNG del PPT se saca de este SVG)
"""
import os, textwrap
from xml.sax.saxutils import escape

W, H = 1600, 1000
FONT = "'Segoe UI','Helvetica Neue',Arial,sans-serif"
C = dict(ink="#1A2126", txt="#46545C", mut="#6B7A82", line="#C9D3D8", flecha="#5A6B73",
         cobre="#A4561F", cobre2="#DA8B4F", carril="#F3F6F7", carril2="#FAFBFB",
         amb_bg="#FFF6DA", amb="#B8860B", roj_bg="#FDECEA", roj="#B23A2E", ver_bg="#E8F6EE", ver="#2F7D4F")
partes = []
add = partes.append

def texto(x, y, s, size=14, weight=400, color=None, anchor="start"):
    add(f'<text x="{x}" y="{y}" font-family="{FONT}" font-size="{size}" font-weight="{weight}" '
        f'fill="{color or C["txt"]}" text-anchor="{anchor}">{escape(s)}</text>')

def lineas(x, y, s, ancho_chars, size=14, color=None, alto=19, anchor="start", weight=400):
    for i, l in enumerate(textwrap.wrap(s, ancho_chars)):
        texto(x, y + i * alto, l, size, weight, color, anchor)
    return len(textwrap.wrap(s, ancho_chars))

def caja(x, y, w, h, titulo, cuerpo, num=None, bg="#FFFFFF", borde=None, color_t=None):
    add(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="12" fill="{bg}" stroke="{borde or C["line"]}" stroke-width="1.5"/>')
    tx = x + 18
    if num:
        add(f'<circle cx="{x+26}" cy="{y+27}" r="13" fill="{C["cobre"]}"/>')
        texto(x + 26, y + 32, num, 14, 700, "#FFFFFF", "middle")
        tx = x + 48
    texto(tx, y + 32, titulo, 17, 700, color_t or C["ink"])
    lineas(x + 18, y + 58, cuerpo, int((w - 36) / 7.3), 14)

def rombo(cx, cy, w, h, l1, l2=None):
    add(f'<path d="M{cx} {cy-h/2} L{cx+w/2} {cy} L{cx} {cy+h/2} L{cx-w/2} {cy} Z" fill="#FFF4E8" stroke="{C["cobre2"]}" stroke-width="1.8"/>')
    if l2:
        texto(cx, cy - 3, l1, 14.5, 700, C["ink"], "middle"); texto(cx, cy + 16, l2, 14.5, 700, C["ink"], "middle")
    else:
        texto(cx, cy + 5, l1, 14.5, 700, C["ink"], "middle")

PUNTA = {C["flecha"]: "punta", C["roj"]: "punta-roja", C["ver"]: "punta-verde"}

def flecha(pts, etiqueta=None, pos=None, color=None, guion=False):
    d = "M" + " L".join(f"{x} {y}" for x, y in pts)
    add(f'<path d="{d}" fill="none" stroke="{color or C["flecha"]}" stroke-width="2"{" stroke-dasharray=\"6 5\"" if guion else ""} marker-end="url(#{PUNTA.get(color or C['flecha'],'punta')})"/>')
    if etiqueta:
        ex, ey = pos
        add(f'<rect x="{ex-22}" y="{ey-14}" width="44" height="20" rx="10" fill="#FFFFFF"/>')
        texto(ex, ey + 1, etiqueta, 13, 700, color or C["flecha"], "middle")

def baliza(x, y, color, txt):
    add(f'<circle cx="{x}" cy="{y}" r="7" fill="{color}"/>')
    texto(x + 14, y + 5, txt, 13.5, 600, C["txt"])

# ---------- Fondo, título y carriles ----------
add(f'<rect width="{W}" height="{H}" fill="#FFFFFF"/>')
texto(40, 52, "Flujo propuesto · Báscula N9 · Refinería Ventanas", 28, 700, C["ink"])
texto(40, 82, "La etiqueta identifica la carga, la báscula controla el peso y todo queda en un solo registro, con la hora real del pesaje.", 16, 400, C["mut"])
carriles = [(30, 305, "1 · Nave", "VT1200 + impresora Zebra"),
            (345, 255, "2 · Traslado", "Grúa horquilla o tractor"),
            (615, 640, "3 · Báscula N9", "Cámara, 4 celdas de carga y balizas"),
            (1270, 300, "4 · Registro", "Excel y sistemas de Codelco")]
for i, (x, w, t, s) in enumerate(carriles):
    add(f'<rect x="{x}" y="110" width="{w}" height="820" rx="16" fill="{C["carril"] if i % 2 == 0 else C["carril2"]}" stroke="#E3E9EC"/>')
    texto(x + 20, 142, t.upper(), 15, 700, C["cobre"])
    texto(x + 20, 163, s, 13.5, 400, C["mut"])

# ---------- 1 · Nave ----------
caja(50, 190, 270, 170, "Elige lo que sigue",
     "La VT1200 ya propone la celda + lado + grupo (cátodos), el tipo y Nº del mes (láminas) o pide el Nº pintado del carro (restos).", "1")
caja(50, 400, 270, 130, "Imprime la etiqueta", "Un toque. Sale el rótulo de siempre (5T21, Nº 274, C22) con un código QR.", "2")
caja(50, 570, 270, 130, "La pega en la carga", "Arriba del paquete, en el pallet o junto al número pintado del carro.", "3")
flecha([(185, 360), (185, 398)])
flecha([(185, 530), (185, 568)])

# ---------- 2 · Traslado ----------
caja(362, 400, 220, 130, "Grúa horquilla", "Paquetes de cátodos y pallets de láminas.")
caja(362, 570, 220, 130, "Tractor", "Carros de restos de ánodos con su rack.")
flecha([(320, 635), (360, 635)])
flecha([(320, 635), (340, 635), (340, 465), (360, 465)])

# ---------- 3 · Báscula N9 ----------
cx = 845
caja(705, 190, 280, 112, "Llega la carga", "La báscula marca el peso en vivo y la cámara lee el QR.", "4")
flecha([(582, 465), (650, 465), (650, 246), (703, 246)])
flecha([(582, 635), (650, 635), (650, 465)])
rombo(cx, 372, 250, 92, "¿Etiqueta leída?")
flecha([(cx, 302), (cx, 324)])
caja(1030, 336, 205, 98, "Identifica el pesador", "Desde la caseta. B1 ámbar.", bg=C["amb_bg"], borde=C["amb"], color_t="#7A5A00")
flecha([(cx + 125, 372), (1028, 372)], "No", (998, 362))
flecha([(1132, 434), (1132, 452), (cx + 30, 452), (cx + 30, 470)], guion=True)
rombo(cx, 520, 300, 104, "¿Grúa fuera, carga en", "el área y báscula en cero?")
flecha([(cx, 418), (cx, 466)], "Sí", (cx - 26, 444))
caja(1030, 478, 205, 112, "Bloqueado", "No registra hasta corregir. B2 roja.", bg=C["roj_bg"], borde=C["roj"], color_t=C["roj"])
flecha([(cx + 150, 520), (1028, 520)], "No", (1000, 510))
flecha([(1080, 590), (1080, 612), (cx + 60, 612), (cx + 60, 560)], guion=True, color=C["roj"])
caja(705, 640, 280, 96, "Peso estable", "Espera que deje de moverse. B2 ámbar y luego verde.", "5")
flecha([(cx, 572), (cx, 638)], "Sí", (cx - 26, 600))
caja(705, 760, 280, 112, "Calcula el neto", "Tara 12 kg por pallet, tara de carro + rack, o dos pallets por diferencia.", "6")
flecha([(cx, 736), (cx, 758)])
rombo(1110, 816, 230, 92, "¿Dentro de lo", "esperado?")
flecha([(985, 816), (993, 816)])

# ---------- 4 · Registro ----------
caja(1290, 190, 260, 150, "Registro único", "Etiqueta, peso, hora real del pesaje y lo que dijo el control. Sin digitar.", "7")
caja(1290, 380, 260, 150, "Excel por turno", "Hojas Cátodos, Láminas, Restos de ánodos y un Resumen con las columnas del Sistema de Cátodos.", "8")
caja(1290, 570, 260, 150, "Sistemas de Codelco", "Sistema de Cátodos y Sistema de Ánodos. Primero con el Excel; la conexión directa se define con TI.", "9")
flecha([(1420, 340), (1420, 378)])
flecha([(1420, 530), (1420, 568)])
flecha([(1225, 816), (1250, 816), (1250, 265), (1288, 265)], "Sí", (1250, 300), C["ver"])
caja(1290, 760, 260, 112, "Se registra con nota", "Peso fuera de lo esperado: B1 roja, el pesador revisa.", bg=C["roj_bg"], borde=C["roj"], color_t=C["roj"])
flecha([(1110, 862), (1110, 900), (1420, 900), (1420, 874)], "No", (1180, 900), C["roj"])
flecha([(1550, 816), (1575, 816), (1575, 265), (1552, 265)], guion=True, color=C["roj"])

# ---------- Balizas ----------
texto(40, 965, "Balizas:", 13.5, 700, C["ink"])
baliza(118, 960, "#2F9E5F", "B1 verde: peso coincide con lo etiquetado")
baliza(430, 960, "#D9A21B", "Ámbar: falta identificar o se estabiliza")
baliza(735, 960, "#C9473B", "Roja: grúa encima, carro fuera, sin cero o peso fuera de lo esperado")

svg = (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}" width="{W}" height="{H}" role="img" '
       f'aria-label="Flujo propuesto para la Báscula N9: nave, traslado, báscula y registro">'
       f'<title>Flujo propuesto · Báscula N9</title>'
       f'<defs>'
       + "".join(f'<marker id="{i}" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse"><path d="M0 0L10 5L0 10z" fill="{c}"/></marker>' for c, i in PUNTA.items()) + '</defs>' + "".join(partes) + "</svg>")
destino = os.path.join(os.path.dirname(os.path.abspath(__file__)), "flujo-bascula-n9.svg")
open(destino, "w", encoding="utf-8").write(svg)
print("listo:", destino)
