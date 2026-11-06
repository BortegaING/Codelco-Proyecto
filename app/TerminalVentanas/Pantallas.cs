using System.Windows;
using System.Windows.Controls;
using Run = System.Windows.Documents.Run;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using static System.Windows.FontWeights;

namespace TerminalVentanas;

/// <summary>
/// Las pantallas: ingreso, principal (etiqueta que sigue, báscula en vivo, área de trabajo y botones) y la pantalla Báscula.
/// Diseño HMI de planta (ISA-101): grises, y el color solo para lo que pide atención. Lo pesado en gris oscuro, lo que está
/// en la báscula en azul, lo que sigue con borde grueso. Los colores salen del tema (claro, oscuro o alto contraste).
/// </summary>
public partial class MainWindow
{
    /* ---------- Piezas que se repiten ---------- */

    /// <summary>La barra de arriba: lo de la izquierda y lo de la derecha.</summary>
    private static Border BarraArriba(UIElement izq, UIElement der)
    {
        if (der is FrameworkElement fe) fe.HorizontalAlignment = HorizontalAlignment.Right;
        return new Border { Background = Ui.Barra, BorderBrush = Ui.BarraBorde, BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(16, 0, 16, 0), Child = Ui.Columnas("* auto", 12, izq, der) };
    }

    /// <summary>Botón de contorno sobre la barra (Salir, Volver).</summary>
    private Border BotonBarra(string texto, Action a) =>
        Ui.Boton(Ui.T(texto, 16, SemiBold, Ui.TextoBarra), A(a), Brushes.Transparent, Ui.SobreBarra, 3, new Thickness(18, 0, 18, 0), 44);

    /// <summary>Los colores de la pantalla (claro, oscuro, alto contraste o como Windows). Con texto en el ingreso; solo el ícono en la barra de trabajo.</summary>
    private Border BotonColores(bool conTexto)
    {
        UIElement c = conTexto ? Ui.Fila(Ui.Ico(Iconos.Colores, 22, 22, Ui.TextoBarra, 2), Ui.T("Colores", 16, SemiBold, Ui.TextoBarra).Margen(8, 0, 0, 0))
            : Ui.Ico(Iconos.Colores, 24, 24, Ui.TextoBarra, 2);
        return Ui.Boton(c, A(() => Abrir("colores")), Brushes.Transparent, Ui.SobreBarra, 3, new Thickness(conTexto ? 14 : 10, 0, conTexto ? 16 : 10, 0), 44);
    }

