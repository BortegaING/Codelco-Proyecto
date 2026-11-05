namespace TerminalVentanas;

/// <summary>Las reglas del terminal: qué etiqueta sigue, qué se imprime y qué pasa al tocar cada botón.</summary>
public partial class MainWindow
{
    /// <summary>Situaciones del panel de prueba (F2).</summary>
    internal static class Esc { public static bool Grua, Fuera, Cero; }
    private int _cajaN;

    private static Estado EstadoInicial()
    {
        var s = new Estado();
        var g21 = new Grupo { G = 21, LadoA = "T", LadoB = "P" };
        s.Grupos[21] = g21;
        // Celdas 1 a 3 ya pesadas y la 4 en camino: llega a la báscula apenas se entra
        int[] ms = { 9 * 60 + 41, 9 * 60 + 48, 9 * 60 + 55, 10 * 60 + 2 };
        for (var i = 0; i < 4; i++)
        {
            int n = i + 1, m = ms[i];
            var h = D.Hhmm(m);
            var l = EtqCat(21, n, "T", h, D.PesoCelda[n], "Operador prueba");
            l.Id = ++s.Uid; l.M = m;
            if (n < 4) { l.Estado = "pesada"; l.Hp = D.Hhmm(m + 3); l.Ctrl = "Dentro de rango"; l.Turno = "A"; g21.Celdas[n] = new Celda { E = "pesada", P = D.PesoCelda[n], H = h, Id = l.Id }; }
            else g21.Celdas[n] = new Celda { E = "camino", H = h, Id = l.Id };
            s.Labels.Insert(0, l);
        }
        return s;
    }

    private Grupo GrupoAct() => GrupoDe(S.Grupo);
    private Grupo GrupoDe(int g)
    {
        if (!S.Grupos.TryGetValue(g, out var gr)) { gr = new Grupo { G = g }; S.Grupos[g] = gr; }
        return gr;
    }
    private static string LadoDe(Grupo g, int n) => n <= 21 ? g.LadoA : g.LadoB;
    /// <summary>La celda que sigue: la primera del orden que todavía no tiene etiqueta.</summary>
    private static int? Siguiente(Grupo g) { foreach (var n in D.Orden) if (!g.Celdas.ContainsKey(n)) return n; return null; }
    private static string CodCat(Grupo g, int n) => $"{n}{LadoDe(g, n)}{g.G}";
    private D.TipoLam TipoLam() => D.LamDe(S.Lam.Tipo);
    private static string CodLam(D.TipoLam t, int n) => (t.Id == "co" ? "C.O · " : "") + $"Nº {n}";
    private string Hora() => D.Hhmm(S.Min);
    private string Tick() { S.Min += 4; return Hora(); }
    private static int PesoEjemplo(int n) => 4600 + (n * 53 % 330);
    private static string EditarTexto(string v, string val, int max) => v == "C" ? "" : v == "del" ? (val.Length > 0 ? val[..^1] : "") : val.Length < max ? val + v : val;
    private static string LadoTxt(string l) => D.LadoTxt.GetValueOrDefault(l, l);

