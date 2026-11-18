"""Prepara un juego de capturas para generar.js desde las pruebas de los tres programas.

Uso:  python preparar_capturas.py <prueba origen> <prueba caseta> <prueba gs1> [img | img_claro]

- Las pruebas las deja construir.py (tema oscuro) o el programa con  --prueba <carpeta> --tema claro.
- img/ es el juego del tema oscuro (el original); img_claro/ el del tema claro. Si la carpeta no existe,
  se parte copiando img/ (logos, maqueta, puntero y las demás imágenes que no son capturas).
- Copia las capturas de las láminas (capturas_app.py) y TODAS las pantallas de la sección "Pantallas del sistema"
  a <carpeta>/pantallas/{etiquetado,pesaje,gs1}, en JPG de 1600 px de ancho (sin deformar: las del panel de pesaje son más anchas).
"""
import glob, os, shutil, subprocess, sys
from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
origen, caseta, gs1 = sys.argv[1:4]
destino = sys.argv[4] if len(sys.argv) > 4 else "img"
carpeta = os.path.join(AQUI, destino)

if not os.path.isdir(carpeta):
    shutil.copytree(os.path.join(AQUI, "img"), carpeta, ignore=shutil.ignore_patterns("pantallas"))

# 1) Las capturas que usan las láminas de la propuesta
subprocess.run([sys.executable, "-I", os.path.join(AQUI, "capturas_app.py"), origen, caseta], check=True,
               env=dict(os.environ, IMG_DIR=destino))
# La etiqueta impresa es la misma en los dos temas (papel blanco). capturas_app.py la recorta buscando el recuadro blanco,
# y en el tema claro las tarjetas también son blancas: se usa la del juego oscuro (preparar img/ primero).
if destino != "img":
    shutil.copy(os.path.join(AQUI, "img", "etq_cat_vt.png"), os.path.join(carpeta, "etq_cat_vt.png"))

# 2) Todas las pantallas, para la sección "Pantallas del sistema"
def copiar(desde, hacia, solo=None):
    d = os.path.join(carpeta, "pantallas", hacia)
    shutil.rmtree(d, ignore_errors=True); os.makedirs(d)
    n = 0
    for f in sorted(glob.glob(os.path.join(desde, "*.png"))):
        nombre = os.path.splitext(os.path.basename(f))[0]
        if solo and nombre not in solo:
            continue
        im = Image.open(f).convert("RGB")
        im.thumbnail((1600, 1600), Image.LANCZOS)
        im.save(os.path.join(d, nombre + ".jpg"), quality=86, optimize=True)
        n += 1
    return n

print("etiquetado:", copiar(origen, "etiquetado"), "· pesaje:", copiar(caseta, "pesaje"),
      "· gs1:", copiar(gs1, "gs1", {"02-principal", "06-impresa"}), "->", carpeta)
