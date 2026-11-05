using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace TerminalVentanas;

/// <summary>
/// Piezas de la pantalla: colores, letra, textos, tarjetas, botones táctiles e íconos.
/// Diseño Grafito Ingmétrica (la familia de ING-MAESTRO) al estándar de 2026: Fluent 2 (Segoe UI, esquinas de 6 y 8 px, tarjetas
/// planas con borde) y WCAG 2.2 AA (contraste medido: texto 4,5:1 o más, bordes de control 3:1 o más; ningún estado solo por color).
/// </summary>
public static class Ui
{
    public static SolidColorBrush B(string hex)
    {
        var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        b.Freeze();
        return b;
    }

    public static readonly SolidColorBrush
        Fondo = B("#F5F6F8"), Tarjeta0 = B("#FFFFFF"), Linea = B("#E1E4E8"), LineaControl = B("#7B8494"),
        Texto = B("#111827"), Texto2 = B("#5B6472"),
        Grafito = B("#151B26"), Grafito2 = B("#232B39"), SobreGrafito = B("#A3ACB9"),
        Azul = B("#015D93"), AzulOsc = B("#014A75"), AzulTinte = B("#E1EEF6"), Amarillo = B("#E1E101"),
        Verde = B("#1E7B4F"), VerdeTinte = B("#E3F2EA"), Rojo = B("#B42318"), RojoTinte = B("#FDECEA"),
        Ambar = B("#8A5300"), AmbarTinte = B("#FFF4D6"), AmbarBorde = B("#E0B54A"),
        VerdeXls = B("#1D6F42"), Blanco = B("#FFFFFF"), Negro = B("#111111");

    /// <summary>La letra de Windows (Fluent 2): ya viene en el equipo, no se embebe.</summary>
    public static readonly FontFamily Fuente = new("Segoe UI");
    /// <summary>Solo para la etiqueta impresa, que imita el papel que sale de la Zebra.</summary>
    public static readonly FontFamily Cond = new(new Uri("pack://application:,,,/"), "./Fuentes/#Barlow Condensed");

    public static Brush ColorEstado(string k) => k switch { "ok" => Verde, "warn" => Ambar, "err" => Rojo, "info" => Azul, _ => Texto2 };
    public static Brush TinteEstado(string k) => k switch { "ok" => VerdeTinte, "warn" => AmbarTinte, "err" => RojoTinte, _ => AzulTinte };

    public static TextBlock T(string s, double tam, FontWeight peso, Brush color, FontFamily? fuente = null, bool mayus = false, bool ajustar = false)
    {
        var t = new TextBlock
        {
            Text = mayus ? s.ToUpperInvariant() : s,
            FontSize = tam, FontWeight = peso, Foreground = color, FontFamily = fuente ?? Fuente,
            TextWrapping = ajustar ? TextWrapping.Wrap : TextWrapping.NoWrap,
            TextTrimming = ajustar ? TextTrimming.None : TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
        };
        System.Windows.Documents.Typography.SetNumeralAlignment(t, FontNumeralAlignment.Tabular);
        TextOptions.SetTextFormattingMode(t, TextFormattingMode.Ideal);
        return t;
    }

    /// <summary>El nombre de un recuadro o de un dato: en minúscula normal, gris y semibold (sin mayúsculas).</summary>
    public static TextBlock Rotulo(string s, double tam = 15, Brush? color = null) => T(s, tam, FontWeights.SemiBold, color ?? Texto2);

    public static Border Tarjeta(UIElement hijo, Brush? fondo = null, Brush? borde = null, double radio = 8, Thickness? relleno = null, double grosor = 1) => new()
    {
        Child = hijo, Background = fondo ?? Tarjeta0, BorderBrush = borde ?? Linea, BorderThickness = new Thickness(grosor),
        CornerRadius = new CornerRadius(radio), Padding = relleno ?? new Thickness(16, 12, 16, 14), SnapsToDevicePixels = true,
    };

    /// <summary>Un recuadro que se toca. Funciona con el dedo (el toque llega como clic) y da un brillo al apretar.</summary>
    public static Border Boton(UIElement contenido, Action? alTocar, Brush? fondo = null, Brush? borde = null, double radio = 6,
        Thickness? relleno = null, double minAlto = 0, Thickness? grosor = null, bool centrar = true)
    {
        var b = new Border
        {
            Child = contenido, Background = fondo ?? Brushes.Transparent, BorderBrush = borde ?? LineaControl, BorderThickness = grosor ?? new Thickness(1),
            CornerRadius = new CornerRadius(radio), Padding = relleno ?? new Thickness(14, 0, 14, 0), MinHeight = minAlto, SnapsToDevicePixels = true,
        };
        if (contenido is FrameworkElement fe && centrar) { fe.HorizontalAlignment = HorizontalAlignment.Center; fe.VerticalAlignment = VerticalAlignment.Center; }
        if (alTocar != null) { b.Cursor = Cursors.Hand; Clic(b, alTocar); }
        return b;
    }