    /// <summary>Un campo que muestra lo que se escribe con el teclado de la pantalla: el nombre arriba y la caja con el valor.</summary>
    private static UIElement Dsp(string etiqueta, string valor, double tam, bool error = false, bool cursor = true)
    {
        var v = Ui.T(valor, tam, SemiBold, valor == "—" ? Ui.Texto2 : Ui.Texto);
        var fila = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        fila.Children.Add(v);
        if (cursor) fila.Children.Add(new Border { Width = 3, Height = tam, Background = error ? Ui.Err : Ui.Acento, Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
        var caja = new Border
        {
            Background = Ui.Lectura, BorderBrush = error ? Ui.Err : Ui.Acento, BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(3),
            Padding = new Thickness(18, 0, 18, 0), Height = tam + 40, Child = fila,
        };
        // El anillo claro alrededor: la caja que está recibiendo lo que se escribe
        var anillo = new Border { BorderBrush = error ? Ui.ErrTinte : Ui.AcentoTinte, BorderThickness = new Thickness(4), CornerRadius = new CornerRadius(6), Child = caja, Margin = new Thickness(-4) };
        return Ui.Columna(Ui.T(etiqueta, 16, SemiBold, Ui.Texto).Margen(0, 0, 0, 10), anillo);
    }

    /// <summary>Teclado numérico grande. Con conK, la tecla de abajo a la izquierda es la K del RUT; si no, Borrar.</summary>
    private UIElement Teclado(Action<string> tecla, double alto, double tam, bool conK = false)
    {
        var teclas = new List<UIElement>();
        foreach (var d in "123456789") { var c = d.ToString(); teclas.Add(Tecla(Ui.T(c, tam, SemiBold, Ui.Texto), () => tecla(c), alto)); }
        teclas.Add(conK ? Tecla(Ui.T("K", tam - 2, SemiBold, Ui.Texto), () => tecla("K"), alto) : Tecla(Ui.T("Borrar", 18, SemiBold, Ui.Texto), () => tecla("C"), alto));
        teclas.Add(Tecla(Ui.T("0", tam, SemiBold, Ui.Texto), () => tecla("0"), alto));
        teclas.Add(Tecla(Ui.Ico(Iconos.Retroceso, 32, 32, Ui.Texto, 2), () => tecla("del"), alto));
        return Ui.Grilla(3, teclas, 10);
    }

    private Border Tecla(UIElement contenido, Action a, double alto) => Ui.Boton(contenido, A(a), Ui.Lectura, Ui.LineaControl, 3, new Thickness(0), alto);

    /// <summary>La acción principal de la pantalla: gris oscuro relleno (amarillo en alto contraste). Deshabilitado: apagado, sin toque.</summary>
    private Border BotonGrande(string texto, string? icono, Action? a, bool deshabilitado, double alto, double tam)
    {
        var color = deshabilitado ? Ui.SobreApagado : Ui.SobreAccion;
        var fila = icono == null ? Ui.Fila(Ui.T(texto, tam, Bold, color))
            : Ui.Fila(Ui.Ico(icono, tam, tam, color, 2.2), Ui.T(texto, tam, Bold, color).Margen(14, 0, 0, 0));
        var conBorde = deshabilitado && Ui.Contraste; // en alto contraste el apagado es negro: se marca con borde
        return Ui.Boton(fila, deshabilitado || a == null ? null : A(a), deshabilitado ? Ui.Apagado : Ui.Accion, conBorde ? Ui.SobreApagado : Brushes.Transparent, 3, new Thickness(18, 0, 18, 0), alto, new Thickness(conBorde ? 2 : 0));
    }

    private Border BotonSec(UIElement contenido, Action a, double alto = 80, Thickness? relleno = null) =>
        Ui.Boton(contenido, A(a), Ui.Tarjeta0, Ui.LineaControl, 3, relleno ?? new Thickness(22, 0, 22, 0), alto);

    private static TextBlock TxtSec(string s, double tam = 19) => Ui.T(s, tam, SemiBold, Ui.Texto);

    private Border BotonMini(string texto, Action a, bool encendido = false) =>
        Ui.Boton(Ui.T(texto, 16, SemiBold, encendido ? Ui.SobreAcento : Ui.Texto), A(a), encendido ? Ui.Acento : Ui.Tarjeta0, encendido ? Ui.Acento : Ui.LineaControl, 3, new Thickness(16, 0, 16, 0), 44);

    /// <summary>Enlace de texto (Cambiar): azul y subrayado, con un área de toque de 44 px.</summary>
    private Border Enlace(string texto, Action a)
    {
        var t = Ui.T(texto, 15, SemiBold, Ui.Acento); t.TextDecorations = TextDecorations.Underline;
        return Ui.Boton(t, A(a), Brushes.Transparent, Brushes.Transparent, 4, new Thickness(6, 0, 6, 0), 36, new Thickness(0));
    }

    private static Border Insignia(int n) => new()
    {
        Background = Ui.Insignia, CornerRadius = new CornerRadius(2), Padding = new Thickness(9, 1, 9, 2), MinWidth = 30, VerticalAlignment = VerticalAlignment.Center,
        Child = new TextBlock { Text = n.ToString(), FontFamily = Ui.Fuente, FontWeight = Bold, FontSize = 16, Foreground = Ui.SobreInsignia, HorizontalAlignment = HorizontalAlignment.Center },
    };

    private static Rectangle Latido()
    {
        var e = new Rectangle { Width = 9, Height = 9, Fill = Ui.Ok };
        e.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, 0.35, TimeSpan.FromSeconds(1)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
        return e;
    }

    /// <summary>Un aviso con su ícono y fondo suave del color del estado (ok, warn, err, info).</summary>
    private static Border Aviso(string k, string texto, double tam = 17) => new()
    {
        Background = Ui.TinteEstado(k), BorderBrush = Ui.ColorEstado(k), BorderThickness = Ui.GrosorAviso, CornerRadius = new CornerRadius(3), Padding = new Thickness(14, 10, 14, 10),
        Child = Ui.Columnas("24 *", 10, Ui.IcoEstado(k, 24), Ui.T(texto, tam, SemiBold, Ui.ColorEstado(k), ajustar: true)),
    };

    /// <summary>Estado de un equipo en una píldora (báscula, Zebra).</summary>
    private static Border Pildora(string icono, Brush color, string texto) => new()
    {
        Background = Ui.Tarjeta0, BorderBrush = Ui.Linea, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3), Height = 40, Padding = new Thickness(14, 0, 16, 0),
        Child = Ui.Fila(Ui.Ico(icono, 20, 20, color, 2.4), Ui.T(texto, 15, SemiBold, Ui.Texto).Margen(8, 0, 0, 0)),
    };

    /* ---------- Ingreso ---------- */
    private UIElement VLogin()
    {
        var marca = Ui.Fila(Ui.Ico(Iconos.Etiqueta, 30, 30, Ui.Indicador, 2.2),
            Ui.Columna(Ui.T("Rotulado de cargas", 20, SemiBold, Ui.TextoBarra), Ui.T("Báscula N9, nave", 14, Regular, Ui.SobreBarra)).Margen(12, 0, 0, 0));
        var turno = new TextBlock { FontFamily = Ui.Fuente, FontSize = 16, Foreground = Ui.SobreBarra, VerticalAlignment = VerticalAlignment.Center };
        turno.Inlines.Add(new Run("Turno ")); turno.Inlines.Add(new Run(S.Turno) { Foreground = Ui.TextoBarra, FontWeight = SemiBold });
        // Salir cierra el programa (con pregunta antes): en la VT1200 a pantalla completa no hay otra forma con el dedo
        var der = Ui.Fila(turno, Ui.T(Hora(), 16, Regular, Ui.SobreBarra).Margen(16, 0, 16, 0), BotonColores(true).Margen(0, 0, 10, 0), BotonBarra("Salir", PedirCerrar));
        var barra = BarraArriba(marca, der);

        var rut = S.Rut; var completo = rut.Length >= 8; var valido = D.RutValido(rut); var error = completo && !valido;
        var izq = new StackPanel();
        izq.Children.Add(Ui.T("Ingrese su RUT", 40, Bold, Ui.Texto).Margen(0, 4, 0, 6));
        izq.Children.Add(Ui.T("Queda registrado quién imprimió cada etiqueta.", 18, Regular, Ui.Texto2, ajustar: true).Margen(0, 0, 0, 26));
        izq.Children.Add(Dsp("RUT del operador", rut.Length > 0 ? D.FmtRut(rut) : "—", 54, error));
        izq.Children.Add(Ui.T("Escriba los números y la K si la tiene. Los puntos y el guion se ponen solos.", 16, Regular, Ui.Texto2, ajustar: true).Margen(0, 12, 0, 0));
        if (valido) izq.Children.Add(Aviso("ok", "RUT válido. Toque Entrar.").Margen(0, 14, 0, 0));
        else if (error) izq.Children.Add(Aviso("err", $"El dígito verificador no corresponde: para {D.FmtRut(rut[..^1] + "0")[..^2]} es {D.DvRut(rut[..^1])}. Revise el último número.").Margen(0, 14, 0, 0));
        var zebra = _cfg.Impresora.Modo == "seco" ? Pildora(Iconos.Warn, Ui.Warn, "Zebra en modo prueba: no imprime") : Pildora(Iconos.Imprimir, Ui.Ok, "Imprime en la Zebra");
        var equipos = Ui.Fila(Pildora(Iconos.Bascula, Ui.Ok, "Báscula N9 en línea"), zebra.Margen(10, 0, 0, 0));
        equipos.VerticalAlignment = VerticalAlignment.Bottom;
        var colIzq = new Grid(); colIzq.Children.Add(izq); colIzq.Children.Add(equipos);

        var cabTeclado = Ui.Columnas("* auto", 10, Ui.T("Teclado", 16, SemiBold, Ui.Texto2), BotonMini("Borrar todo", () => Rut("C")));
        var teclado = Ui.Tarjeta(Ui.Filas("auto * auto", 12, cabTeclado, Teclado(Rut, 108, 34, true), BotonGrande("Entrar", null, Entrar, !valido, 88, 30)), relleno: new Thickness(16));
        var cuerpo = Ui.Columnas("* 400", 24, colIzq, teclado); cuerpo.Margin = new Thickness(28, 28, 28, 24);
        return Ui.Filas("64 *", 0, barra, cuerpo);
    }

    /* ---------- Principal ---------- */
    private UIElement VMain(Modelo m)
    {
        UIElement Tab(string v, string txt)
        {
            var on = S.Prod == v;
            var c = Ui.Fila(Ui.IcoProd(v, 30, 22, on ? Ui.TextoBarra : Ui.SobreBarra), Ui.T(txt, 18, on ? Bold : SemiBold, on ? Ui.TextoBarra : Ui.SobreBarra).Margen(10, 0, 0, 0));
            var g = new Grid { Height = 64 };
            g.Children.Add(Ui.Boton(c, A(() => Prod(v)), Brushes.Transparent, Brushes.Transparent, 0, new Thickness(16, 0, 16, 0), 64, new Thickness(0)));
            if (on) g.Children.Add(new Border { Height = 4, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(12, 0, 12, 0), Background = Ui.Indicador, IsHitTestVisible = false });
            return g;
        }
        var tabs = Ui.Fila(Tab("cat", "Cátodos"), Tab("lam", "Láminas"), Tab("rs", "Restos de ánodos"));
        var who = Ui.Fila(Ui.T(S.User, 16, SemiBold, Ui.TextoBarra), Ui.T(Hora(), 16, Regular, Ui.SobreBarra).Margen(14, 0, 14, 0), BotonColores(false).Margen(0, 0, 10, 0), BotonBarra("Salir", Salir));
        var barra = BarraArriba(tabs, who);

        var top = Ui.Columnas("* 360", 12, Hero(m), Balanza());
        var trabajo = m.Trabajo?.Invoke() ?? new Grid();
        var registro = BotonSec(Ui.Fila(Ui.Ico(Iconos.Lista, 26, 26, Ui.Texto), TxtSec("Registro").Margen(10, 0, 0, 0), Insignia(S.Labels.Count).Margen(10, 0, 0, 0)), () => Abrir("registro"));
        var excel = BotonSec(Ui.Fila(Ui.Ico(Iconos.Excel, 26, 26, Ui.Texto), TxtSec("Excel").Margen(10, 0, 0, 0)), BajarExcel);
        var acc = Ui.Columnas("* auto auto", 12, BotonGrande(m.ImpTxt, Iconos.Imprimir, Imprimir, m.ImpDis, 80, 30), registro, excel);
        var cuerpo = Ui.Filas("244 * 80", 12, top, trabajo, acc); cuerpo.Margin = new Thickness(16, 12, 16, 16);
        return Ui.Filas("64 *", 0, barra, cuerpo);
    }

    private Border Hero(Modelo m)
    {
        var cod = m.Corto ?? m.Codigo; var pre = cod.StartsWith("Nº "); var num = pre ? cod[3..] : cod;
        var fs = Math.Min(118, Math.Floor(300 / (0.62 * Math.Max(1, num.Length + (pre ? 1.2 : 0)))));
        var codigo = new TextBlock { FontFamily = Ui.Fuente, FontWeight = Bold, FontSize = fs, Foreground = m.Vacio || cod == "—" ? Ui.Texto2 : Ui.Texto, LineHeight = fs, LineStackingStrategy = LineStackingStrategy.BlockLineHeight };
        System.Windows.Documents.Typography.SetNumeralAlignment(codigo, FontNumeralAlignment.Tabular);
        if (pre) codigo.Inlines.Add(new Run("Nº ") { FontSize = fs * 0.42, Foreground = Ui.Texto2, FontWeight = SemiBold });
        codigo.Inlines.Add(new Run(num));
        var e = m.Estado;
        var estado = new WrapPanel { Orientation = Orientation.Horizontal };
        estado.Children.Add(Ui.IcoEstado(e.K, 22).Margen(0, 1, 8, 0));
        var txt = Ui.T(e.Txt, 17, SemiBold, Ui.ColorEstado(e.K), ajustar: true); txt.MaxWidth = 290; txt.LineHeight = 22;
        estado.Children.Add(txt);
        if (e.Volver != null)
            estado.Children.Add(Ui.Boton(Ui.T($"Volver a {e.Volver}", 16, SemiBold, Ui.Warn), A(Volver), Ui.WarnTinte, Ui.WarnBorde, 3, new Thickness(14, 0, 14, 0), 44).Margen(0, 8, 0, 0));
        var htxt = Ui.Columna(Ui.Rotulo(m.Lbl, 15), codigo.Margen(0, 10, 0, 10), estado);
        htxt.VerticalAlignment = VerticalAlignment.Center;

        // La boca de la Zebra: la etiqueta sale de ahí (baja al imprimir)
        var boca = new Border
        {
            Width = 266, Height = 14, CornerRadius = new CornerRadius(3), Background = Ui.Boca,
            Child = new Border { Height = 2, Margin = new Thickness(14, 6, 14, 0), VerticalAlignment = VerticalAlignment.Top, Background = Ui.Ranura },
        };
        Panel.SetZIndex(boca, 2);
        var salida = new Grid { Width = 262, Height = 183, ClipToBounds = true, Margin = new Thickness(0, -6, 0, 0) };
        var st = Sticker.Crear(m.Sticker, S.Fase); st.VerticalAlignment = VerticalAlignment.Top; st.Margin = new Thickness(0, 6, 0, 0);
        salida.Children.Add(st);
        var zebra = new Grid { Width = 266, VerticalAlignment = VerticalAlignment.Center };
        zebra.Children.Add(Ui.Columna(boca, salida));
        return Ui.Tarjeta(Ui.Columnas("* 266", 14, htxt, zebra), radio: 3, relleno: new Thickness(20, 12, 14, 12));
    }

    /* ---------- La Báscula N9 en vivo ---------- */
    private Border Balanza()
    {
        var vivo = Ui.Fila(Latido(), Ui.T("En línea", 14, SemiBold, Ui.Ok).Margen(7, 0, 0, 0));
        var abrir = BotonMini("Abrir", AbrirBas);
        var cab = Ui.Columnas("* auto auto", 12, Ui.Rotulo("Báscula N9", 15), vivo, abrir);
        _balRes = new ContentControl { Content = VRes(), MinHeight = 40 };
        return Ui.Tarjeta(Ui.Filas("auto * auto", 8, cab, Lectura(), _balRes), radio: 3, relleno: new Thickness(16, 12, 16, 12));
    }

    /// <summary>La lectura en vivo: el estado (chip) y el número grande, en su recuadro.</summary>
    private UIElement Lectura()
    {
        var ch = Chips[B.Chip]; _balChip = Ui.Chip(ch.Txt, ch.Clase);
        var t = D.Kg((int)Math.Max(0, Math.Round(B.Lect)));
        _balNum = Ui.T(t, t.Length > 5 ? 48 : 56, Bold, Ui.Texto);
        var kg = Ui.T("kg", 24, Medium, Ui.Texto2).Margen(6, 0, 0, 8); kg.VerticalAlignment = VerticalAlignment.Bottom;
        var num = Ui.Fila(_balNum, kg); num.VerticalAlignment = VerticalAlignment.Bottom;

        var col = Ui.Columna(_balChip, num.Margen(0, 6, 0, 0)); col.VerticalAlignment = VerticalAlignment.Center;
        return new Border { Background = Ui.Lectura, BorderBrush = Ui.Linea, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(2), Padding = new Thickness(16, 6, 16, 6), Child = col };
    }

    private UIElement VRes()
    {
        var r = B.Res;
        var t1 = Ui.T(r.T1, 15, SemiBold, r.K is "warn" or "err" ? Ui.ColorEstado(r.K) : Ui.Texto);
        var col = Ui.Columna(t1, Ui.T(r.T2, 14, Regular, Ui.Texto2));
        var ico = Ui.IcoEstado(r.K, 20); ico.VerticalAlignment = VerticalAlignment.Top; ico.Margin = new Thickness(0, 1, 0, 0);
        return Ui.Columnas("20 *", 8, ico, col);
    }

    /* ---------- Área de trabajo de cada producto ---------- */

    /// <summary>Muestra de la leyenda: el mismo dibujo que la celda (relleno, borde e ícono).</summary>
    private static UIElement Muestra(string clase, string texto)
    {
        var (fondo, borde, gro) = clase switch
        {
            "pes" => (Ui.Pesada, Ui.Pesada, 1.0), "bas" => (Ui.EnBascula, Ui.EnBascula, 1.0), "cam" => (Ui.Tarjeta0, Ui.Camino, 2.0), _ => (Ui.Sigue, Ui.SigueBorde, 3.0),
        };
        var b = new Border { Width = 14, Height = 14, CornerRadius = new CornerRadius(1), Background = fondo, BorderBrush = borde, BorderThickness = new Thickness(gro) };
        return Ui.Fila(b, Ui.T(texto, 14, Regular, Ui.Texto2).Margen(6, 0, 14, 0));
    }

    /// <summary>Una celda del mapa: color, número e ícono o peso, para que no dependa solo del color.</summary>
    private UIElement Celda(int n, Celda? c, bool esSig, bool elegida)
    {
        Brush fondo = Ui.Lectura, borde = Ui.Linea, color = Ui.Texto2; var grosor = 1.0; UIElement? extra = null;
        if (c != null)
            switch (c.E)
            {
                case "pesada": fondo = Ui.Pesada; borde = Ui.Pesada; color = Ui.SobrePesada; if (c.P > 0) extra = Ui.T(D.Kg(c.P), 10.5, SemiBold, Ui.SobrePesada); break;
                case "bascula": fondo = Ui.EnBascula; borde = Ui.EnBascula; color = Ui.SobreEnBascula; extra = Ui.Ico(Iconos.Bascula, 16, 16, Ui.SobreEnBascula, 2.4); break;
                default: fondo = Ui.Tarjeta0; borde = Ui.Camino; color = Ui.Camino; grosor = 2; extra = Ui.Ico(Iconos.Etiqueta, 14, 14, Ui.Camino, 2.2); break;
            }
        else if (elegida) { fondo = Ui.Sigue; borde = Ui.SigueBorde; color = Ui.SobreSigue; grosor = 3; extra = Ui.Ico(Iconos.Flecha, 16, 16, Ui.SobreSigue, 3); }
        else if (esSig) { fondo = Ui.Tarjeta0; borde = Ui.SigueBorde; color = Ui.SigueBorde; grosor = 2; }
        if (elegida && c != null) { borde = Ui.SigueBorde; grosor = 3; }
        var col = Ui.Columna(Centro(Ui.T(n.ToString(), 19, Bold, color)));
        if (extra is FrameworkElement fe) col.Children.Add(Centro(fe).Margen(0, 2, 0, 0));
        col.VerticalAlignment = VerticalAlignment.Center;
        return Ui.Boton(col, A(() => CeldaElegida(n)), fondo, borde, 2, new Thickness(0), 72, new Thickness(grosor));
    }

    private UIElement TrabajoCat(Grupo G, int? sig)
    {
        var cel = G.Celdas.Values.ToList();
        int pes = cel.Count(c => c.E == "pesada"), bas = cel.Count(c => c.E == "bascula"), cam = cel.Count - pes - bas;
        var partes = new List<string> { pes == 1 ? "1 pesada" : $"{pes} pesadas" };
        if (bas > 0) partes.Add($"{bas} en la báscula");
        if (cam > 0) partes.Add($"{cam} en camino");
        var cab = Ui.Columnas("auto auto * auto auto", 12, Ui.T($"Grupo {G.G}", 24, Bold, Ui.Texto), Ui.T(string.Join(", ", partes), 16, Regular, Ui.Texto2),
            new Grid(), BotonMini(S.Muestra ? "Muestra: sí" : "Muestra", AlternarMuestra, S.Muestra), BotonMini("Cambiar grupo", () => Abrir("grupo")));
        cab.MinHeight = 44;
        var ley = Ui.Fila(Muestra("pes", "Pesada"), Muestra("bas", "En la báscula"), Muestra("cam", "En camino"), Muestra("sig", "Sigue"));
        ley.HorizontalAlignment = HorizontalAlignment.Right;
        var nota = Ui.T("Si el paquete es otro, toque su celda", 14, Regular, Ui.Texto2); nota.HorizontalAlignment = HorizontalAlignment.Right;
        var f1 = Ui.Columnas("auto auto *", 4, Ui.T($"Lado {LadoTxt(G.LadoA)}, celdas 1 a 21", 15, Regular, Ui.Texto2), Enlace("Cambiar", () => Abrir("lado", "a")), ley);
        var f2 = Ui.Columnas("auto auto *", 4, Ui.T($"Lado {LadoTxt(G.LadoB)}, celdas 42 a 22", 15, Regular, Ui.Texto2), Enlace("Cambiar", () => Abrir("lado", "b")), nota);
        var elegida = S.Muestra ? (int?)null : S.Sel ?? sig;
        UIElement Sq(int n) { G.Celdas.TryGetValue(n, out var c); return Celda(n, c, n == sig, n == elegida); }
        var m1 = Ui.Grilla(21, D.Orden.Take(21).Select(Sq), 6);
        var m2 = Ui.Grilla(21, D.Orden.Skip(21).Select(Sq), 6);
        var caja = Ui.Tarjeta(Ui.Filas("auto auto auto auto auto", 6, cab, f1, m1, f2, m2), radio: 3, relleno: new Thickness(16, 12, 16, 14));
        caja.VerticalAlignment = VerticalAlignment.Top;
        return caja;
    }

    private static T Centro<T>(T el) where T : FrameworkElement { el.HorizontalAlignment = HorizontalAlignment.Center; return el; }

    /// <summary>Recuadro chico de un número de lámina o de un carro, con su estado (color, borde y texto).</summary>
    private static Border Ficha(string titulo, string sub, string clase, double ancho = 120, double alto = 66, double tam = 22)
    {
        Brush fondo = Ui.Lectura, borde = Ui.Linea, ct = Ui.Texto, cs = Ui.Texto2; var grosor = 1.0; var op = 1.0;
        switch (clase)
        {
            case "pesada": fondo = Ui.Pesada; borde = Ui.Pesada; ct = Ui.SobrePesada; cs = Ui.SobrePesada; break;
            case "camino": fondo = Ui.Tarjeta0; borde = Ui.Camino; grosor = 2; ct = Ui.Camino; cs = Ui.Camino; break;
            case "bascula": fondo = Ui.EnBascula; borde = Ui.EnBascula; ct = Ui.SobreEnBascula; cs = Ui.SobreEnBascula; break;
            case "sintara": fondo = Ui.ErrTinte; borde = Ui.Err; grosor = 2; ct = Ui.Err; cs = Ui.Err; break;
            case "sig": fondo = Ui.Sigue; borde = Ui.SigueBorde; grosor = 3; ct = Ui.SobreSigue; cs = Ui.SobreSigue; break;
            case "vacio": op = 0.6; ct = Ui.Texto2; break;
        }
        var col = Ui.Columna(Ui.T(titulo, tam, Bold, ct), Ui.T(sub, 13.5, SemiBold, cs));
        col.VerticalAlignment = VerticalAlignment.Center;
        var b = new Border { Background = fondo, BorderBrush = borde, BorderThickness = new Thickness(grosor), CornerRadius = new CornerRadius(2), Padding = new Thickness(12, 0, 12, 0), Height = alto, Opacity = op, Child = col };
        if (ancho > 0) b.Width = ancho;
        return b;
    }

    private UIElement Cabecera(string titulo, string cuenta, UIElement? derecha = null)
    {
        var g = Ui.Columnas("auto * auto", 12, Ui.T(titulo, 20, Bold, Ui.Texto), Ui.T(cuenta, 15, Regular, Ui.Texto2), derecha ?? new Grid());
        g.MinHeight = 44; return g;
    }

    private UIElement TrabajoLam(D.TipoLam T, int n)
    {
        var seg = D.Lam.Select(t =>
        {
            var on = t.Id == T.Id;
            var col = Ui.Columna(Ui.T(t.Corto, 24, Bold, Ui.Texto), Ui.T($"Sigue el Nº {S.Lam.Sig[t.Id]}", 15, SemiBold, on ? Ui.Acento : Ui.Texto2));
            col.HorizontalAlignment = HorizontalAlignment.Left; col.VerticalAlignment = VerticalAlignment.Center;
            return (UIElement)Ui.Boton(col, A(() => LamTipo(t.Id)), on ? Ui.AcentoTinte : Ui.Tarjeta0, on ? Ui.Acento : Ui.LineaControl, 3, new Thickness(18, 0, 18, 0), 88, new Thickness(on ? 2 : 1), false);
        });
        var caja1 = Ui.Tarjeta(Ui.Filas("auto auto", 8, Cabecera("Tipo de lámina", "Una etiqueta por pallet. Si van dos montados, una para cada uno."), Ui.Grilla(4, seg, 10)));
        var info = new SortedDictionary<int, (string E, int Kg, bool Ayer)>();
        foreach (var (num, kg) in T.Prev) info[num] = ("pesada", kg, true);
        foreach (var l in S.Labels.Where(l => l.Prod == "lam" && l.Ref.T == T.Id)) info[l.Ref.N] = (l.Estado, l.Neto, false);
        var fila = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var x in info.Keys.Where(k => k < n).TakeLast(4))
        {
            var i = info[x];
            fila.Children.Add(Ficha($"Nº {x}", (i.E == "pesada" ? $"{D.Kg(i.Kg)} kg" : D.EstTxt[i.E]) + (i.Ayer ? ", 21-10" : ""), i.E).Margen(0, 0, 8, 0));
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
                slots.Add(Ficha($"Carro {i}: C{x.Carro}", est == "pesada" ? $"{D.Kg(l!.Neto)} kg" : D.EstTxt[est], est, 0, 62, 19));
            }
            else if (i == n) slots.Add(Ficha($"Carro {i}", "Sigue", "sig", 0, 62, 19));
            else slots.Add(Ficha($"{i}", " ", "vacio", 0, 62, 19));
        }
        var izq = Ui.Tarjeta(Ui.Filas("auto auto", 10, Dsp("Nº pintado del carro", R.Carro.Length > 0 ? R.Carro : "—", 26), Teclado(Carro, 38, 22)), relleno: new Thickness(16, 12, 16, 12));
        var der = Ui.Tarjeta(Ui.Filas("auto auto", 8, Cabecera($"Carros del grupo {R.Grupo}", $"{hechos.Count} con tarjeta, unos 11 por grupo", BotonMini("Cambiar grupo", () => Abrir("rsgrupo"))), Ui.Grilla(4, slots, 8)));
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
                new("g", "Grupo", f.G.ToString()), new("n", "Cuba (celda)", mu ? "00" : D.Pad2(f.N), Dis: mu), new("lado", "Lado", mu ? "—" : f.Lado == "P" ? "P, parcial" : "T, total", Dis: mu),
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

