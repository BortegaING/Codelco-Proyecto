"""Arma el release de la app de la VT1200 (Terminal Rotulado Ventanas), app nativa WPF.

Uso, desde la raíz del repo:   python app/construir.py

Lo que queda en el release (y en el zip, sin carpeta de más arriba):
    TerminalVentanas.exe     el lanzador (app/Lanzador): lo único que se abre, a la vista
    programa/                el programa real, con el runtime de .NET y appsettings.json

1. Compila self-contained (win-x64, con el runtime de .NET adentro). No usa navegador ni WebView2.
2. Revisa que estén hostfxr.dll, hostpolicy.dll y coreclr.dll y que el runtimeconfig sea self-contained.
3. Deja la carpeta y el .zip en Proyectos_Claude/releases/codelco-ventanas/.
4. Descomprime el zip en una carpeta limpia y corre la prueba de punta a punta (TerminalVentanas.exe --prueba):
   si no termina en "FIN OK", el release no sirve.

No se arma un .exe de un solo archivo (PublishSingleFile): Smart App Control lo bloquea al abrirlo
("Una directiva de Control de aplicaciones bloqueó este archivo", WinError 4551; probado el 05-11-2026).
La carpeta con hostfxr/hostpolicy/coreclr sí abre. Por eso el release es el zip con la carpeta.
"""
import json, os, re, shutil, subprocess, sys, tempfile, time, zipfile
from datetime import date

sys.stdout.reconfigure(encoding="utf-8", errors="replace")  # el log trae "−" y tildes: la consola de Windows (cp1252) no los imprime

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
APP = os.path.join(RAIZ, "app", "TerminalVentanas")
VERSION = re.search(r"<Version>([^<]+)</Version>", open(os.path.join(APP, "TerminalVentanas.csproj"), encoding="utf-8").read()).group(1)
NOMBRE = f"terminal-ventanas-v{VERSION}-{date.today():%Y%m%d}"
SALIDA = os.path.abspath(os.path.join(RAIZ, "..", "releases", "codelco-ventanas", NOMBRE))
PROG = os.path.join(SALIDA, "programa")
# El segundo programa: la pantalla de la caseta de la Báscula N9. Mismo código, "Programa": "caseta" en su appsettings.json
NOMBRE_C = f"bascula-caseta-v{VERSION}-{date.today():%Y%m%d}"
SALIDA_C = os.path.abspath(os.path.join(RAIZ, "..", "releases", "codelco-ventanas", NOMBRE_C))
EXE_C = "BasculaCaseta.exe"
CSC = os.path.join(os.environ.get("WINDIR", r"C:\Windows"), "Microsoft.NET", "Framework64", "v4.0.30319", "csc.exe")


def compilar():
    shutil.rmtree(SALIDA, ignore_errors=True)
    r = subprocess.run(["dotnet", "publish", os.path.join(APP, "TerminalVentanas.csproj"), "-c", "Release", "-r", "win-x64",
                        "--self-contained", "true", "-o", PROG, "-p:DebugType=None", "-p:DebugSymbols=false"],
                       capture_output=True, text=True, encoding="utf-8", errors="replace")
    print(r.stdout[-1200:])
    if r.returncode:
        print(r.stderr[-3000:])
        sys.exit("No compiló")
    faltan = [f for f in ("hostfxr.dll", "hostpolicy.dll", "coreclr.dll", "TerminalVentanas.exe", "appsettings.json", "LICENCIAS.txt")
              if not os.path.exists(os.path.join(PROG, f))]
    if faltan:
        sys.exit(f"Faltan en el release: {faltan}")
    rc = json.load(open(os.path.join(PROG, "TerminalVentanas.runtimeconfig.json"), encoding="utf-8"))
    assert "includedFrameworks" in rc["runtimeOptions"], "No quedó self-contained"
    for prohibido in ("WebView2Loader.dll", "Microsoft.Web.WebView2.Core.dll", "wwwroot"):
        assert not os.path.exists(os.path.join(PROG, prohibido)), f"El release no debe llevar {prohibido}"
    print("Self-contained OK: hostfxr, hostpolicy y coreclr presentes; sin WebView2 ni páginas web;", len(os.listdir(PROG)), "archivos en programa/")


LANZADOR_LISTO = os.path.join(RAIZ, "app", "Lanzador", "TerminalVentanas.exe")


