using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static System.Windows.FontWeights;

namespace TerminalVentanas;

/// <summary>Las ventanas sobre la pantalla (una pregunta y botones grandes) y el panel de prueba F2.</summary>
public partial class MainWindow
{
    private static UIElement Encabezado(string chico, string grande) =>
        Ui.Columna(Ui.Rotulo(chico, 15), Ui.T(grande, 34, Bold, Ui.Tinta, Ui.Cond).Margen(0, 2, 0, 0));

    private Border Ventana(double ancho, params UIElement[] partes)
    {
        var col = new StackPanel();
        for (var i = 0; i < partes.Length; i++) { if (partes[i] is FrameworkElement fe && i > 0) fe.Margin = new Thickness(0, 14, 0, 0); col.Children.Add(partes[i]); }
        return new Border
        {
            Width = ancho, MaxHeight = 736, Background = Ui.Fondo, BorderBrush = Ui.Linea2, BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(16),
            Padding = new Thickness(20, 18, 20, 18), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Child = col,
            Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 50, ShadowDepth = 20, Direction = 270, Opacity = 0.6 },
        };
    }

    private UIElement Opcion(string titulo, string sub, bool on, Action a, double alto = 160, double tam = 32)
    {
        var col = Ui.Columna(Ui.T(titulo, tam, Bold, Ui.Tinta, Ui.Cond), Ui.T(sub, alto < 100 ? 15 : 18, SemiBold, on ? Ui.CobreClaro : Ui.Mut, ajustar: true).Margen(0, 6, 0, 0));
        col.HorizontalAlignment = HorizontalAlignment.Left; col.VerticalAlignment = VerticalAlignment.Center;
        return Ui.Boton(col, A(a), Ui.Panel, on ? Ui.Cobre : Ui.Linea, 14, new Thickness(20, 12, 20, 12), alto, on ? new Thickness(5, 2, 2, 2) : new Thickness(1), false);
    }

    private UIElement Acciones(params UIElement[] botones)
    {
        var def = string.Join(" ", botones.Select(_ => "*"));
        return Ui.Columnas(def, 12, botones);
    }

    private Border BotonVentana(string txt, Action a, bool principal = false, bool deshabilitado = false, string? icono = null, Brush? color = null)
    {
        if (principal) return BotonGrande(txt, icono, a, deshabilitado, 72, 24);
        UIElement c = icono == null ? Ui.T(txt.ToUpperInvariant(), 24, SemiBold, color ?? Ui.Tinta, Ui.Cond)
            : Ui.Fila(Ui.Ico(icono, 26, 26, color ?? Ui.Mut), Ui.T(txt.ToUpperInvariant(), 24, SemiBold, color ?? Ui.Tinta, Ui.Cond).Margen(10, 0, 0, 0));
        return Ui.Boton(c, A(a), Brushes.Transparent, color == null ? Ui.Linea2 : Ui.WarnBorde, 14, new Thickness(18, 0, 18, 0), 72);
    }

    private UIElement VModal(Modal md)
    {
        var G = GrupoAct();
        switch (md.Tipo)
        {
            case "lado":
            {
                var k = md.Val; var r = k == "a" ? "1 a 21" : "42 a 22"; var actual = k == "a" ? G.LadoA : G.LadoB;
                void Poner(string v) { if (k == "a") G.LadoA = v; else G.LadoB = v; S.Msg = null; Cerrar(); }
                return Ventana(720, Encabezado($"Grupo {G.G} · celdas {r}", "¿Se sacan las 21 celdas de este lado?"),
                    Ui.Columnas("* *", 12, Opcion("Sí, las 21", "Lado total · T", actual == "T", () => Poner("T")), Opcion("No, solo algunas", "Lado parcial · P", actual == "P", () => Poner("P"))),
                    Acciones(BotonVentana("Cancelar", Cerrar)));
            }
            case "grupo":
            {
                // Paso 1: el layout de la nave, como el plano de circuitos. Se toca el grupo que se va a rotular
                Border BotGrupo(int g)
                {
                    var lam = D.EsLaminas(g); var actual = g == S.Grupo;
                    var hechas = S.Grupos.TryGetValue(g, out var gg) ? gg.Celdas.Count : 0;
                    var txt = Ui.T($"Grupo {g}", 17, Bold, actual ? Ui.SobreCobre : lam ? Ui.Mut2 : Ui.Tinta, Ui.Cond);
                    UIElement c = actual ? txt
                        : hechas > 0 ? Ui.Fila(txt, Ui.T($"· {hechas}", 14, SemiBold, Ui.CobreClaro, Ui.Cond).Margen(5, 0, 0, 0)) : txt;
                    var b = Ui.Boton(c, lam ? null : A(() => ElegirGrupo(g)), actual ? Ui.Cobre : lam ? Brushes.Transparent : Ui.Panel, actual ? Ui.Cobre : Ui.Linea3, 8, new Thickness(6, 0, 6, 0), 42, new Thickness(actual ? 2 : 1));
                    if (lam) b.Opacity = 0.55;
                    return b;
                }
                UIElement Bloque(string nombre, (int A, int B)[] pares, string? pie = null)
                {
                    var bts = new List<UIElement>();
                    foreach (var (a, b) in pares) { bts.Add(BotGrupo(a)); bts.Add(BotGrupo(b)); }
                    var cab = new Border { Background = Ui.Panel2, CornerRadius = new CornerRadius(9, 9, 0, 0), Padding = new Thickness(0, 6, 0, 6), Child = Centro(Ui.T(nombre.ToUpperInvariant(), 16, Bold, Ui.Tinta, Ui.Cond)) };
                    var col = Ui.Columna(cab, Ui.Grilla(2, bts, 6).Margen(9, 9, 9, pie == null ? 9 : 4));
                    if (pie != null) col.Children.Add(Centro(Ui.T(pie, 13, SemiBold, Ui.Mut, Ui.Cond, true)).Margen(0, 0, 0, 8));
                    return new Border { BorderBrush = Ui.Linea2, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Child = col };
                }
                var cs = D.Circuitos;
                var fila1 = Ui.Columnas("* * * *", 10, Bloque(cs[0].Nombre, cs[0].Pares), Bloque(cs[1].Nombre, cs[1].Pares), Bloque(cs[2].Nombre, cs[2].Pares), Bloque(cs[3].Nombre, cs[3].Pares));
                var ew = new Border
                {
                    BorderBrush = Ui.Linea2, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 12, 14, 12), VerticalAlignment = VerticalAlignment.Top,
                    Child = Ui.Columna(Ui.T("EW", 26, Bold, Ui.VerdeT, Ui.Cond), Ui.T("Cátodos electro-obtenidos: hoy llegan sin rótulo. Por confirmar con Codelco.", 14, Medium, Ui.Mut, ajustar: true).Margen(0, 4, 0, 0)),
                };
                var nota = Ui.T($"Toque el grupo que va a rotular. La pantalla lo muestra en grande y deja el que sigue a un toque. Los grupos 1 y 2 son el circuito de láminas.", 15, Medium, Ui.Mut, ajustar: true);
                nota.VerticalAlignment = VerticalAlignment.Top;
                var fila2 = Ui.Columnas("* * 2*", 10, Bloque(cs[4].Nombre, cs[4].Pares, "1 y 2: circuito de láminas"), Bloque(cs[5].Nombre, cs[5].Pares), Ui.Filas("auto auto", 12, ew, nota));
                return Ventana(984, Encabezado("Paso 1 · Layout de la nave", "¿Qué grupo va a rotular?"), fila1, fila2,
                    Acciones(BotonVentana($"Terminar el grupo {G.G}", () => Abrir("terminar"), icono: Iconos.Warn, color: Ui.AmbarT), BotonVentana("Cancelar", Cerrar)));
            }
            case "terminar":
            {
                var a = D.Orden.Take(21).Count(n => G.Celdas.ContainsKey(n)); var b = D.Orden.Skip(21).Count(n => G.Celdas.ContainsKey(n));
                UIElement Linea(string izq, string der) => new Border
                {
                    BorderBrush = Ui.Linea, BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(6, 10, 6, 10),
                    Child = Ui.Columnas("* auto", 10, Ui.T(izq, 18, Medium, Ui.Tinta), Ui.T(der, 18, Bold, Ui.CobreClaro)),
                };
                return Ventana(720, Encabezado($"Grupo {G.G}", "¿Terminar el grupo?"),
                    Ui.Columna(Linea($"Celdas 1 a 21 · lado {LadoTxt(G.LadoA)}", $"{a} paquetes"), Linea($"Celdas 42 a 22 · lado {LadoTxt(G.LadoB)}", $"{b} paquetes"), Linea("Muestra", G.MuestraH != null ? "Sí" : "No")),
                    Ui.T("La báscula sabrá que no vienen más paquetes de este grupo. Después se elige el grupo que sigue.", 15, Medium, Ui.Mut, ajustar: true),
                    Acciones(BotonVentana($"Terminar grupo {G.G}", OkTerminar, true), BotonVentana("Volver", Cerrar)));
            }
            case "rsgrupo":
                return Ventana(560, Encabezado("Restos de ánodos", "¿De qué grupo son los carros?"), Dsp("Grupo", md.Val.Length > 0 ? md.Val : "—", 46), Teclado(Kp, 64, 28),
                    Acciones(BotonVentana("Aceptar", OkRsGrupo, true, md.Val.Length == 0), BotonVentana("Cancelar", Cerrar)));
            case "bfkp":
            {
                var (lbl, preg, kg) = md.Campo switch
                {
                    "g" => ("Grupo", "¿De qué grupo es?", false), "n" => ("Cuba (celda)", "¿Qué celda o cuba?", false), "num" => ("Número", "¿Qué número tiene el pallet?", false),
                    "nr" => ("Nº en el grupo", "¿Qué carro del grupo es? (1 a 12)", false), "carro" => ("Carro", "¿Qué número tiene pintado el carro?", false),
                    "tara" => ("Tara", "¿Cuál es la tara?", true), "bruto" => ("Peso bruto", "¿Cuánto pesa?", true), "cant" => ("Cajas o paquetes", "¿Cuántas cajas o paquetes son?", false),
                    _ => ("Nº de bote", "¿Qué número tiene el bote?", false),
                };
                var v = md.Val; var ahora = AhoraDe(md.Campo);
                return Ventana(560, Encabezado($"Báscula · {lbl}{(ahora.Length > 0 ? $" · ahora {ahora}" : "")}", preg),
                    Dsp(lbl + (kg ? " (kg)" : ""), v.Length > 0 ? (kg && int.TryParse(v, out var n) ? D.Kg(n) : v) : "—", 46), Teclado(Kp, 64, 28),
                    Acciones(BotonVentana("Aceptar", OkBfkp, true, v.Length == 0), BotonVentana("Cancelar", Cerrar)));
            }
            case "bfop":
            {
                var f = S.Bf!; var k = md.Campo;
                (string V, string A, string B)[] ops; string tit; var tres = false; string actual;
                switch (k)
                {
                    case "lado": tit = "¿De qué lado es?"; ops = new[] { ("T", "Total · T", "Se sacaron las 21 celdas"), ("P", "Parcial · P", "Solo algunas celdas") }; actual = f.Lado; break;
                    case "mu": tit = "¿Es la muestra del grupo?"; ops = new[] { ("N", "No", "Paquete de una celda"), ("S", "Sí", "Se graba como cuba 00, sin lado") }; actual = f.Mu; break;
                    case "t": tit = "¿Qué tipo de lámina?"; ops = D.Lam.Select(t => (t.Id, t.Corto, t.Nombre)).ToArray(); tres = true; actual = f.T; break;
                    default: tit = "¿Qué es la carga?"; ops = D.Sub.Select(t => (t.Id, t.Corto, t.Nombre)).ToArray(); tres = true; actual = f.St; break;
                }
                var opciones = ops.Select(o => Opcion(o.A, o.B, actual == o.V, () => SetBfop(o.V), tres ? 82 : 160, tres ? 24 : 32)).ToList();
                return Ventana(720, Encabezado("Báscula", tit), Ui.Grilla(2, opciones, 12), Acciones(BotonVentana("Cancelar", Cerrar)));
            }
            case "colores":
            {
                // Se ve al tocar: el tema cambia en el momento y esta ventana se queda abierta para comparar
                var win = Tema.Nombre(Tema.DeWindows()).ToLowerInvariant();
                UIElement Op(string t, string sub) => Opcion(Tema.Nombre(t), sub, Tema.Elegido == t, () => ElegirTema(t), 118, 26);
                var ops = new[]
                {
                    Op("oscuro", "El diseño original: fondo oscuro y cobre"), Op("claro", "El mismo diseño con fondo claro, para el día"),
                    Op("contraste", "Negro, blanco y amarillo, para leer con reflejo o de lejos"), Op("auto", $"Igual que Windows (ahora {win})"),
                };
                return Ventana(720, Encabezado("Pantalla", "¿Con qué colores se ve?"), Ui.Grilla(2, ops, 12),
                    Ui.T("Queda elegido en este equipo para todos los turnos. Lo impreso no cambia: la etiqueta es siempre blanca.", 17, Medium, Ui.Mut, ajustar: true),
                    Acciones(BotonVentana("Listo", Cerrar, true)));
            }
            case "cerrar":
            {
                var nota = Ui.T("Lo impreso y lo pesado queda guardado en el equipo. Al volver a abrir el programa, sigue donde quedó.", 19, Medium, Ui.Mut, ajustar: true);
                return Ventana(560, Encabezado("Terminal de rotulado", "¿Cerrar el programa?"), nota,
                    Acciones(BotonVentana("Cerrar programa", CerrarPrograma, true), BotonVentana("Volver", Cerrar)));
            }
            default:
                return VRegistro();
        }
    }

    private string AhoraDe(string campo)
    {
        var f = S.Bf; if (f == null) return "";
        return campo switch
        {
            "g" => f.G.ToString(), "n" => f.N.ToString(), "num" => f.Num.ToString(), "nr" => f.Nr.ToString(), "carro" => f.Carro,
            "tara" => f.Tara != null ? D.Kg(f.Tara.Value) + " kg" : "", "bruto" => f.Bruto != null ? D.Kg(f.Bruto.Value) + " kg" : "", "cant" => f.Cant.ToString(), "bote" => f.Bote, _ => "",
        };
    }

    private UIElement VRegistro()
    {
        var tabla = new Grid();
        foreach (var w in new[] { 84.0, 136, double.NaN, 150, 110, 230 })
            tabla.ColumnDefinitions.Add(new ColumnDefinition { Width = double.IsNaN(w) ? new GridLength(1, GridUnitType.Star) : new GridLength(w) });
        void Celda(UIElement el, int fila, int col, bool linea = true)
        {
            var b = new Border { Child = el, Padding = new Thickness(14, 7, 14, 7), BorderBrush = Ui.Linea, BorderThickness = new Thickness(0, 0, 0, linea ? 1 : 0), Background = fila == 0 ? Ui.Panel : Brushes.Transparent };
            Grid.SetRow(b, fila); Grid.SetColumn(b, col); tabla.Children.Add(b);
        }
        tabla.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var cab = new[] { "Hora", "Etiqueta", "Detalle", "Báscula", "Neto", "" };
        for (var c = 0; c < cab.Length; c++) { var t = Ui.Rotulo(cab[c], 13); if (c == 4) t.HorizontalAlignment = HorizontalAlignment.Right; Celda(t, 0, c); }
        var r = 1;
        foreach (var l in S.Labels)
        {
            tabla.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Celda(Ui.T(l.Hora, 17, Medium, Ui.Tinta), r, 0);
            Celda(Ui.T(l.Codigo, 23, Bold, Ui.CobreClaro, Ui.Cond), r, 1);
            var det = new TextBlock { TextWrapping = TextWrapping.Wrap, FontFamily = Ui.Barlow, FontSize = 17, FontWeight = Medium, Foreground = Ui.Tinta, VerticalAlignment = VerticalAlignment.Center };
            det.Inlines.Add(l.Detalle);
            var notas = new List<string>();
            if (l.Nota.Length > 0) notas.Add(l.Nota);
            if (l.Ctrl.Length > 0 && l.Ctrl != "Dentro de rango") notas.Add(l.Ctrl.Replace("Dentro de rango · ", "").ToLowerInvariant());
            if (notas.Count > 0) det.Inlines.Add(new System.Windows.Documents.Run(" · " + string.Join(" · ", notas)) { Foreground = Ui.AmbarL, FontSize = 14 });
            Celda(det, r, 2);
            var (fc, txt) = l.Estado switch { "pesada" => (Ui.ChipEstable, "PESADA"), "bascula" => (Ui.ChipInestable, "EN LA BÁSCULA"), "sintara" => (Ui.ChipBloqueado, "SIN TARA"), _ => (Ui.ChipEspera, "PENDIENTE") };
            var est = new Border { Background = fc, CornerRadius = new CornerRadius(999), Padding = new Thickness(10, 3, 10, 4), HorizontalAlignment = HorizontalAlignment.Left, Child = Ui.T(txt, 13, Bold, Ui.SobreChip, Ui.Cond) };
            Celda(l.Hp.Length > 0 ? Ui.Columna(est, Ui.T(l.Hp, 14, SemiBold, Ui.Mut, Ui.Mono).Margen(2, 4, 0, 0)) : est, r, 3);
            var neto = Ui.T(l.Estado == "pesada" ? $"{D.Kg(l.Neto)} kg" : "—", 17, Medium, Ui.Tinta, Ui.Mono); neto.HorizontalAlignment = HorizontalAlignment.Right;
            Celda(neto, r, 4);
            var bts = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var id = l.Id; var esta = l;
            if (l.Prod != "sub" && !l.Manual)
                bts.Children.Add(Ui.Boton(Ui.T("REIMPRIMIR", 16, SemiBold, Ui.Tinta, Ui.Cond), A(() => Reimprimir(esta)), Brushes.Transparent, Ui.Linea2, 10, new Thickness(14, 0, 14, 0), 50).Margen(0, 0, 8, 0));

            Celda(bts, r, 5);
            r++;
        }
        var lista = new Border { Background = Ui.Panel, CornerRadius = new CornerRadius(12), Child = new ScrollViewer { Content = tabla, MaxHeight = 470, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, PanningMode = PanningMode.VerticalOnly } };
        var nota = Ui.T("El Excel trae una hoja por planilla y un resumen como el del Sistema de Cátodos. Los pesos aparecen cuando la báscula los captura.", 15, Medium, Ui.Mut, ajustar: true);
        return Ventana(984, Encabezado($"Registro del {D.FECHA}", $"{S.Labels.Count} etiquetas"), lista, nota,
            Acciones(BotonVentana("Bajar Excel", BajarExcel, true, icono: Iconos.Excel), BotonVentana("Cerrar", Cerrar)));
    }

    /* ---------- Panel de prueba (F2): solo para la demostración ---------- */
    internal void RenderPanel()
    {
        PanelPrueba.Children.Clear();
        if (!_panelAbierto || S.View == "login") { PanelPrueba.Visibility = Visibility.Collapsed; return; }
        PanelPrueba.Visibility = Visibility.Visible;
        var fuera = new Border { Background = Brushes.Transparent };
        Ui.Clic(fuera, () => { _panelAbierto = false; RenderPanel(); });
        PanelPrueba.Children.Add(fuera);
        UIElement Esc1(string k, string titulo, string desc, bool lista)
        {
            var t = Ui.T(titulo + (lista ? " · lista" : ""), 19, Bold, lista ? Ui.CobreClaro : Ui.Tinta);
            var col = Ui.Columna(t, Ui.T(desc, 14, Medium, Ui.Mut, ajustar: true).Margen(0, 3, 0, 0));
            col.HorizontalAlignment = HorizontalAlignment.Left;
            return Ui.Boton(col, () => Escenario(k), lista ? Ui.Panel2 : Ui.Fondo, lista ? Ui.Cobre : Ui.Linea2, 10, new Thickness(14, 10, 14, 10), 64, null, false).Margen(0, 0, 0, 10);
        }
        var col = Ui.Columna(
            Ui.T("PANEL DE PRUEBA", 24, Bold, Ui.Tinta, Ui.Cond),
            Ui.T("Solo para la demostración. Se abre y se cierra con F2. La Báscula N9 está simulada: pesa cada etiqueta impresa unos segundos después y la marca como pesada en el mapa.", 14, Medium, Ui.Mut, ajustar: true).Margen(0, 4, 0, 16),
            Ui.Boton(Ui.T("REINICIAR EL REGISTRO", 18, SemiBold, Ui.RojoT, Ui.Cond), () => { _panelAbierto = false; Persistencia.Olvidar(); Arrancar(false); }, Brushes.Transparent, Ui.ErrBorde, 10, new Thickness(14, 0, 14, 0), 52).Margen(0, 6, 0, 10),
            Ui.Boton(Ui.T("CERRAR EL PANEL", 18, SemiBold, Ui.SobreCobre, Ui.Cond), () => { _panelAbierto = false; RenderPanel(); }, Ui.Cobre, Ui.Cobre, 10, new Thickness(14, 0, 14, 0), 52));
        var cajon = new Border { Width = 360, HorizontalAlignment = HorizontalAlignment.Right, Background = Ui.Panel, BorderBrush = Ui.Cobre, BorderThickness = new Thickness(2, 0, 0, 0), Padding = new Thickness(20, 20, 20, 20), Child = new ScrollViewer { Content = col, VerticalScrollBarVisibility = ScrollBarVisibility.Auto } };
        PanelPrueba.Children.Add(cajon);
    }
}
