using System.Text;

namespace TerminalVentanas;

/// <summary>La etiqueta en ZPL para la Zebra (203 dpi, 100 x 70 mm): lo mismo que se ve en pantalla.</summary>
public static class Zpl
{
    private static string T(string? s) => (s ?? "").Replace('^', ' ').Replace('~', ' ');

    public static string De(Etiqueta l)
    {
        var z = new StringBuilder();
        z.Append("^XA^CI28^PW812^LL568^LH0,0");
        z.Append($"^FO30,28^GB300,56,56^FS^FO44,40^A0N,40,40^FR^FD{T(l.Tag)}^FS");
        z.Append($"^FO340,36^A0N,44,44^FB442,1,0,R^FD{T(l.Codigo)}^FS");
        z.Append($"^FO26,108^BQN,2,8^FDQA,{T(l.Qr)}^FS");
        for (var i = 0; i < l.Cajas.Count; i++)
        {
            var x = 322 + i * 162;
            z.Append($"^FO{x},124^GB150,190,5^FS^FO{x},138^A0N,26,26^FB150,1,0,C^FD{T(l.Cajas[i][0])}^FS^FO{x},182^A0N,100,86^FB150,1,0,C^FD{T(l.Cajas[i][1])}^FS");
        }
        z.Append($"^FO30,512^A0N,28,28^FB760,1,0,L^FD{T(l.Pie)}^FS^XZ");
        return z.ToString();
    }
}
