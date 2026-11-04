"""Arma el release de la app de la VT1200 (Terminal Rotulado Ventanas).

Uso, desde la raíz del repo:   python app/construir.py

1. Copia terminal-vt1200.html a app/TerminalVentanas/wwwroot/index.html con las librerías
   (código QR y Excel) y las fuentes dentro, para que funcione sin internet.
2. Compila self-contained (win-x64, con el runtime de .NET adentro).
3. Revisa que estén hostfxr.dll, hostpolicy.dll y coreclr.dll.
4. Deja la carpeta y el .zip en Proyectos_Claude/releases/codelco-ventanas/.
"""
import io, os, re, shutil, subprocess, sys, urllib.request, zipfile, json
from datetime import date

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
APP = os.path.join(RAIZ, "app", "TerminalVentanas")
WWW = os.path.join(APP, "wwwroot")
CACHE = os.path.join(RAIZ, "app", ".cache")
VERSION = re.search(r"<Version>([^<]+)</Version>", open(os.path.join(APP, "TerminalVentanas.csproj"), encoding="utf-8").read()).group(1)
NOMBRE = f"terminal-ventanas-v{VERSION}-{date.today():%Y%m%d}"
SALIDA = os.path.abspath(os.path.join(RAIZ, "..", "releases", "codelco-ventanas", NOMBRE))
UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36"
LIBS = {
    "https://cdnjs.cloudflare.com/ajax/libs/qrcode-generator/1.4.4/qrcode.min.js": "lib/qrcode.min.js",
    "https://cdnjs.cloudflare.com/ajax/libs/xlsx/0.18.5/xlsx.full.min.js": "lib/xlsx.full.min.js",
}


def bajar(url, binario=True):
    os.makedirs(CACHE, exist_ok=True)
    local = os.path.join(CACHE, re.sub(r"[^A-Za-z0-9._-]", "_", url)[-150:])
    if not os.path.exists(local):
        with urllib.request.urlopen(urllib.request.Request(url, headers={"User-Agent": UA}), timeout=60) as r:
            open(local, "wb").write(r.read())
    data = open(local, "rb").read()
    return data if binario else data.decode("utf-8")


def preparar_web():
    html = open(os.path.join(RAIZ, "terminal-vt1200.html"), encoding="utf-8").read()
    shutil.rmtree(WWW, ignore_errors=True)
    os.makedirs(os.path.join(WWW, "lib"))
    os.makedirs(os.path.join(WWW, "fonts"))
    for url, dest in LIBS.items():
        assert url in html, f"El HTML ya no usa {url}"
        open(os.path.join(WWW, dest), "wb").write(bajar(url))
        html = html.replace(url, dest)
    # Fuentes de Google: el CSS y cada archivo woff2, con rutas locales
    fuentes = re.search(r'href="(https://fonts\.googleapis\.com/css2\?[^"]+)"', html).group(1)
    css = bajar(fuentes.replace("&amp;", "&"), binario=False)
    for i, u in enumerate(sorted(set(re.findall(r"url\((https://fonts\.gstatic\.com/[^)]+)\)", css)))):
        nombre = f"f{i:02d}.woff2"
        open(os.path.join(WWW, "fonts", nombre), "wb").write(bajar(u))
        css = css.replace(u, nombre)
    open(os.path.join(WWW, "fonts", "fonts.css"), "w", encoding="utf-8").write(css)
    html = html.replace(fuentes, "fonts/fonts.css")
    html = re.sub(r'<link rel="preconnect"[^>]*>\s*', "", html)
    externas = re.findall(r'(?:src|href)="(https?://[^"]+)"', html)
    assert not externas, f"Quedan recursos de internet: {externas}"
    pagina = ('<!doctype html><html lang="es"><head><meta charset="utf-8">'
              '<meta name="viewport" content="width=device-width,initial-scale=1"></head><body>\n' + html + "\n</body></html>\n")
    open(os.path.join(WWW, "index.html"), "w", encoding="utf-8").write(pagina)
    print("wwwroot listo:", sum(len(f) for _, _, f in os.walk(WWW)), "archivos")


def compilar():
    shutil.rmtree(SALIDA, ignore_errors=True)
    r = subprocess.run(["dotnet", "publish", os.path.join(APP, "TerminalVentanas.csproj"), "-c", "Release", "-r", "win-x64",
                        "--self-contained", "true", "-o", SALIDA, "-p:DebugType=None", "-p:DebugSymbols=false"],
                       capture_output=True, text=True, encoding="utf-8", errors="replace")
    print(r.stdout[-1500:])
    if r.returncode:
        print(r.stderr[-3000:])
        sys.exit("No compiló")
    faltan = [f for f in ("hostfxr.dll", "hostpolicy.dll", "coreclr.dll", "TerminalVentanas.exe", "WebView2Loader.dll", "wwwroot/index.html", "appsettings.json")
              if not os.path.exists(os.path.join(SALIDA, f))]
    rc = json.load(open(os.path.join(SALIDA, "TerminalVentanas.runtimeconfig.json"), encoding="utf-8"))
    assert "includedFrameworks" in rc["runtimeOptions"], "No quedó self-contained"
    if faltan:
        sys.exit(f"Faltan en el release: {faltan}")
    print("Self-contained OK: hostfxr, hostpolicy y coreclr presentes;", len(os.listdir(SALIDA)), "archivos en la raíz")


def comprimir():
    zip_ruta = SALIDA + ".zip"
    if os.path.exists(zip_ruta):
        os.remove(zip_ruta)
    with zipfile.ZipFile(zip_ruta, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as z:
        for base, _, archivos in os.walk(SALIDA):
            for f in archivos:
                p = os.path.join(base, f)
                z.write(p, os.path.join(NOMBRE, os.path.relpath(p, SALIDA)))
    print("Zip:", zip_ruta, round(os.path.getsize(zip_ruta) / 1048576, 1), "MB")


if __name__ == "__main__":
    preparar_web()
    compilar()
    comprimir()
    print("Release:", SALIDA)
