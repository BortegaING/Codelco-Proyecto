using System.Windows;
using System.Windows.Controls;
using Run = System.Windows.Documents.Run;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using static System.Windows.FontWeights;

namespace TerminalVentanas;

/// <summary>Las pantallas: ingreso, principal (etiqueta que sigue, báscula en vivo, área de trabajo y botones) y la pantalla Báscula.</summary>
public partial class MainWindow
{
    /* ---------- Piezas que se repiten ---------- */
    private static Border Dsp(string etiqueta, string valor, double tam)
    {
        var lbl = Ui.Rotulo(etiqueta, 14); lbl.VerticalAlignment = VerticalAlignment.Bottom; lbl.Margin = new Thickness(0, 0, 0, 6);
        var v = Ui.T(valor, tam, Bold, Ui.CobreClaro, Ui.Mono); v.HorizontalAlignment = HorizontalAlignment.Right;
        return new Border
        {
            Background = Ui.Fondo, BorderBrush = Ui.Cobre, BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16, 4, 16, 4), MinHeight = 58, Child = Ui.Columnas("* auto", 10, lbl, v),
        };
    }

    private UIElement Teclado(Action<string> tecla, double alto, double tam)
    {
        var teclas = new List<UIElement>();
        foreach (var d in "123456789") { var c = d.ToString(); teclas.Add(Tecla(Ui.T(c, tam, Bold, Ui.Tinta, Ui.Mono), () => tecla(c), alto)); }
        teclas.Add(Tecla(Ui.T("BORRAR", 18, SemiBold, Ui.Tinta, Ui.Cond), () => tecla("C"), alto));
        teclas.Add(Tecla(Ui.T("0", tam, Bold, Ui.Tinta, Ui.Mono), () => tecla("0"), alto));
        teclas.Add(Tecla(Ui.Ico(Iconos.Retroceso, 30, 30, Ui.Tinta, 2), () => tecla("del"), alto));
        return Ui.Grilla(3, teclas, 8);
    }

    private Border Tecla(UIElement contenido, Action a, double alto) => Ui.Boton(contenido, A(a), Ui.Tecla, Ui.Linea3, 10, new Thickness(0), alto);

    private Border BotonGrande(string texto, string? icono, Action? a, bool deshabilitado, double alto, double tam)
    {
        var color = deshabilitado ? Ui.Mut2 : Ui.SobreCobre;
        var fila = icono == null ? Ui.Fila(Ui.T(texto, tam, Bold, color, Ui.Cond, true))
            : Ui.Fila(Ui.Ico(icono, tam + 5, tam + 5, color, 2.2), Ui.T(texto, tam, Bold, color, Ui.Cond, true).Margen(14, 0, 0, 0));
        return Ui.Boton(fila, deshabilitado || a == null ? null : A(a), deshabilitado ? Ui.Linea : Ui.Cobre, Brushes.Transparent, 14, new Thickness(18, 0, 18, 0), alto, new Thickness(0));
    }

    private Border BotonSec(UIElement contenido, Action a, double alto = 84, Thickness? relleno = null) =>
        Ui.Boton(contenido, A(a), Brushes.Transparent, Ui.Linea2, 14, relleno ?? new Thickness(22, 0, 22, 0), alto);

    private Border BotonMini(string texto, Action a, bool encendido = false) =>
        Ui.Boton(Ui.T(texto, 17, SemiBold, encendido ? Ui.SobreCobre : Ui.Tinta, Ui.Cond, true), A(a), encendido ? Ui.Cobre : Brushes.Transparent, encendido ? Ui.Cobre : Ui.Linea2, 10, new Thickness(14, 0, 14, 0), 44);

    private static Border Insignia(int n) => new()
    {
        Background = Ui.Linea, CornerRadius = new CornerRadius(999), Padding = new Thickness(9, 1, 9, 2), MinWidth = 30,
        Child = new TextBlock { Text = n.ToString(), FontFamily = Ui.Mono, FontWeight = Bold, FontSize = 16, Foreground = Ui.Tinta, HorizontalAlignment = HorizontalAlignment.Center },
    };

    private static Ellipse Latido()
    {
        var e = new Ellipse { Width = 9, Height = 9, Fill = Ui.VerdeL };
        e.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, 0.35, TimeSpan.FromSeconds(1)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
        return e;
    }

    /* ---------- Ingreso ---------- */
    private UIElement VLogin()
    {
        var marca = Ui.Fila(Ui.Ico(Iconos.Etiqueta, 52, 52, Ui.CobreMedio),
            Ui.Columna(Ui.T("ROTULADO DE CARGAS", 30, Bold, Ui.Tinta, Ui.Cond), Ui.Rotulo("Nave · Báscula N9 · VT1200", 15)).Margen(14, 0, 0, 0));
        var nota = Ui.T("El RUT de prueba ya está escrito: toque Entrar.", 15, Medium, Ui.Mut); nota.HorizontalAlignment = HorizontalAlignment.Center;
        var col = Ui.Columna(marca, Dsp("RUT del operador", S.Rut.Length > 0 ? D.FmtRut(S.Rut) : "—", 46).Margen(0, 16, 0, 0),
            ((FrameworkElement)Teclado(Rut, 70, 30)).Margen(0, 14, 0, 0), BotonGrande("Entrar", null, Entrar, false, 84, 33).Margen(0, 14, 0, 0), nota.Margen(0, 14, 0, 0));
        col.Width = 520; col.HorizontalAlignment = HorizontalAlignment.Center; col.VerticalAlignment = VerticalAlignment.Center;
        // Salir cierra el programa (con pregunta antes): en la VT1200 a pantalla completa no hay otra forma con el dedo
        var salir = BotonSec(Ui.T("SALIR", 20, SemiBold, Ui.Tinta, Ui.Cond), PedirCerrar, 56, new Thickness(24, 0, 24, 0));
        salir.HorizontalAlignment = HorizontalAlignment.Right; salir.VerticalAlignment = VerticalAlignment.Top;
        var g = new Grid(); g.Children.Add(col); g.Children.Add(salir);
        return g;
    }

    /* ---------- Principal ---------- */
    private UIElement VMain(Modelo m)
    {
        Border Tab(string v, string txt)
        {
            var on = S.Prod == v;
            var c = Ui.Fila(Ui.IcoProd(v, 36, 27, on ? Ui.CobreMedio : Ui.Mut2), Ui.T(txt, 21, Bold, on ? Ui.Tinta : Ui.Mut, Ui.Cond, true).Margen(10, 0, 0, 0));
            return Ui.Boton(c, A(() => Prod(v)), on ? Ui.Panel : Brushes.Transparent, on ? Ui.Cobre : Ui.Linea, 12, new Thickness(18, 0, 18, 0), 56, on ? new Thickness(1, 1, 1, 4) : new Thickness(1));
        }
        var tabs = Ui.Fila(Tab("cat", "Cátodos"), Tab("lam", "Láminas").Margen(8, 0, 0, 0), Tab("rs", "Restos de ánodos").Margen(8, 0, 0, 0));
        var quien = new TextBlock { FontFamily = Ui.Cond, FontSize = 15, FontWeight = SemiBold, Foreground = Ui.Mut, VerticalAlignment = VerticalAlignment.Center };
        quien.Inlines.Add(new Run(S.User.ToUpperInvariant()) { Foreground = Ui.Tinta }); quien.Inlines.Add(new Run($" · {Hora()}"));
        var who = Ui.Fila(quien, BotonSec(Ui.T("SALIR", 17, SemiBold, Ui.Tinta, Ui.Cond), Salir, 46, new Thickness(16, 0, 16, 0)).Margen(12, 0, 0, 0));
        who.HorizontalAlignment = HorizontalAlignment.Right;
        var bar = Ui.Columnas("* auto", 12, tabs, who);
        var top = Ui.Columnas("* 380", 12, Hero(m), Balanza());
        var trabajo = m.Trabajo?.Invoke() ?? new Grid();
        var registro = BotonSec(Ui.Fila(Ui.Ico(Iconos.Lista, 26, 26, Ui.Mut), Ui.T("REGISTRO", 21, SemiBold, Ui.Tinta, Ui.Cond).Margen(10, 0, 0, 0), Insignia(S.Labels.Count).Margen(10, 0, 0, 0)), () => Abrir("registro"));
        var excel = BotonSec(Ui.Fila(Ui.Ico(Iconos.Excel, 26, 26, Ui.VerdeXls), Ui.T("EXCEL", 21, SemiBold, Ui.Tinta, Ui.Cond).Margen(10, 0, 0, 0)), BajarExcel);
        var acc = Ui.Columnas("* auto auto", 12, BotonGrande(m.ImpTxt, Iconos.Imprimir, Imprimir, m.ImpDis, 84, 33), registro, excel);
        return Ui.Filas("56 auto * 84", 12, bar, top, trabajo, acc);
    }

    private Border Hero(Modelo m)
    {
        var cod = m.Corto ?? m.Codigo; var pre = cod.StartsWith("Nº "); var num = pre ? cod[3..] : cod;
        var fs = Math.Min(92, Math.Floor((pre ? 240 : 270) / (0.6 * Math.Max(1, num.Length))));
        var codigo = new TextBlock { FontFamily = Ui.Mono, FontWeight = Bold, FontSize = fs, Foreground = m.Vacio || cod == "—" ? Ui.Mut2 : Ui.Tinta, LineHeight = fs, LineStackingStrategy = LineStackingStrategy.BlockLineHeight };
        if (pre) codigo.Inlines.Add(new Run("Nº ") { FontFamily = Ui.Cond, FontSize = fs * 0.42, Foreground = Ui.Mut });
        codigo.Inlines.Add(new Run(num));
        var e = m.Estado;
        var estado = new WrapPanel { Orientation = Orientation.Horizontal };
        estado.Children.Add(Ui.IcoEstado(e.K, 26).Margen(0, 0, 10, 0));
        var txt = Ui.T(e.Txt, 19, SemiBold, Ui.ColorEstado(e.K), ajustar: true); txt.MaxWidth = 232; txt.LineHeight = 24;
        estado.Children.Add(txt);
        if (e.Volver != null)
            estado.Children.Add(Ui.Boton(Ui.T($"Volver a {e.Volver}", 18, Bold, Ui.B("#FFF4D6"), Ui.Cond, true), A(Volver), Ui.B("#3F3005"), Ui.B("#A07A12"), 10, new Thickness(16, 0, 16, 0), 46).Margen(0, 8, 0, 0));
        var htxt = Ui.Columna(Ui.Rotulo(m.Lbl, 15), codigo.Margen(0, 8, 0, 6), estado);
        htxt.VerticalAlignment = VerticalAlignment.Center;

        var boca = new Border
        {
            Width = 266, Height = 16, CornerRadius = new CornerRadius(8), BorderBrush = Ui.B("#3A5563"), BorderThickness = new Thickness(0, 0, 0, 2),
            Background = new LinearGradientBrush(Ui.B("#22313A").Color, Ui.B("#0A1013").Color, 90),
            Child = new Border { Height = 2, Margin = new Thickness(14, 6, 14, 0), VerticalAlignment = VerticalAlignment.Top, Background = Ui.B("#05080A") },
        };
        Panel.SetZIndex(boca, 2);
        var salida = new Grid { Width = 262, Height = 181, ClipToBounds = true, Margin = new Thickness(0, -6, 0, 0) };
        var st = Sticker.Crear(m.Sticker, S.Fase); st.VerticalAlignment = VerticalAlignment.Top; st.Margin = new Thickness(0, 6, 0, 0);
        salida.Children.Add(st);
        var zebra = new Grid { Width = 266 };
        zebra.Children.Add(Ui.Columna(boca, salida));
        return Ui.Tarjeta(Ui.Columnas("* 266", 14, htxt, zebra), relleno: new Thickness(22, 12, 14, 12));
    }

    /* ---------- La Báscula N9 en vivo ---------- */
    private Border Balanza()
    {
        var titulo = Ui.Rotulo("Báscula N9", 15);
        var vivo = Ui.Fila(Latido(), Ui.T("EN LÍNEA", 13, SemiBold, Ui.VerdeT, Ui.Cond).Margen(7, 0, 0, 0));
        var abrir = Ui.Boton(Ui.Fila(Ui.T("ABRIR", 14, SemiBold, Ui.CobreClaro, Ui.Cond), Ui.Ico(Iconos.Lapiz, 15, 15, Ui.CobreClaro).Margen(6, 0, 0, 0)),
            A(AbrirBas), Brushes.Transparent, Ui.Cobre, 8, new Thickness(12, 0, 12, 0), 34);
        var cab = Ui.Columnas("* auto auto", 10, titulo, vivo, abrir);
        var lect = Lectura();
        var dsp = Ui.Tarjeta(Ui.Columnas("* 96", 8, lect, _pic!.Raiz), Ui.Oscuro, Ui.B("#2A3F49"), 12, new Thickness(14, 10, 10, 10));
        _balRes = new ContentControl { Content = VRes(), MinHeight = 44 };
        return Ui.Tarjeta(Ui.Filas("auto auto auto", 9, cab, dsp, _balRes), relleno: new Thickness(14, 12, 14, 12));
    }

    private UIElement Lectura()
    {
        var ch = Chips[B.Chip]; _balChip = Ui.Chip(ch.Txt, ch.Clase);
        var t = D.Kg((int)Math.Max(0, Math.Round(B.Lect)));
        _balNum = Ui.T(t, t.Length > 5 ? 48 : 58, Bold, Ui.Brillo, Ui.Mono);
        var kg = Ui.T("kg", 22, SemiBold, Ui.Mut).Margen(6, 0, 0, 7); kg.VerticalAlignment = VerticalAlignment.Bottom;
        var num = Ui.Fila(_balNum, kg); num.VerticalAlignment = VerticalAlignment.Bottom;
        _pic = new Pictograma(B.Pic, B.ProdPic);
        return Ui.Columna(_balChip, num.Margen(0, 6, 0, 0));
    }

    private UIElement VRes()
    {
        var r = B.Res;
        var col = Ui.Columna(Ui.T(r.T1, 19, SemiBold, Ui.ColorEstado(r.K)), Ui.T(r.T2, 14, Medium, Ui.Mut3));
        var ico = Ui.IcoEstado(r.K, 24); ico.VerticalAlignment = VerticalAlignment.Top; ico.Margin = new Thickness(0, 1, 0, 0);
        return Ui.Columnas("24 *", 8, ico, col);
    }

    /* ---------- Área de trabajo de cada producto ---------- */
    private static Border Cuadro(string clase, double tam = 13)
    {
        var b = new Border { Width = tam, Height = tam, CornerRadius = new CornerRadius(3), Margin = new Thickness(10, 0, 6, 0) };
        switch (clase)
        {
            case "pes": b.Background = Ui.VerdeL; break;
            case "bas": b.Background = Ui.AmbarL; break;
            case "cam": b.Background = Ui.AzulL; break;
            default: b.BorderBrush = Ui.Cobre; b.BorderThickness = new Thickness(2); break;
        }
        return b;
    }

    private UIElement TrabajoCat(Grupo G, int? sig)
    {
        var cel = G.Celdas.Values.ToList(); var pes = cel.Count(c => c.E == "pesada"); var cam = cel.Count - pes;
        TextBlock L(string s) => Ui.T(s, 14, Medium, Ui.Mut);
        var ley = Ui.Fila(Cuadro("pes"), L("Pesada"), Cuadro("bas"), L("En báscula"), Cuadro("cam"), L("En camino"), Cuadro("sig"), L("Sigue"));
        var cab = Ui.Columnas("auto auto * auto auto auto", 12, Ui.T($"GRUPO {G.G}", 21, Bold, Ui.Tinta, Ui.Cond), Ui.T($"{pes} pesadas · {cam} en camino", 15, Medium, Ui.Mut),
            new Grid(), ley, BotonMini(S.Muestra ? "Muestra: sí" : "Muestra", AlternarMuestra, S.Muestra), BotonMini("Cambiar grupo", () => Abrir("grupo")));
        cab.MinHeight = 44;
        Border Lado(string k)
        {
            var l = k == "a" ? G.LadoA : G.LadoB;
            return Ui.Boton(Ui.Fila(Ui.T($"LADO {LadoTxt(l).ToUpperInvariant()}", 14, SemiBold, Ui.CobreClaro, Ui.Cond), Ui.Ico(Iconos.Lapiz, 16, 16, Ui.CobreClaro).Margen(6, 0, 0, 0)),
                A(() => Abrir("lado", k)), Brushes.Transparent, Ui.B("#9A6337"), 8, new Thickness(10, 0, 10, 0), 34);
        }
        var nota = L("Si el paquete es otro, toque su celda"); nota.HorizontalAlignment = HorizontalAlignment.Right;
        var f1 = Ui.Columnas("auto auto *", 8, Ui.Rotulo("Celdas 1 a 21 ·", 14), Lado("a"), nota);
        var f2 = Ui.Columnas("auto auto *", 8, Ui.Rotulo("Celdas 42 a 22 ·", 14), Lado("b"), new Grid());
        UIElement Sq(int n)
        {
            G.Celdas.TryGetValue(n, out var c);
            Brush fondo = Ui.Fondo, borde = Ui.Linea, color = Ui.Mut; var grosor = 1.0;
            if (c != null) (fondo, borde, color) = c.E == "pesada" ? (Ui.VerdeF, Ui.VerdeL, Ui.VerdeC) : c.E == "bascula" ? (Ui.AmbarF, Ui.AmbarL, Ui.AmbarC) : (Ui.AzulF, Ui.AzulL, Ui.AzulC);
            else if (n == sig) { borde = Ui.Cobre; color = Ui.CobreClaro; grosor = 3; }
            if (!S.Muestra && n == S.Sel && n != sig) { fondo = Ui.Cobre; borde = Ui.Cobre; color = Ui.SobreCobre; }
            var col = Ui.Columna(Centro(Ui.T(n.ToString(), 19, Bold, color)));
            if (c is { E: "pesada" } && c.P > 0) { var w = Ui.T(D.Kg(c.P), 10.5, SemiBold, Ui.VerdeC, Ui.Mono); w.Opacity = 0.85; col.Children.Add(Centro(w).Margen(0, 2, 0, 0)); }
            return Ui.Boton(col, A(() => CeldaElegida(n)), fondo, borde, 9, new Thickness(0), 72, new Thickness(grosor));
        }
        var m1 = Ui.Grilla(21, D.Orden.Take(21).Select(Sq), 4);
        var m2 = Ui.Grilla(21, D.Orden.Skip(21).Select(Sq), 4);
        var caja = Ui.Tarjeta(Ui.Filas("auto auto auto auto auto", 8, cab, f1, m1, f2, m2));
        caja.VerticalAlignment = VerticalAlignment.Top;
        return caja;
    }

    private static T Centro<T>(T el) where T : FrameworkElement { el.HorizontalAlignment = HorizontalAlignment.Center; return el; }

    /// <summary>Recuadro chico de un número de lámina o de un carro, con su estado.</summary>
    private static Border Ficha(string titulo, string sub, string clase, double ancho = 120, double alto = 66, double tam = 22)
    {
        Brush fondo = Ui.Fondo, borde = Ui.Linea, ct = Ui.Tinta, cs = Ui.Mut; var grosor = 1.0; var op = 1.0;
        switch (clase)
        {
            case "pesada": fondo = Ui.VerdeF; borde = Ui.VerdeL; cs = Ui.VerdeC; break;
            case "camino": fondo = Ui.AzulF; borde = Ui.AzulL; cs = Ui.AzulC; break;
            case "bascula": fondo = Ui.AmbarF; borde = Ui.AmbarL; cs = Ui.AmbarC; break;
            case "sintara": fondo = Ui.RojoF; borde = Ui.RojoL; cs = Ui.RojoC; break;
            case "sig": borde = Ui.Cobre; grosor = 3; ct = Ui.CobreClaro; cs = Ui.CobreClaro; break;
            case "vacio": op = 0.5; ct = Ui.Mut; break;
        }
        var col = Ui.Columna(Ui.T(titulo, tam, Bold, ct, Ui.Cond), Ui.T(sub, 13.5, SemiBold, cs, Ui.Cond));
        col.VerticalAlignment = VerticalAlignment.Center;
        var b = new Border { Background = fondo, BorderBrush = borde, BorderThickness = new Thickness(grosor), CornerRadius = new CornerRadius(12), Padding = new Thickness(12, 0, 12, 0), Height = alto, Opacity = op, Child = col };
        if (ancho > 0) b.Width = ancho;
        return b;
    }

    private UIElement Cabecera(string titulo, string cuenta, UIElement? derecha = null)
    {
        var g = Ui.Columnas("auto * auto", 12, Ui.T(titulo, 21, Bold, Ui.Tinta, Ui.Cond, true), Ui.T(cuenta, 15, Medium, Ui.Mut), derecha ?? new Grid());
        g.MinHeight = 44; return g;
    }

    private UIElement TrabajoLam(D.TipoLam T, int n)
    {
        var seg = D.Lam.Select(t =>
        {
            var on = t.Id == T.Id;
            var col = Ui.Columna(Ui.T(t.Corto, 27, Bold, Ui.Tinta, Ui.Cond), Ui.T($"Sigue Nº {S.Lam.Sig[t.Id]}", 15, SemiBold, on ? Ui.CobreClaro : Ui.Mut));
            col.HorizontalAlignment = HorizontalAlignment.Left; col.VerticalAlignment = VerticalAlignment.Center;
            return (UIElement)Ui.Boton(col, A(() => LamTipo(t.Id)), on ? Ui.Panel2 : Ui.Fondo, on ? Ui.Cobre : Ui.Linea3, 14, new Thickness(18, 0, 18, 0), 90, on ? new Thickness(5, 2, 2, 2) : new Thickness(1), false);
        });
        var caja1 = Ui.Tarjeta(Ui.Filas("auto auto", 8, Cabecera("Tipo de lámina", "Una etiqueta por pallet. Si van dos montados, una para cada uno."), Ui.Grilla(4, seg, 10)));
        var info = new SortedDictionary<int, (string E, int Kg, bool Ayer)>();
        foreach (var (num, kg) in T.Prev) info[num] = ("pesada", kg, true);
        foreach (var l in S.Labels.Where(l => l.Prod == "lam" && l.Ref.T == T.Id)) info[l.Ref.N] = (l.Estado, l.Neto, false);
        var fila = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var x in info.Keys.Where(k => k < n).TakeLast(4))
        {
            var i = info[x];
            fila.Children.Add(Ficha($"Nº {x}", (i.E == "pesada" ? $"{D.Kg(i.Kg)} kg" : D.EstTxt[i.E]) + (i.Ayer ? " · 21-10" : ""), i.E).Margen(0, 0, 8, 0));
        }
        fila.Children.Add(Ficha($"Nº {n}", "Sigue", "sig").Margen(0, 0, 8, 0));
        fila.Children.Add(Ficha($"Nº {n + 1}", " ", "vacio").Margen(0, 0, 8, 0));
        fila.Children.Add(Ficha($"Nº {n + 2}", " ", "vacio"));
        var termino = T.Prev.Length > 0 ? $"El 21-10 terminó en el Nº {T.Prev[^1].N}" : "El 21-10 no hubo";
        var caja2 = Ui.Tarjeta(Ui.Filas("auto auto", 8, Cabecera("Número del mes", $"{termino}. Vuelve a 1 el primer día de cada mes."), fila));
        var g = Ui.Filas("auto auto", 12, caja1, caja2); g.VerticalAlignment = VerticalAlignment.Top;
        return g;
    }

    private UIElement TrabajoRs(RsEstado R)
    {
        var hechos = R.Lista.Where(x => x.G == R.Grupo).ToList(); var n = hechos.Count + 1;
        var slots = new List<UIElement>();
        for (var i = 1; i <= 12; i++)
        {
            if (i <= hechos.Count)
            {
                var x = hechos[i - 1]; var l = S.Labels.FirstOrDefault(y => y.Id == x.Id);
                var est = l?.Estado ?? "camino";
                slots.Add(Ficha($"{i} · C{x.Carro}", est == "pesada" ? $"{D.Kg(l!.Neto)} kg" : D.EstTxt[est], est, 0, 64, 21));
            }
            else if (i == n) slots.Add(Ficha($"Carro {i}", "Sigue", "sig", 0, 64, 21));
            else slots.Add(Ficha($"{i}", " ", "vacio", 0, 64, 21));
        }
        var izq = Ui.Tarjeta(Ui.Filas("auto auto", 8, Dsp("Nº pintado del carro", R.Carro.Length > 0 ? R.Carro : "—", 40), Teclado(Carro, 44, 26)));
        var der = Ui.Tarjeta(Ui.Filas("auto auto", 8, Cabecera($"Carros del grupo {R.Grupo}", $"{hechos.Count} con tarjeta · unos 11 por grupo", BotonMini("Cambiar grupo", () => Abrir("rsgrupo"))), Ui.Grilla(4, slots, 8)));
        izq.VerticalAlignment = VerticalAlignment.Stretch; der.VerticalAlignment = VerticalAlignment.Stretch;
        return Ui.Columnas("330 *", 12, izq, der);
    }

    /* ---------- Pantalla Báscula: el pesaje con los datos de las planillas y del sistema, editables ---------- */
    private sealed record Campo(string K, string Lbl, string V, bool S2 = false, bool Calc = false, bool Dis = false, bool Vacio = false, string? Tag = null);

    private List<Campo> CamposDe(Form f)
    {
        var l = RegDe(f.Id); var pesable = l == null || l.Estado is "pesada" or "sintara";
        string Kg(int? v) => v == null ? "—" : $"{D.Kg(v.Value)} kg";
        Campo Peso(bool s2) => pesable
            ? new Campo("bruto", "Peso bruto", f.Bruto != null ? Kg(f.Bruto) : "Toque Tomar peso", s2, Vacio: f.Bruto == null, Tag: f.Ed.Contains("bruto") ? "digitado" : f.Bruto != null ? "báscula" : "")
            : new Campo("bruto", "Peso bruto", "Se toma al pesar", s2, Calc: true, Vacio: true);
        if (f.Prod == "cat")
        {
            var mu = f.Mu == "S"; (int, int)? R = mu ? null : D.Rango["cat"];
            return new()
            {
                new("g", "Grupo", f.G.ToString()), new("n", "Cuba (celda)", mu ? "00" : D.Pad2(f.N), Dis: mu), new("lado", "Lado", mu ? "—" : f.Lado == "P" ? "P · parcial" : "T · total", Dis: mu),
                new("mu", "Muestra", mu ? "Sí" : "No", Dis: f.Modo != "nuevo"), Peso(true),
                new("x", "Esperado", R != null ? $"{D.Kg(R.Value.Item1)} a {D.Kg(R.Value.Item2)} kg" : "Sin rango", true, Calc: true),
            };
        }
        if (f.Prod == "lam")
        {
            var T = D.LamDe(f.T); var d = S.Labels.Where(x => x.Prod == "lam" && x.Ref.T == f.T).OrderBy(x => x.Id).ToList(); var i = l != null ? d.IndexOf(l) : -1;
            return new()
            {
                new("t", "Tipo de lámina", T.Corto, true), new("num", "Número", $"Nº {f.Num}"), new("x", "Nº de paquete", (i >= 0 ? i + 1 : d.Count + 1).ToString(), Calc: true),
                Peso(false), new("tara", "Peso tara", Kg(f.Tara)), new("x", "Producción neto", Kg(NetoBF(f)), true, Calc: true),
            };
        }
        if (f.Prod == "rs")
        {
            var nt = NetoBF(f);
            return new()
            {
                new("g", "Grupo", f.G.ToString()), new("nr", "Nº en el grupo", f.Nr.ToString()), new("carro", "Carro (pintado)", f.Carro.Length > 0 ? "C" + f.Carro : "—", Vacio: f.Carro.Length == 0),
                new("tara", "Tara carro + rack", f.Tara != null ? Kg(f.Tara) : "Sin tara", Vacio: f.Tara == null, Tag: f.Ed.Contains("tara") ? "digitada" : f.Tara != null ? "lista" : ""),
                Peso(true), new("x", "Neto", nt != null ? Kg(nt) : f.Tara == null ? "Falta la tara" : "—", true, Calc: true),
            };
        }
        var TS = D.SubDe(f.St);
        return new()
        {
            new("st", "Qué es", TS.Corto, true), new("cant", "Cajas o paquetes", f.Cant.ToString()),
            TS.Bote ? new("bote", "Nº de bote", f.Bote.Length > 0 ? f.Bote : "—", Vacio: f.Bote.Length == 0) : new("x", "Turno", S.Turno, Calc: true),
            Peso(true), new("x", "Destino", TS.Bote ? "Traspaso a RAF" : "Productos finales", true, Calc: true),
        };
    }

    private UIElement VCampos(Form f)
    {
        var g = new Grid();
        for (var i = 0; i < 4; i++) { if (i > 0) g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) }); g.ColumnDefinitions.Add(new ColumnDefinition()); }
        int col = 0, fila = 0;
        foreach (var c in CamposDe(f))
        {
            var ancho = c.S2 ? 2 : 1;
            if (col + ancho > 4) { col = 0; fila++; }
            while (g.RowDefinitions.Count <= fila * 2) { if (g.RowDefinitions.Count > 0) g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) }); g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); }
            var el = VCampo(c, f);
            Grid.SetColumn(el, col * 2); Grid.SetColumnSpan(el, ancho * 2 - 1); Grid.SetRow(el, fila * 2);
            g.Children.Add(el);
            col += ancho; if (col >= 4) { col = 0; fila++; }
        }
        return g;
    }

    private UIElement VCampo(Campo c, Form f)
    {
        var calc = c.Calc || c.Dis; var ed = f.Ed.Contains(c.K);
        var tag = c.Tag ?? (calc ? "" : ed ? "cambiado" : f.Modo == "nuevo" ? "propuesto" : "etiqueta");
        Brush borde = calc ? Ui.Linea3 : ed ? Ui.AzulL : Ui.Cobre, fondo = calc ? Ui.B("#13202A") : Ui.Fondo;
        Brush valor = c.Vacio ? Ui.B("#6F8893") : calc ? Ui.B("#B9CAD2") : ed ? Ui.Blanco : Ui.CobreClaro;
        var g = new Grid { MinHeight = 74 };
        var lbl = Ui.Rotulo(c.Lbl, 13.5); lbl.VerticalAlignment = VerticalAlignment.Top; g.Children.Add(lbl);
        if (tag.Length > 0) { var t = Ui.T(tag.ToUpperInvariant(), 12, SemiBold, ed ? Ui.AzulT : Ui.Mut, Ui.Cond); t.HorizontalAlignment = HorizontalAlignment.Right; t.VerticalAlignment = VerticalAlignment.Top; g.Children.Add(t); }
        var v = Ui.T(c.V, 32, Bold, valor, Ui.Cond); v.VerticalAlignment = VerticalAlignment.Bottom; v.Margin = new Thickness(0, 0, 24, 0); g.Children.Add(v);
        if (!calc) { var p = Ui.Ico(Iconos.Lapiz, 18, 18, Ui.Mut); p.HorizontalAlignment = HorizontalAlignment.Right; p.VerticalAlignment = VerticalAlignment.Bottom; g.Children.Add(p); }
        return Ui.Boton(g, calc ? null : A(() => BfCampo(c.K)), fondo, borde, 12, new Thickness(14, 10, 14, 10), 96, calc ? new Thickness(1) : new Thickness(4, 1, 1, 1), false);
    }

    private UIElement VBas()
    {
        var f = S.Bf ??= FormNuevo("cat");
        var l = RegDe(f.Id);
        Border ProdB(string v, string txt)
        {
            var on = f.Prod == v; var dis = f.Modo != "nuevo" && !on;
            var rot = Ui.T(txt, 17, Bold, on ? Ui.Tinta : Ui.Mut, Ui.Cond, true, ajustar: true); rot.MaxWidth = 130; rot.LineHeight = 19;
            var c = Ui.Fila(Ui.IcoProd(v, 34, 26, on ? Ui.CobreMedio : Ui.Mut2), rot.Margen(10, 0, 0, 0));
            var b = Ui.Boton(c, dis ? null : A(() => BfProd(v)), on ? Ui.Panel2 : Ui.Fondo, on ? Ui.Cobre : Ui.Linea3, 12, new Thickness(12, 0, 12, 0), 66, new Thickness(on ? 2 : 1), false);
            ((FrameworkElement)b.Child).VerticalAlignment = VerticalAlignment.Center;
            if (dis) b.Opacity = 0.35;
            return b;
        }
        Border BotTurno(string x) => Ui.Boton(Ui.T($"TURNO {x}", 17, SemiBold, S.Turno == x ? Ui.SobreCobre : Ui.Mut, Ui.Cond), A(() => this.Turno(x)),
            S.Turno == x ? Ui.Cobre : Brushes.Transparent, Brushes.Transparent, 0, new Thickness(16, 0, 16, 0), 46, new Thickness(0));
        var turno = new Border { BorderBrush = Ui.Linea2, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), ClipToBounds = true, Child = Ui.Fila(BotTurno("A"), BotTurno("C")) };
        var (cf, cb, cc, ci, ct) = S.Cero == null ? ("#3F3005", "#A07A12", "#FFF4D6", Iconos.Warn, "Hacer control de cero")
            : S.Cero.Ok ? ("#123C25", "#2F8C55", "#B9F6CA", Iconos.Ok, $"Cero OK {S.Cero.H}") : ("#4A1713", "#B63A30", "#FFC9C2", Iconos.Err, $"Cero: falla {S.Cero.H}");
        var cero = Ui.Boton(Ui.Fila(Ui.Ico(ci, 20, 20, Ui.B(cc), 2.4), Ui.T(ct.ToUpperInvariant(), 16, SemiBold, Ui.B(cc), Ui.Cond).Margen(8, 0, 0, 0)), A(ControlCero), Ui.B(cf), Ui.B(cb), 10, new Thickness(14, 0, 14, 0), 46);
        var titulo = Ui.T("BÁSCULA N9 · PESAJE", 24, Bold, Ui.Tinta, Ui.Cond);
        var bar = Ui.Columnas("auto * auto auto", 12, BotonSec(Ui.T("‹ VOLVER", 17, SemiBold, Ui.Tinta, Ui.Cond), CerrarBas, 46, new Thickness(16, 0, 16, 0)), titulo, turno, cero);

        var lect = Lectura();
        var peso = new Grid();
        peso.ColumnDefinitions.Add(new ColumnDefinition()); peso.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });
        peso.RowDefinitions.Add(new RowDefinition()); peso.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        peso.Children.Add(lect); Grid.SetColumn(_pic!.Raiz, 1); _pic.Raiz.VerticalAlignment = VerticalAlignment.Center; peso.Children.Add(_pic.Raiz);
        _balRes = new ContentControl { Content = VRes(), Margin = new Thickness(0, 8, 0, 0) }; Grid.SetRow(_balRes, 1); Grid.SetColumnSpan(_balRes, 2); peso.Children.Add(_balRes);
        var pesoCaja = Ui.Tarjeta(peso, Ui.Oscuro, Ui.B("#2A3F49"), 16, new Thickness(16, 12, 16, 12));
        var prods = Ui.Grilla(2, new UIElement[] { ProdB("cat", "Cátodos"), ProdB("lam", "Láminas"), ProdB("rs", "Restos de ánodos"), ProdB("sub", "Despuntes y otros") }, 8);
        var top = Ui.Columnas("* 420", 12, pesoCaja, prods);

        var modo = new TextBlock { FontFamily = Ui.Cond, FontSize = 15, FontWeight = SemiBold, Foreground = Ui.Mut };
        if (l != null)
        {
            modo.Inlines.Add(new Run(l.Codigo.ToUpperInvariant()) { Foreground = Ui.Tinta, FontWeight = Bold });
            modo.Inlines.Add(new Run(" · " + (l.Estado == "pesada" ? $"REGISTRADO {l.Hp}" : D.EstTxt[l.Estado].ToUpperInvariant()) + (l.Manual ? " · INGRESO MANUAL" : "")));
        }
        else { modo.Inlines.Add(new Run("PESAJE NUEVO") { Foreground = Ui.Tinta, FontWeight = Bold }); modo.Inlines.Add(new Run(B.Esperando ? " · CARGA SIN ETIQUETA EN LA BÁSCULA" : " · SIN ETIQUETA")); }
        var ayuda = Ui.T("Toque un dato para cambiarlo", 14, Medium, Ui.Mut); ayuda.HorizontalAlignment = HorizontalAlignment.Right;
        var form = Ui.Tarjeta(Ui.Filas("auto auto", 10, Ui.Columnas("* auto", 10, modo, ayuda), VCampos(f)), relleno: new Thickness(16, 12, 16, 14));
        form.VerticalAlignment = VerticalAlignment.Stretch;

        UIElement aviso = new Grid();
        if (f.Msg != null)
        {
            var (af, ab, ac) = f.Msg.K switch { "ok" => ("#123C25", "#2F8C55", "#E9FFF1"), "warn" => ("#3F3005", "#A07A12", "#FFF4D6"), "err" => ("#4A1713", "#B63A30", "#FFE9E6"), _ => ("#13324F", "#3D72B0", "#E6F1FF") };
            aviso = new Border
            {
                Background = Ui.B(af), BorderBrush = Ui.B(ab), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(14, 8, 14, 8), MinHeight = 48,
                Child = Ui.Fila(Ui.Ico(f.Msg.K switch { "ok" => Iconos.Ok, "warn" => Iconos.Warn, "err" => Iconos.Err, _ => Iconos.Info }, 24, 24, Ui.B(ac), 2.4), Ui.T(f.Msg.Txt, 18, SemiBold, Ui.B(ac)).Margen(10, 0, 0, 0)),
            };
        }
        var grabar = BotonGrande(f.Modo == "nuevo" ? "Grabar" : "Grabar cambios", Iconos.Grabar, GrabarBF, false, 84, 30);
        var tomar = BotonSec(Ui.Fila(Ui.Ico(Iconos.Bascula, 26, 26, Ui.Mut), Ui.T("TOMAR PESO", 21, SemiBold, Ui.Tinta, Ui.Cond).Margen(10, 0, 0, 0)), TomarPeso);
        var ver = BotonSec(Ui.Fila(Ui.Ico(Iconos.Lista, 26, 26, Ui.Mut), Ui.T("VER DATOS", 21, SemiBold, Ui.Tinta, Ui.Cond).Margen(10, 0, 0, 0), Insignia(S.Labels.Count).Margen(10, 0, 0, 0)), () => Abrir("registro"));
        var limpiar = BotonSec(Ui.T("LIMPIAR", 21, SemiBold, Ui.Tinta, Ui.Cond), LimpiarBF);
        var acc = Ui.Columnas("* auto auto auto", 12, grabar, tomar, ver, limpiar);
        return Ui.Filas("56 auto * auto 84", 12, bar, top, form, aviso, acc);
    }
}
