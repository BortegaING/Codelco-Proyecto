using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace TerminalVentanas;

/// <summary>
/// La pantalla de la VT1200 en la nave. Todo se dibuja desde el estado (como en la maqueta): cada cambio llama a Render().
/// La báscula se simula aparte y solo actualiza su número, su estado y su dibujo.
/// </summary>
public partial class MainWindow : Window
{
    private readonly Config _cfg = Config.Leer();
    private readonly string? _carpetaPrueba;
    private Estado S = new();
    private Bascula B = new();
    private readonly Queue<Etiqueta> _cola = new();
    private bool _ocupada;
    private int _gen;
    private readonly DispatcherTimer _ticker = new() { Interval = TimeSpan.FromMilliseconds(120) };
    private readonly DispatcherTimer _toastT = new() { Interval = TimeSpan.FromMilliseconds(3800) };
    private bool _panelAbierto;
    private readonly Random _azar = new();

    public MainWindow()
    {
        InitializeComponent();
        var args = Environment.GetCommandLineArgs();
        var i = Array.IndexOf(args, "--prueba");
        if (i >= 0 && i + 1 < args.Length) _carpetaPrueba = args[i + 1];
        if (_carpetaPrueba != null)
        {
            // La prueba corre sola: fuera de la pantalla, sin quitar el foco y sin aceptar toques ni teclas
            ShowActivated = false; WindowStartupLocation = WindowStartupLocation.Manual; Left = -4000; Top = 0;
            Pantalla.IsHitTestVisible = false;
            Persistencia.Carpeta = System.IO.Path.Combine(_carpetaPrueba, "datos");
            _cfg.CarpetaExcel = System.IO.Path.Combine(_carpetaPrueba, "Excel");
            _cfg.Impresora.Modo = "seco";
            _cfg.Impresora.CarpetaZpl = System.IO.Path.Combine(_carpetaPrueba, "Etiquetas");
        }
        else if (_cfg.PantallaCompleta) PantallaCompleta(true);
        _ticker.Tick += (_, _) => Leer();
        _toastT.Tick += (_, _) => { Toast.Visibility = Visibility.Collapsed; _toastT.Stop(); };
        PreviewKeyDown += AlTeclear;
        Loaded += async (_, _) =>
        {
            Arrancar(_carpetaPrueba == null);
            _ticker.Start();
            if (_carpetaPrueba != null) await Prueba.Correr(this, _carpetaPrueba);
        };
    }

    private void PantallaCompleta(bool si)
    {
        WindowStyle = si ? WindowStyle.None : WindowStyle.SingleBorderWindow;
        ResizeMode = si ? ResizeMode.NoResize : ResizeMode.CanResize;
        WindowState = si ? WindowState.Maximized : WindowState.Normal;
    }

    /// <summary>Parte de cero o retoma lo registrado en el equipo. Lo que quedó en camino vuelve a la fila de la báscula.</summary>
    internal void Arrancar(bool conGuardado)
    {
        _gen++; _cola.Clear(); _ocupada = false; Toast.Visibility = Visibility.Collapsed;
        B = new Bascula(); Esc.Grua = Esc.Fuera = Esc.Cero = false;
        S = EstadoInicial();
        var o = conGuardado ? Persistencia.Recuperar() : null;
        if (o != null && o.Labels.Count > 0) { S = o; S.View = "login"; S.Rut = "111111111"; }
        var pend = S.Labels.Where(l => l.Estado is "camino" or "bascula").Reverse().ToList();
        foreach (var l in pend) Marcar(l, "camino");
        Render();
        foreach (var l in pend) Encolar(l, 2500);
    }