    /// <summary>Un dato del pesaje. Se toca para cambiarlo (lápiz); los calculados no se tocan. Cambiado a mano: azul.</summary>
    private UIElement VCampo(Campo c, Form f)
    {
        var calc = c.Calc || c.Dis; var ed = f.Ed.Contains(c.K);
        var tag = c.Tag ?? (calc ? "" : ed ? "Cambiado" : f.Modo == "nuevo" ? "Propuesto" : "De la etiqueta");
        if (tag.Length > 0) tag = char.ToUpperInvariant(tag[0]) + tag[1..];
        Brush borde = calc ? Ui.Linea : ed ? Ui.Acento : Ui.LineaControl, fondo = calc ? Ui.Fondo : ed ? Ui.AcentoTinte : Ui.Tarjeta0;
        Brush valor = c.Vacio ? Ui.Texto2 : Ui.Texto;
        var g = new Grid { MinHeight = 72 };
        var lbl = Ui.Rotulo(c.Lbl, 14); lbl.VerticalAlignment = VerticalAlignment.Top; g.Children.Add(lbl);
        if (tag.Length > 0) { var t = Ui.T(tag, 13, SemiBold, ed ? Ui.Acento : Ui.Texto2); t.HorizontalAlignment = HorizontalAlignment.Right; t.VerticalAlignment = VerticalAlignment.Top; g.Children.Add(t); }
        var v = Ui.T(c.V, 28, SemiBold, valor); v.VerticalAlignment = VerticalAlignment.Bottom; v.Margin = new Thickness(0, 0, 26, 0); g.Children.Add(v);
        if (!calc) { var p = Ui.Ico(Iconos.Lapiz, 18, 18, ed ? Ui.Acento : Ui.Texto2); p.HorizontalAlignment = HorizontalAlignment.Right; p.VerticalAlignment = VerticalAlignment.Bottom; g.Children.Add(p); }
        return Ui.Boton(g, calc ? null : A(() => BfCampo(c.K)), fondo, borde, 3, new Thickness(14, 10, 14, 10), 94, new Thickness(ed ? 2 : 1), false);
    }

