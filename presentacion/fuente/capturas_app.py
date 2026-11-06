"""Copia a img/ las capturas de la app nativa (TerminalVentanas.exe) que usa generar.js.
Uso: python capturas_app.py <carpeta de la prueba>
La carpeta la deja `TerminalVentanas.exe --prueba <carpeta>` (o construir.py, que la nombra al final). Las capturas son de 2048 x 1536.
Las pantallas van enteras; de la báscula se recorta su tarjeta. Las etiquetas (etq_*) siguen saliendo de capturas.py.
"""
import os, sys
from PIL import Image

prueba = sys.argv[1]
img = os.path.join(os.path.dirname(os.path.abspath(__file__)), "img")
BASCULA = (1296, 152, 2016, 640)  # la tarjeta Báscula N9: x 648, y 76, 360 x 244 en la pantalla de 1024 x 768

pantallas = {"vt_cat_pesando": "02-catodos-pesando", "vt_lam": "07-laminas", "vt_rs": "09-restos-sin-tara", "vt_registro": "10-registro"}
bascula = {"bal_1llegando": "02a-catodos-llegando", "bal_2estabilizando": "02-catodos-pesando", "bal_3registrado": "03-catodos-registrado",
           "bal_6lam": "07-laminas", "bal_4grua": "06-grua-encima", "bal_7fuera": "20b-restos-fuera", "bal_5sincero": "06b-sin-cero",
           "bal_9sintara": "09-restos-sin-tara"}

def abrir(nombre):
    return Image.open(os.path.join(prueba, nombre + ".png")).convert("RGB")

for destino, origen in pantallas.items():
    abrir(origen).save(os.path.join(img, destino + ".png"), optimize=True)
for destino, origen in bascula.items():
    abrir(origen).crop(BASCULA).save(os.path.join(img, destino + ".png"), optimize=True)
abrir("05-impresa").resize((1400, 1050), Image.LANCZOS).save(os.path.join(img, "app_impresa.jpg"), quality=90)
print("Capturas de la app copiadas a", img)
