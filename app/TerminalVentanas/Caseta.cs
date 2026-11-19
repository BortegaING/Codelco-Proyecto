using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Run = System.Windows.Documents.Run;
using static System.Windows.FontWeights;

namespace TerminalVentanas;

/// <summary>
/// Programa 2: la pantalla de la caseta de la Báscula N9 ("Programa": "caseta" en appsettings.json).
/// Lee lo que identifican las dos cámaras, captura el peso, maneja la baliza y lleva la grilla de lo registrado.
/// Recibe las cargas anunciadas desde el origen (las etiquetas que imprimió la VT1200) y deja identificar a mano lo que la cámara no leyó.
/// Usa el mismo motor de la báscula que la simulación; las cámaras y el visor están simulados.
/// </summary>
public partial class MainWindow
{
    internal bool Caseta => _cfg.Programa == "caseta";
    private string _manual = "";

    /// <summary>Lo anunciado desde el origen: 1T21 a 3T21 ya pesadas y 4T21 a 8T21 pendientes, que van llegando a la báscula.</summary>
    private static Estado EstadoCaseta()
    {
        var s = EstadoInicial();
        var g = s.Grupos[21];
        var m = 10 * 60 + 4;
        for (var n = 5; n <= 8; n++)
        {
            var h = D.Hhmm(m); m += 3;
            var l = EtqCat(21, n, "T", h, D.PesoCelda[n], "Operador origen");
            l.Id = ++s.Uid; l.M = m;
            g.Celdas[n] = new Celda { E = "camino", H = h, Id = l.Id };
            s.Labels.Insert(0, l);
        }
        return s;
    }

    /* ---------- La pantalla ---------- */
    private UIElement VCaseta()
    {
        var marca = Ui.Fila(Ui.Ico(Iconos.Bascula, 40, 40, Ui.CobreMedio, 2),
            Ui.Columna(Ui.T("BÁSCULA N9 · CASETA", 24, Bold, Ui.Tinta, Ui.Cond), Ui.Rotulo("Identificación por cámaras y captura del peso", 13)).Margen(12, 0, 0, 0));
        var quien = new TextBlock { FontFamily = Ui.Cond, FontSize = 15, FontWeight = SemiBold, Foreground = Ui.Mut, VerticalAlignment = VerticalAlignment.Center };
        quien.Inlines.Add(new Run(S.User.ToUpperInvariant()) { Foreground = Ui.Tinta }); quien.Inlines.Add(new Run($" · {Hora()}"));
        Border BotTurno(string x) => Ui.Boton(Ui.T($"TURNO {x}", 15, SemiBold, S.Turno == x ? Ui.SobreCobre : Ui.Mut, Ui.Cond), A(() => { S.Turno = x; Render(); }),
            S.Turno == x ? Ui.Cobre : Brushes.Transparent, Brushes.Transparent, 0, new Thickness(12, 0, 12, 0), 44, new Thickness(0));
        var turno = new Border { BorderBrush = Ui.Linea2, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), ClipToBounds = true, Child = Ui.Fila(BotTurno("A"), BotTurno("C")) };
        var who = Ui.Fila(quien, turno.Margen(10, 0, 0, 0), BotonPesaje().Margen(6, 0, 0, 0), BotonColores(46, false).Margen(6, 0, 0, 0), BotonSec(Ui.T("SALIR", 17, SemiBold, Ui.Tinta, Ui.Cond), Salir, 46, new Thickness(14, 0, 14, 0)).Margen(6, 0, 0, 0));
        who.HorizontalAlignment = HorizontalAlignment.Right;
        var bar = Ui.Columnas("* auto", 12, marca, who);

