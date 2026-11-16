using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TerminalVentanas;

/// <summary>
/// Prueba de punta a punta sin tocar la pantalla: TerminalVentanas.exe --prueba &lt;carpeta&gt;.
/// Recorre el flujo de la VT1200 del origen (paso 1 grupo en el layout, paso 2 celda, imprimir, pendientes de pesaje, registro, panel F2, colores y Excel)
/// y deja aparte la tarjeta de la báscula en cada estado (disponible, registrando, registrado, horquilla encima, sin cero, carro fuera, sin tara),
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

    /// <summary>
    /// La tarjeta de la Báscula N9 sola, para las láminas de la propuesta: en la VT1200 del origen no se muestra
    /// (la báscula tiene su propio programa), pero el motor de la simulación es el mismo.
    /// </summary>
    private async Task FotoBascula(string carpeta, string nombre)
    {
        await Task.Delay(200);
        var host = new System.Windows.Controls.Border { Width = 380, Background = Ui.Fondo, Child = Balanza() };
        host.Measure(new Size(380, double.PositiveInfinity)); host.Arrange(new Rect(host.DesiredSize)); host.UpdateLayout();
        var h = Math.Ceiling(host.ActualHeight);
        var rtb = new RenderTargetBitmap(760, (int)(h * 2), 192, 192, PixelFormats.Pbgra32);
        rtb.Render(host);
        var enc = new PngBitmapEncoder(); enc.Frames.Add(BitmapFrame.Create(rtb));
        using var fs = File.Create(Path.Combine(carpeta, nombre + ".png"));
        enc.Save(fs);
    }

    internal async Task Pasos(string c, Action<string> L)
    {
        await Foto(c, "01-ingreso");
        PedirCerrar(); await Foto(c, "01b-salir"); L($"Salir: pregunta '{S.Md?.Tipo}'");
        Cerrar(); if (S.Md != null || S.View != "login") throw new Exception("Volver no cerró la pregunta de Salir");
        ElegirTema("claro"); await Foto(c, "01c-ingreso-claro"); ElegirTema("contraste"); await Foto(c, "01d-ingreso-contraste"); ElegirTema("oscuro");
        Entrar();
        if (S.Prod != "cat") throw new Exception("La VT1200 debe quedar en cátodos");
        await Esperar(() => B.Chip == "espera", "4T21 llega a la báscula"); await Foto(c, "02-principal"); await FotoBascula(c, "bal-2-leyendo");
        await Esperar(() => B.Chip == "inestable", "4T21 registrando"); await Task.Delay(400); await FotoBascula(c, "bal-3-registrando"); L($"4T21: {B.Res.T1}");
        await Esperar(() => S.Labels.Any(l => l.Codigo == "4T21" && l.Estado == "pesada"), "4T21 registrado");
        await FotoBascula(c, "bal-4-registrado"); await Foto(c, "03-pesada"); L($"4T21: {B.Res.T1} | {B.Res.T2}");
        await Esperar(() => B.Chip == "cero", "plataforma disponible"); await Task.Delay(300); await FotoBascula(c, "bal-1-disponible");

        // Paso 1: el layout de circuitos para elegir el grupo
        Abrir("grupo"); await Foto(c, "04-paso1-grupos"); L("Paso 1: layout de circuitos");
        ElegirGrupo(21); if (S.Grupo != 21 || S.Par != 21) throw new Exception("No quedó el grupo 21");

        // Imprimir 5T21: la horquilla queda encima y la báscula la frena
        Esc.Grua = true; Imprimir(); await Task.Delay(300);
        await Foto(c, "05-imprimiendo");
        await Esperar(() => S.Fase == "impresa", "etiqueta impresa", 3000); await Foto(c, "06-impresa");
        await Esperar(() => B.Chip == "bloqueado", "horquilla encima"); await FotoBascula(c, "bal-5-horquilla"); L($"Horquilla: {B.Res.T1} | {B.Res.T2}");
        await Esperar(() => S.Labels.Any(l => l.Codigo == "5T21" && l.Estado == "pesada"), "5T21 registrado");
        await Esperar(() => B.Chip == "cero", "plataforma disponible");
        Escenario("cero"); await Esperar(() => B.Chip == "sincero", "sin cero"); await FotoBascula(c, "bal-6-sincero"); L($"Sin cero: {B.Res.T1}");
        await Esperar(() => B.Chip == "cero", "cero recuperado", 15000);

        // Restos de ánodos los rotula otra área: aquí solo se simula su paso por la báscula, para las láminas de la propuesta
        Esc.Fuera = true; Encolar(EtqRs(21, "22", 1, Hora(), D.RestosNetos[0], D.TaraCarro["22"], "Báscula"), 200);
        await Esperar(() => B.Chip == "bloqueado", "carro fuera del área"); await FotoBascula(c, "bal-7-carro-fuera"); L($"Carro fuera: {B.Res.T1}");
        await Esperar(() => B.Chip == "cero", "plataforma disponible", 20000);
        Encolar(EtqRs(21, "31", 2, Hora(), D.RestosNetos[1], null, "Báscula"), 200);
        await Esperar(() => B.Res.T2.Contains("Sin tara"), "carro sin tara", 20000); await Task.Delay(300); await FotoBascula(c, "bal-8-sin-tara"); L($"Sin tara: {B.Res.T1}");
        await Esperar(() => B.Chip == "cero", "plataforma disponible", 20000);

        // Paso 2: otro grupo (el 22) y su celda 1
        CeldaElegida(22, 1); await Foto(c, "07-grupo22"); Imprimir();
        await Esperar(() => S.Labels.Any(l => l.Codigo == "1T22"), "1T22 impresa", 5000); L("1T22 impresa desde la fila del grupo 22");
        await Esperar(() => S.Fase == null, "vuelve a la pantalla", 5000); await Foto(c, "08-pendientes");
        await Esperar(() => S.Labels.Any(l => l.Codigo == "1T22" && l.Estado == "pesada"), "1T22 registrado");

        Abrir("registro"); await Foto(c, "09-registro"); Cerrar();
        _panelAbierto = true; RenderPanel(); await Foto(c, "10-panel-f2"); _panelAbierto = false; RenderPanel();
        CeldaElegida(21, 6); await Foto(c, "11-final");

        // Los otros colores: claro, alto contraste, la ventana para elegir y el modo como Windows
        ElegirTema("claro"); await Foto(c, "21-claro"); Abrir("grupo"); await Foto(c, "22-claro-grupos"); Cerrar(); Abrir("registro"); await Foto(c, "22b-claro-registro"); Cerrar();
        ElegirTema("contraste"); await Foto(c, "23-contraste"); Abrir("grupo"); await Foto(c, "24-contraste-grupos"); Cerrar();
        Abrir("colores"); await Foto(c, "25-colores");
        ElegirTema("auto"); if (Tema.Actual != Tema.DeWindows()) throw new Exception("Como Windows no tomó los colores de Windows");
        var guardado = File.ReadAllText(Path.Combine(Persistencia.Carpeta, "tema.txt"));
        if (guardado != "auto") throw new Exception("No quedó guardado el tema elegido: " + guardado);
        L($"Colores: claro, contraste y como Windows (ahora {Tema.Actual}); tema.txt = {guardado}");
        Cerrar(); ElegirTema("oscuro");
        var ruta = Excel.Guardar(S, _cfg.CarpetaExcel); L($"Excel: {ruta} ({new FileInfo(ruta).Length} bytes)");
        var zpl = Directory.Exists(_cfg.Impresora.CarpetaZpl) ? Directory.GetFiles(_cfg.Impresora.CarpetaZpl, "*.zpl").Length : 0; L($"ZPL guardados: {zpl}");
        L($"Registro: {S.Labels.Count} · " + string.Join(" | ", S.Labels.OrderBy(l => l.Id).Select(l => $"{l.Codigo} {l.Estado} {l.Neto}")));
    }
}