    internal void Render()
    {
        var lista = Capa.Children.Count > 0 ? Buscar<ScrollViewer>(Capa) : null;
        var arriba = lista?.VerticalOffset ?? 0;
        Raiz.Children.Clear();
        Raiz.Children.Add(S.View == "login" ? VLogin() : S.View == "bas" ? VBas() : VMain(S.Snap ?? ModeloActual()));
        Capa.Children.Clear();
        if (S.Md != null)
        {
            Capa.Children.Add(VModal(S.Md));
            Capa.Visibility = Visibility.Visible;
            if (arriba > 0) Dispatcher.BeginInvoke(() => Buscar<ScrollViewer>(Capa)?.ScrollToVerticalOffset(arriba), DispatcherPriority.Loaded);
        }
        else Capa.Visibility = Visibility.Collapsed;
        RenderPanel();
        if (S.View != "login") Persistencia.Guardar(S);
    }

    private static T? Buscar<T>(DependencyObject raiz) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(raiz); i++)
        {
            var h = VisualTreeHelper.GetChild(raiz, i);
            if (h is T t) return t;
            var r = Buscar<T>(h);
            if (r != null) return r;
        }
        return null;
    }

    internal void Avisar(string txt, string k = "info")
    {
        var (fondo, borde, color) = k switch
        {
            "ok" => ("#E3F2EA", "#1E7B4F", "#1E7B4F"), "warn" => ("#FFF4D6", "#E0B54A", "#8A5300"),
            "err" => ("#FDECEA", "#B42318", "#B42318"), _ => ("#E1EEF6", "#015D93", "#015D93"),
        };
        Toast.Background = Ui.B(fondo); Toast.BorderBrush = Ui.B(borde);
        var t = Ui.T(txt, 18, FontWeights.SemiBold, Ui.B(color), ajustar: true).Margen(12, 0, 0, 0);
        Toast.Child = Ui.Fila(Ui.Ico(k switch { "ok" => Iconos.Ok, "warn" => Iconos.Warn, "err" => Iconos.Err, _ => Iconos.Info }, 26, 26, Ui.B(color), 2.4), t);
        Toast.Effect ??= new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 24, ShadowDepth = 6, Direction = 270, Opacity = 0.2 };
        Toast.Visibility = Visibility.Visible;
        _toastT.Stop(); _toastT.Start();
    }

    private void AlTeclear(object sender, KeyEventArgs e)
    {
        if (S.Fase != null || _carpetaPrueba != null) return;
        if (e.Key == Key.F11) { PantallaCompleta(WindowStyle != WindowStyle.None); e.Handled = true; return; }
        if (e.Key == Key.F2 && S.View != "login") { _panelAbierto = !_panelAbierto; RenderPanel(); e.Handled = true; return; }
        if (e.Key == Key.Escape)
        {
            if (_panelAbierto) { _panelAbierto = false; RenderPanel(); }
            else if (S.Md != null) Cerrar();
            else if (S.View == "bas") CerrarBas();
            e.Handled = true; return;
        }
        if (S.Md?.Tipo == "cerrar") return;
        var digito = e.Key is >= Key.D0 and <= Key.D9 ? (char)('0' + (e.Key - Key.D0)) : e.Key is >= Key.NumPad0 and <= Key.NumPad9 ? (char)('0' + (e.Key - Key.NumPad0)) : (char?)null;
        var kp = S.Md != null && S.Md.Tipo is "grupo" or "rsgrupo" or "bfkp";
        Action<string>? destino = kp ? Kp : S.View == "login" ? Rut : S.Md == null && S.View == "main" && S.Prod == "rs" ? Carro : null;
        if (S.View == "login" && S.Md == null && e.Key == Key.K) { Rut("K"); e.Handled = true; return; }
        if (destino != null && digito != null) { destino(digito.Value.ToString()); e.Handled = true; }
        else if (destino != null && e.Key == Key.Back) { destino("del"); e.Handled = true; }
        else if (e.Key == Key.Enter)
        {
            if (S.View == "login") Entrar();
            else if (kp) OkKeypad();
            else if (S.Md == null && S.View == "bas") GrabarBF();
            else if (S.Md == null) Imprimir();
            e.Handled = true;
        }
    }
}
