"""Saca las capturas que usa generar.js (maqueta VT1200 y simulación), con Playwright y Brave en modo headless.
Uso: python capturas.py   (otro navegador Chromium: variable NAVEGADOR con la ruta del ejecutable)
"""
import os
from playwright.sync_api import sync_playwright
brave = os.environ.get("NAVEGADOR", r"C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe")
base = "file:///" + os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")).replace(os.sep, "/") + "/"
out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "img"); os.makedirs(out, exist_ok=True)
STUB = """window.claude={use:(n)=>Promise.resolve(n==='downloads'?{save:async()=>({status:'saved'})}:null)};"""
CHIP = "t=>{const c=document.getElementById('balChip');return c&&c.textContent.trim().toLowerCase()===t}"
RES = "t=>{const r=document.getElementById('balRes');return r&&r.textContent.toLowerCase().includes(t)}"
errs=[]
with sync_playwright() as p:
    b = p.chromium.launch(executable_path=brave, headless=True, args=["--no-first-run"])
    ctx = b.new_context(viewport={"width":1440,"height":1000}, device_scale_factor=2)
    ctx.add_init_script(STUB)
    pg = ctx.new_page(); pg.on("pageerror", lambda e: errs.append(str(e)))
    pg.goto(base+"terminal-vt1200.html", wait_until="load", timeout=90000); pg.wait_for_timeout(1200)
    def act(a,v=None,w=250):
        pg.locator(f'#vt [data-act="{a}"]'+(f'[data-v="{v}"]' if v else '')).first.click(); pg.wait_for_timeout(w)
    chip=lambda t,ms=25000: pg.wait_for_function(CHIP, arg=t, timeout=ms)
    res=lambda t,ms=25000: pg.wait_for_function(RES, arg=t, timeout=ms)
    bal=lambda n: pg.locator('#vt .balanza').screenshot(path=f"{out}/bal_{n}.png")
    dev=lambda n: pg.locator('#marco').screenshot(path=f"{out}/vt_{n}.png")
    dev('login')
    act('entrar', w=100)
    pg.locator('#vt .sticker').screenshot(path=f"{out}/etq_cat.png")
    pg.wait_for_function("()=>document.getElementById('balChip').textContent.trim().toLowerCase()==='llegando'", timeout=15000)
    pg.wait_for_timeout(150); bal('1llegando')
    chip('estabilizando'); pg.wait_for_timeout(450); bal('2estabilizando'); dev('cat_pesando')
    res('registrado'); bal('3registrado'); dev('cat')
    chip('en cero', 10000)
    pg.locator('[data-esc="grua"]').click(); act('imprimir', w=1500)
    chip('bloqueado'); pg.wait_for_timeout(200); bal('4grua')
    res('registrado'); chip('en cero', 10000)
    pg.locator('[data-esc="cero"]').click(); pg.wait_for_timeout(500); bal('5sincero')
    res('cero recuperado', 9000)
    act('prod','lam'); pg.locator('#vt .sticker').screenshot(path=f"{out}/etq_lam.png")
    act('imprimir', w=1900); res('neto'); chip('estable'); pg.wait_for_timeout(300); bal('6lam'); dev('lam')
    res('registrado')
    act('prod','rs'); pg.locator('[data-esc="fuera"]').click()
    act('carro','2'); act('carro','2'); pg.locator('#vt .sticker').screenshot(path=f"{out}/etq_rs.png"); act('imprimir', w=1900)
    act('carro','3'); act('carro','1'); act('imprimir', w=1900)
    chip('bloqueado', 30000); pg.wait_for_timeout(300); bal('7fuera')
    res('dentro de rango', 20000); pg.wait_for_timeout(200); bal('8rs_ok')
    res('sin tara', 30000); pg.wait_for_timeout(300); bal('9sintara'); dev('rs')
    chip('en cero', 12000)
    act('prod','cat'); act('registro', w=500); dev('registro'); act('cerrar')
    ctx.close()
    # Simulacion (pagina principal) en modo claro
    ctx = b.new_context(viewport={"width":1440,"height":1000}, device_scale_factor=2, color_scheme="light")
    pg = ctx.new_page(); pg.on("pageerror", lambda e: errs.append(str(e)))
    pg.goto(base+"simulacion-bascula.html", wait_until="load", timeout=90000); pg.wait_for_timeout(2500)
    for sid in ["simulacion","flujo","etiqueta","reglas","sistema","registro"]:
        el = pg.locator(f"#{sid}")
        el.scroll_into_view_if_needed(); pg.wait_for_timeout(500)
        el.screenshot(path=f"{out}/sim_{sid}.png")
    b.close()
from PIL import Image
im = Image.open(f"{out}/sim_simulacion.png"); im.crop((218, 250, 1880, 1235)).save(f"{out}/sim_escena.png")
print("listo", sorted(os.listdir(out)), "errores:", errs)