        var top = Ui.Columnas("380 150 *", 12, Balanza(), BalizaCard(), Camaras());
        var medio = Ui.Columnas("* 210", 12, Grilla(), Anunciadas());
        return Ui.Filas("56 auto * 84", 12, bar, top, medio, Manual());
    }

    /* ---------- Baliza (torre de luces) ---------- */
    /// <summary>Qué luz se enciende y qué le dice al operador de la horquilla o del carro.</summary>
    private (string Luz, string Txt, string Sub) EstadoBaliza()
    {
        if (B.Chip == "cero") return ("verde", "Disponible", "Puede ingresar");
        if (B.Chip == "espera") return ("amarillo", "Posicionar", "Deje la carga y retírese");
        if (B.Chip == "inestable") return B.Pic == "sale" ? ("verde", "Retirar", "Peso registrado") : ("rojo", "Capturando", "No se mueva");
        if (B.Chip == "estable") return B.Esperando ? ("amarillo", "Espere", "Identificación manual")
            : B.Res.K == "warn" ? ("amarillo", "Alerta", "Fuera de rango: retire la carga") : ("verde", "Retirar", "Peso registrado");
        if (B.Chip == "sintara") return ("verde", "Retirar", "Registrado con observación");
        if (B.Chip == "sincero") return ("amarillo", "Revisar", "Residuos en la plataforma");
        return ("amarillo", "Corregir", B.Pic == "grua" ? "Retire la horquilla" : "Acomode el carro");
    }

    private Border BalizaCard()
    {
        var (luz, txt, sub) = EstadoBaliza();
        var colores = new (string K, Color C)[] { ("rojo", Color.FromRgb(0xE5, 0x48, 0x3B)), ("amarillo", Color.FromRgb(0xF2, 0xB3, 0x3D)), ("verde", Color.FromRgb(0x35, 0xB6, 0x6B)) };
        var torre = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        torre.Children.Add(new Border { Width = 36, Height = 8, CornerRadius = new CornerRadius(4, 4, 0, 0), Background = Ui.Linea2, HorizontalAlignment = HorizontalAlignment.Center });
        foreach (var (k, c) in colores)
        {
            var on = k == luz;
            var seg = new Border
            {
                Width = 44, Height = 30, Margin = new Thickness(0, 0, 0, 2), CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(on ? c : Color.FromArgb(55, c.R, c.G, c.B)), BorderBrush = new SolidColorBrush(Color.FromArgb(on ? (byte)255 : (byte)110, c.R, c.G, c.B)), BorderThickness = new Thickness(1.5),
            };
            if (on) seg.Effect = new DropShadowEffect { Color = c, BlurRadius = 22, ShadowDepth = 0, Opacity = 0.95 };
            torre.Children.Add(seg);
        }
        torre.Children.Add(new Border { Width = 12, Height = 18, Background = Ui.Linea2, HorizontalAlignment = HorizontalAlignment.Center });
        torre.Children.Add(new Border { Width = 40, Height = 6, CornerRadius = new CornerRadius(2), Background = Ui.Linea2, HorizontalAlignment = HorizontalAlignment.Center });
        var t = Ui.T(txt.ToUpperInvariant(), 18, Bold, luz == "rojo" ? Ui.RojoT : luz == "amarillo" ? Ui.AmbarT : Ui.VerdeT, Ui.Cond); t.HorizontalAlignment = HorizontalAlignment.Center;
        var s = Ui.T(sub, 13, Medium, Ui.Mut, ajustar: true); s.TextAlignment = TextAlignment.Center; s.HorizontalAlignment = HorizontalAlignment.Center;
        return Ui.Tarjeta(Ui.Filas("auto auto auto auto", 6, Ui.Rotulo("Baliza", 15), torre.Margen(0, 4, 0, 0), t, s), relleno: new Thickness(12, 12, 12, 12));
    }

    /* ---------- Las dos cámaras ---------- */
    private (string Txt, string K) CamSuperior()
    {
        var l = B.Carga;
        if (l == null) return B.Chip == "sincero" ? ("Plataforma vacía, con lectura distinta de cero", "warn") : ("Plataforma vacía", "muted");
        if (B.Pic == "sale") return ("Carga saliendo", "muted");
        if (l.SinEtq) return ("Cobre detectado · etiqueta no legible", "warn");
        if (l.Prod == "rs") return ($"Carro detectado · C{l.Ref.Carro}", "ok");
        if (B.Chip == "espera") return ($"Cobre detectado · leyendo {l.Codigo}", "info");
        return ($"Cátodo · etiqueta {l.Codigo}", "ok");
    }

    private (string Txt, string K) CamLateral()
    {
        var l = B.Carga;
        if (l == null) return ("Sin carga en la plataforma", "muted");
        if (B.Pic == "grua") return ("Horquilla sobre la plataforma", "err");
        if (B.Pic == "fuera") return ("Carro fuera del área", "err");
        if (B.Pic == "sale") return ("Carga saliendo", "muted");
        if (l.Prod == "rs") return ($"Carro dentro del área · tara {(l.SinTara ? "sin marca validada" : D.Kg(l.Tara) + " kg")}", l.SinTara ? "warn" : "ok");
        return B.Chip == "espera" ? ("Horquilla depositando la carga", "info") : ("1 paquete · horquilla fuera", "ok");
    }

    private Border Camaras()
    {
        UIElement Cam(string nombre, (string Txt, string K) v)
        {
            var ico = Ui.Ico(Iconos.Camara, 30, 30, Ui.CobreMedio, 2); ico.VerticalAlignment = VerticalAlignment.Top;
            var col = Ui.Columna(Ui.Rotulo(nombre, 14), Ui.T(v.Txt, 17, SemiBold, Ui.ColorEstado(v.K), ajustar: true).Margen(0, 3, 0, 0));
            return new Border { Background = Ui.Fondo, BorderBrush = Ui.Linea, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(12, 10, 12, 10), Child = Ui.Columnas("30 *", 10, ico, col) };
        }
        return Ui.Tarjeta(Ui.Filas("auto auto auto", 8, Ui.Rotulo("Cámaras", 15), Cam("Cámara superior · producto y etiqueta", CamSuperior()), Cam("Cámara lateral · posición y paquetes", CamLateral())),
            relleno: new Thickness(14, 12, 14, 12));
    }

    /* ---------- Grilla de lo identificado y registrado ---------- */
    private UIElement Grilla()
    {
        var filas = S.Labels.Where(l => l.Estado is "pesada" or "sintara" or "bascula").OrderByDescending(l => l.Estado == "bascula").ThenByDescending(l => l.Hp).ThenByDescending(l => l.Id).ToList();
        var tabla = new Grid();
        foreach (var w in new[] { 64.0, 150, 80, 84, 74, 84, double.NaN })
            tabla.ColumnDefinitions.Add(new ColumnDefinition { Width = double.IsNaN(w) ? new GridLength(1, GridUnitType.Star) : new GridLength(w) });
        void Celda(UIElement el, int fila, int col)
        {
            var b = new Border { Child = el, Padding = new Thickness(8, 6, 8, 6), BorderBrush = Ui.Linea, BorderThickness = new Thickness(0, 0, 0, 1), Background = fila == 0 ? Ui.Panel2 : Brushes.Transparent };
            Grid.SetRow(b, fila); Grid.SetColumn(b, col); tabla.Children.Add(b);
        }
        TextBlock Der(TextBlock t) { t.HorizontalAlignment = HorizontalAlignment.Right; return t; }
        tabla.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var cab = new[] { "Hora", "Identificación", "Lectura", "Bruto", "Tara", "Neto", "Validación" };
        for (var c = 0; c < cab.Length; c++) { var t = Ui.Rotulo(cab[c], 13); if (c is >= 3 and <= 5) Der(t); Celda(t, 0, c); }
        var r = 1;
        if (B.Esperando && B.Carga != null)
        {
            tabla.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Celda(Ui.T("—", 15, Medium, Ui.Mut), r, 0); Celda(Ui.T("Sin identificar", 16, Bold, Ui.AmbarT, Ui.Cond), r, 1); Celda(Ui.T("Pendiente", 14, SemiBold, Ui.AmbarT), r, 2);
            Celda(Der(Ui.T(D.Kg((int)Math.Round(B.Lect)), 15, SemiBold, Ui.Tinta, Ui.Mono)), r, 3); Celda(Der(Ui.T("—", 15, Medium, Ui.Mut)), r, 4); Celda(Der(Ui.T("—", 15, Medium, Ui.Mut)), r, 5);
            Celda(Ui.T("Escriba el rótulo abajo y toque Asociar", 13, SemiBold, Ui.AmbarT, ajustar: true), r, 6);
            r++;
        }
        foreach (var l in filas)
        {
            tabla.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var enBas = l.Estado == "bascula";
            var id = l.Prod == "rs" ? $"Carro {CortoDe(l)}" : l.Ref.M ? $"Muestra G{l.Ref.G}" : $"Cátodo {l.Codigo}";
            Celda(Ui.T(enBas ? "ahora" : l.Hp, 15, Medium, Ui.Tinta, Ui.Mono), r, 0);
            Celda(Ui.T(id, 17, Bold, enBas ? Ui.AmbarT : Ui.Tinta, Ui.Cond), r, 1);
            Celda(Ui.T(l.Manual ? "Manual" : "Cámara", 14, SemiBold, l.Manual ? Ui.AmbarT : Ui.Mut), r, 2);
            Celda(Der(Ui.T(enBas ? "—" : D.Kg(l.Bruto), 15, SemiBold, Ui.Tinta, Ui.Mono)), r, 3);
            Celda(Der(Ui.T(l.Prod == "rs" && !l.SinTara ? D.Kg(l.Tara) : "—", 15, Medium, Ui.Mut, Ui.Mono)), r, 4);
            Celda(Der(Ui.T(enBas || l.Estado == "sintara" ? "—" : D.Kg(l.Neto), 15, Bold, Ui.Tinta, Ui.Mono)), r, 5);
            var (vk, vt) = enBas ? ("info", "Registrando…") : l.Estado == "sintara" ? ("warn", "Sin tara validada") : l.Ctrl.StartsWith("Dentro") ? ("ok", l.Ctrl.Split(" · ")[0]) : ("warn", l.Ctrl.Split(" · ")[0]);
            Celda(Ui.T(vt, 13, SemiBold, Ui.ColorEstado(vk), ajustar: true), r, 6);
            r++;
        }
        var cab2 = Ui.Columnas("auto * auto", 10, Ui.T("REGISTRO DE LA BÁSCULA", 18, Bold, Ui.Tinta, Ui.Cond), Ui.T("lo que identifican las cámaras, con su peso", 14, Medium, Ui.Mut), Insignia(filas.Count));
        var lista = new ScrollViewer { Content = tabla, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, PanningMode = PanningMode.VerticalOnly };
        var g = Ui.Filas("auto *", 8, cab2, lista);
        return Ui.Tarjeta(g, relleno: new Thickness(14, 10, 14, 10));
    }

    /// <summary>Las etiquetas impresas en el origen que todavía no pasan por la báscula.</summary>
    private UIElement Anunciadas()
    {
        var pend = S.Labels.Where(l => l.Prod == "cat" && l.Estado == "camino").OrderBy(l => l.Id).ToList();
        var col = new StackPanel();
        col.Children.Add(Ui.T("ANUNCIADAS", 18, Bold, Ui.Tinta, Ui.Cond));
        col.Children.Add(Ui.T($"desde el origen · {pend.Count} pendientes", 13, Medium, Ui.Mut).Margen(0, 0, 0, 8));
        foreach (var l in pend.Take(7))
        {
            col.Children.Add(new Border
            {
                Background = Ui.AzulF, BorderBrush = Ui.AzulL, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 4, 10, 5), Margin = new Thickness(0, 0, 0, 6),
                Child = Ui.Columnas("* auto", 6, Ui.T(l.Codigo, 18, Bold, Ui.AzulC, Ui.Cond), Ui.T(l.Hora, 13, SemiBold, Ui.AzulC, Ui.Mono)),
            });
        }
        if (pend.Count == 0) col.Children.Add(Ui.T("No hay cargas pendientes.", 14, Medium, Ui.Mut, ajustar: true));
        else if (pend.Count > 7) col.Children.Add(Ui.T($"y {pend.Count - 7} más", 13, Medium, Ui.Mut));
        return Ui.Tarjeta(col, relleno: new Thickness(14, 10, 14, 10));
    }

    /* ---------- Identificación manual: el espacio para escribir ---------- */
    private UIElement Manual()
    {
        var activa = B.Esperando && B.Liberar != null;
        var tb = new TextBox
        {
            Text = _manual, FontFamily = Ui.Mono, FontSize = 30, FontWeight = Bold, Foreground = Ui.Tinta, Background = Ui.Fondo, CaretBrush = Ui.Tinta,
            BorderBrush = activa ? Ui.AmbarL : Ui.Linea2, BorderThickness = new Thickness(2), Padding = new Thickness(12, 0, 12, 0), MaxLength = 8,
            VerticalContentAlignment = VerticalAlignment.Center, CharacterCasing = CharacterCasing.Upper, IsEnabled = activa,
        };
        tb.TextChanged += (_, _) => _manual = tb.Text;
        if (activa) Dispatcher.BeginInvoke(() => { tb.Focus(); tb.CaretIndex = tb.Text.Length; }, DispatcherPriority.Loaded);
        var rot = Ui.Columna(Ui.T("IDENTIFICACIÓN MANUAL", 17, Bold, activa ? Ui.AmbarT : Ui.Tinta, Ui.Cond),
            Ui.T(activa ? "La cámara no leyó la etiqueta: escriba el rótulo (5T21) o el carro (C22)." : "Se activa si la cámara no identifica la carga.", 13, Medium, Ui.Mut, ajustar: true));
        rot.VerticalAlignment = VerticalAlignment.Center;
        var asociar = BotonGrande("Asociar", Iconos.Ok, AsociarManual, !activa, 84, 26);
        var excel = BotonSec(Ui.Fila(Ui.Ico(Iconos.Excel, 26, 26, Ui.VerdeXls), Ui.T("EXCEL", 21, SemiBold, Ui.Tinta, Ui.Cond).Margen(10, 0, 0, 0)), BajarExcel);
        var (ck, ci, ct) = S.Cero == null ? ("warn", Iconos.Warn, "CONTROL DE CERO") : S.Cero.Ok ? ("ok", Iconos.Ok, $"CERO OK {S.Cero.H}") : ("err", Iconos.Err, $"CERO: FALLA {S.Cero.H}");
        var cero = BotonSec(Ui.Fila(Ui.Ico(ci, 22, 22, Ui.ColorEstado(ck), 2.4), Ui.T(ct, 17, SemiBold, Ui.Tinta, Ui.Cond).Margen(8, 0, 0, 0)), () => { ControlCero(); Render(); }, 84, new Thickness(16, 0, 16, 0));
        return Ui.Columnas("230 * 190 auto auto", 12, rot, tb, asociar, cero, excel);
    }

    /// <summary>El pesador escribe lo que la cámara no leyó: si es una etiqueta anunciada, se usa esa; si no, se crea el registro con lo escrito.</summary>
    internal void AsociarManual()
    {
        if (!B.Esperando || B.Liberar == null) { Avisar("No hay una carga esperando identificación manual.", "info"); return; }
        var t = _manual.Trim().ToUpperInvariant().Replace(" ", "");
        var W = (int)Math.Round(B.Lect);
        var l = S.Labels.FirstOrDefault(x => x.Codigo == t && x.Estado == "camino");
        if (l == null)
        {
            var m = Regex.Match(t, @"^(\d{1,2})([TP])(\d{1,2})$");
            var c = Regex.Match(t, @"^C(\d{1,3})$");
            if (m.Success && int.Parse(m.Groups[1].Value) is >= 1 and <= 42)
            {
                int n = int.Parse(m.Groups[1].Value), g = int.Parse(m.Groups[3].Value);
                l = EtqCat(g, n, m.Groups[2].Value, Hora(), W, S.User); Agregar(l);
                var G = GrupoDe(g); if (!G.Celdas.ContainsKey(n)) G.Celdas[n] = new Celda { E = "camino", H = l.Hora, Id = l.Id };
            }
            else if (c.Success)
            {
                var carro = c.Groups[1].Value; int? tara = D.TaraCarro.TryGetValue(carro, out var tt) ? tt : null;
                l = EtqRs(S.Rs.Grupo, carro, S.Rs.Lista.Count + 1, Hora(), 0, tara, S.User); Agregar(l);
            }
        }
        if (l == null) { Avisar("Escriba el rótulo de la carga: celda, lado y grupo (por ejemplo 5T21) o el carro (C22).", "warn"); return; }
        l.Bruto = W; l.Manual = true; l.Nota = "identificada a mano en la caseta";
        Registrar(l, Evaluar(l, W), "identificación manual", true);
        _manual = "";
        B.Liberar.TrySetResult(l);
        Avisar($"{l.Codigo} identificada a mano y registrada.", "ok");
    }

    /* ---------- Peso de prueba (panel de pesaje, F3): uno mismo pone el peso para ver el control de rango ---------- */
    private string _pesoPrueba = "4750";

    /// <summary>La próxima celda anunciada llega a la báscula con este bruto en vez del peso real.</summary>
    internal void PesarPrueba(int kg)
    {
        if (kg <= 0 || kg > 20000) { Avisar("Escriba un peso entre 1 y 20.000 kg.", "warn"); return; }
        var gr = 21; var G = GrupoDe(gr); var n = Siguiente(G);
        while (n == null && gr < 48) { gr++; G = GrupoDe(gr); n = Siguiente(G); }
        if (n == null) { Avisar("No quedan celdas por anunciar.", "info"); return; }
        var l = EtqCat(gr, n.Value, LadoDe(G, n.Value), Tick(), kg, "Operador origen");
        l.M = S.Min; l.Nota = "peso de prueba"; Agregar(l); G.Celdas[n.Value] = new Celda { E = "camino", H = l.Hora, Id = l.Id }; Encolar(l, 1500);
        Avisar($"{l.Codigo} llega a la báscula con {D.Kg(kg)} kg (peso de prueba).", "info");
    }


    /// <summary>Las situaciones que se muestran en la demostración (panel de pesaje, F3).</summary>
    private void EscenarioCaseta(string k)
    {
        switch (k)
        {
            case "anuncio":
            {
                var G = GrupoDe(21); var n = Siguiente(G);
                if (n == null) { Avisar("El grupo 21 ya no tiene celdas por anunciar.", "info"); break; }
                var l = EtqCat(21, n.Value, LadoDe(G, n.Value), Tick(), D.PesoCelda.TryGetValue(n.Value, out var p) ? p : PesoEjemplo(n.Value), "Operador origen");
                l.M = S.Min; Agregar(l); G.Celdas[n.Value] = new Celda { E = "camino", H = l.Hora, Id = l.Id }; Encolar(l, 1500);
                Avisar($"El origen imprimió {l.Codigo}: queda anunciada y llega a la báscula.", "info"); break;
            }
            case "sinetq":
                Encolar(new Etiqueta { SinEtq = true, Prod = "cat", Codigo = "Sin etiqueta", Bruto = D.PesoCelda[9], Estado = "camino" }, 900);
                Avisar("Llega un paquete de cátodos con la etiqueta ilegible.", "info"); break;
            case "carro":
            case "fuera":
            {
                if (k == "fuera") Esc.Fuera = true;
                var carros = new[] { "22", "7", "31", "15" }; var i = S.Rs.Lista.Count % carros.Length; var carro = carros[i];
                int? tara = D.TaraCarro.TryGetValue(carro, out var tt) ? tt : null;
                var l = EtqRs(S.Rs.Grupo, carro, S.Rs.Lista.Count + 1, Hora(), D.RestosNetos[i % D.RestosNetos.Length], tara, "Báscula");
                Agregar(l); S.Rs.Lista.Add(new RsItem { G = S.Rs.Grupo, Carro = carro, N = l.Ref.N, Id = l.Id }); Encolar(l, 900);
                Avisar($"Llega el carro C{carro} con restos de ánodos.", "info"); break;
            }
            default: Escenario(k); _pesaje?.Dibujar(); return;
        }
        _pesaje?.Dibujar();
    }
}
