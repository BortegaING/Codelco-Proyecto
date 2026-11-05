using System.IO;
using ClosedXML.Excel;

namespace TerminalVentanas;

/// <summary>
/// El Excel del registro, con la forma de las planillas de hoy: cuaderno de cátodos, hoja de láminas despegue, restos por grupo,
/// formulario de subproductos en PM y un resumen con las columnas del Sistema de Cátodos.
/// </summary>
public static class Excel
{
    public static string Guardar(Estado s, string carpeta)
    {
        Directory.CreateDirectory(carpeta);
        var ruta = Path.Combine(carpeta, $"Rotulado Bascula N9 {D.FECHA} {DateTime.Now:yyyyMMdd-HHmmss}.xlsx");
        using var wb = new XLWorkbook();
        Armar(s, wb);
        wb.SaveAs(ruta);
        return ruta;
    }

    private static void Hoja(XLWorkbook wb, string nombre, List<object?[]> filas, int[] anchos)
    {
        var ws = wb.AddWorksheet(nombre);
        for (var r = 0; r < filas.Count; r++)
            for (var c = 0; c < filas[r].Length; c++) Poner(ws.Cell(r + 1, c + 1), filas[r][c]);
        for (var i = 0; i < anchos.Length; i++) ws.Column(i + 1).Width = anchos[i];
    }

    private static void Poner(IXLCell c, object? v)
    {
        switch (v)
        {
            case null: break;
            case int i: c.Value = (double)i; break;
            case double d: c.Value = d; break;
            case string t when t.Length > 0: c.Value = t; break;
        }
    }

    private static object?[] F(params object?[] v) => v;

