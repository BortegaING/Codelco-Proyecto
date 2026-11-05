"""Arma el release de la app de la VT1200 (Terminal Rotulado Ventanas), app nativa WPF.

Uso, desde la raíz del repo:   python app/construir.py

1. Compila self-contained (win-x64, con el runtime de .NET adentro). No usa navegador ni WebView2.
2. Revisa que estén hostfxr.dll, hostpolicy.dll y coreclr.dll y que el runtimeconfig sea self-contained.
3. Deja la carpeta y el .zip en Proyectos_Claude/releases/codelco-ventanas/.
4. Descomprime el zip en una carpeta limpia y corre la prueba de punta a punta (TerminalVentanas.exe --prueba):
   si no termina en "FIN OK", el release no sirve.
"""
import json, os, re, shutil, subprocess, sys, tempfile, zipfile
from datetime import date

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
APP = os.path.join(RAIZ, "app", "TerminalVentanas")
VERSION = re.search(r"<Version>([^<]+)</Version>", open(os.path.join(APP, "TerminalVentanas.csproj"), encoding="utf-8").read()).group(1)
NOMBRE = f"terminal-ventanas-v{VERSION}-{date.today():%Y%m%d}"
SALIDA = os.path.abspath(os.path.join(RAIZ, "..", "releases", "codelco-ventanas", NOMBRE))


def compilar():
    shutil.rmtree(SALIDA, ignore_errors=True)
    r = subprocess.run(["dotnet", "publish", os.path.join(APP, "TerminalVentanas.csproj"), "-c", "Release", "-r", "win-x64",
                        "--self-contained", "true", "-o", SALIDA, "-p:DebugType=None", "-p:DebugSymbols=false"],
                       capture_output=True, text=True, encoding="utf-8", errors="replace")
    print(r.stdout[-1200:])
    if r.returncode:
        print(r.stderr[-3000:])
        sys.exit("No compiló")
    faltan = [f for f in ("hostfxr.dll", "hostpolicy.dll", "coreclr.dll", "TerminalVentanas.exe", "appsettings.json", "LICENCIAS.txt")
              if not os.path.exists(os.path.join(SALIDA, f))]
    if faltan:
        sys.exit(f"Faltan en el release: {faltan}")
    rc = json.load(open(os.path.join(SALIDA, "TerminalVentanas.runtimeconfig.json"), encoding="utf-8"))
    assert "includedFrameworks" in rc["runtimeOptions"], "No quedó self-contained"
    for prohibido in ("WebView2Loader.dll", "Microsoft.Web.WebView2.Core.dll", "wwwroot"):
        assert not os.path.exists(os.path.join(SALIDA, prohibido)), f"El release no debe llevar {prohibido}"
    print("Self-contained OK: hostfxr, hostpolicy y coreclr presentes; sin WebView2 ni páginas web;", len(os.listdir(SALIDA)), "archivos en la raíz")


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
    return zip_ruta


def probar(zip_ruta):
    limpia = tempfile.mkdtemp(prefix="terminal-ventanas-")
    zipfile.ZipFile(zip_ruta).extractall(limpia)
    exe = os.path.join(limpia, NOMBRE, "TerminalVentanas.exe")
    prueba = os.path.join(limpia, "prueba")
    r = subprocess.run([exe, "--prueba", prueba], timeout=300)
    txt = open(os.path.join(prueba, "prueba.txt"), encoding="utf-8").read()
    print(txt)
    if "FIN OK" not in txt:
        sys.exit("La prueba de punta a punta falló")
    print("Prueba de punta a punta OK desde el zip, en", prueba)


if __name__ == "__main__":
    compilar()
    z = comprimir()
    probar(z)
    print("Release:", SALIDA)
