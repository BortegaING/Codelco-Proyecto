using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace TerminalVentanas;

/// <summary>
/// La plataforma de la Báscula N9 en isométrico. Todo se dibuja con cajas en coordenadas del piso: u a lo largo de la plataforma
/// (hacia abajo a la derecha), v a lo ancho (hacia abajo a la izquierda) y h hacia arriba. Así cada pieza tiene sus tres caras con
/// la luz desde el mismo lado y se mueve a lo largo de la plataforma sin deformarse.
///
/// El flujo de una carga (lingada de cátodos o pallet de láminas): la horquilla entra con la carga levantada, la baja sobre la
/// plataforma, las 4 celdas de carga la sienten (pasan a verde) y la horquilla retrocede. Al salir, la horquilla vuelve, levanta
/// la carga y cruza la plataforma. El carro de restos cruza solo; si queda pasado, se ve fuera del área y vuelve.
/// El dibujo vive entre un redibujo y otro de la pantalla (lo guarda MainWindow), para que las animaciones no se corten.
/// </summary>
public sealed class Pictograma
{
    // Proyección: x = OX + (u − v)·K ; y = OY + (u + v)·K/2 − h·K
    private const double K = 4.4, OX = 41, OY = 21;
    public const double Ancho = 104, Alto = 74;
    private const double Lejos = -17, Fuera = 20; // de dónde llega (arriba a la izquierda) y hasta dónde sale (abajo a la derecha)
    private const double Pasado = 3.2;            // cuánto se pasa el carro cuando queda fuera del área
    private const double Alzada = 0.55;           // cuánto levanta la horquilla la carga

    public readonly Canvas Raiz = new() { Width = Ancho, Height = Alto, ClipToBounds = true };
    private readonly Canvas _carga = new(), _alza = new(), _cat = new(), _lam = new(), _rs = new(), _horq = new(), _horca = new(), _nod = new();
    private readonly TranslateTransform _ttCarga = new(), _ttAlza = new(), _ttHorq = new(), _ttHorca = new();
    private readonly Polygon _sombra = new() { Fill = new SolidColorBrush(Color.FromArgb(90, 0, 0, 0)) };
    private readonly Ellipse[] _celdas = new Ellipse[4];
    private readonly SolidColorBrush[] _luz = new SolidColorBrush[4];
    private string _pic = "", _prod = "";

    private static readonly Color Gris = C("#4A5F69"), Verde = C("#3BB86F"), Rojo = C("#EE5B4D"), Ambar = C("#E3A824");
    private static readonly IEasingFunction Frena = new CubicEase { EasingMode = EasingMode.EaseOut },
        Arranca = new QuadraticEase { EasingMode = EasingMode.EaseIn }, Suave = new SineEase { EasingMode = EasingMode.EaseInOut };

