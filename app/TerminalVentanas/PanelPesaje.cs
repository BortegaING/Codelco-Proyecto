using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static System.Windows.FontWeights;

namespace TerminalVentanas;

/// <summary>
/// Panel de control de pesaje del programa de la caseta: una ventana aparte que se abre con el botón PESAJE de la barra o con F3.
/// Se pega al costado de la ventana del programa y la achica lo justo para no taparla: el pesador coloca el peso y sigue
/// viendo la pantalla de la caseta. Dos tamaños:
///  - Completo: una columna a todo el alto, con la lectura en vivo, el peso a colocar, los pesos rápidos y las situaciones.
///  - Compacto: una cajita en la esquina de arriba o de abajo, con la lectura, el peso a colocar y los pesos rápidos.
/// Al cerrarlo, el programa vuelve al tamaño y lugar que tenía (y a pantalla completa, si estaba así).
/// </summary>
public sealed class VentanaPesaje : Window
{
    internal const double AnchoCompleto = 340, AnchoCompacto = 300, AltoCompacto = 336;
    /// <summary>La ventana del programa: la pantalla de 1024 x 768 más el marco de Windows (la misma proporción del XAML).</summary>
    private const double AnchoPrograma = 1040, AltoPrograma = 807;

    private readonly MainWindow _m;
    private readonly Rect _antes;
    private readonly WindowState _estadoAntes;
    private readonly bool _completaAntes;
    private bool _appCerrando;
    internal bool Compacto, Derecha = true, Arriba;

    public VentanaPesaje(MainWindow m, bool completaAntes)
    {
        _m = m; _completaAntes = completaAntes;
        _antes = new Rect(m.Left, m.Top, m.Width, m.Height); _estadoAntes = m.WindowState;
        Owner = m; Title = "Panel de pesaje"; ShowInTaskbar = false; WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.Manual; UseLayoutRounding = true; SnapsToDevicePixels = true;
        EventHandler seguir = (_, _) => Pegar();
        SizeChangedEventHandler seguirTam = (_, _) => Pegar();
        System.ComponentModel.CancelEventHandler cierra = (_, _) => _appCerrando = true;
        m.LocationChanged += seguir; m.SizeChanged += seguirTam; m.Closing += cierra;
        Closed += (_, _) =>
        {
            m.LocationChanged -= seguir; m.SizeChanged -= seguirTam; m.Closing -= cierra;
            m.AlCerrarPesaje(_antes, _estadoAntes, _completaAntes, _appCerrando);
        };
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key is Key.Escape or Key.F3) { Close(); e.Handled = true; }
            else if (e.Key == Key.F11) e.Handled = true; // la pantalla completa taparía el panel
        };
    }

    /// <summary>Vuelve a dibujar el panel (al abrirlo, al cambiar de tamaño o de colores).</summary>
    internal void Dibujar()
    {
        Background = Ui.Fondo; BorderBrush = Ui.Cobre; BorderThickness = new Thickness(1);
        Content = _m.VistaPesaje(this);
    }

    /// <summary>
    /// Deja libre el costado del panel: el programa queda con la proporción de la pantalla de la VT1200, lo más grande posible
    /// en lo que sobra del escritorio, y el panel se pega a su lado (izquierdo o derecho).
    /// </summary>
    internal void Acomodar()
    {
        var (prog, _) = Disposicion(SystemParameters.WorkArea, Compacto, Derecha, Arriba);
        _m.WindowState = WindowState.Normal;
        _m.Width = prog.Width; _m.Height = prog.Height; _m.Left = prog.Left; _m.Top = prog.Top;
        Pegar();
    }

    /// <summary>
    /// Dónde va el programa y dónde el panel dentro del área de trabajo del escritorio (sin la barra de tareas):
    /// el programa, lo más grande posible con la proporción de la pantalla de la VT1200; el panel, pegado a su costado.
    /// Es un cálculo aparte para que la prueba lo revise con varios tamaños de pantalla.
    /// </summary>
    internal static (Rect Programa, Rect Panel) Disposicion(Rect wa, bool compacto, bool derecha, bool arriba)
    {
        var ancho = compacto ? AnchoCompacto : AnchoCompleto;
        double h = wa.Height, w = h * AnchoPrograma / AltoPrograma;
        if (w > wa.Width - ancho) { w = wa.Width - ancho; h = w * AltoPrograma / AnchoPrograma; }
        w = Math.Floor(w); h = Math.Floor(h);
        var prog = new Rect(derecha ? wa.Left : wa.Right - w, wa.Top, w, h);
        var altoPanel = compacto ? AltoCompacto : h;
        var panel = new Rect(derecha ? prog.Right : prog.Left - ancho, compacto && !arriba ? prog.Bottom - AltoCompacto : prog.Top, ancho, altoPanel);
        return (prog, panel);
    }

    /// <summary>Pega el panel al costado de la ventana del programa (la sigue si la mueven).</summary>
    internal void Pegar()
    {
        if (_appCerrando || !_m.IsLoaded) return;
        var mw = double.IsNaN(_m.Width) ? _m.ActualWidth : _m.Width;
        var mh = double.IsNaN(_m.Height) ? _m.ActualHeight : _m.Height;
        Width = Compacto ? AnchoCompacto : AnchoCompleto;
        Height = Compacto ? AltoCompacto : mh;
        Left = Derecha ? _m.Left + mw : _m.Left - Width;
        Top = Compacto && !Arriba ? _m.Top + mh - AltoCompacto : _m.Top;
    }

    internal void CambiarTamano() { Compacto = !Compacto; Dibujar(); Acomodar(); }
    internal void CambiarLado() { Derecha = !Derecha; Acomodar(); }
    internal void CambiarEsquina() { Arriba = !Arriba; Dibujar(); Pegar(); }
}

