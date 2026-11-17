using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using QRCoder;

namespace TerminalVentanas;

/// <summary>La etiqueta tal como sale de la Zebra: tipo, código, QR, las tres cajas (celda, lado, grupo) y el pie.</summary>
public static class Sticker
{
    public const double Ancho = 252, Alto = 173;

    /// <summary>El código QR como dibujo vectorial (módulos negros), sin el margen blanco.</summary>
    public static Image Qr(string texto, double tam)
    {
        var img = new Image { Width = tam, Height = tam, Stretch = Stretch.Uniform };
        try
        {
            using var gen = new QRCodeGenerator();
            using var datos = gen.CreateQrCode(string.IsNullOrEmpty(texto) ? "VEN" : texto, QRCodeGenerator.ECCLevel.M);
            var m = datos.ModuleMatrix;
            const int borde = 4;
            var n = m.Count - 2 * borde;
            var geo = new GeometryGroup { FillRule = FillRule.Nonzero };
            for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++)
                    if (m[y + borde][x + borde]) geo.Children.Add(new RectangleGeometry(new Rect(x, y, 1.02, 1.02)));
            var fondo = new GeometryDrawing(Brushes.White, null, new RectangleGeometry(new Rect(0, 0, n, n)));
            var dib = new DrawingGroup();
            dib.Children.Add(fondo);
            dib.Children.Add(new GeometryDrawing(Ui.Negro, null, geo));
            dib.Freeze();
            img.Source = new DrawingImage(dib);
            RenderOptions.SetEdgeMode(img, EdgeMode.Aliased);
        }
        catch { /* sin QR la etiqueta igual se muestra */ }
        return img;
    }

    /// <summary>fase: null, "imprimiendo" (baja de la impresora) o "impresa" (visto verde encima).</summary>
    public static Border Crear(Etiqueta l, string? fase)
    {
        var tag = new Border { Background = Ui.Negro, Padding = new Thickness(7, 0, 7, 0), Child = Ui.T(l.Tag, 14, FontWeights.Bold, Ui.Blanco, Ui.Cond, true) };
        var id = Ui.T(l.Codigo, 13, FontWeights.Bold, Ui.Negro, Ui.Cond);
        id.HorizontalAlignment = HorizontalAlignment.Right;
        var arriba = Ui.Columnas("auto *", 6, tag, id);

        // Arriba los números que se leen de lejos (celda, lado y grupo); abajo el QR, grande, y lo que trae la carga
        var cajas = new UniformGrid3();
        foreach (var c in l.Cajas)
        {
            var caja = new Border
            {
                BorderBrush = Ui.Negro, BorderThickness = new Thickness(2), Margin = new Thickness(1.5, 0, 1.5, 0), Padding = new Thickness(0, 0, 0, 1),
                Child = Ui.Columna(Centro(Ui.T(c[0], 7.5, FontWeights.Bold, Ui.Negro, Ui.Cond, true)), Centro(Ui.T(c[1], 27, FontWeights.Bold, Ui.Negro, Ui.Cond))),
            };
            cajas.Panel.Children.Add(caja);
        }
        var cant = l.Prod == "cat" && !l.Ref.M && l.Ref.Cant > 1 ? Ui.T($"{l.Ref.Cant} CÁTODOS", 21, FontWeights.Bold, Ui.Negro, Ui.Cond) : null;
        var pie = Ui.T(l.Pie, 8.5, FontWeights.Medium, Ui.Negro, ajustar: true);
        var der = cant != null ? Ui.Columna(cant, pie.Margen(0, 3, 0, 0)) : Ui.Columna(pie);
        der.VerticalAlignment = VerticalAlignment.Center;
        // Versión DataMatrix GS1: el código cuadrado y, al lado, los datos legibles con sus IA
        var gs1 = Gs1.Activo && l.Prod == "cat" && l.Codigo != "—";
        if (gs1) der.Children.Insert(cant != null ? 1 : 0, Ui.T(Gs1.Legible(l), 7.5, FontWeights.SemiBold, Ui.Negro, Ui.Mono, ajustar: true).Margen(0, 2, 0, 0));
        // Logo de Codelco abajo a la derecha, junto al pie (en negro, como sale en la impresora térmica)
        UIElement derecha = der;
        if (LogoCodelco.Activo)
        {
            var g = new Grid(); g.Children.Add(der);
            var logo = LogoCodelco.Vista(22, Ui.Negro); logo.HorizontalAlignment = HorizontalAlignment.Right; logo.VerticalAlignment = VerticalAlignment.Bottom;
            g.Children.Add(logo); derecha = g;
        }
        var abajo = Ui.Columnas("86 *", 8, gs1 ? Gs1.Dibujo(l, 80) : Qr(l.Qr, 86), derecha);
        var cuerpo = Ui.Filas("auto auto *", 5, arriba, cajas.Panel, abajo);

        var hoja = new Grid();
        hoja.Children.Add(new Border { Padding = new Thickness(8, 7, 8, 7), Child = cuerpo });
        if (fase == "impresa")
        {
            var ok = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(225, 23, 112, 61)), CornerRadius = new CornerRadius(3),
                Child = Ui.Columna(Centro(Ui.Ico(Iconos.Ok, 60, 60, Ui.Blanco, 2.4)), Centro(Ui.T("IMPRESA", 24, FontWeights.Bold, Ui.Blanco, Ui.Cond))),
            };
            ((StackPanel)ok.Child).VerticalAlignment = VerticalAlignment.Center;
            hoja.Children.Add(ok);
        }
        var s = new Border
        {
            Width = Ancho, Height = Alto, Background = Ui.Blanco, CornerRadius = new CornerRadius(3), Child = hoja, HorizontalAlignment = HorizontalAlignment.Center,
            Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 14, ShadowDepth = 6, Direction = 270, Opacity = 0.45 },
        };
        if (fase == "imprimiendo")
        {
            var tt = new TranslateTransform(0, -Alto - 4);
            s.RenderTransform = tt;
            var a = new DoubleAnimation(0, TimeSpan.FromMilliseconds(600)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            tt.BeginAnimation(TranslateTransform.YProperty, a);
        }
        return s;
    }

    private static T Centro<T>(T el) where T : FrameworkElement { el.HorizontalAlignment = HorizontalAlignment.Center; return el; }

    private sealed class UniformGrid3
    {
        public readonly System.Windows.Controls.Primitives.UniformGrid Panel = new() { Columns = 3, VerticalAlignment = VerticalAlignment.Center };
    }
}
