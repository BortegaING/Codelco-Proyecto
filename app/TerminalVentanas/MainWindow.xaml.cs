using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;

namespace TerminalVentanas;

/// <summary>
/// La pantalla de la VT1200: muestra el terminal (wwwroot/index.html, sin internet) a pantalla completa.
/// El equipo hace lo que la página no puede: guardar el Excel en la carpeta de la báscula e imprimir en la Zebra.
/// </summary>
public partial class MainWindow : Window
{
    private const string Host = "terminal.ventanas";
    private readonly Config _cfg = Config.Leer();

    public MainWindow()
    {
        InitializeComponent();
        if (_cfg.PantallaCompleta) PantallaCompleta(true);
        PreviewKeyDown += (_, e) => { if (e.Key == Key.F11) { PantallaCompleta(WindowStyle != WindowStyle.None); e.Handled = true; } };
        Loaded += async (_, _) => await IniciarAsync();
    }

    private void PantallaCompleta(bool si)
    {
        WindowStyle = si ? WindowStyle.None : WindowStyle.SingleBorderWindow;
        ResizeMode = si ? ResizeMode.NoResize : ResizeMode.CanResize;
        WindowState = si ? WindowState.Maximized : WindowState.Normal;
    }

    private async Task IniciarAsync()
    {
        var www = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        if (!File.Exists(Path.Combine(www, "index.html")))
        {
            MostrarAviso($"Falta la carpeta wwwroot junto al programa ({www}). Copie la carpeta completa del release.");
            return;
        }
        try
        {
            // Los datos de la página (el registro del día) quedan en el perfil del usuario y sobreviven a reinicios
            var datos = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IngMetrica", "TerminalVentanas");
            var entorno = await CoreWebView2Environment.CreateAsync(null, datos);
            await Web.EnsureCoreWebView2Async(entorno);
        }
        catch (WebView2RuntimeNotFoundException)
        {
            MostrarAviso("A este equipo le falta Microsoft Edge WebView2 Runtime, que el terminal necesita para mostrar la pantalla. " +
                         "Instálelo con el instalador sin conexión de Microsoft (WebView2 Runtime x64) y vuelva a abrir el terminal.");
            return;
        }
        catch (Exception ex)
        {
            MostrarAviso("No se pudo abrir la pantalla del terminal: " + ex.Message);
            return;
        }

        var core = Web.CoreWebView2;
        core.SetVirtualHostNameToFolderMapping(Host, www, CoreWebView2HostResourceAccessKind.Allow);
        var s = core.Settings;
        s.AreDefaultContextMenusEnabled = _cfg.HerramientasDesarrollo;
        s.AreDevToolsEnabled = _cfg.HerramientasDesarrollo;
        s.AreBrowserAcceleratorKeysEnabled = _cfg.HerramientasDesarrollo;
        s.IsZoomControlEnabled = false;
        s.IsStatusBarEnabled = false;
        core.NewWindowRequested += (_, e) => e.Handled = true;
        core.DownloadStarting += AlBajarExcel;
        core.WebMessageReceived += AlRecibirMensaje;
        core.Navigate($"https://{Host}/index.html");
    }

    /// <summary>El Excel no pregunta dónde guardarse: va directo a la carpeta de la báscula, con la hora en el nombre.</summary>
    private void AlBajarExcel(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(_cfg.CarpetaExcel);
            var nombre = Path.GetFileNameWithoutExtension(e.ResultFilePath);
            var ruta = Path.Combine(_cfg.CarpetaExcel, $"{nombre} {DateTime.Now:yyyyMMdd-HHmmss}{Path.GetExtension(e.ResultFilePath)}");
            e.ResultFilePath = ruta;
            e.Handled = true;
            e.DownloadOperation.StateChanged += (op, _) =>
            {
                var d = (CoreWebView2DownloadOperation)op!;
                if (d.State == CoreWebView2DownloadState.Completed) Avisar(new { tipo = "excel", ok = true, ruta });
                else if (d.State == CoreWebView2DownloadState.Interrupted) Avisar(new { tipo = "excel", ok = false, error = d.InterruptReason.ToString() });
            };
        }
        catch (Exception ex)
        {
            e.Cancel = true;
            Avisar(new { tipo = "excel", ok = false, error = ex.Message });
        }
    }

    /// <summary>La página pide imprimir: llega el ZPL de la etiqueta y se manda a la Zebra (o a un .zpl en modo seco).</summary>
    private void AlRecibirMensaje(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var doc = JsonDocument.Parse(e.WebMessageAsJson);
            var m = doc.RootElement;
            if (!m.TryGetProperty("tipo", out var tipo) || tipo.GetString() != "imprimir") return;
            var codigo = m.TryGetProperty("codigo", out var c) ? c.GetString() ?? "" : "";
            var zpl = m.TryGetProperty("zpl", out var z) ? z.GetString() ?? "" : "";
            var destino = Impresion.Enviar(_cfg.Impresora, codigo, zpl);
            Avisar(new { tipo = "impresion", ok = true, destino });
        }
        catch (Exception ex)
        {
            Avisar(new { tipo = "impresion", ok = false, error = ex.Message });
        }
    }

    private void Avisar(object mensaje)
    {
        try { Web.CoreWebView2?.PostWebMessageAsJson(JsonSerializer.Serialize(mensaje)); } catch { /* la página se está recargando */ }
    }

    private void MostrarAviso(string texto)
    {
        AvisoTexto.Text = texto;
        Aviso.Visibility = Visibility.Visible;
    }
}
