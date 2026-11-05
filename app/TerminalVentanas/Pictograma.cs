using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace TerminalVentanas;

/// <summary>
/// La plataforma de la Báscula N9 vista en isométrico: cubierta, área marcada, nódulos en la junta, las 4 celdas de carga
/// y lo que está encima (paquete de cátodos, pallet de láminas, carro de restos o la grúa).
/// </summary>
public sealed class Pictograma
{
    public readonly Canvas Raiz = new() { Width = 96, Height = 68, ClipToBounds = false };
    private readonly Canvas _carga = new(), _cat = new(), _lam = new(), _rs = new(), _grua = new(), _nod = new();
    private readonly Ellipse[] _celdas = new Ellipse[4];
    private readonly TranslateTransform _ttCarga = new(), _ttGrua = new();
    private static readonly SolidColorBrush Gris = Ui.B("#4A5F69"), Verde = Ui.B("#3BB86F"), Rojo = Ui.B("#EE5B4D"), Ambar = Ui.B("#E3A824");

    public Pictograma(string pic, string prod)
    {
        P("M6 42L48 63L90 42V46L48 67L6 46Z", "#0B1418", Raiz);
        P("M48 21L90 42L48 63L6 42Z", "#22343D", Raiz, "#3A5462");
        var area = new Path { Data = Geometry.Parse("M48 27L78 42L48 57L18 42Z"), Stroke = Ui.Cobre, StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 2.5, 2 }, Opacity = 0.55 };
        Raiz.Children.Add(area);
        foreach (var (x, y, r) in new[] { (22.0, 49.5, 1.4), (27, 52.6, 1), (69, 52.4, 1.3), (74, 49.6, 1), (17.5, 47.2, 1) }) E(x, y, r, Ui.B("#D08A4F"), _nod);
        Raiz.Children.Add(_nod);
        var pos = new[] { (48.0, 25.6), (83, 42), (48, 58.4), (13, 42) };
        for (var i = 0; i < 4; i++) _celdas[i] = E(pos[i].Item1, pos[i].Item2, 2.8, Gris, Raiz);