    /* ---------- Etiquetas: lo mismo que hoy se escribe a mano, más el QR ---------- */
    private static Etiqueta EtqCat(int gr, int n, string lado, string h, int peso, string oper) => new()
    {
        Hora = h, Prod = "cat", Codigo = $"{n}{lado}{gr}", Detalle = $"Celda {n}, lado {LadoTxt(lado)}, grupo {gr}", Lado = lado, Neto = peso, Bruto = peso, Oper = oper, Tag = "CÁTODO",
        Qr = $"VEN|CAT|C{D.Pad2(n)}|L{lado}|G{gr}|M-N", Cajas = new() { new[] { "CELDA", n.ToString() }, new[] { "LADO", lado }, new[] { "GRUPO", gr.ToString() } },
        Pie = $"Cátodo comercial · lado {LadoTxt(lado)} · {D.FECHA} {h}", Ref = new Ref { G = gr, N = n },
    };
    private static Etiqueta EtqMuestra(int gr, string h, string oper) => new()
    {
        Hora = h, Prod = "cat", Codigo = $"MUESTRA G{gr}", Detalle = $"Muestra del grupo {gr} (cuba 00)", Neto = D.PesoMuestra, Bruto = D.PesoMuestra, Oper = oper, Tag = "MUESTRA",
        Qr = $"VEN|CAT|C00|G{gr}|M-S", Cajas = new() { new[] { "CUBA", "00" }, new[] { "MUESTRA", "S" }, new[] { "GRUPO", gr.ToString() } },
        Pie = $"Muestra del grupo · cuba 00 · {D.FECHA} {h}", Ref = new Ref { G = gr, M = true },
    };
    private static Etiqueta EtqLam(D.TipoLam t, int n, string h, int neto, string oper) => new()
    {
        Hora = h, Prod = "lam", Codigo = CodLam(t, n), Detalle = t.Nombre, TipoNombre = t.Nombre, Neto = neto, Tara = 12, Bruto = neto + 12, Oper = oper, Tag = "LÁMINAS",
        Qr = $"VEN|OTP|{t.Q}|{D.FQ}|N{n}|T12", Cajas = new() { new[] { "DÍA", D.DIA }, new[] { "MES", D.MES }, new[] { "Nº", n.ToString() } },
        Pie = $"{t.Nombre} · tara pallet 12 kg · {h}", Ref = new Ref { T = t.Id, N = n },
    };
    private static Etiqueta EtqRs(int gr, string carro, int n, string h, int neto, int? tara, string oper) => new()
    {
        Hora = h, Prod = "rs", Codigo = $"G{gr} · C{carro}", Detalle = $"Restos de ánodos · carro {n} del grupo {gr}", Neto = neto, Tara = tara ?? 0, Bruto = neto + (tara ?? 2150),
        SinTara = tara == null, Oper = oper, Tag = "RESTOS ÁNODOS", Qr = $"VEN|RAN|G{gr}|CARRO{carro}|N{n}",
        Cajas = new() { new[] { "GRUPO", gr.ToString() }, new[] { "CARRO", carro }, new[] { "Nº", n.ToString() } },
        Pie = tara != null ? $"Tara carro + rack {D.Kg(tara.Value)} kg · {D.FECHA} {h}" : $"Sin tara registrada · {D.FECHA} {h}", Ref = new Ref { G = gr, Carro = carro, N = n },
    };
    private static Etiqueta EtqVacia(string tag) => new() { Tag = tag, Codigo = "—", Qr = "VEN", Cajas = new() { new[] { "—", "—" }, new[] { "—", "—" }, new[] { "—", "—" } }, Pie = "Sin etiqueta pendiente" };
    private static string CodSub(D.TipoSub t, string bote) => t.Corto + (bote.Length > 0 ? $" · bote {bote}" : "");
    private static Etiqueta EtqSub(D.TipoSub t, int cant, string bote, string h, int peso, string oper) => new()
    {
        Hora = h, Prod = "sub", Codigo = CodSub(t, bote), Detalle = t.Nombre + (cant > 1 ? $" · {cant} {(t.Caja ? "cajas" : "paquetes")}" : ""), Neto = peso, Bruto = peso, Oper = oper,
        Tag = "SUBPRODUCTO", Ref = new Ref { St = t.Id, Cant = cant, Bote = bote },
    };

    /* ---------- Lo que muestra la pantalla según el producto ---------- */
    private Modelo ModeloActual() => S.Prod switch { "lam" => ModeloLam(), "rs" => ModeloRs(), _ => ModeloCat() };