public partial class MainWindow
{
    private VentanaPesaje? _pesaje;
    // Lo que se actualiza en vivo en el panel, sin volver a dibujarlo (para no perder lo que se está escribiendo)
    private TextBlock? _ppNum, _ppRes;
    private Border? _ppChip;
    internal bool PesajeAbierto => _pesaje != null;

    /// <summary>Abre o cierra el panel de pesaje (botón PESAJE de la barra de la caseta o F3).</summary>
    internal void AlternarPesaje()
    {
        if (!Caseta || S.View == "login") return;
        if (_pesaje != null) { _pesaje.Close(); return; }
        var completa = WindowStyle == WindowStyle.None;
        if (completa) PantallaCompleta(false);
        _pesaje = new VentanaPesaje(this, completa);
        _pesaje.Dibujar(); _pesaje.Acomodar(); _pesaje.Show();
        Render();
    }

    /// <summary>Al cerrar el panel, el programa vuelve a como estaba.</summary>
    internal void AlCerrarPesaje(Rect antes, WindowState estado, bool completa, bool appCerrando)
    {
        _pesaje = null; _ppNum = _ppRes = null; _ppChip = null;
        if (appCerrando) return;
        WindowState = WindowState.Normal;
        Left = antes.Left; Top = antes.Top; Width = antes.Width; Height = antes.Height;
        if (completa) PantallaCompleta(true); else WindowState = estado;
        Render();
    }

    /// <summary>El botón de la barra de la caseta: se marca en cobre mientras el panel está abierto.</summary>
    private Border BotonPesaje()
    {
        var abierto = _pesaje != null;
        var color = abierto ? Ui.SobreCobre : Ui.Tinta;
        var cont = Ui.Fila(Ui.Ico(Iconos.Bascula, 20, 20, abierto ? Ui.SobreCobre : Ui.CobreMedio, 2), Ui.T("PESAJE", 16, SemiBold, color, Ui.Cond).Margen(5, 0, 0, 0));
        return Ui.Boton(cont, AlternarPesaje, abierto ? Ui.Cobre : Brushes.Transparent, abierto ? Ui.Cobre : Ui.Linea2, 10, new Thickness(9, 0, 9, 0), 46);
    }