    public static void Clic(FrameworkElement el, Action a)
    {
        var abajo = false;
        el.MouseLeftButtonDown += (_, e) => { abajo = true; el.Opacity = 0.8; el.CaptureMouse(); e.Handled = true; };
        el.MouseLeftButtonUp += (_, e) =>
        {
            var era = abajo; abajo = false; el.Opacity = 1; el.ReleaseMouseCapture();
            var p = e.GetPosition(el);
            if (era && p.X >= 0 && p.Y >= 0 && p.X <= el.ActualWidth && p.Y <= el.ActualHeight) a();
            e.Handled = true;
        };
        el.LostMouseCapture += (_, _) => { abajo = false; el.Opacity = 1; };
    }

    /// <summary>Ícono de líneas (como los del diseño): el trazo en su caja original y escalado al tamaño pedido.</summary>
    public static Viewbox Ico(string datos, double ancho, double alto, Brush color, double trazo = 2.2, double cajaAncho = 24, double cajaAlto = 24)
    {
        var p = new Path
        {
            Data = Geometry.Parse(datos), Stroke = color, StrokeThickness = trazo,
            StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round,
        };
        var c = new Canvas { Width = cajaAncho, Height = cajaAlto };
        c.Children.Add(p);
        return new Viewbox { Width = ancho, Height = alto, Child = c, Stretch = Stretch.Uniform };
    }

    public static Viewbox IcoProd(string prod, double ancho, double alto, Brush color) =>
        Ico(prod switch { "lam" => Iconos.Lam, "rs" => Iconos.Rs, "sub" => Iconos.Caja, _ => Iconos.Cat }, ancho, alto, color, 4, 96, 72);

    public static Viewbox IcoEstado(string k, double tam) =>
        Ico(k switch { "ok" => Iconos.Ok, "warn" => Iconos.Warn, "err" => Iconos.Err, "info" => Iconos.Info, _ => Iconos.Bascula }, tam, tam, ColorEstado(k), 2.4);

    /// <summary>El estado de la báscula: color y además ícono y texto (no solo color).</summary>
    public static Border Chip(string texto, string clase, double tam = 14)
    {
        var (fondo, ico) = clase switch
        {
            "espera" => (Azul, Iconos.Info), "inestable" => (Ambar, Iconos.Reloj), "estable" => (Verde, Iconos.Ok),
            "bloqueado" => (Rojo, Iconos.Err), _ => (Texto2, Iconos.Bascula),
        };
        return new Border
        {
            Background = fondo, CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 3, 10, 4), HorizontalAlignment = HorizontalAlignment.Left,
            Child = Fila(Ico(ico, 15, 15, Blanco, 2.6), T(texto, tam, FontWeights.Bold, Blanco).Margen(6, 0, 0, 0)),
        };
    }

    public static StackPanel Fila(params UIElement[] hijos)
    {
        var s = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        foreach (var h in hijos) s.Children.Add(h);
        return s;
    }

    public static StackPanel Columna(params UIElement[] hijos)
    {
        var s = new StackPanel();
        foreach (var h in hijos) s.Children.Add(h);
        return s;
    }

    public static T Margen<T>(this T el, double izq, double arr, double der, double aba) where T : FrameworkElement { el.Margin = new Thickness(izq, arr, der, aba); return el; }

    public static UniformGrid Grilla(int columnas, IEnumerable<UIElement> hijos, double hueco)
    {
        var g = new UniformGrid { Columns = columnas, Margin = new Thickness(-hueco / 2) };
        foreach (var h in hijos) { if (h is FrameworkElement fe) fe.Margin = new Thickness(hueco / 2); g.Children.Add(h); }
        return g;
    }

    /// <summary>Grilla de columnas fijas o proporcionales: "*", "380", "auto".</summary>
    public static Grid Columnas(string definicion, double hueco, params UIElement[] hijos)
    {
        var g = new Grid();
        var partes = definicion.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < partes.Length; i++)
        {
            if (i > 0) g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(hueco) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = Largo(partes[i]) });
        }
        for (var i = 0; i < hijos.Length && i < partes.Length; i++) { Grid.SetColumn(hijos[i], i * 2); g.Children.Add(hijos[i]); }
        return g;
    }

    public static Grid Filas(string definicion, double hueco, params UIElement[] hijos)
    {
        var g = new Grid();
        var partes = definicion.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < partes.Length; i++)
        {
            if (i > 0) g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(hueco) });
            g.RowDefinitions.Add(new RowDefinition { Height = Largo(partes[i]) });
        }
        for (var i = 0; i < hijos.Length && i < partes.Length; i++) { Grid.SetRow(hijos[i], i * 2); g.Children.Add(hijos[i]); }
        return g;
    }

    private static GridLength Largo(string p) => p == "*" ? new GridLength(1, GridUnitType.Star)
        : p == "auto" ? GridLength.Auto
        : p.EndsWith('*') ? new GridLength(double.Parse(p[..^1], System.Globalization.CultureInfo.InvariantCulture), GridUnitType.Star)
        : new GridLength(double.Parse(p, System.Globalization.CultureInfo.InvariantCulture));

    public static void Animar(DependencyObject obj, DependencyProperty prop, double hasta, double ms, double? desde = null)
    {
        var a = new DoubleAnimation(hasta, TimeSpan.FromMilliseconds(ms)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        if (desde.HasValue) a.From = desde.Value;
        if (obj is IAnimatable an) an.BeginAnimation(prop, a);
    }
}

