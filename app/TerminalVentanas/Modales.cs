using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static System.Windows.FontWeights;

namespace TerminalVentanas;

/// <summary>Las ventanas sobre la pantalla (una pregunta y botones grandes) y el panel de prueba F2.</summary>
public partial class MainWindow
{
    private static UIElement Encabezado(string chico, string grande) =>
        Ui.Columna(Ui.Rotulo(chico, 15), Ui.T(grande, 30, Bold, Ui.Texto, ajustar: true).Margen(0, 2, 0, 0));

    private Border Ventana(double ancho, params UIElement[] partes)
    {
        var col = new StackPanel();
        for (var i = 0; i < partes.Length; i++) { if (partes[i] is FrameworkElement fe && i > 0) fe.Margin = new Thickness(0, 14, 0, 0); col.Children.Add(partes[i]); }
        return new Border
        {
            Width = ancho, MaxHeight = 736, Background = Ui.Tarjeta0, BorderBrush = Ui.Linea, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8),
            Padding = new Thickness(20, 18, 20, 18), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Child = col,
            Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 40, ShadowDepth = 12, Direction = 270, Opacity = 0.22 },
        };
    }

    private UIElement Opcion(string titulo, string sub, bool on, Action a, double alto = 160, double tam = 32)
    {
        var col = Ui.Columna(Ui.T(titulo, tam, Bold, Ui.Texto), Ui.T(sub, alto < 100 ? 15 : 18, SemiBold, on ? Ui.Azul : Ui.Texto2, ajustar: true).Margen(0, 6, 0, 0));
        col.HorizontalAlignment = HorizontalAlignment.Left; col.VerticalAlignment = VerticalAlignment.Center;
        return Ui.Boton(col, A(a), on ? Ui.AzulTinte : Ui.Tarjeta0, on ? Ui.Azul : Ui.LineaControl, 6, new Thickness(20, 12, 20, 12), alto, new Thickness(on ? 2 : 1), false);
    }

    private UIElement Acciones(params UIElement[] botones)
    {
        var def = string.Join(" ", botones.Select(_ => "*"));
        return Ui.Columnas(def, 12, botones);
    }

    private Border BotonVentana(string txt, Action a, bool principal = false, bool deshabilitado = false, string? icono = null, Brush? color = null)
    {
        if (principal) return BotonGrande(txt, icono, a, deshabilitado, 72, 24);
        UIElement c = icono == null ? Ui.T(txt, 20, SemiBold, color ?? Ui.Texto)
            : Ui.Fila(Ui.Ico(icono, 24, 24, color ?? Ui.Texto), Ui.T(txt, 20, SemiBold, color ?? Ui.Texto).Margen(10, 0, 0, 0));
        return Ui.Boton(c, A(a), color == null ? Ui.Tarjeta0 : Ui.AmbarTinte, color == null ? Ui.LineaControl : Ui.AmbarBorde, 6, new Thickness(18, 0, 18, 0), 72);
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
                return Ventana(720, Encabezado($"Grupo {G.G}, celdas {r}", "¿Se sacan las 21 celdas de este lado?"),
                    Ui.Columnas("* *", 12, Opcion("Sí, las 21", "Lado total, T", actual == "T", () => Poner("T")), Opcion("No, solo algunas", "Lado parcial, P", actual == "P", () => Poner("P"))),
                    Acciones(BotonVentana("Cancelar", Cerrar)));
            }
            case "grupo":
                return Ventana(560, Encabezado("Cátodos", "¿Qué grupo va a etiquetar?"), Dsp("Grupo", md.Val.Length > 0 ? md.Val : "—", 46), Teclado(Kp, 64, 28),
                    Acciones(BotonVentana("Aceptar", OkGrupo, true, md.Val.Length == 0), BotonVentana("Cancelar", Cerrar)),
                    Acciones(BotonVentana($"Terminar el grupo {G.G}", () => Abrir("terminar"), icono: Iconos.Warn, color: Ui.Ambar)));
            case "terminar":
            {
                var a = D.Orden.Take(21).Count(n => G.Celdas.ContainsKey(n)); var b = D.Orden.Skip(21).Count(n => G.Celdas.ContainsKey(n));
                UIElement Linea(string izq, string der) => new Border
                {
                    BorderBrush = Ui.Linea, BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(6, 10, 6, 10),
                    Child = Ui.Columnas("* auto", 10, Ui.T(izq, 18, Regular, Ui.Texto), Ui.T(der, 18, Bold, Ui.Azul)),
                };
                return Ventana(720, Encabezado($"Grupo {G.G}", "¿Terminar el grupo?"),
                    Ui.Columna(Linea($"Celdas 1 a 21, lado {LadoTxt(G.LadoA)}", $"{a} paquetes"), Linea($"Celdas 42 a 22, lado {LadoTxt(G.LadoB)}", $"{b} paquetes"), Linea("Muestra", G.MuestraH != null ? "Sí" : "No")),
                    Ui.T("La báscula sabrá que no vienen más paquetes de este grupo. Después se elige el grupo que sigue.", 16, Regular, Ui.Texto2, ajustar: true),
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
                return Ventana(560, Encabezado($"Báscula, {lbl.ToLowerInvariant()}{(ahora.Length > 0 ? $" (ahora {ahora})" : "")}", preg),
                    Dsp(lbl + (kg ? " (kg)" : ""), v.Length > 0 ? (kg && int.TryParse(v, out var n) ? D.Kg(n) : v) : "—", 46), Teclado(Kp, 64, 28),
                    Acciones(BotonVentana("Aceptar", OkBfkp, true, v.Length == 0), BotonVentana("Cancelar", Cerrar)));
            }
            case "bfop":
            {
                var f = S.Bf!; var k = md.Campo;
                (string V, string A, string B)[] ops; string tit; var tres = false; string actual;
                switch (k)
                {
                    case "lado": tit = "¿De qué lado es?"; ops = new[] { ("T", "Total, T", "Se sacaron las 21 celdas"), ("P", "Parcial, P", "Solo algunas celdas") }; actual = f.Lado; break;
                    case "mu": tit = "¿Es la muestra del grupo?"; ops = new[] { ("N", "No", "Paquete de una celda"), ("S", "Sí", "Se graba como cuba 00, sin lado") }; actual = f.Mu; break;
                    case "t": tit = "¿Qué tipo de lámina?"; ops = D.Lam.Select(t => (t.Id, t.Corto, t.Nombre)).ToArray(); tres = true; actual = f.T; break;
                    default: tit = "¿Qué es la carga?"; ops = D.Sub.Select(t => (t.Id, t.Corto, t.Nombre)).ToArray(); tres = true; actual = f.St; break;
                }
                var opciones = ops.Select(o => Opcion(o.A, o.B, actual == o.V, () => SetBfop(o.V), tres ? 82 : 160, tres ? 24 : 32)).ToList();
                return Ventana(720, Encabezado("Báscula", tit), Ui.Grilla(2, opciones, 12), Acciones(BotonVentana("Cancelar", Cerrar)));
            }
            case "cerrar":
            {
                var nota = Ui.T("Lo impreso y lo pesado queda guardado en el equipo. Al volver a abrir el programa, sigue donde quedó.", 18, Regular, Ui.Texto2, ajustar: true);
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
            var b = new Border { Child = el, Padding = new Thickness(14, 8, 14, 8), BorderBrush = Ui.Linea, BorderThickness = new Thickness(0, 0, 0, linea ? 1 : 0), Background = fila == 0 ? Ui.Fondo : Brushes.Transparent };
            Grid.SetRow(b, fila); Grid.SetColumn(b, col); tabla.Children.Add(b);
        }
        tabla.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var cab = new[] { "Hora", "Etiqueta", "Detalle", "Báscula", "Neto", "" };
        for (var c = 0; c < cab.Length; c++) { var t = Ui.Rotulo(cab[c], 14); if (c == 4) t.HorizontalAlignment = HorizontalAlignment.Right; Celda(t, 0, c); }
        var r = 1;
        foreach (var l in S.Labels)
        {
            tabla.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Celda(Ui.T(l.Hora, 17, Regular, Ui.Texto), r, 0);
            Celda(Ui.T(l.Codigo, 21, Bold, Ui.Texto), r, 1);
            var det = new TextBlock { TextWrapping = TextWrapping.Wrap, FontFamily = Ui.Fuente, FontSize = 16, Foreground = Ui.Texto, VerticalAlignment = VerticalAlignment.Center };
            det.Inlines.Add(l.Detalle);
            var notas = new List<string>();
            if (l.Nota.Length > 0) notas.Add(l.Nota);
            if (l.Ctrl.Length > 0 && l.Ctrl != "Dentro de rango") notas.Add(l.Ctrl.Replace("Dentro de rango · ", "").ToLowerInvariant());
            if (notas.Count > 0) det.Inlines.Add(new System.Windows.Documents.Run(". " + string.Join(". ", notas)) { Foreground = Ui.Ambar, FontSize = 14, FontWeight = SemiBold });
            Celda(det, r, 2);
            var (fc, txt) = l.Estado switch { "pesada" => ("#1E7B4F", "Pesada"), "bascula" => ("#8A5300", "En la báscula"), "sintara" => ("#B42318", "Sin tara"), _ => ("#015D93", "En camino") };
            var est = new Border { Background = Ui.B(fc), CornerRadius = new CornerRadius(4), Padding = new Thickness(10, 3, 10, 4), HorizontalAlignment = HorizontalAlignment.Left, Child = Ui.T(txt, 13, Bold, Ui.Blanco) };
            Celda(l.Hp.Length > 0 ? Ui.Columna(est, Ui.T(l.Hp, 14, Regular, Ui.Texto2).Margen(2, 4, 0, 0)) : est, r, 3);
            var neto = Ui.T(l.Estado == "pesada" ? $"{D.Kg(l.Neto)} kg" : "—", 17, SemiBold, Ui.Texto); neto.HorizontalAlignment = HorizontalAlignment.Right;
            Celda(neto, r, 4);
            var bts = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var id = l.Id; var esta = l;
            if (l.Prod != "sub" && !l.Manual)
                bts.Children.Add(Ui.Boton(Ui.T("Reimprimir", 16, SemiBold, Ui.Texto), A(() => Reimprimir(esta)), Ui.Tarjeta0, Ui.LineaControl, 6, new Thickness(14, 0, 14, 0), 50).Margen(0, 0, 8, 0));
            bts.Children.Add(Ui.Boton(Ui.T("Editar", 16, SemiBold, Ui.Azul), A(() => Editar(id)), Ui.Tarjeta0, Ui.Azul, 6, new Thickness(14, 0, 14, 0), 50));
            Celda(bts, r, 5);
            r++;
        }
        var lista = new Border { Background = Ui.Tarjeta0, BorderBrush = Ui.Linea, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), ClipToBounds = true, Child = new ScrollViewer { Content = tabla, MaxHeight = 470, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, PanningMode = PanningMode.VerticalOnly } };
        var nota = Ui.T("El Excel trae una hoja por planilla y un resumen como el del Sistema de Cátodos. Los pesos aparecen cuando la báscula los captura.", 15, Regular, Ui.Texto2, ajustar: true);
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
            var t = Ui.T(titulo + (lista ? ": lista" : ""), 18, Bold, lista ? Ui.Azul : Ui.Texto);
            var col = Ui.Columna(t, Ui.T(desc, 14, Regular, Ui.Texto2, ajustar: true).Margen(0, 3, 0, 0));
            col.HorizontalAlignment = HorizontalAlignment.Left;
            return Ui.Boton(col, () => Escenario(k), lista ? Ui.AzulTinte : Ui.Tarjeta0, lista ? Ui.Azul : Ui.LineaControl, 6, new Thickness(14, 10, 14, 10), 64, new Thickness(lista ? 2 : 1), false).Margen(0, 0, 0, 10);
        }
        var col = Ui.Columna(
            Ui.T("Panel de prueba", 22, Bold, Ui.Texto),
            Ui.T("Solo para la demostración: simula lo que pasa en la báscula. Se abre y se cierra con F2.", 14, Regular, Ui.Texto2, ajustar: true).Margen(0, 4, 0, 16),
            Esc1("grua", "Grúa encima", "La próxima carga marca 1.850 kg de más y queda bloqueada hasta que la grúa sale.", Esc.Grua),
            Esc1("fuera", "Carro fuera del área", "El próximo carro de restos marca 960 kg de menos y queda bloqueado hasta que se acomoda.", Esc.Fuera),
            Esc1("cero", "Báscula sin cero", "Vacía marca 6 kg (nódulos en la junta) y no registra hasta volver a cero.", Esc.Cero),
            Esc1("caja", "Caja sin etiqueta", "Llega una caja de despuntes: queda en la báscula hasta que se ingresa en la pantalla Báscula.", false),
            Ui.Boton(Ui.T("Reiniciar el registro", 17, SemiBold, Ui.Rojo), () => { _panelAbierto = false; Persistencia.Olvidar(); Arrancar(false); }, Ui.Tarjeta0, Ui.Rojo, 6, new Thickness(14, 0, 14, 0), 52).Margen(0, 6, 0, 10),
            Ui.Boton(Ui.T("Cerrar el panel", 17, SemiBold, Ui.Blanco), () => { _panelAbierto = false; RenderPanel(); }, Ui.Azul, Ui.Azul, 6, new Thickness(14, 0, 14, 0), 52));
        var cajon = new Border { Width = 360, HorizontalAlignment = HorizontalAlignment.Right, Background = Ui.Tarjeta0, BorderBrush = Ui.Azul, BorderThickness = new Thickness(4, 0, 0, 0), Padding = new Thickness(20, 20, 20, 20), Child = new ScrollViewer { Content = col, VerticalScrollBarVisibility = ScrollBarVisibility.Auto } };
        PanelPrueba.Children.Add(cajon);
    }
}
