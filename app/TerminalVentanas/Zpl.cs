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
        // Arriba las tres cajas (celda, lado, grupo) a todo el ancho; abajo el QR grande y lo que trae la carga
        for (var i = 0; i < l.Cajas.Count; i++)
        {
            var x = 30 + i * 254;
            z.Append($"^FO{x},100^GB242,170,5^FS^FO{x},112^A0N,26,26^FB242,1,0,C^FD{T(l.Cajas[i][0])}^FS^FO{x},146^A0N,110,96^FB242,1,0,C^FD{T(l.Cajas[i][1])}^FS");
        }
        if (Gs1.Activo && l.Prod == "cat")
        {
            // DataMatrix GS1 (ECC 200, cuadrado): FNC1 al inicio y entre IA variables con la secuencia de escape _1
            z.Append($"^FO30,290^BXN,10,200,0,0,6,_^FD{Gs1.Zpl(l)}^FS");
            if (!l.Ref.M && l.Ref.Cant > 1) z.Append($"^FO330,300^A0N,62,56^FD{l.Ref.Cant} CATODOS^FS");
            z.Append($"^FO330,372^A0N,22,22^FB460,2,2,L^FD{T(Gs1.Legible(l))}^FS");
            z.Append($"^FO330,430^A0N,26,26^FB460,4,4,L^FD{T(l.Pie)}^FS^XZ");
            return z.ToString();
        }
        z.Append($"^FO22,282^BQN,2,9^FDQA,{T(l.Qr)}^FS");
        if (l.Prod == "cat" && !l.Ref.M && l.Ref.Cant > 1) z.Append($"^FO330,320^A0N,70,62^FD{l.Ref.Cant} CATODOS^FS");
        z.Append($"^FO330,420^A0N,28,28^FB460,4,4,L^FD{T(l.Pie)}^FS^XZ");
        return z.ToString();
    }
}