    public static void Armar(Estado s, XLWorkbook wb)
    {
        List<Etiqueta> De(string p) => s.Labels.Where(l => l.Prod == p).OrderBy(l => l.Id).ToList();
        static bool Ps(Etiqueta l) => l.Estado == "pesada";
        static int Suma(IEnumerable<Etiqueta> a) => a.Sum(l => l.Neto);
        var cat = De("cat"); var lam = De("lam"); var rs = De("rs"); var sub = De("sub");
        var cero = s.Cero == null ? "Pendiente" : $"{(s.Cero.Ok ? "OK" : "Falla")} {s.Cero.H}";
        string Est(Etiqueta l) => D.EstTxt.GetValueOrDefault(l.Estado, l.Estado);

        // 1. Cátodos, como el cuaderno del pesador: un bloque por grupo y el detalle
        var C = new List<object?[]> { F("Registro de cátodos comerciales · Báscula N9"), F("Fecha", D.FECHA, "", "Control de cero", cero) };
        foreach (var gr in cat.Select(l => l.Ref.G).Distinct())
        {
            var d = cat.Where(l => l.Ref.G == gr && Ps(l)).ToList();
            var g = s.Grupos.GetValueOrDefault(gr) ?? new Grupo { G = gr };
            object? Pe(int n) { var l = d.FirstOrDefault(x => !x.Ref.M && x.Ref.N == n); return l?.Neto; }
            var hs = d.Select(l => l.Hp).Where(h => h.Length > 0).OrderBy(h => h).ToList();
            var T = d.Where(l => !l.Ref.M && l.Lado == "T"); var P = d.Where(l => !l.Ref.M && l.Lado == "P"); var M = d.Where(l => l.Ref.M);
            C.Add(F()); C.Add(F("Grupo", gr, "", "Hora", hs.FirstOrDefault() ?? ""));
            C.Add(F("Celda", $"Lado {g.LadoA} (kg)", "", "Celda", $"Lado {g.LadoB} (kg)"));
            for (var i = 0; i < 21; i++) C.Add(F(i + 1, Pe(i + 1), "", 42 - i, Pe(42 - i)));
            C.Add(F("T", Suma(T))); C.Add(F("P", Suma(P))); C.Add(F("M", Suma(M))); C.Add(F("TOTAL", Suma(T) + Suma(P) + Suma(M)));
        }
        C.Add(F()); C.Add(F("Detalle"));
        C.Add(F("Fecha", "Turno", "Hora etiqueta", "Hora pesaje", "Etiqueta", "Grupo", "Cuba (celda)", "Lado (P/T)", "Muestra (S/N)", "Peso kg", "Estado", "Control báscula", "Operador", "Nota"));
        foreach (var l in cat)
            C.Add(F(D.FECHA, l.Turno, l.Hora, l.Hp, l.Codigo, l.Ref.G, l.Ref.M ? "00" : D.Pad2(l.Ref.N), l.Ref.M ? "" : l.Lado, l.Ref.M ? "S" : "N",
                Ps(l) ? l.Neto : null, Est(l), l.Ctrl, l.Oper, l.Nota));
        Hoja(wb, "Cátodos", C, new[] { 22, 13, 10, 16, 13, 8, 12, 10, 13, 10, 13, 40, 18, 30 });

        // 2. Láminas, como la hoja de láminas despegue: bloques H. INIC., C.O, L.VENTA y L.RECHAZO S/O
        var Lm = new List<object?[]> { F("HOJA DE LÁMINAS DESPEGUE"), F("Fecha", D.FECHA, "", "Turno", "A - C"), F(),
            F("Tipo", "Número", "Peso bruto", "Peso tara", "Producción neto", "Número de paquetes", "Turno", "Hora pesaje", "Control báscula") };
        foreach (var (t, tot) in new[] { ("aprob", "TOTAL H. INIC."), ("co", "TOTAL C.O"), ("venta", "TOTAL VENTA"), ("rso", "TOTAL RECHAZO S/O") })
        {
            var T = D.LamDe(t); var d = lam.Where(l => l.Ref.T == t).ToList();
            for (var i = 0; i < d.Count; i++)
            {
                var l = d[i];
                Lm.Add(F(T.Corto, l.Ref.N, Ps(l) ? l.Bruto : null, l.Tara, Ps(l) ? l.Neto : null, i + 1, l.Turno, l.Hp, Ps(l) ? l.Ctrl : Est(l)));
            }
            Lm.Add(F(tot, "", "", "", Suma(d.Where(Ps)), d.Count));
        }
        Hoja(wb, "Láminas despegue", Lm, new[] { 20, 9, 11, 10, 15, 18, 8, 12, 40 });

        // 3. Restos de ánodos, como la recepción de restos: un bloque por grupo con los carros 1 a 12
        var Rs = new List<object?[]> { F("RECEPCIÓN DE RESTOS DE ÁNODOS COMERCIALES"), F("PRODUCCIÓN DE RESTOS", "", "", "", "", D.FECHA), F() };
        var grupos = rs.Select(l => l.Ref.G).Distinct().ToList();
        for (var k = 0; k < grupos.Count; k += 4)
        {
            var lote = grupos.Skip(k).Take(4).ToList();
            var bloque = Enumerable.Range(0, 14).Select(_ => new object?[12]).ToList();
            for (var j = 0; j < lote.Count; j++)
            {
                var c = j * 3; var gr = lote[j]; var d = rs.Where(l => l.Ref.G == gr).ToList();
                bloque[0][c] = "GRUPO"; bloque[0][c + 1] = gr;
                for (var i = 1; i <= 12; i++)
                {
                    var l = d.FirstOrDefault(x => x.Ref.N == i);
                    bloque[i][c] = i;
                    bloque[i][c + 1] = l == null ? null : Ps(l) ? l.Neto : l.Estado == "sintara" ? "sin tara" : null;
                }
                bloque[13][c] = "TOTAL"; bloque[13][c + 1] = Suma(d.Where(Ps));
            }
            Rs.AddRange(bloque); Rs.Add(F());
        }
        var hm = sub.Where(l => l.Ref.St == "hm").ToList();
        Rs.Add(F("PRODUCCIÓN DE RESTOS DE HOJAS MADRES")); Rs.Add(F("Nº bote", "Peso kg", "Turno", "Hora pesaje"));
        foreach (var l in hm) Rs.Add(F(l.Ref.Bote, l.Neto, l.Turno, l.Hp));
        Rs.Add(F("TOTAL", Suma(hm))); Rs.Add(F()); Rs.Add(F("Detalle de carros"));
        Rs.Add(F("Fecha", "Turno", "Hora tarjeta", "Hora pesaje", "Grupo", "Nº en el grupo", "Carro (pintado)", "Peso bruto", "Tara carro + rack", "Neto", "Estado", "Control báscula", "Operador"));
        foreach (var l in rs)
            Rs.Add(F(D.FECHA, l.Turno, l.Hora, l.Hp, l.Ref.G, l.Ref.N, int.TryParse(l.Ref.Carro, out var nc) ? nc : l.Ref.Carro,
                Ps(l) || l.Estado == "sintara" ? l.Bruto : null, l.SinTara ? null : l.Tara, Ps(l) ? l.Neto : null, Est(l), l.Ctrl, l.Oper));
        Hoja(wb, "Restos de ánodos", Rs, new[] { 14, 10, 10, 13, 10, 12, 15, 11, 16, 10, 12, 40, 18 });

        // 4. Subproductos, como el formulario de entrega y recepción en PM: cajas y peso por turno
        var filas = new (string Nombre, Func<Etiqueta, bool> Es)[]
        {
            ("D.G · despuntes de guillotina", l => l.Prod == "sub" && l.Ref.St == "dg"), ("D.O · despuntes de orejas", l => l.Prod == "sub" && l.Ref.St == "do"),
            ("Láminas rech S/O despegue", l => l.Prod == "lam" && l.Ref.T == "rso"), ("Láminas venta despegue", l => l.Prod == "lam" && l.Ref.T == "venta"),
            ("Láminas venta MFCI", l => l.Prod == "sub" && l.Ref.St == "lvm"), ("Láminas rech S/O MFCI", l => l.Prod == "sub" && l.Ref.St == "lrsm"),
            ("Láminas rech C/O", l => l.Prod == "sub" && l.Ref.St == "lrco"), ("Cu sucio", l => l.Prod == "sub" && l.Ref.St == "cus"),
        };
        var pes = s.Labels.Where(Ps).OrderBy(l => l.Id).ToList();
        int Cant(IEnumerable<Etiqueta> a) => a.Sum(l => l.Prod == "sub" ? Math.Max(1, l.Ref.Cant) : 1);
        List<Etiqueta> DeT(Func<Etiqueta, bool> f, string t) => pes.Where(l => f(l) && (l.Turno.Length > 0 ? l.Turno : "A") == t).ToList();
        var Sp = new List<object?[]> { F("Refinería electrolítica"), F("De", "Báscula N9"), F("Ref", "Formulario entrega y recepción de subproductos en PM"),
            F("Fecha", D.FECHA, "", "Turno", "A y C"), F(), F("Despuntes y láminas con destino a productos finales"),
            F("Procedencia del producto", "Cajas y paquetes T/A", "Cajas y paquetes T/C", "Peso kg T/A", "Peso kg T/C") };
        foreach (var (n, f) in filas) { var a = DeT(f, "A"); var c = DeT(f, "C"); Sp.Add(F(n, Cant(a), Cant(c), Suma(a), Suma(c))); }
        Sp.Add(F()); Sp.Add(F("Peso individual de subproductos (kg)")); Sp.Add(F("Producto", "Turno", "Pesos"));
        foreach (var (n, f) in filas)
            foreach (var t in new[] { "A", "C" })
            {
                var d = DeT(f, t);
                if (d.Count > 0) Sp.Add(new object?[] { n, t }.Concat(d.Select(l => (object?)l.Neto)).ToArray());
            }
        Sp.Add(F()); Sp.Add(F("Entregó conforme", "Pesador turno A", "", "Pesador turno C")); Sp.Add(F("Registro de encargados", "Productos Metalúrgicos"));
        Hoja(wb, "Subproductos", Sp, new[] { 34, 20, 20, 13, 13, 10, 10, 10, 10 });

        // 5. Resumen, con las mismas columnas que el Resumen del Sistema de Cátodos
        var R = new List<object?[]> { F("Resumen del día", D.FECHA), F("Control de cero", cero), F(), F("Cátodos (mismas columnas que el Resumen del Sistema de Cátodos)"),
            F("Día", "Prod.", "Descripción", "Grupo", "Lado-P", "Lado-T", "Muestra", "Tot-peso", "Col-parc", "Col-total", "Tot-coli") };
        var cp = cat.Where(Ps).ToList();
        foreach (var gr in cp.Select(l => l.Ref.G).Distinct())
        {
            var d = cp.Where(l => l.Ref.G == gr).ToList();
            var P = d.Where(l => !l.Ref.M && l.Lado == "P").ToList(); var T = d.Where(l => !l.Ref.M && l.Lado == "T").ToList(); var M = d.Where(l => l.Ref.M).ToList();
            R.Add(F(D.FECHA, 18, "Cátodo comercial", gr, Suma(P), Suma(T), Suma(M), Suma(P) + Suma(T) + Suma(M), P.Count, T.Count, P.Count + T.Count));
        }
        R.Add(F()); R.Add(F("Láminas")); R.Add(F("Tipo", "Pallets pesados", "Neto kg"));
        foreach (var t in D.Lam) { var d = lam.Where(l => l.Ref.T == t.Id && Ps(l)).ToList(); if (d.Count > 0) R.Add(F(t.Nombre, d.Count, Suma(d))); }
        R.Add(F()); R.Add(F("Restos de ánodos")); R.Add(F("Grupo", "Carros pesados", "Neto kg"));
        foreach (var gr in grupos) { var d = rs.Where(l => l.Ref.G == gr && Ps(l)).ToList(); R.Add(F(gr, d.Count, Suma(d))); }
        R.Add(F()); R.Add(F("Despuntes y otros (sin etiqueta)")); R.Add(F("Qué es", "Registros", "Peso kg"));
        foreach (var t in D.Sub) { var d = sub.Where(l => l.Ref.St == t.Id && Ps(l)).ToList(); if (d.Count > 0) R.Add(F(t.Nombre, d.Count, Suma(d))); }
        R.Add(F()); R.Add(F("Nota", "Solo suman los pesos que ya capturó la báscula. Las etiquetas en camino aparecen en cada hoja sin peso."));
        R.Add(F("", "Hora pesaje: la hora real en que la báscula tomó el peso estable (no la hora en que se digita)."));
        R.Add(F("", "Lo ingresado o corregido a mano en la pantalla Báscula queda marcado en la columna Control báscula."));
        Hoja(wb, "Resumen", R, new[] { 26, 15, 18, 8, 10, 10, 10, 11, 10, 10, 10 });
    }
}