    private Modelo ModeloCat()
    {
        var G = GrupoAct(); var sig = Siguiente(G); var mu = S.Muestra;
        if (!mu && S.Sel == null) S.Sel = sig;
        var n = S.Sel;
        Celda? c = n != null && G.Celdas.TryGetValue(n.Value, out var cc) ? cc : null;
        var m = new Modelo
        {
            Lbl = "Etiqueta que sigue", Codigo = mu ? "MUESTRA" : n != null ? CodCat(G, n.Value) : "—",
            Sticker = mu ? EtqMuestra(G.G, Hora(), "") : n != null ? EtqCat(G.G, n.Value, LadoDe(G, n.Value), Hora(), 0, "") : EtqVacia("CÁTODO"),
            Trabajo = () => TrabajoCat(G, sig), ImpTxt = $"Imprimir {(mu ? "muestra" : n != null ? CodCat(G, n.Value) : "")}",
        };
        if (mu)
        {
            m.Lbl = $"Muestra del grupo {G.G}";
            if (G.MuestraH != null) { m.Estado = new("info", $"Ya tiene etiqueta ({G.MuestraH}). Reimprima solo si se dañó."); m.ImpTxt = "Reimprimir muestra"; }
            else m.Estado = new("info", "Se graba como cuba 00, sin lado");
        }
        else if (n == null) { m.Lbl = "Cátodos"; m.Estado = new("ok", $"Grupo {G.G} completo. Toque Cambiar grupo."); m.ImpTxt = "Imprimir"; m.ImpDis = true; }
        else if (c != null) { m.Lbl = "Celda con etiqueta"; m.Estado = new("info", $"Ya tiene etiqueta ({c.H}). Reimprima solo si se dañó."); m.ImpTxt = $"Reimprimir {CodCat(G, n.Value)}"; }
        else if (sig != null && n != sig) { m.Lbl = "Etiqueta elegida"; m.Estado = new("warn", $"Ojo: la que sigue es {CodCat(G, sig.Value)}", CodCat(G, sig.Value)); }
        else m.Estado = S.Msg ?? new("ok", "Es la que sigue");
        return m;
    }

    private Modelo ModeloLam()
    {
        var T = TipoLam(); var n = S.Lam.Sig[T.Id];
        return new Modelo
        {
            Lbl = "Pallet que sigue", Codigo = CodLam(T, n), Corto = $"Nº {n}", Sticker = EtqLam(T, n, Hora(), 0, ""), Trabajo = () => TrabajoLam(T, n),
            ImpTxt = $"Imprimir {CodLam(T, n)}", Estado = S.Msg ?? new("ok", "Es el que sigue"),
        };
    }

    private Modelo ModeloRs()
    {
        var R = S.Rs; var hechos = R.Lista.Where(x => x.G == R.Grupo).ToList(); var n = hechos.Count + 1;
        int? tara = D.TaraCarro.TryGetValue(R.Carro, out var t) ? t : null;
        var dup = R.Carro.Length > 0 && hechos.Any(x => x.Carro == R.Carro);
        var m = new Modelo
        {
            Lbl = $"Carro {n} del grupo {R.Grupo}", Codigo = R.Carro.Length > 0 ? $"C{R.Carro}" : "—", Corto = R.Carro.Length > 0 ? $"C{R.Carro}" : "C_", Vacio = R.Carro.Length == 0,
            Sticker = EtqRs(R.Grupo, R.Carro.Length > 0 ? R.Carro : "—", n, Hora(), 0, tara, ""), Trabajo = () => TrabajoRs(R), ImpTxt = $"Imprimir carro {R.Carro}",
        };
        if (R.Carro.Length == 0) { m.Estado = S.Msg ?? new("info", "Escriba el número pintado en el carro"); m.ImpTxt = "Imprimir tarjeta"; m.ImpDis = true; }
        else if (dup) { m.Estado = new("err", $"El carro {R.Carro} ya tiene tarjeta en este grupo"); m.ImpTxt = "Imprimir tarjeta"; m.ImpDis = true; }
        else if (tara == null) m.Estado = new("warn", "Sin tara registrada: imprima y avise para pesarlo vacío");
        else m.Estado = new("ok", $"Tara de carro + rack: {D.Kg(tara.Value)} kg");
        return m;
    }