    /// <param name="depositando">Para una foto quieta: la horquilla todavía junto a la carga que acaba de dejar.</param>
    public Pictograma(string pic, string prod, bool depositando = false)
    {
        // Foso de concreto, cubierta de planchas, franjas de seguridad en la entrada y en la salida, y el área marcada
        Caja(Raiz, -0.8, 12.8, -0.8, 7.8, -0.7, 0, "#2C3B42", "#0E171B", "#0A1114");
        Caja(Raiz, 0, 12, 0, 7, 0, 0.18, "#253942", "#18262D", "#121D23", "#3A5462");
        foreach (var v in new[] { 1.75, 3.5, 5.25 }) Linea(Raiz, "#30495A", 0.6, (0.9, v, 0.18), (11.1, v, 0.18));
        foreach (var u0 in new[] { 0.0, 11.35 })
            for (var i = 0; i < 10; i++)
                Poli(Raiz, i % 2 == 0 ? "#D9A21B" : "#1A1F22", (u0, i * 0.7, 0.18), (u0 + 0.65, i * 0.7, 0.18), (u0 + 0.65, (i + 1) * 0.7, 0.18), (u0, (i + 1) * 0.7, 0.18));
        var area = new Polygon { Points = Puntos((2.6, 0.9, 0.18), (9.4, 0.9, 0.18), (9.4, 6.1, 0.18), (2.6, 6.1, 0.18)), Stroke = Ui.B("#C9733A"), StrokeThickness = 0.9,
            StrokeDashArray = new DoubleCollection { 2.4, 1.8 }, Opacity = 0.75 };
        Raiz.Children.Add(area);
        // Nódulos en la junta (sin cero): solo se ven en esa situación
        foreach (var (u, v, r) in new[] { (2.2, 7.4, 1.3), (5.6, 7.4, 1.0), (8.9, 7.35, 1.2), (12.4, 2.1, 1.1), (12.4, 5.0, 1.3) }) Punto(_nod, u, v, 0, r, "#E09A5C");
        _nod.Opacity = 0;
        Raiz.Children.Add(_nod);
        // Las 4 celdas de carga en las esquinas: gris sin carga, verde con la carga apoyada, ámbar o rojo si algo no calza
        var esquinas = new[] { (0.95, 0.95), (11.05, 0.95), (11.05, 6.05), (0.95, 6.05) };
        for (var i = 0; i < 4; i++)
        {
            _luz[i] = new SolidColorBrush(Gris);
            _celdas[i] = Punto(Raiz, esquinas[i].Item1, esquinas[i].Item2, 0.18, 2.1, null, _luz[i]);
            _celdas[i].Stroke = Ui.B("#0B1418"); _celdas[i].StrokeThickness = 0.8;
        }

        Horquilla();
        Raiz.Children.Add(_horq);

        Cat(); Lam(); Rs();
        _alza.Children.Add(_cat); _alza.Children.Add(_lam); _alza.Children.Add(_rs);
        _alza.RenderTransform = _ttAlza;
        _carga.Children.Add(_sombra); _carga.Children.Add(_alza);
        _carga.RenderTransform = _ttCarga;
        Raiz.Children.Add(_carga);

        Productos(prod);
        _pic = pic; _prod = prod;
        Quieto(pic, prod == "rs", depositando);
        Celdas(pic, 0, false);
    }

    /* ---------- Las piezas ---------- */

    /// <summary>Lingada de cátodos: placas apiladas, dos zunchos y la etiqueta arriba en la esquina (la que lee la cámara superior).</summary>
    private void Cat()
    {
        Caja(_cat, 3.5, 8.5, 1.5, 5.5, 0.18, 2.0, "#E8A26A", "#B9692F", "#93521F");
        for (var h = 0.55; h < 1.95; h += 0.35) Linea(_cat, "#7A4119", 0.55, (3.5, 5.5, h), (8.5, 5.5, h), (8.5, 1.5, h));
        foreach (var u in new[] { 4.7, 7.3 }) Linea(_cat, "#4A2A12", 1.0, (u, 1.5, 2.0), (u, 5.5, 2.0), (u, 5.5, 0.18));
        Poli(_cat, "#FFFFFF", (6.6, 4.0, 2.0), (8.1, 4.0, 2.0), (8.1, 5.3, 2.0), (6.6, 5.3, 2.0));
        Poli(_cat, "#1A2126", (7.0, 4.3, 2.0), (7.6, 4.3, 2.0), (7.6, 4.9, 2.0), (7.0, 4.9, 2.0));
    }

    /// <summary>Pallet de láminas (o caja de despuntes) con su etiqueta.</summary>
    private void Lam()
    {
        Caja(_lam, 3.5, 8.5, 1.5, 5.5, 0.18, 0.6, "#9A7048", "#6E4E2F", "#58401F");
        foreach (var u in new[] { 4.6, 6.0, 7.4 }) Linea(_lam, "#3A2814", 0.8, (u, 5.5, 0.6), (u, 5.5, 0.18));
        Caja(_lam, 3.7, 8.3, 1.7, 5.3, 0.6, 1.85, "#EDB585", "#C88552", "#A4673A");
        for (var h = 0.9; h < 1.8; h += 0.3) Linea(_lam, "#8A5530", 0.5, (3.7, 5.3, h), (8.3, 5.3, h), (8.3, 1.7, h));
        Poli(_lam, "#FFFFFF", (6.6, 3.9, 1.85), (8.0, 3.9, 1.85), (8.0, 5.1, 1.85), (6.6, 5.1, 1.85));
    }