    /// <summary>Lo que se ve en el panel de pesaje (también lo usa la prueba para sacar la captura).</summary>
    internal UIElement VistaPesaje(VentanaPesaje? w, bool? compactoPrueba = null)
    {
        var compacto = compactoPrueba ?? w?.Compacto ?? false;
        Border Mini(string txt, Action? a, bool marcado = false) =>
            Ui.Boton(Ui.T(txt, 13, SemiBold, marcado ? Ui.SobreCobre : Ui.Tinta, Ui.Cond), a, marcado ? Ui.Cobre : Brushes.Transparent, Ui.Linea2, 8, new Thickness(8, 0, 8, 0), 32);

        // Encabezado: título y los botones del panel
        var titulo = Ui.Fila(Ui.Ico(Iconos.Bascula, 24, 24, Ui.CobreMedio, 2), Ui.T(compacto ? "PESAJE" : "PANEL DE PESAJE", compacto ? 18 : 20, Bold, Ui.Tinta, Ui.Cond).Margen(8, 0, 0, 0));
        var botones = Ui.Fila(
            Mini(compacto ? "COMPLETO" : "COMPACTO", () => w?.CambiarTamano()),
            compacto ? Mini(w?.Arriba == true ? "▼ ABAJO" : "▲ ARRIBA", () => w?.CambiarEsquina()).Margen(4, 0, 0, 0) : new Border(),
            Mini(w?.Derecha == false ? "DERECHA ▶" : "◀ IZQUIERDA", () => w?.CambiarLado()).Margen(4, 0, 0, 0),
            Mini("✕", () => w?.Close()).Margen(4, 0, 0, 0));
        botones.HorizontalAlignment = HorizontalAlignment.Right;
        var cabeza = compacto ? (UIElement)Ui.Columna(titulo, botones.Margen(0, 6, 0, 0)) : Ui.Columna(titulo, botones.Margen(0, 8, 0, 0));

        // La lectura de la báscula en vivo (formato chico)
        var ch = Chips[B.Chip];
        _ppChip = Ui.Chip(ch.Txt, ch.Clase, 12);
        _ppNum = Ui.T(D.Kg((int)Math.Max(0, Math.Round(B.Lect))), compacto ? 32 : 40, Bold, Ui.Brillo, Ui.Mono);
        var kg = Ui.T("kg", compacto ? 15 : 17, SemiBold, Ui.Mut3, Ui.Mono).Margen(6, 0, 0, 4); kg.VerticalAlignment = VerticalAlignment.Bottom;
        _ppRes = Ui.T(B.Res.T1, 13, SemiBold, Ui.ColorEstado(B.Res.K)); _ppRes.TextTrimming = TextTrimming.CharacterEllipsis;
        var lectura = Ui.Tarjeta(Ui.Columna(Ui.Columnas("* auto", 8, Ui.Rotulo("Báscula N9 · en vivo", 12), _ppChip), Ui.Fila(_ppNum, kg).Margen(0, 2, 0, 2), _ppRes),
            Ui.Oscuro, Ui.LecturaBorde, 12, new Thickness(12, 8, 12, 10));

        // El peso a colocar: la próxima carga anunciada llega a la báscula con este bruto
        var (min, max) = D.Rango["cat"];
        var tb = new TextBox
        {
            Text = _pesoPrueba, FontFamily = Ui.Mono, FontSize = compacto ? 22 : 26, FontWeight = Bold, Foreground = Ui.Tinta, Background = Ui.Panel, CaretBrush = Ui.Tinta,
            BorderBrush = Ui.Linea2, BorderThickness = new Thickness(2), Padding = new Thickness(10, 0, 10, 0), MaxLength = 5, Height = compacto ? 44 : 52,
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        tb.TextChanged += (_, _) => _pesoPrueba = new string(tb.Text.Where(char.IsDigit).ToArray());
        tb.KeyDown += (_, e) => { if (e.Key == Key.Enter) { PesarDesdePanel(); e.Handled = true; } };
        var pesar = Ui.Boton(Ui.T("PESAR", compacto ? 16 : 18, Bold, Ui.SobreCobre, Ui.Cond), PesarDesdePanel, Ui.Cobre, Ui.Cobre, 10, new Thickness(14, 0, 14, 0), compacto ? 44 : 52);
        Border Rapido(int k, string txt) => Ui.Boton(Ui.Columna(Centro(Ui.T(D.Kg(k), compacto ? 15 : 17, Bold, Ui.Tinta, Ui.Mono)), Centro(Ui.T(txt, 11, SemiBold, Ui.Mut))),
            () => { _pesoPrueba = k.ToString(); tb.Text = _pesoPrueba; PesarPrueba(k); }, Ui.Panel, Ui.Linea2, 8, new Thickness(4), compacto ? 44 : 50);
        var peso = Ui.Columna(
            Ui.Rotulo("Peso a colocar (kg)", 13),
            Ui.Columnas("* auto", 8, tb, pesar).Margen(0, 4, 0, 0),
            Ui.Columnas("* * *", 6, Rapido(4750, "dentro"), Rapido(3900, "bajo"), Rapido(5300, "sobre")).Margen(0, 6, 0, 0));

        if (compacto)
            return new Border { Padding = new Thickness(12, 10, 12, 10), Child = Ui.Columna(cabeza, lectura.Margen(0, 8, 0, 0), peso.Margen(0, 8, 0, 0)) };

        // Completo: además, las situaciones de la báscula
        Border Sit(string k, string t, string d, bool lista) => Ui.Boton(
            Ui.Columna(Ui.T(t + (lista ? " · lista" : ""), 15, Bold, lista ? Ui.CobreClaro : Ui.Tinta), Ui.T(d, 12, Medium, Ui.Mut, ajustar: true).Margen(0, 2, 0, 0)),
            () => EscenarioCaseta(k), lista ? Ui.Panel2 : Ui.Panel, lista ? Ui.Cobre : Ui.Linea2, 10, new Thickness(12, 5, 12, 5), 44, null, false).Margen(0, 0, 0, 5);
        var situaciones = Ui.Columna(
            Ui.Rotulo("Situaciones", 13).Margen(0, 0, 0, 4),
            Sit("anuncio", "Nueva carga anunciada", "El origen imprime la etiqueta que sigue.", false),
            Sit("sinetq", "Etiqueta ilegible", "La cámara no la lee: se identifica a mano.", false),
            Sit("grua", "Horquilla encima", "La próxima carga queda con la horquilla sobre la báscula.", Esc.Grua),
            Sit("carro", "Carro de restos", "Carro de arrastre con rack; tara de la lista.", false),
            Sit("fuera", "Carro fuera del área", "Llega un carro pasado de la plataforma.", false),
            Sit("cero", "Sin cero", "Vacía marca 6 kg por nódulos en la junta.", Esc.Cero));
        var nota = Ui.T($"Rango de cátodos: {D.Kg(min)} a {D.Kg(max)} kg. Pesos de prueba: las cámaras y el visor están simulados.", 12, Medium, Ui.Mut, ajustar: true);
        var cuerpo = Ui.Columna(lectura, peso.Margen(0, 10, 0, 0), situaciones.Margen(0, 12, 0, 0), nota.Margen(0, 4, 0, 0));
        return Ui.Filas("auto *", 10, new Border { Padding = new Thickness(14, 12, 14, 0), Child = cabeza },
            new ScrollViewer { Content = new Border { Padding = new Thickness(14, 0, 14, 12), Child = cuerpo }, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
    }

    private void PesarDesdePanel()
    {
        if (int.TryParse(_pesoPrueba, out var kg)) PesarPrueba(kg);
        else Avisar("Escriba el peso en kg, solo números.", "warn");
    }

    /// <summary>La lectura, el estado y el resultado del panel siguen a la báscula sin volver a dibujarlo.</summary>
    private void PintarPesajeVivo()
    {
        if (_ppChip != null)
        {
            var ch = Chips[B.Chip]; var nuevo = Ui.Chip(ch.Txt, ch.Clase, 12);
            var texto = nuevo.Child; nuevo.Child = null;
            _ppChip.Background = nuevo.Background; _ppChip.Child = texto;
        }
        if (_ppRes != null) { _ppRes.Text = B.Res.T1; _ppRes.Foreground = Ui.ColorEstado(B.Res.K); }
    }
}