    /* ---------- Imprimir: la etiqueta baja de la Zebra, aparece el visto y el terminal pasa a la que sigue ---------- */
    private async void ImprimirEtiqueta(Etiqueta l, Modelo m, Func<Aviso> fin, string donde)
    {
        S.Md = null; S.Msg = null;
        MandarZebra(l);
        var snap = m.Copia();
        snap.Lbl = "Imprimiendo…"; snap.Codigo = l.Codigo; snap.Corto = CortoDe(l); snap.Vacio = false; snap.Sticker = l;
        snap.Estado = new("info", $"Imprimiendo {l.Codigo}…"); snap.ImpTxt = "Imprimiendo…"; snap.ImpDis = true;
        S.Snap = snap; S.Fase = "imprimiendo"; Render();
        var yo = _gen;
        await Task.Delay(650); if (yo != _gen) return;
        var despues = fin(); Persistencia.Guardar(S);
        var s2 = S.Snap!.Copia(); s2.Lbl = "Etiqueta impresa"; s2.Estado = new("ok", donde); s2.ImpTxt = "Impresa";
        S.Snap = s2; S.Fase = "impresa"; Render();
        await Task.Delay(900); if (yo != _gen) return;
        S.Fase = null; S.Snap = null; S.Msg = despues; Render();
    }

    private void MandarZebra(Etiqueta l)
    {
        try { Impresion.Enviar(_cfg.Impresora, l.Codigo, Zpl.De(l)); }
        catch (Exception ex) { Avisar($"La Zebra no imprimió: {ex.Message}", "err"); }
    }

    internal void Imprimir()
    {
        if (S.Fase != null || S.View != "main") return;
        var m = ModeloActual(); if (m.ImpDis) return;
        if (S.Prod == "cat")
        {
            var G = GrupoAct();
            if (S.Muestra)
            {
                if (G.MuestraH != null) { var lm = S.Labels.FirstOrDefault(x => x.Prod == "cat" && x.Ref.M && x.Ref.G == G.G); if (lm != null) Reimprimir(lm); return; }
                var l = EtqMuestra(G.G, Tick(), S.User); l.M = S.Min;
                ImprimirEtiqueta(l, m, () =>
                {
                    Agregar(l); G.MuestraH = l.Hora; S.Muestra = false; Encolar(l);
                    var s = Siguiente(G);
                    return new("ok", s != null ? $"Muestra impresa · ahora sigue {CodCat(G, s.Value)}" : "Muestra impresa");
                }, "Péguela en el paquete de muestra");
                return;
            }
            var n = S.Sel!.Value; var sig = Siguiente(G);
            if (G.Celdas.ContainsKey(n)) { var lc = S.Labels.FirstOrDefault(x => x.Prod == "cat" && !x.Ref.M && x.Ref.G == G.G && x.Ref.N == n); if (lc != null) Reimprimir(lc); return; }
            var l2 = EtqCat(G.G, n, LadoDe(G, n), Tick(), D.PesoCelda.TryGetValue(n, out var p) ? p : PesoEjemplo(n), S.User);
            l2.M = S.Min; if (sig != null && n != sig) l2.Nota = "fuera de orden";
            ImprimirEtiqueta(l2, m, () =>
            {
                Agregar(l2); G.Celdas[n] = new Celda { E = "camino", H = l2.Hora, Id = l2.Id }; S.Sel = Siguiente(G); Encolar(l2);
                var nx = S.Sel;
                return new("ok", nx != null ? $"{l2.Codigo} impresa · ahora sigue {CodCat(G, nx.Value)}" : $"{l2.Codigo} impresa · grupo {G.G} completo");
            }, "Péguela arriba del paquete, en la esquina");
        }
        else if (S.Prod == "lam")
        {
            var T = TipoLam(); var n = S.Lam.Sig[T.Id];
            var l = EtqLam(T, n, Tick(), T.Ej[S.Lam.Ej++ % T.Ej.Length], S.User); l.M = S.Min;
            ImprimirEtiqueta(l, m, () => { Agregar(l); S.Lam.Sig[T.Id]++; Encolar(l); return new("ok", $"{CodLam(T, n)} impresa · ahora sigue el {n + 1}"); }, "Péguela en el paquete del pallet");
        }
        else
        {
            var R = S.Rs; var n = R.Lista.Count(x => x.G == R.Grupo) + 1;
            int? tara = D.TaraCarro.TryGetValue(R.Carro, out var t) ? t : null;
            var l = EtqRs(R.Grupo, R.Carro, n, Tick(), D.RestosNetos[(n - 1) % D.RestosNetos.Length], tara, S.User); l.M = S.Min;
            ImprimirEtiqueta(l, m, () =>
            {
                Agregar(l); R.Lista.Add(new RsItem { G = R.Grupo, Carro = R.Carro, N = n, Id = l.Id }); var c = R.Carro; R.Carro = ""; Encolar(l);
                return tara != null ? new("ok", $"C{c} impresa · escriba el carro que sigue") : new("warn", $"C{c} impresa sin tara · escriba el que sigue");
            }, "Cuélguela junto al número pintado");
        }
    }