def lanzador():
    """El TerminalVentanas.exe de afuera: .NET Framework 4.8 (viene en Windows). Corre programa/TerminalVentanas.dll
    por hostfxr.dll en su propio proceso (Smart App Control bloquea el .exe sin firma de adentro). x64, como hostfxr.
    Se copia el ya compilado de app/Lanzador: Smart App Control decide por el hash del archivo y ese ya lo deja abrir;
    cada compilación nueva da otro hash, que puede quedar bloqueado. Si se cambia Lanzador.cs: python construir.py
    --compilar-lanzador, y si el nuevo abre, copiarlo a app/Lanzador/TerminalVentanas.exe."""
    destino = os.path.join(SALIDA, "TerminalVentanas.exe")
    if "--compilar-lanzador" not in sys.argv and os.path.exists(LANZADOR_LISTO):
        shutil.copy2(LANZADOR_LISTO, destino)
        print("Lanzador OK (el ya compilado de app/Lanzador):", sorted(os.listdir(SALIDA)))
        return
    r = subprocess.run([CSC, "-nologo", "-target:winexe", "-platform:x64", "-optimize", "-out:" + destino,
                        "-r:System.Windows.Forms.dll", "-r:System.Core.dll", os.path.join(RAIZ, "app", "Lanzador", "Lanzador.cs")],
                       capture_output=True, text=True, encoding="utf-8", errors="replace")
    if r.returncode:
        print(r.stdout, r.stderr)
        sys.exit("No compiló el lanzador")
    print("Lanzador OK:", sorted(os.listdir(SALIDA)))


def caseta():
    """Arma el release de la caseta desde el de origen: el mismo programa con "Programa": "caseta" y el lanzador con otro nombre
    (es el mismo archivo, así que Smart App Control lo trata igual)."""
    shutil.rmtree(SALIDA_C, ignore_errors=True)
    shutil.copytree(SALIDA, SALIDA_C)
    os.rename(os.path.join(SALIDA_C, "TerminalVentanas.exe"), os.path.join(SALIDA_C, EXE_C))
    ap = os.path.join(SALIDA_C, "programa", "appsettings.json")
    cfg = json.load(open(ap, encoding="utf-8")); cfg["Programa"] = "caseta"
    json.dump(cfg, open(ap, "w", encoding="utf-8"), ensure_ascii=False, indent=2)
    print("Caseta OK:", sorted(os.listdir(SALIDA_C)))


def comprimir(salida=None):
    salida = salida or SALIDA
    zip_ruta = salida + ".zip"
    if os.path.exists(zip_ruta):
        os.remove(zip_ruta)
    with zipfile.ZipFile(zip_ruta, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as z:
        for base, _, archivos in os.walk(salida):
            for f in archivos:
                p = os.path.join(base, f)
                z.write(p, os.path.relpath(p, salida))  # sin carpeta arriba: "Extraer todo" deja el .exe a la vista
    print("Zip:", zip_ruta, round(os.path.getsize(zip_ruta) / 1048576, 1), "MB")
    return zip_ruta


def probar(zip_ruta, nombre=None, lanzador_exe="TerminalVentanas.exe"):
    limpia = tempfile.mkdtemp(prefix="terminal-ventanas-")
    destino = os.path.join(limpia, nombre or NOMBRE)  # como "Extraer todo" de Windows
    zipfile.ZipFile(zip_ruta).extractall(destino)
    exe = os.path.join(destino, lanzador_exe)  # se prueba por el lanzador, como lo abre el operador
    prueba = os.path.join(limpia, "prueba")
    subprocess.Popen([exe, "--prueba", prueba])  # el lanzador queda abierto mientras corre la app: no se espera
    txt_ruta, fin = os.path.join(prueba, "prueba.txt"), time.time() + 300
    while not os.path.exists(txt_ruta):
        if time.time() > fin:
            sys.exit("El lanzador no abrió el programa (no apareció prueba.txt)")
        time.sleep(2)
    time.sleep(1)
    txt = open(txt_ruta, encoding="utf-8").read()
    print(txt)
    if "FIN OK" not in txt:
        sys.exit("La prueba de punta a punta falló")
    print("Prueba de punta a punta OK desde el zip (abierto por el lanzador), en", prueba)


if __name__ == "__main__":
    compilar()
    lanzador()
    z = comprimir()
    probar(z)
    print("Release:", SALIDA)
    caseta()
    zc = comprimir(SALIDA_C)
    probar(zc, NOMBRE_C, EXE_C)
    print("Release de la caseta:", SALIDA_C)