    /// <summary>Carro de arrastre con su rack de restos de ánodos, la barra de tiro y la marca de tara (la que lee la cámara lateral).</summary>
    private void Rs()
    {
        Linea(_rs, "#3B4247", 1.4, (2.5, 3.5, 0.7), (0.7, 3.5, 0.7));
        Punto(_rs, 0.55, 3.5, 0.7, 1.0, "#3B4247");
        Caja(_rs, 2.5, 9.5, 1.4, 5.6, 0.55, 0.85, "#3B4A52", "#222C31", "#1A2226");
        Caja(_rs, 3.0, 9.0, 1.8, 5.2, 0.85, 2.3, "#6E5038", "#2B373D", "#222C31");
        for (var u = 3.8; u < 9.0; u += 0.8) Linea(_rs, "#6E828C", 0.8, (u, 5.2, 0.85), (u, 5.2, 2.3));
        for (var v = 2.6; v < 5.2; v += 0.8) Linea(_rs, "#55666E", 0.8, (9.0, v, 0.85), (9.0, v, 2.3));
        Caja(_rs, 3.5, 5.0, 2.2, 3.7, 2.3, 2.75, "#A8754B", "#7A5234", "#5F3F27");
        Caja(_rs, 5.6, 7.2, 3.1, 4.7, 2.3, 2.6, "#9C6A43", "#6E4A2F", "#5A3C25");
        Caja(_rs, 7.4, 8.6, 2.0, 3.2, 2.3, 2.85, "#B07D52", "#7E5536", "#634129");
        Poli(_rs, "#FFFFFF", (3.4, 5.2, 1.95), (4.7, 5.2, 1.95), (4.7, 5.2, 1.3), (3.4, 5.2, 1.3));
        foreach (var u in new[] { 3.6, 8.4 }) { var w = Punto(_rs, u, 5.6, 0.45, 2.0, "#0B0F11"); w.Stroke = Ui.B("#3B4247"); w.StrokeThickness = 0.7; }
    }

    /// <summary>La grúa horquilla, mirando hacia la plataforma: contrapeso, cuerpo, asiento, techo protector, mástil y horquillas.</summary>
    private void Horquilla()
    {
        Caja(_horq, -3.8, -2.6, 1.4, 5.6, 0.5, 2.2, "#D9A12E", "#A9771A", "#8A610F");
        Caja(_horq, -2.6, 2.7, 1.4, 5.6, 0.5, 1.6, "#F2BC3E", "#C48F16", "#9C710F");
        foreach (var (u, v) in new[] { (-2.0, 1.4), (1.8, 1.4) }) Linea(_horq, "#2B3034", 1.0, (u, v, 1.6), (u, v, 3.35));
        Caja(_horq, -1.7, -0.3, 2.6, 4.4, 1.6, 2.4, "#2B3034", "#1E2226", "#16191C");
        Caja(_horq, -2.1, 1.9, 1.4, 5.6, 3.3, 3.45, "#3B4247", "#2B3034", "#22272B");
        foreach (var u in new[] { -2.0, 1.8 }) Linea(_horq, "#2B3034", 1.0, (u, 5.6, 1.6), (u, 5.6, 3.35));
        Caja(_horq, 2.7, 3.0, 1.6, 2.0, 0.3, 3.9, "#525C62", "#3B4247", "#2E3439");
        Caja(_horq, 2.7, 3.0, 5.0, 5.4, 0.3, 3.9, "#525C62", "#3B4247", "#2E3439");
        Caja(_horq, 2.7, 3.0, 1.6, 5.4, 3.7, 3.9, "#525C62", "#3B4247", "#2E3439");
        // Lo que sube y baja: el carro de las horquillas y las dos horquillas
        Caja(_horca, 3.0, 3.25, 1.6, 5.4, 0.3, 1.5, "#2B3034", "#1E2226", "#16191C");
        Caja(_horca, 3.25, 8.3, 2.1, 2.7, 0.22, 0.38, "#4B545A", "#2B3034", "#22272B");
        Caja(_horca, 3.25, 8.3, 4.3, 4.9, 0.22, 0.38, "#4B545A", "#2B3034", "#22272B");
        _horca.RenderTransform = _ttHorca;
        _horq.Children.Add(_horca);
        foreach (var (u, r) in new[] { (-2.5, 2.0), (1.6, 2.3) }) { var w = Punto(_horq, u, 5.6, 0.5, r, "#0B0F11"); w.Stroke = Ui.B("#3B4247"); w.StrokeThickness = 0.8; }
        _horq.RenderTransform = _ttHorq;
    }

