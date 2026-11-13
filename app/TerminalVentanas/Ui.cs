using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace TerminalVentanas;

/// <summary>Piezas de la pantalla: colores, fuentes, textos, tarjetas, botones táctiles e íconos. Mismo lenguaje visual que la pantalla del pesador.</summary>
public static class Ui
{
    public static SolidColorBrush B(string hex)
    {
        var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        b.Freeze();
        return b;
    }

    /// <summary>El papel de la etiqueta: blanco y negro en todos los temas (es lo que sale de la Zebra).</summary>
    public static readonly SolidColorBrush Blanco = B("#FFFFFF"), Negro = B("#111111");

    // Los colores del tema que se ve. Los pone Paleta(); cada Render() los vuelve a leer.
    public static SolidColorBrush
        Fondo = null!, Panel = null!, Panel2 = null!, Linea = null!, Linea2 = null!, Linea3 = null!, Tecla = null!, Tinta = null!, Mut = null!, Mut2 = null!, Mut3 = null!,
        Cobre = null!, CobreClaro = null!, CobreMedio = null!, CobreBorde = null!, Oscuro = null!, SobreCobre = null!, Brillo = null!,
        VerdeT = null!, AmbarT = null!, AzulT = null!, RojoT = null!, VerdeXls = null!,
        VerdeF = null!, VerdeL = null!, VerdeC = null!, AzulF = null!, AzulL = null!, AzulC = null!, AmbarF = null!, AmbarL = null!, AmbarC = null!, RojoF = null!, RojoL = null!, RojoC = null!,
        LecturaBorde = null!, BocaBorde = null!, BocaArriba = null!, BocaAbajo = null!, Ranura = null!,
        CampoCalc = null!, ValorVacio = null!, ValorCalc = null!, ValorEd = null!,
        OkFondo = null!, OkBorde = null!, OkTexto = null!, WarnFondo = null!, WarnBorde = null!, WarnTexto = null!,
        ErrFondo = null!, ErrBorde = null!, ErrTexto = null!, InfoFondo = null!, InfoBorde = null!, InfoTexto = null!,
        ChipEspera = null!, ChipInestable = null!, ChipEstable = null!, ChipBloqueado = null!, ChipNeutro = null!, SobreChip = null!,
        ApagadoFondo = null!, ApagadoTexto = null!, InsigniaFondo = null!, SobreInsignia = null!, Capa = null!, PanelFondo = null!;

    /// <summary>Alto contraste: fondos negros, así que los avisos y botones se marcan con borde.</summary>
    public static bool Contraste;