    private void Reimprimir(Etiqueta l) => ImprimirEtiqueta(l, ModeloActual(), () => new("info", $"{l.Codigo} impresa de nuevo"), "Reemplace la etiqueta dañada");
    private void Agregar(Etiqueta l) { l.Id = ++S.Uid; S.Labels.Insert(0, l); }

    /* ---------- Lo que hace cada botón ---------- */
    private void Rut(string v) { S.Rut = EditarTexto(v, S.Rut, 9); Render(); }
    internal void Entrar()
    {
        if (S.Rut.Length < 8) return;
        S.User = S.Rut == "111111111" ? "Operador prueba" : "Operador " + S.Rut[^5..^1];
        S.View = "main"; S.Msg = null; Render(); Proximo();
    }
    private void Salir() { S.Md = null; S.View = "login"; S.User = ""; S.Rut = "111111111"; Render(); }
    internal void Prod(string v) { if (S.Prod == v) return; S.Prod = v; S.Msg = null; S.Muestra = false; Render(); }
    private void CeldaElegida(int n) { S.Sel = n; S.Muestra = false; S.Msg = null; Render(); }
    private void Volver() { S.Sel = Siguiente(GrupoAct()); S.Msg = null; Render(); }
    private void AlternarMuestra() { S.Muestra = !S.Muestra; S.Msg = null; Render(); }
    private void Abrir(string tipo, string val = "", int max = 2, string campo = "") { S.Md = new Modal { Tipo = tipo, Val = val, Max = max, Campo = campo }; Render(); }
    internal void Cerrar() { S.Md = null; Render(); }
    private void Kp(string v) { if (S.Md == null) return; S.Md.Val = EditarTexto(v, S.Md.Val, S.Md.Max); Render(); }
    private void Carro(string v) { S.Rs.Carro = EditarTexto(v, S.Rs.Carro, 3); S.Msg = null; Render(); }
    private void LamTipo(string v) { S.Lam.Tipo = v; S.Msg = null; Render(); }
    private void OkKeypad()
    {
        switch (S.Md?.Tipo) { case "grupo": OkGrupo(); break; case "rsgrupo": OkRsGrupo(); break; case "bfkp": OkBfkp(); break; }
    }
    private void OkGrupo()
    {
        if (!int.TryParse(S.Md!.Val, out var gr) || gr <= 0) return;
        var nuevo = !S.Grupos.ContainsKey(gr); S.Grupo = gr; var G = GrupoAct(); S.Sel = Siguiente(G); S.Muestra = false;
        S.Msg = nuevo ? new("info", $"Grupo {gr} nuevo · si un lado es parcial, toque su lado") : null;
        Cerrar();
    }
    private void OkTerminar()
    {
        var gr = S.Grupo; S.Md = new Modal { Tipo = "grupo", Val = Math.Min(99, gr + 1).ToString() };
        Avisar($"Grupo {gr} terminado. Elija el grupo que sigue.", "ok"); Render();
    }
    private void OkRsGrupo()
    {
        if (!int.TryParse(S.Md!.Val, out var gr) || gr <= 0) return;
        S.Rs.Grupo = gr; S.Rs.Carro = ""; S.Msg = null; Cerrar();
    }
    private void BajarExcel()
    {
        try { var ruta = Excel.Guardar(S, _cfg.CarpetaExcel); Avisar($"Excel guardado en {ruta}", "ok"); }
        catch (Exception ex) { Avisar($"No se pudo guardar el Excel: {ex.Message}", "err"); }
    }

    /// <summary>Envuelve la acción de un botón: mientras la etiqueta sale de la Zebra no se toca nada.</summary>
    private Action A(Action a) => () => { if (S.Fase == null) a(); };
}