    /* ---------- Lo que se ve y cómo cambia ---------- */

    private void Productos(string prod)
    {
        _cat.Visibility = prod == "cat" ? Visibility.Visible : Visibility.Collapsed;
        _lam.Visibility = prod is "lam" or "sub" ? Visibility.Visible : Visibility.Collapsed;
        _rs.Visibility = prod == "rs" ? Visibility.Visible : Visibility.Collapsed;
        _sombra.Points = prod == "rs" ? Puntos((2.5, 1.4, 0.18), (9.5, 1.4, 0.18), (9.5, 5.6, 0.18), (2.5, 5.6, 0.18))
            : Puntos((3.5, 1.5, 0.18), (8.5, 1.5, 0.18), (8.5, 5.5, 0.18), (3.5, 5.5, 0.18));
    }

    /// <summary>
    /// Cambia lo que se ve: vacia, sobre, grua, fuera, sale o cero. Con animar, pasa de lo que había a lo nuevo con su maniobra
    /// (la horquilla deja la carga, la retira o se la lleva; el carro entra, se acomoda o sale). Repetir el mismo estado no hace nada.
    /// </summary>
    public void Poner(string pic, string prod, bool animar)
    {
        if (pic == _pic && prod == _prod) return;
        var antes = _pic; _pic = pic; _prod = prod;
        Productos(prod);
        var carro = prod == "rs";
        var vacia = antes is "vacia" or "cero" or "sale" or "";
        if (!animar) { Quieto(pic, carro, false); Celdas(pic, 0, false); return; }

        if (pic is "sobre" or "grua" or "fuera" && vacia) Llegada(pic, carro);
        else if (pic == "sobre" && antes == "grua") { Retroceder(0); Celdas(pic, 300, true); }
        else if (pic == "sobre" && antes == "fuera") { MoverU(_ttCarga, (0, DuDe(_ttCarga), null), (800, 0, Suave)); Celdas(pic, 700, true); }
        else if (pic == "sale" && antes is "sobre" or "grua" or "fuera") Salida(carro, antes == "grua");
        else if (pic == "vacia" && antes == "sale") { /* la salida ya termina fuera de la vista */ }
        else if (pic == "cero") { Quieto(pic, carro, false); Anim(_nod, UIElement.OpacityProperty, (0, 0, null), (400, 1, Suave)); Celdas(pic, 0, true); }
        else if (pic == "vacia" && antes == "cero") { Anim(_nod, UIElement.OpacityProperty, (0, 1, null), (500, 0, Suave)); Celdas(pic, 0, true); }
        else { Quieto(pic, carro, false); Celdas(pic, 0, true); }
    }

    /// <summary>Llega la carga. Con horquilla: entra con la carga arriba, la baja, las celdas la sienten y retrocede (en "grua" se queda encima).</summary>
    private void Llegada(string pic, bool carro)
    {
        var fin = pic == "fuera" ? Pasado : 0;
        _nod.BeginAnimation(UIElement.OpacityProperty, null); _nod.Opacity = 0;
        if (carro)
        {
            Fijar(_ttAlza, 0, true); Fijar(_ttHorq, Lejos, false); Op(_horq, 0);
            MoverU(_ttCarga, (0, Lejos, null), (1200, fin, Frena));
            Anim(_carga, UIElement.OpacityProperty, (0, 0, null), (150, 1, null));
            Celdas(pic, 1050, true);
            return;
        }
        const double llega = 1000, baja = 1250, vuelve = 1950;
        MoverU(_ttCarga, (0, Lejos, null), (llega, fin, Frena));
        Alzar(_ttAlza, (0, Alzada, null), (llega, Alzada, null), (baja, 0, Suave));
        Anim(_carga, UIElement.OpacityProperty, (0, 0, null), (150, 1, null));
        Alzar(_ttHorca, (0, Alzada, null), (llega, Alzada, null), (baja, 0, Suave));
        if (pic == "grua")
        {
            MoverU(_ttHorq, (0, Lejos, null), (llega, fin, Frena));
            Anim(_horq, UIElement.OpacityProperty, (0, 0, null), (150, 1, null));
        }
        else
        {
            MoverU(_ttHorq, (0, Lejos, null), (llega, fin, Frena), (baja + 120, fin, null), (vuelve, Lejos, Arranca));
            Anim(_horq, UIElement.OpacityProperty, (0, 0, null), (150, 1, null), (vuelve - 1, 1, null), (vuelve, 0, null));
        }
        Celdas(pic, baja - 80, true);
    }