    /// <summary>
    /// Pone los colores de un tema. "oscuro" es el diseño original (fondo azul petróleo y cobre);
    /// "claro" es el mismo diseño de día (la lectura de la báscula y la boca de la Zebra siguen oscuras, como un visor);
    /// "contraste" es negro, blanco, amarillo y celeste, como los temas de contraste de Windows.
    /// </summary>
    public static void Paleta(string tema)
    {
        Contraste = tema == "contraste";
        var k = tema == "claro" ? 1 : Contraste ? 2 : 0;
        SolidColorBrush P(string oscuro, string claro, string contraste) => B(k == 0 ? oscuro : k == 1 ? claro : contraste);
        Fondo = P("#0E181E", "#EDF0F1", "#000000"); Panel = P("#16242C", "#FFFFFF", "#000000"); Panel2 = P("#1A2A33", "#FBF1E8", "#000000");
        Linea = P("#24363F", "#D3DBDE", "#FFFFFF"); Linea2 = P("#35505C", "#8E9EA6", "#FFFFFF"); Linea3 = P("#2C4450", "#B4C1C7", "#FFFFFF");
        Tecla = P("#1D2E37", "#F3F6F7", "#000000"); Tinta = P("#D6E5EB", "#13212A", "#FFFFFF");
        Mut = P("#86A0AB", "#4C606A", "#FFFFFF"); Mut2 = P("#5F7883", "#7D9099", "#BDBDBD"); Mut3 = P("#9DB3BD", "#4C606A", "#FFFFFF");
        Cobre = P("#DA8B4F", "#B05C20", "#FFFF00"); CobreClaro = P("#F6C79F", "#93460F", "#FFFF00"); CobreMedio = P("#F0B37E", "#B05C20", "#FFFF00");
        CobreBorde = P("#9A6337", "#C99470", "#FFFF00"); Oscuro = P("#0A1317", "#16232A", "#000000"); SobreCobre = P("#1A0F07", "#FFFFFF", "#000000");
        Brillo = P("#EEF6F8", "#EEF6F8", "#FFFFFF");
        VerdeT = P("#8FE0A9", "#1C7442", "#3FF23F"); AmbarT = P("#F3C969", "#875100", "#FFFF00"); AzulT = P("#A9CBF2", "#1C5CA3", "#1AEBFF");
        RojoT = P("#FF9C92", "#B42318", "#FF6E6E"); VerdeXls = P("#5FD38F", "#1C7442", "#3FF23F");
        VerdeF = P("#1B4D2B", "#DCF2E3", "#000000"); VerdeL = P("#3BB86F", "#2E9E5B", "#3FF23F"); VerdeC = P("#B9F6CA", "#135530", "#3FF23F");
        AzulF = P("#13324F", "#E0EBF9", "#000000"); AzulL = P("#4F92E2", "#3B7FD0", "#1AEBFF"); AzulC = P("#CFE3FA", "#1B4C86", "#1AEBFF");
        AmbarF = P("#4A3B06", "#FFF0C7", "#000000"); AmbarL = P("#E3A824", "#C9910F", "#FFFF00"); AmbarC = P("#FFE7A3", "#664400", "#FFFF00");
        RojoF = P("#3A1714", "#FCE4E1", "#000000"); RojoL = P("#EE5B4D", "#D9473A", "#FF6E6E"); RojoC = P("#FF9C92", "#8E1F16", "#FF6E6E");
        LecturaBorde = P("#2A3F49", "#2A3F49", "#FFFFFF"); BocaBorde = P("#3A5563", "#3A5563", "#FFFFFF");
        BocaArriba = P("#22313A", "#22313A", "#000000"); BocaAbajo = P("#0A1013", "#0A1013", "#000000"); Ranura = P("#05080A", "#05080A", "#FFFFFF");
        CampoCalc = P("#13202A", "#F0F3F4", "#000000"); ValorVacio = P("#6F8893", "#6E818A", "#BDBDBD"); ValorCalc = P("#B9CAD2", "#3E525C", "#FFFFFF"); ValorEd = P("#FFFFFF", "#13212A", "#1AEBFF");
        OkFondo = P("#123C25", "#E2F3E8", "#000000"); OkBorde = P("#2F8C55", "#2E9E5B", "#3FF23F"); OkTexto = P("#E9FFF1", "#135530", "#3FF23F");
        WarnFondo = P("#3F3005", "#FFF3D2", "#000000"); WarnBorde = P("#A07A12", "#C9910F", "#FFFF00"); WarnTexto = P("#FFF4D6", "#664400", "#FFFF00");
        ErrFondo = P("#4A1713", "#FCE7E4", "#000000"); ErrBorde = P("#B63A30", "#D9473A", "#FF6E6E"); ErrTexto = P("#FFE9E6", "#8E1F16", "#FF6E6E");
        InfoFondo = P("#13324F", "#E2EDF9", "#000000"); InfoBorde = P("#3D72B0", "#3B7FD0", "#1AEBFF"); InfoTexto = P("#E6F1FF", "#1B4C86", "#1AEBFF");
        ChipEspera = P("#1E4F86", "#1C5CA3", "#1AEBFF"); ChipInestable = P("#8A6200", "#875100", "#FFFF00"); ChipEstable = P("#17703D", "#1C7442", "#3FF23F");
        ChipBloqueado = P("#A12A21", "#B42318", "#FF6E6E"); ChipNeutro = P("#2A3A43", "#5A6B73", "#FFFFFF"); SobreChip = P("#EEF6F8", "#FFFFFF", "#000000");
        ApagadoFondo = P("#24363F", "#D3DBDE", "#000000"); ApagadoTexto = P("#5F7883", "#5E717A", "#BDBDBD");
        InsigniaFondo = P("#24363F", "#D3DBDE", "#FFFFFF"); SobreInsignia = P("#D6E5EB", "#13212A", "#000000");
        Capa = P("#D1050A0E", "#8013212A", "#E6000000"); PanelFondo = P("#99050A0E", "#5913212A", "#CC000000");
    }

