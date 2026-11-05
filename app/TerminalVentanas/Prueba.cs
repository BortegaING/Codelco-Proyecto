using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TerminalVentanas;

/// <summary>
/// Prueba de punta a punta sin tocar la pantalla: TerminalVentanas.exe --prueba &lt;carpeta&gt;.
/// Recorre el flujo (cátodos, grúa encima, láminas, restos sin tara, registro, corregir la tara, caja sin etiqueta, panel F2 y Excel),
/// guarda una captura de cada paso, el Excel y los .zpl en la carpeta, y escribe prueba.txt con lo que pasó. No usa el registro real.
/// </summary>
internal static class Prueba
{
    public static async Task Correr(MainWindow w, string carpeta)
    {
        Directory.CreateDirectory(carpeta);
        var log = new StringBuilder();
        void L(string s) { log.AppendLine($"{DateTime.Now:HH:mm:ss} {s}"); }
        try
        {
            await w.Pasos(carpeta, L);
            L("FIN OK");
        }
        catch (Exception ex) { L("ERROR " + ex); }
        File.WriteAllText(Path.Combine(carpeta, "prueba.txt"), log.ToString(), new UTF8Encoding(false));
        w.CerrarPrograma(); // termina igual que el botón Cerrar programa: si el proceso no se cierra, construir.py lo nota

    }
}

public partial class MainWindow
{
    private async Task Esperar(Func<bool> cond, string que, int ms = 30000)
    {
        var fin = DateTime.Now.AddMilliseconds(ms);
        while (!cond()) { if (DateTime.Now > fin) throw new TimeoutException("No pasó: " + que); await Task.Delay(100); }
    }

    private async Task Foto(string carpeta, string nombre)
    {
        await Task.Delay(180);
        Pantalla.UpdateLayout();
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen()) dc.DrawRectangle(new VisualBrush(Pantalla), null, new Rect(0, 0, 1024, 768));
        var rtb = new RenderTargetBitmap(2048, 1536, 192, 192, PixelFormats.Pbgra32);
        rtb.Render(dv);
        var enc = new PngBitmapEncoder(); enc.Frames.Add(BitmapFrame.Create(rtb));
        using var fs = File.Create(Path.Combine(carpeta, nombre + ".png"));
        enc.Save(fs);
    }

    internal async Task Pasos(string c, Action<string> L)
    {
        await Foto(c, "01-ingreso");
        PedirCerrar(); await Foto(c, "01b-salir"); L($"Salir: pregunta '{S.Md?.Tipo}'");
        Cerrar(); if (S.Md != null || S.View != "login") throw new Exception("Volver no cerró la pregunta de Salir");
        Entrar();
        await Esperar(() => B.Chip == "inestable", "4T21 estabilizando"); await Task.Delay(400);
        await Foto(c, "02-catodos-pesando"); L($"4T21 estabilizando: {B.Res.T1}");
        await Esperar(() => S.Labels.Any(l => l.Codigo == "4T21" && l.Estado == "pesada"), "4T21 registrado");
        await Foto(c, "03-catodos-registrado"); L($"4T21: {B.Res.T1} | {B.Res.T2}");
        await Esperar(() => B.Chip == "cero", "báscula libre");

        Esc.Grua = true; Imprimir(); await Task.Delay(300);
        await Foto(c, "04-imprimiendo");
        await Esperar(() => S.Fase == "impresa", "etiqueta impresa", 3000); await Foto(c, "05-impresa");
        await Esperar(() => B.Chip == "bloqueado", "grúa encima"); await Foto(c, "06-grua-encima"); L($"Grúa: {B.Res.T1} | {B.Res.T2} | lectura {Math.Round(B.Lect)}");
        await Esperar(() => S.Labels.Any(l => l.Codigo == "5T21" && l.Estado == "pesada"), "5T21 registrado");
        L($"5T21: {S.Labels.First(l => l.Codigo == "5T21").Neto} kg · {S.Labels.First(l => l.Codigo == "5T21").Ctrl}");
        await Esperar(() => B.Chip == "cero", "báscula libre");

        Prod("lam"); Imprimir();
        await Esperar(() => B.Carga?.Prod == "lam" && B.Chip == "estable", "lámina estable"); await Task.Delay(300);
        await Foto(c, "07-laminas"); L($"Lámina: {B.Res.T1} | {B.Res.T2}");
        await Esperar(() => B.Chip == "cero", "báscula libre", 15000);

        Prod("rs"); Carro("2"); Carro("2"); await Foto(c, "08-restos-carro22"); Imprimir(); await Task.Delay(1900);
        Carro("3"); Carro("1"); Imprimir();
        await Esperar(() => B.Res.T2.Contains("Sin tara"), "C31 sin tara", 40000); await Task.Delay(300);
        await Foto(c, "09-restos-sin-tara"); L($"C31: {B.Res.T1} | {B.Res.T2}");
        await Esperar(() => B.Chip == "cero", "báscula libre", 15000);

        Abrir("registro"); await Foto(c, "10-registro"); Cerrar();
        var c31 = S.Labels.First(l => l.Ref.Carro == "31");
        Editar(c31.Id); await Foto(c, "11-bascula-editar-c31");
        BfCampo("tara"); foreach (var d in "2190") Kp(d.ToString()); await Foto(c, "12-teclado-tara"); OkBfkp();
        await Foto(c, "13-tara-digitada"); GrabarBF(); await Foto(c, "14-grabado");
        L($"C31 corregido: estado {c31.Estado}, bruto {c31.Bruto}, tara {c31.Tara}, neto {c31.Neto} · {c31.Ctrl}");
        CerrarBas();

        Escenario("caja");
        await Esperar(() => B.Esperando, "caja sin etiqueta en la báscula");
        AbrirBas(); await Foto(c, "15-caja-sin-etiqueta");
        BfCampo("st"); await Foto(c, "16-que-es"); SetBfop("dg");
        GrabarBF(); await Foto(c, "17-caja-grabada");
        var dg = S.Labels.First(l => l.Prod == "sub"); L($"Caja: {dg.Codigo} {dg.Neto} kg · {dg.Ctrl} · {dg.Nota}");
        await Esperar(() => B.Chip == "cero", "báscula libre", 15000);
        Turno("C"); ControlCero(); await Foto(c, "18-turno-cero"); L($"Turno {S.Turno}, cero {S.Cero?.H} {S.Cero?.Ok}");
        CerrarBas();

        _panelAbierto = true; RenderPanel(); await Foto(c, "19-panel-f2"); _panelAbierto = false; RenderPanel();
        Prod("cat"); await Foto(c, "20-catodos-final");
        var ruta = Excel.Guardar(S, _cfg.CarpetaExcel); L($"Excel: {ruta} ({new FileInfo(ruta).Length} bytes)");
        var zpl = Directory.Exists(_cfg.Impresora.CarpetaZpl) ? Directory.GetFiles(_cfg.Impresora.CarpetaZpl, "*.zpl").Length : 0; L($"ZPL guardados: {zpl}");
        L($"Registro: {S.Labels.Count} · " + string.Join(" | ", S.Labels.OrderBy(l => l.Id).Select(l => $"{l.Codigo} {l.Estado} {l.Neto}")));
    }
}