    private UIElement VBas()
    {
        var f = S.Bf ??= FormNuevo("cat");
        var l = RegDe(f.Id);
        Border ProdB(string v, string txt)
        {
            var on = f.Prod == v; var dis = f.Modo != "nuevo" && !on;
            var rot = Ui.T(txt, 17, SemiBold, Ui.Texto, ajustar: true); rot.MaxWidth = 130; rot.LineHeight = 20;
            var c = Ui.Fila(Ui.IcoProd(v, 34, 26, on ? Ui.Acento : Ui.Texto2), rot.Margen(10, 0, 0, 0));
            var b = Ui.Boton(c, dis ? null : A(() => BfProd(v)), on ? Ui.AcentoTinte : Ui.Tarjeta0, on ? Ui.Acento : Ui.LineaControl, 3, new Thickness(12, 0, 12, 0), 66, new Thickness(on ? 2 : 1), false);
            ((FrameworkElement)b.Child).VerticalAlignment = VerticalAlignment.Center;
            if (dis) b.Opacity = 0.4;
            return b;
        }
        Border BotTurno(string x)
        {
            var on = S.Turno == x;
            return Ui.Boton(Ui.T($"Turno {x}", 16, SemiBold, on ? Ui.SobreAccion : Ui.TextoBarra), A(() => this.Turno(x)), on ? Ui.Accion : Brushes.Transparent, Brushes.Transparent, 0, new Thickness(16, 0, 16, 0), 42, new Thickness(0));
        }
        var turno = new Border { BorderBrush = Ui.SobreBarra, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3), ClipToBounds = true, Child = Ui.Fila(BotTurno("A"), BotTurno("C")) };
        var (ck, ct) = S.Cero == null ? ("warn", "Hacer control de cero") : S.Cero.Ok ? ("ok", $"Cero OK {S.Cero.H}") : ("err", $"Cero con falla {S.Cero.H}");
        var cero = Ui.Boton(Ui.Fila(Ui.IcoEstado(ck, 20), Ui.T(ct, 16, SemiBold, Ui.ColorEstado(ck)).Margen(8, 0, 0, 0)), A(ControlCero), Ui.TinteEstado(ck), Ui.ColorEstado(ck), 3, new Thickness(14, 0, 14, 0), 44, Ui.GrosorAviso);
        var izqBarra = Ui.Fila(BotonBarra("‹ Volver", CerrarBas), Ui.T("Báscula N9, pesaje", 20, SemiBold, Ui.TextoBarra).Margen(16, 0, 0, 0));
        var barra = BarraArriba(izqBarra, Ui.Fila(turno, cero.Margen(12, 0, 0, 0)));