/// <summary>Los íconos del diseño, como trazos (caja de 24 x 24, o 96 x 72 los de producto).</summary>
public static class Iconos
{
    // Siempre con punto decimal: en un Windows en español los números con coma romperían el trazo
    private static string Circulo(double cx, double cy, double r) => FormattableString.Invariant($"M{cx - r},{cy} a{r},{r} 0 1 0 {2 * r},0 a{r},{r} 0 1 0 {-2 * r},0");
    public const string Cat = "M12 50h72v10H12zM12 38h72v10H12zM12 26h72v10H12z M20 26v-9h11v9M65 26v-9h11v9";
    public const string Lam = "M14 16h68v26H14zM14 29h68 M8 42h80v9H8zM16 51v9M48 51v9M80 51v9";
    public static readonly string Rs = "M10 14h76v32H10z M10 14l16 32M30 14l16 32M50 14l16 32M70 14l16 32 " + Circulo(26, 57, 7) + " " + Circulo(70, 57, 7);
    public const string Caja = "M16 30h64v30H16zM16 30l10-14h44l10 14M40 30v9h16v-9";
    public const string Imprimir = "M7 9V3h10v6 M5 9h14a2 2 0 0 1 2 2v4a2 2 0 0 1 -2 2H5a2 2 0 0 1 -2 -2v-4a2 2 0 0 1 2 -2z M7 14h10v7H7z";
    public static readonly string Lista = "M9 6h11M9 12h11M9 18h11 " + Circulo(4.5, 6, 1) + " " + Circulo(4.5, 12, 1) + " " + Circulo(4.5, 18, 1);
    public const string Excel = "M5 3h10l4 4v14H5z M15 3v4h4 M8.5 11l5 6M13.5 11l-5 6";
    public const string Lapiz = "M4 20l1-5L16 4l4 4L9 19z M13.5 6.5l4 4";
    public static readonly string Ok = Circulo(12, 12, 9.5) + " M7.5 12.5l3 3 6-6.5";
    public const string Warn = "M12 3L2 20h20z M12 10v4.5M12 17.5v.5";
    public static readonly string Info = Circulo(12, 12, 9.5) + " M12 11v6M12 7.5v.5";
    public static readonly string Err = Circulo(12, 12, 9.5) + " M8.5 8.5l7 7M15.5 8.5l-7 7";
    public static readonly string Etiqueta = "M3 5v6l9 9 8-8-9-9H5z " + Circulo(7.5, 8.5, 1.5);
    public const string Bascula = "M3 15.5h18v3H3z M7 15.5V12h10v3.5";
    public const string Grabar = "M4 4h13l3 3v13H4z M8 4v5h8V4M8 20v-6h8v6";
    public const string Retroceso = "M9 5h11v14H9l-6-7z M12 9l5 6M17 9l-5 6";
    public const string Flecha = "M6 9l6 6 6-6";
    public static readonly string Reloj = Circulo(12, 12, 9.5) + " M12 7v5l3 2";
}
