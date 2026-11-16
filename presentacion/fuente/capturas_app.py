"""Copia a img/ las capturas de la app nativa (TerminalVentanas.exe) que usa generar.js.
Uso: python capturas_app.py <carpeta de la prueba de origen> [<carpeta de la prueba de la caseta>]
La carpeta la deja `TerminalVentanas.exe --prueba <carpeta>` (o construir.py, que la nombra al final). Las capturas son de 2048 x 1536.
La VT1200 del origen rotula solo cátodos. La tarjeta de la Báscula N9 sale aparte (bal-*.png): es la pantalla del programa de la báscula,
con el mismo motor de la simulación. La etiqueta de cátodo se recorta de la pantalla principal; las de láminas y restos siguen saliendo de capturas.py.
También dibuja el puntero (cursor.png) que marca lo que se toca en las láminas de los pasos 1 y 2.
"""
import os, sys
from PIL import Image, ImageDraw, ImageFilter

prueba = sys.argv[1]
img = os.path.join(os.path.dirname(os.path.abspath(__file__)), "img")

pantallas = {"vt_principal": "02-principal", "vt_paso1": "04-paso1-grupos", "vt_paso2": "07-grupo22", "vt_registro": "09-registro"}
bascula = ["bal-1-disponible", "bal-2-leyendo", "bal-3-registrando", "bal-4-registrado",
           "bal-5-horquilla", "bal-6-sincero", "bal-7-carro-fuera", "bal-8-sin-tara"]

def abrir(nombre):
    return Image.open(os.path.join(prueba, nombre + ".png")).convert("RGB")

for destino, origen in pantallas.items():
    abrir(origen).save(os.path.join(img, destino + ".png"), optimize=True)
# Las tarjetas de la báscula salen de la prueba de la caseta (la VT1200 de origen no pesa)
if len(sys.argv) > 2:
    for b in bascula:
        Image.open(os.path.join(sys.argv[2], b + ".png")).convert("RGB").save(os.path.join(img, b.replace("-", "_", 1) + ".png"), optimize=True)
def etiqueta(im):
    """El recuadro blanco de la etiqueta en la tarjeta de arriba (se busca, porque cambia de lugar con el diseño)."""
    import numpy as np
    a = np.asarray(im.crop((0, 100, 2048, 720)).convert("L"))
    ys, xs = np.where(a > 240)
    return (int(xs.min()), int(ys.min()) + 100, int(xs.max()) + 1, int(ys.max()) + 101)
im = abrir("02-principal"); im.crop(etiqueta(im)).save(os.path.join(img, "etq_cat_vt.png"), optimize=True)
abrir("06-impresa").resize((1400, 1050), Image.LANCZOS).save(os.path.join(img, "app_impresa.jpg"), quality=90)
# El programa de la caseta (BasculaCaseta.exe): su prueba deja c*.png
if len(sys.argv) > 2:
    caseta = sys.argv[2]
    for destino, origen in {"cs_principal": "c03-capturando", "cs_manual": "c06-sin-etiqueta"}.items():
        Image.open(os.path.join(caseta, origen + ".png")).convert("RGB").save(os.path.join(img, destino + ".png"), optimize=True)

# Puntero de mouse (flecha blanca con borde negro y sombra), para "aquí se toca"
E = 8
punta = [(0, 0), (0, 17), (4, 13), (7, 20), (10, 19), (7, 12), (12, 12)]
pts = [(x * E + 12, y * E + 12) for x, y in punta]
cur = Image.new("RGBA", (13 * E + 40, 21 * E + 40), (0, 0, 0, 0))
sombra = Image.new("RGBA", cur.size, (0, 0, 0, 0))
ImageDraw.Draw(sombra).polygon([(x + 8, y + 10) for x, y in pts], fill=(0, 0, 0, 110))
cur = Image.alpha_composite(cur, sombra.filter(ImageFilter.GaussianBlur(6)))
d = ImageDraw.Draw(cur)
d.polygon(pts, fill=(255, 255, 255, 255))
d.line(pts + [pts[0]], fill=(17, 17, 17, 255), width=7, joint="curve")
cur.save(os.path.join(img, "cursor.png"))
print("Capturas de la app copiadas a", img)