        var peso = Ui.Filas("* auto", 8, Lectura(), _balRes = new ContentControl { Content = VRes() });
        var pesoCaja = Ui.Tarjeta(peso, radio: 3, relleno: new Thickness(14, 12, 14, 12));
        var prods = Ui.Grilla(2, new UIElement[] { ProdB("cat", "Cátodos"), ProdB("lam", "Láminas"), ProdB("rs", "Restos de ánodos"), ProdB("sub", "Despuntes y otros") }, 8);
        var top = Ui.Columnas("* 420", 12, pesoCaja, prods);

        var modo = new TextBlock { FontFamily = Ui.Fuente, FontSize = 16, Foreground = Ui.Texto2, VerticalAlignment = VerticalAlignment.Center };
        if (l != null)
        {
            modo.Inlines.Add(new Run(l.Codigo) { Foreground = Ui.Texto, FontWeight = Bold });
            modo.Inlines.Add(new Run(", " + (l.Estado == "pesada" ? $"registrado a las {l.Hp}" : D.EstTxt[l.Estado].ToLowerInvariant()) + (l.Manual ? ", ingreso manual" : "")));
        }
        else { modo.Inlines.Add(new Run("Pesaje nuevo") { Foreground = Ui.Texto, FontWeight = Bold }); modo.Inlines.Add(new Run(B.Esperando ? ": carga sin etiqueta en la báscula" : ", sin etiqueta")); }
        var ayuda = Ui.T("Toque un dato para cambiarlo", 14, Regular, Ui.Texto2); ayuda.HorizontalAlignment = HorizontalAlignment.Right;
        var form = Ui.Tarjeta(Ui.Filas("auto auto", 10, Ui.Columnas("* auto", 10, modo, ayuda), VCampos(f)), radio: 3, relleno: new Thickness(16, 12, 16, 14));
        form.VerticalAlignment = VerticalAlignment.Stretch;

        UIElement aviso = f.Msg != null ? Aviso(f.Msg.K, f.Msg.Txt) : new Grid();
        var grabar = BotonGrande(f.Modo == "nuevo" ? "Grabar" : "Grabar cambios", Iconos.Grabar, GrabarBF, false, 80, 28);
        var tomar = BotonSec(Ui.Fila(Ui.Ico(Iconos.Bascula, 26, 26, Ui.Texto), TxtSec("Tomar peso").Margen(10, 0, 0, 0)), TomarPeso);
        var ver = BotonSec(Ui.Fila(Ui.Ico(Iconos.Lista, 26, 26, Ui.Texto), TxtSec("Ver datos").Margen(10, 0, 0, 0), Insignia(S.Labels.Count).Margen(10, 0, 0, 0)), () => Abrir("registro"));
        var limpiar = BotonSec(TxtSec("Limpiar"), LimpiarBF);
        var acc = Ui.Columnas("* auto auto auto", 12, grabar, tomar, ver, limpiar);
        var cuerpo = Ui.Filas("auto * auto 80", 12, top, form, aviso, acc); cuerpo.Margin = new Thickness(16, 12, 16, 16);
        return Ui.Filas("64 *", 0, barra, cuerpo);
    }
}