    /// <summary>Fondo, borde y texto de un aviso (ok, warn, err o info).</summary>
    public static (Brush Fondo, Brush Borde, Brush Texto) Aviso(string k) => k switch
    {
        "ok" => (OkFondo, OkBorde, OkTexto), "warn" => (WarnFondo, WarnBorde, WarnTexto), "err" => (ErrFondo, ErrBorde, ErrTexto), _ => (InfoFondo, InfoBorde, InfoTexto),
    };

    public static readonly FontFamily Barlow = new(new Uri("pack://application:,,,/"), "./Fuentes/#Barlow");
    public static readonly FontFamily Cond = new(new Uri("pack://application:,,,/"), "./Fuentes/#Barlow Condensed");
    public static readonly FontFamily Mono = new(new Uri("pack://application:,,,/"), "./Fuentes/#JetBrains Mono");

    public static Brush ColorEstado(string k) => k switch { "ok" => VerdeT, "warn" => AmbarT, "err" => RojoT, "info" => AzulT, _ => Mut };

    public static TextBlock T(string s, double tam, FontWeight peso, Brush color, FontFamily? fuente = null, bool mayus = false, bool ajustar = false)
    {
        var t = new TextBlock
        {
            Text = mayus ? s.ToUpperInvariant() : s,
            FontSize = tam, FontWeight = peso, Foreground = color, FontFamily = fuente ?? Barlow,
            TextWrapping = ajustar ? TextWrapping.Wrap : TextWrapping.NoWrap,
            TextTrimming = ajustar ? TextTrimming.None : TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
        };
        TextOptions.SetTextFormattingMode(t, TextFormattingMode.Ideal);
        return t;
    }

    /// <summary>Rótulo chico en mayúsculas (Barlow Condensed), como los títulos de cada recuadro.</summary>
    public static TextBlock Rotulo(string s, double tam = 15, Brush? color = null) => T(s, tam, FontWeights.SemiBold, color ?? Mut, Cond, true);

    public static Border Tarjeta(UIElement hijo, Brush? fondo = null, Brush? borde = null, double radio = 16, Thickness? relleno = null, double grosor = 1) => new()
    {
        Child = hijo, Background = fondo ?? Panel, BorderBrush = borde ?? Linea, BorderThickness = new Thickness(grosor),
        CornerRadius = new CornerRadius(radio), Padding = relleno ?? new Thickness(16, 12, 16, 14), SnapsToDevicePixels = true,
    };

    /// <summary>Un recuadro que se toca. Funciona con el dedo (el toque llega como clic) y da un brillo al apretar.</summary>
    public static Border Boton(UIElement contenido, Action? alTocar, Brush? fondo = null, Brush? borde = null, double radio = 10,
        Thickness? relleno = null, double minAlto = 0, Thickness? grosor = null, bool centrar = true)
    {
        var b = new Border
        {
            Child = contenido, Background = fondo ?? Brushes.Transparent, BorderBrush = borde ?? Linea2, BorderThickness = grosor ?? new Thickness(1),
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

    public static Border Chip(string texto, string clase, double tam = 13)
    {
        var fondo = clase switch { "espera" => ChipEspera, "inestable" => ChipInestable, "estable" => ChipEstable, "bloqueado" => ChipBloqueado, _ => ChipNeutro };
        return new Border
        {
            Background = fondo, CornerRadius = new CornerRadius(5), Padding = new Thickness(10, 3, 10, 3), HorizontalAlignment = HorizontalAlignment.Left,
            Child = T(texto, tam, FontWeights.Bold, SobreChip, Cond, true),
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
    /// <summary>Un círculo con la mitad rayada: los colores de la pantalla.</summary>
    public static readonly string Colores = Circulo(12, 12, 9) + " M12 3v18M12 7h7M12 11h8.5M12 15h8M12 19h5.5";
}