        // Paquete de cátodos: placas apiladas con la etiqueta arriba
        P("M26 34L48 45V53L26 42Z", "#B9692F", _cat); P("M48 45L70 34V42L48 53Z", "#93521F", _cat); P("M48 23L70 34L48 45L26 34Z", "#E8A26A", _cat);
        _cat.Children.Add(new Path { Data = Geometry.Parse("M26 36.7L48 47.7L70 36.7M26 39.4L48 50.4L70 39.4"), Stroke = Ui.B("#6F3B16"), StrokeThickness = 0.7 });
        P("M45 31L50 33.5L45 36L40 33.5Z", "#FFFFFF", _cat);
        // Pallet de láminas (o caja de despuntes)
        P("M26 39L48 50V53L26 42Z", "#5A4129", _lam); P("M48 50L70 39V42L48 53Z", "#45311F", _lam);
        P("M26 30L48 41V50L26 39Z", "#C88552", _lam); P("M48 41L70 30V39L48 50Z", "#A4673A", _lam); P("M48 19L70 30L48 41L26 30Z", "#EDB585", _lam);
        P("M45 27L50 29.5L45 32L40 29.5Z", "#FFFFFF", _lam);
        // Carro reticulado de restos con su tarjeta
        P("M26 31L48 42V51L26 40Z", "#2B373D", _rs); P("M48 42L70 31V40L48 51Z", "#222C31", _rs);
        _rs.Children.Add(new Path { Data = Geometry.Parse("M32 34V43M38 37V46M44 40V49M54 39V48M60 36V45M66 33V42"), Stroke = Ui.B("#6E828C"), StrokeThickness = 0.9 });
        P("M48 20L70 31L48 42L26 31Z", "#7C5A3E", _rs, "#8FA1AA");
        _rs.Children.Add(new Path { Data = Geometry.Parse("M36 28L44 32M44 25L54 30M52 33L60 29"), Stroke = Ui.B("#5B3F2A"), StrokeThickness = 1.2 });
        P("M33 37.5L38 40V44L33 41.5Z", "#FFFFFF", _rs);
        foreach (var (x, y) in new[] { (30.0, 44.0), (44, 51), (52, 51), (66, 44) }) E(x, y, 2.2, Ui.B("#0B0F11"), _rs);
        _carga.Children.Add(_cat); _carga.Children.Add(_lam); _carga.Children.Add(_rs);
        _carga.RenderTransform = _ttCarga;
        Raiz.Children.Add(_carga);
        // La grúa horquilla que no retrocede
        P("M72 34L86 41V50L72 43Z", "#C48F16", _grua); P("M86 41L94 37V46L86 50Z", "#9C710F", _grua); P("M80 30L94 37L86 41L72 34Z", "#F2BC3E", _grua);
        P("M68 14L72 16V44L68 42Z", "#3B4247", _grua);
        _grua.Children.Add(new Path { Data = Geometry.Parse("M68 42L56 48M70 43L58 49"), Stroke = Ui.B("#2B3034"), StrokeThickness = 1.6 });
        _grua.RenderTransform = _ttGrua;
        Raiz.Children.Add(_grua);
        Poner(pic, prod, false);
    }

    /// <summary>Cambia lo que se ve: vacia, sobre, grua, fuera, sale o cero. Con animar, la carga entra y sale deslizándose.</summary>
    public void Poner(string pic, string prod, bool animar)
    {
        _cat.Visibility = prod == "cat" ? Visibility.Visible : Visibility.Collapsed;
        _lam.Visibility = prod is "lam" or "sub" ? Visibility.Visible : Visibility.Collapsed;
        _rs.Visibility = prod == "rs" ? Visibility.Visible : Visibility.Collapsed;
        var (x, y, op) = pic switch { "sobre" or "grua" => (0.0, 0.0, 1.0), "fuera" => (15, 7.5, 1), "sale" => (26, 13, 0), _ => (-26, -13, 0) };
        var g = pic == "grua";
        Ir(_ttCarga, TranslateTransform.XProperty, x, animar ? 700 : 0);
        Ir(_ttCarga, TranslateTransform.YProperty, y, animar ? 700 : 0);
        Ir(_carga, UIElement.OpacityProperty, op, animar ? 450 : 0);
        Ir(_grua, UIElement.OpacityProperty, g ? 1 : 0, animar ? 400 : 0);
        Ir(_ttGrua, TranslateTransform.XProperty, g ? 0 : 12, animar ? 600 : 0);
        Ir(_ttGrua, TranslateTransform.YProperty, g ? 0 : 6, animar ? 600 : 0);
        _nod.Opacity = pic == "cero" ? 1 : 0;
        for (var i = 0; i < 4; i++)
        {
            var c = _celdas[i];
            c.BeginAnimation(UIElement.OpacityProperty, null);
            c.Opacity = 1;
            c.Fill = pic switch
            {
                "sobre" => Verde,
                "fuera" => i is 1 or 2 ? Ambar : Verde,
                "grua" or "cero" => Rojo,
                _ => Gris,
            };
            if (pic == "cero")
                c.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimationUsingKeyFrames
                {
                    RepeatBehavior = RepeatBehavior.Forever, Duration = TimeSpan.FromSeconds(1),
                    KeyFrames = { new DiscreteDoubleKeyFrame(1, KeyTime.FromPercent(0)), new DiscreteDoubleKeyFrame(0.25, KeyTime.FromPercent(0.5)) },
                });
        }
    }

    private static void Ir(DependencyObject o, DependencyProperty p, double v, double ms)
    {
        if (o is not IAnimatable a) return;
        if (ms <= 0) { a.BeginAnimation(p, null); o.SetValue(p, v); return; }
        a.BeginAnimation(p, new DoubleAnimation(v, TimeSpan.FromMilliseconds(ms)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
    }

    private static void P(string d, string relleno, Panel en, string? borde = null) =>
        en.Children.Add(new Path { Data = Geometry.Parse(d), Fill = Ui.B(relleno), Stroke = borde == null ? null : Ui.B(borde), StrokeThickness = borde == null ? 0 : 0.8 });

    private static Ellipse E(double cx, double cy, double r, Brush relleno, Panel en)
    {
        var e = new Ellipse { Width = 2 * r, Height = 2 * r, Fill = relleno };
        Canvas.SetLeft(e, cx - r); Canvas.SetTop(e, cy - r);
        en.Children.Add(e);
        return e;
    }
}