    /// <summary>La horquilla que quedó encima retrocede y sale de la vista.</summary>
    private void Retroceder(double desde)
    {
        MoverU(_ttHorq, (desde, DuDe(_ttHorq), null), (desde + 700, Lejos, Arranca));
        Anim(_horq, UIElement.OpacityProperty, (0, 1, null), (desde + 699, 1, null), (desde + 700, 0, null));
    }

    /// <summary>Sale la carga: la horquilla vuelve, la levanta y cruza la plataforma con ella. El carro sale tirado hacia adelante.</summary>
    private void Salida(bool carro, bool horquillaPuesta)
    {
        var du = DuDe(_ttCarga);
        if (carro)
        {
            MoverU(_ttCarga, (0, du, null), (1200, Fuera, Arranca));
            Anim(_carga, UIElement.OpacityProperty, (0, 1, null), (1199, 1, null), (1200, 0, null));
            Celdas("vacia", 350, true);
            return;
        }
        const double llega = 800, alza = 1000, fin = 1900;
        MoverU(_ttHorq, (0, horquillaPuesta ? du : Lejos, null), (llega, du, Frena), (alza, du, null), (fin, Fuera, Arranca));
        Anim(_horq, UIElement.OpacityProperty, (0, horquillaPuesta ? 1 : 0, null), (150, 1, null), (fin - 1, 1, null), (fin, 0, null));
        Alzar(_ttHorca, (0, 0, null), (llega, 0, null), (alza, Alzada, Suave));
        Alzar(_ttAlza, (0, 0, null), (llega, 0, null), (alza, Alzada, Suave));
        MoverU(_ttCarga, (0, du, null), (alza, du, null), (fin, Fuera, Arranca));
        Anim(_carga, UIElement.OpacityProperty, (0, 1, null), (fin - 1, 1, null), (fin, 0, null));
        Celdas("vacia", alza - 100, true);
    }

    /// <summary>Sin animación: cada pieza donde corresponde para ese estado.</summary>
    private void Quieto(string pic, bool carro, bool depositando)
    {
        var duC = pic switch { "sobre" or "grua" => 0.0, "fuera" => Pasado, "sale" => Fuera, _ => Lejos };
        var hay = pic is "sobre" or "grua" or "fuera";
        Fijar(_ttCarga, duC, false); Fijar(_ttAlza, 0, true); Op(_carga, hay ? 1 : 0);
        var horq = !carro && hay && (pic == "grua" || depositando);
        Fijar(_ttHorq, horq ? duC : Lejos, false); Fijar(_ttHorca, 0, true); Op(_horq, horq ? 1 : 0);
        _nod.BeginAnimation(UIElement.OpacityProperty, null); _nod.Opacity = pic == "cero" ? 1 : 0;
    }

