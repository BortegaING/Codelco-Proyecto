using System.Text;

namespace TerminalVentanas;

/// <summary>La etiqueta en ZPL para la Zebra (203 dpi, 100 x 70 mm): lo mismo que se ve en pantalla.
/// Con una impresora más angosta (por ejemplo la Godex MX30 de 72 mm, que entiende ZPL) todo se escala al ancho configurado.</summary>
public static class Zpl
{
    /// <summary>Ancho de impresión en puntos a 203 dpi: 812 = 100 mm (Zebra); 576 = 72 mm (Godex MX30). Se fija al arrancar desde appsettings.json.</summary>
    public static int Ancho { get; set; } = 812;

    /// <summary>Papel en blanco al final de la etiqueta (puntos), para que el corte no caiga sobre el código. Se fija desde appsettings.json.</summary>
    public static int LargoExtra { get; set; } = 0;

    private static string T(string? s) => (s ?? "").Replace('^', ' ').Replace('~', ' ');

    public static string De(Etiqueta l)
    {
        // El diseño está en puntos de la etiqueta de 812 de ancho; k lo lleva al ancho de la impresora
        var k = Math.Clamp(Ancho, 200, 1200) / 812.0;
        int S(double v) => (int)Math.Round(v * k);
        string F(int x, int y) => $"^FO{S(x)},{S(y)}";
        string A(int alto, int ancho) => $"^A0N,{S(alto)},{S(ancho)}";

        var z = new StringBuilder();
        z.Append($"^XA^CI28^PW{S(812)}^LL{S(568) + Math.Clamp(LargoExtra, 0, 800)}^LH0,0");
        z.Append($"{F(30, 28)}^GB{S(300)},{S(56)},{S(56)}^FS{F(44, 40)}{A(40, 40)}^FR^FD{T(l.Tag)}^FS");
        z.Append($"{F(340, 36)}{A(44, 44)}^FB{S(442)},1,0,R^FD{T(l.Codigo)}^FS");
        // Arriba las tres cajas (celda, lado, grupo) a todo el ancho; abajo el código grande y lo que trae la carga
        for (var i = 0; i < l.Cajas.Count; i++)
        {
            var x = 30 + i * 254;
            z.Append($"{F(x, 100)}^GB{S(242)},{S(170)},{Math.Max(2, S(5))}^FS{F(x, 112)}{A(26, 26)}^FB{S(242)},1,0,C^FD{T(l.Cajas[i][0])}^FS{F(x, 146)}{A(110, 96)}^FB{S(242)},1,0,C^FD{T(l.Cajas[i][1])}^FS");
        }
        if (Gs1.Activo && l.Prod == "cat")
        {
            // DataMatrix GS1 (ECC 200, cuadrado): FNC1 al inicio y entre IA variables con la secuencia de escape _1
            z.Append($"{F(30, 290)}^BXN,{Math.Max(3, S(10))},200,0,0,6,_^FD{Gs1.Zpl(l)}^FS");
            if (!l.Ref.M && l.Ref.Cant > 1) z.Append($"{F(330, 300)}{A(62, 56)}^FD{l.Ref.Cant} CATODOS^FS");
            z.Append($"{F(330, 372)}{A(22, 22)}^FB{S(460)},2,2,L^FD{T(Gs1.Legible(l))}^FS");
            z.Append($"{F(330, 430)}{A(26, 26)}^FB{S(460)},4,4,L^FD{T(l.Pie)}^FS^XZ");
            return z.ToString();
        }
        z.Append($"{F(22, 282)}^BQN,2,{Math.Max(3, S(9))}^FDQA,{T(l.Qr)}^FS");
        if (l.Prod == "cat" && !l.Ref.M && l.Ref.Cant > 1) z.Append($"{F(330, 320)}{A(70, 62)}^FD{l.Ref.Cant} CATODOS^FS");
        z.Append($"{F(330, 420)}{A(28, 28)}^FB{S(460)},4,4,L^FD{T(l.Pie)}^FS^XZ");
        return z.ToString();
    }
}
