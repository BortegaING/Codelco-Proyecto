using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZXing;
using ZXing.Common;
using ZXing.Datamatrix;

namespace TerminalVentanas;

/// <summary>
/// Versión de la etiqueta con DataMatrix GS1 en vez de QR ("Codigo": "datamatrix" en appsettings.json).
/// Los datos van con Identificadores de Aplicación (IA) GS1:
///   (01) GTIN de 14 dígitos, solo si se configura ("Gtin"): lo asigna Codelco con su prefijo GS1, por definir.
///   (11) fecha de producción AAMMDD · (30) cantidad de cátodos · (10) lote = el rótulo (5T21)
///   (91) dato interno de la compañía: VEN-C05-T-G21 (refinería, celda, lado, grupo).
/// Los IA de largo variable se separan con FNC1 (en el código va como el carácter GS, ASCII 29).
/// </summary>
public static class Gs1
{
    public const char GS = (char)29;

    /// <summary>"qr" (el de siempre) o "datamatrix" (DataMatrix GS1). Se fija al arrancar desde appsettings.json.</summary>
    public static string Codigo { get; set; } = "qr";
    public static string Gtin { get; set; } = "";
    public static bool Activo => Codigo == "datamatrix";

    /// <summary>Los IA de la etiqueta, en orden: (IA, valor). Los de largo fijo primero; los variables al final.</summary>
    public static List<(string Ia, string Valor)> Campos(Etiqueta l)
    {
        var c = new List<(string, string)>();
        if (Gtin.Length == 14 && Gtin.All(char.IsDigit)) c.Add(("01", Gtin));
        c.Add(("11", D.FQ[2..]));                                   // 261022
        if (l.Ref.M) { c.Add(("10", $"M00G{l.Ref.G}")); c.Add(("91", $"VEN-C00-M-G{l.Ref.G}")); }
        else
        {
            c.Add(("30", Math.Max(1, l.Ref.Cant).ToString()));      // 39 cátodos
            c.Add(("10", l.Codigo.Replace(" ", "")));               // 5T21
            c.Add(("91", $"VEN-C{D.Pad2(l.Ref.N)}-{l.Lado}-G{l.Ref.G}"));
        }
        return c;
    }

    private static readonly HashSet<string> Fijos = new() { "00", "01", "02", "11", "13", "15", "17" };

    /// <summary>Lo que va dentro del DataMatrix: IA y valor seguidos, con GS después de cada IA variable que no es el último.</summary>
    public static string Cadena(Etiqueta l)
    {
        var c = Campos(l); var s = new System.Text.StringBuilder();
        for (var i = 0; i < c.Count; i++)
        {
            s.Append(c[i].Ia).Append(c[i].Valor);
            if (!Fijos.Contains(c[i].Ia) && i < c.Count - 1) s.Append(GS);
        }
        return s.ToString();
    }

    /// <summary>La línea legible bajo el código: (11)261022 (30)39 (10)5T21 (91)VEN-C05-T-G21.</summary>
    public static string Legible(Etiqueta l) => string.Join(" ", Campos(l).Select(x => $"({x.Ia}){x.Valor}"));

    /// <summary>Lo mismo para la Zebra (^BX, ECC 200): FNC1 al inicio y como separador, con la secuencia de escape _1.</summary>
    public static string Zpl(Etiqueta l) => "_1" + Cadena(l).Replace(GS.ToString(), "_1");

    /// <summary>La matriz de módulos del DataMatrix GS1 (cuadrado, sin margen).</summary>
    public static BitMatrix Matriz(Etiqueta l)
    {
        var pistas = new Dictionary<EncodeHintType, object>
        {
            [EncodeHintType.GS1_FORMAT] = true, [EncodeHintType.DATA_MATRIX_COMPACT] = true,
            [EncodeHintType.DATA_MATRIX_SHAPE] = ZXing.Datamatrix.Encoder.SymbolShapeHint.FORCE_SQUARE, [EncodeHintType.MARGIN] = 0,
        };
        return new DataMatrixWriter().encode(Cadena(l), BarcodeFormat.DATA_MATRIX, 0, 0, pistas);
    }

    /// <summary>El DataMatrix como dibujo vectorial (módulos negros), igual que el QR.</summary>
    public static Image Dibujo(Etiqueta l, double tam)
    {
        var img = new Image { Width = tam, Height = tam, Stretch = Stretch.Uniform };
        try
        {
            var m = Matriz(l);
            var geo = new GeometryGroup { FillRule = FillRule.Nonzero };
            for (var y = 0; y < m.Height; y++)
                for (var x = 0; x < m.Width; x++)
                    if (m[x, y]) geo.Children.Add(new RectangleGeometry(new Rect(x, y, 1.02, 1.02)));
            var dib = new DrawingGroup();
            dib.Children.Add(new GeometryDrawing(Brushes.White, null, new RectangleGeometry(new Rect(0, 0, m.Width, m.Height))));
            dib.Children.Add(new GeometryDrawing(Ui.Negro, null, geo));
            dib.Freeze();
            img.Source = new DrawingImage(dib);
            RenderOptions.SetEdgeMode(img, EdgeMode.Aliased);
        }
        catch { /* sin código la etiqueta igual se muestra */ }
        return img;
    }

    /// <summary>Para la prueba: dibuja la matriz con zona blanca, la vuelve a leer con el lector de ZXing y devuelve el texto y el identificador
    /// de simbología (]d2 = DataMatrix GS1, con FNC1 al inicio).</summary>
    public static (string Texto, string Simbologia) Leer(Etiqueta l)
    {
        var m = Matriz(l); const int e = 6, q = 3;
        int w = (m.Width + 2 * q) * e, h = (m.Height + 2 * q) * e;
        var px = new byte[w * h]; Array.Fill(px, (byte)255);
        for (var y = 0; y < m.Height; y++)
            for (var x = 0; x < m.Width; x++)
                if (m[x, y])
                    for (var dy = 0; dy < e; dy++)
                        for (var dx = 0; dx < e; dx++) px[((y + q) * e + dy) * w + (x + q) * e + dx] = 0;
        var src = new RGBLuminanceSource(px, w, h, RGBLuminanceSource.BitmapFormat.Gray8);
        var r = new DataMatrixReader().decode(new BinaryBitmap(new HybridBinarizer(src)), new Dictionary<DecodeHintType, object> { [DecodeHintType.PURE_BARCODE] = true });
        if (r == null) return ("", "");
        var sim = r.ResultMetadata != null && r.ResultMetadata.TryGetValue(ResultMetadataType.SYMBOLOGY_IDENTIFIER, out var s) ? s?.ToString() ?? "" : "";
        return (r.Text, sim);
    }
}