    /// <summary>Las celdas de carga cambian de color cuando la carga se apoya (después de ms); sin cero, parpadean en rojo.</summary>
    private void Celdas(string pic, double ms, bool animar)
    {
        for (var i = 0; i < 4; i++)
        {
            var c = pic switch { "sobre" => Verde, "fuera" => i is 1 or 2 ? Ambar : Verde, "grua" or "cero" => Rojo, _ => Gris };
            if (animar) _luz[i].BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(c, TimeSpan.FromMilliseconds(280)) { BeginTime = TimeSpan.FromMilliseconds(ms) });
            else { _luz[i].BeginAnimation(SolidColorBrush.ColorProperty, null); _luz[i].Color = c; }
            var e = _celdas[i];
            e.BeginAnimation(UIElement.OpacityProperty, null); e.Opacity = 1;
            if (pic == "cero")
                e.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimationUsingKeyFrames
                {
                    RepeatBehavior = RepeatBehavior.Forever, Duration = TimeSpan.FromSeconds(1),
                    KeyFrames = { new DiscreteDoubleKeyFrame(1, KeyTime.FromPercent(0)), new DiscreteDoubleKeyFrame(0.25, KeyTime.FromPercent(0.5)) },
                });
        }
    }

    /// <summary>Saca el dibujo de la pantalla anterior para ponerlo en la nueva (un elemento de WPF tiene un solo padre).</summary>
    public void Soltar()
    {
        switch (Raiz.Parent)
        {
            case Panel p: p.Children.Remove(Raiz); break;
            case Decorator d: d.Child = null; break;
            case ContentControl cc: cc.Content = null; break;
        }
    }

    /* ---------- Animación ---------- */

    private static void Anim(IAnimatable o, DependencyProperty p, params (double t, double v, IEasingFunction? e)[] k)
    {
        var a = new DoubleAnimationUsingKeyFrames();
        foreach (var (t, v, e) in k) a.KeyFrames.Add(new EasingDoubleKeyFrame(v, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(t)), e));
        o.BeginAnimation(p, a);
    }
    /// <summary>A lo largo de la plataforma (u): una unidad corre K a la derecha y K/2 hacia abajo.</summary>
    private static void MoverU(TranslateTransform tt, params (double t, double du, IEasingFunction? e)[] k)
    {
        Anim(tt, TranslateTransform.XProperty, k.Select(x => (x.t, x.du * K, x.e)).ToArray());
        Anim(tt, TranslateTransform.YProperty, k.Select(x => (x.t, x.du * K / 2, x.e)).ToArray());
    }
    private static void Alzar(TranslateTransform tt, params (double t, double h, IEasingFunction? e)[] k) =>
        Anim(tt, TranslateTransform.YProperty, k.Select(x => (x.t, -x.h * K, x.e)).ToArray());
    private static double DuDe(TranslateTransform tt) => tt.X / K;
    private static void Fijar(TranslateTransform tt, double v, bool alto)
    {
        tt.BeginAnimation(TranslateTransform.XProperty, null); tt.BeginAnimation(TranslateTransform.YProperty, null);
        if (alto) { tt.X = 0; tt.Y = -v * K; } else { tt.X = v * K; tt.Y = v * K / 2; }
    }
    private static void Op(UIElement e, double v) { e.BeginAnimation(UIElement.OpacityProperty, null); e.Opacity = v; }

    /* ---------- Dibujo en isométrico ---------- */

    private static Point Pt(double u, double v, double h) => new(OX + (u - v) * K, OY + (u + v) * K / 2 - h * K);
    private static PointCollection Puntos(params (double u, double v, double h)[] p) => new(p.Select(q => Pt(q.u, q.v, q.h)));
    private static Color C(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    private static void Poli(Panel en, string relleno, params (double u, double v, double h)[] p) =>
        en.Children.Add(new Polygon { Points = Puntos(p), Fill = Ui.B(relleno) });

    /// <summary>Una caja con sus tres caras a la vista: arriba, la que mira abajo a la izquierda (v = v1) y la que mira abajo a la derecha (u = u1).</summary>
    private static void Caja(Panel en, double u0, double u1, double v0, double v1, double h0, double h1, string arriba, string izq, string der, string? borde = null)
    {
        Poli(en, izq, (u0, v1, h1), (u1, v1, h1), (u1, v1, h0), (u0, v1, h0));
        Poli(en, der, (u1, v0, h1), (u1, v1, h1), (u1, v1, h0), (u1, v0, h0));
        var tapa = new Polygon { Points = Puntos((u0, v0, h1), (u1, v0, h1), (u1, v1, h1), (u0, v1, h1)), Fill = Ui.B(arriba) };
        if (borde != null) { tapa.Stroke = Ui.B(borde); tapa.StrokeThickness = 0.7; }
        en.Children.Add(tapa);
    }

    private static void Linea(Panel en, string color, double grosor, params (double u, double v, double h)[] p) =>
        en.Children.Add(new Polyline { Points = Puntos(p), Stroke = Ui.B(color), StrokeThickness = grosor, StrokeLineJoin = PenLineJoin.Round });

    private static Ellipse Punto(Panel en, double u, double v, double h, double r, string? color, Brush? relleno = null)
    {
        var e = new Ellipse { Width = 2 * r, Height = 2 * r, Fill = relleno ?? Ui.B(color!) };
        var p = Pt(u, v, h);
        Canvas.SetLeft(e, p.X - r); Canvas.SetTop(e, p.Y - r);
        en.Children.Add(e);
        return e;
    }
}
