using System.Windows;
using System.Windows.Controls;

namespace TerminalVentanas;

/// <summary>Lo que marca la Báscula N9 en este momento (simulada) y qué hay encima.</summary>
public sealed class Bascula
{
    public double Lect, Meta, Ruido;
    public Etiqueta? Carga;
    public string ProdPic = "cat", Pic = "vacia", Chip = "cero";
    public Resultado Res = new("muted", "Báscula libre", "Esperando la próxima carga");
    public bool Esperando;
    public TaskCompletionSource<Etiqueta>? Liberar;
}

public sealed record Resultado(string K, string T1, string T2, string Estado = "pesada", string Ctrl = "", string Corto = "");

public partial class MainWindow
{
    private static readonly Dictionary<string, (string Clase, string Txt)> Chips = new()
    {
        ["cero"] = ("cero", "En cero"), ["espera"] = ("espera", "Llegando"), ["inestable"] = ("inestable", "Estabilizando"),
        ["estable"] = ("estable", "Estable"), ["bloqueado"] = ("bloqueado", "Bloqueado"), ["sincero"] = ("bloqueado", "Sin cero"),
    };

    // Lo que se actualiza en vivo sin volver a dibujar la pantalla
    private TextBlock? _balNum;
    private Border? _balChip;
    private ContentControl? _balRes;

    private static string CortoDe(Etiqueta l) => l.Prod == "rs" ? $"C{l.Ref.Carro}" : l.Prod == "lam" ? $"Nº {l.Ref.N}" : l.Prod == "cat" && l.Ref.M ? "MUESTRA" : l.Codigo;
    private static (int Min, int Max)? RangoDe(Etiqueta l) => l.Prod switch
    {
        "cat" => l.Ref.M ? null : D.Rango["cat"],
        "lam" => D.Rango.TryGetValue(l.Ref.T, out var r) ? r : null,
        "rs" => D.Rango["rs"],
        _ => null,
    };

    /// <summary>La lectura se acerca al peso real con el vaivén de la carga, como en el terminal de pesaje.</summary>
    private void Leer()
    {
        if (B.Ruido > 0 || Math.Abs(B.Meta - B.Lect) > 0.5) B.Lect += (B.Meta - B.Lect) * 0.42 + (_azar.NextDouble() * 2 - 1) * B.Ruido;
        else B.Lect = B.Meta;
        PintarNum();
    }

    private void PintarNum()
    {
        if (_balNum == null) return;
        var t = D.Kg((int)Math.Max(0, Math.Round(B.Lect)));
        if (_balNum.Text != t) { _balNum.Text = t; _balNum.FontSize = t.Length > 5 ? 48 : 56; }
    }

    private void Fijar(string chip, string k, string t1, string t2) { B.Chip = chip; B.Res = new Resultado(k, t1, t2); PintarBal(); }
    private void Fijar(string chip, Resultado r) { B.Chip = chip; B.Res = r; PintarBal(); }

    private void PintarBal()
    {
        if (_balChip != null)
        {
            var ch = Chips[B.Chip]; var nuevo = Ui.Chip(ch.Txt, ch.Clase);
            var texto = nuevo.Child; nuevo.Child = null;
            _balChip.Background = nuevo.Background; _balChip.Child = texto;
        }
        if (_balRes != null) _balRes.Content = VRes();
        PintarNum();
    }

    /// <summary>El control: con el peso estable, calcula el neto y lo compara con lo esperado para ese producto.</summary>
    private static Resultado Evaluar(Etiqueta l, int W)
    {
        var c = CortoDe(l); var R = RangoDe(l);
        if (l.Prod == "rs" && l.SinTara) return new("warn", $"{c} · bruto {D.Kg(W)} kg", "Sin tara: falta pesar el carro vacío", "sintara", "Sin tara de carro + rack", "sin tara");
        var tl = l.Prod == "lam" ? l.Tara : l.Tara;
        var conTara = l.Prod is "lam" or "rs";
        var neto = conTara ? W - tl : W;
        var det = conTara ? $"Bruto {D.Kg(W)} − tara {D.Kg(tl)}" : "";
        var t1 = $"{c} · {(det.Length > 0 ? "neto " : "")}{D.Kg(neto)} kg";
        if (R == null) return new("ok", t1, det.Length > 0 ? $"{det} · rango por definir" : "Peso capturado · rango por definir", "pesada", "Rango por definir", "rango por definir");
        if (neto < R.Value.Min || neto > R.Value.Max)
        {
            var bajo = neto < R.Value.Min; var ult = l.Prod == "rs" && bajo;
            return new("warn", t1, ult ? "Bajo lo esperado: ¿último carro del grupo?" : $"{(bajo ? "Bajo" : "Sobre")} lo esperado ({D.Kg(R.Value.Min)} a {D.Kg(R.Value.Max)})",
                "pesada", ult ? "Bajo lo esperado: último carro del grupo" : $"{(bajo ? "Bajo" : "Sobre")} lo esperado", "fuera de rango");
        }
        return new("ok", t1, det.Length > 0 ? $"{det} · dentro de rango" : $"Dentro de rango ({D.Kg(R.Value.Min)} a {D.Kg(R.Value.Max)})", "pesada", "Dentro de rango", "dentro de rango");
    }

    /// <summary>Deja el peso en el registro: neto, hora real del pesaje, turno y lo que dijo el control.</summary>
    private void Registrar(Etiqueta l, Resultado r, string extra, bool ahora = false)
    {
        var m = Math.Max(ahora ? S.Min : (l.M > 0 ? l.M : S.Min) + 3, S.UltPes + 1);
        S.UltPes = m; if (m > S.Min) S.Min = m;
        if (r.Estado == "pesada") l.Neto = l.Prod is "lam" or "rs" ? l.Bruto - l.Tara : l.Bruto;
        l.Hp = D.Hhmm(m); l.Turno = S.Turno;
        l.Ctrl = r.Ctrl + (extra.Length > 0 ? " · " + extra : "") + (l.Editado ? " · datos corregidos en la báscula" : "");
        Marcar(l, r.Estado); Refrescar();
    }

    private void Marcar(Etiqueta l, string e)
    {
        l.Estado = e;
        if (l.Prod == "cat" && !l.Ref.M && S.Grupos.TryGetValue(l.Ref.G, out var G) && G.Celdas.TryGetValue(l.Ref.N, out var c))
        {
            c.E = e; if (e == "pesada") c.P = l.Neto;
        }
    }

    private void Refrescar()
    {
        if (S.Fase != null) { Persistencia.Guardar(S); return; }
        // Si la pantalla Báscula muestra un registro sin cambios a medio hacer, lo trae al día
        if (S.View == "bas" && S.Bf is { Dirty: false, Id: not null })
        {
            var l = RegDe(S.Bf.Id); if (l != null) { var msg = S.Bf.Msg; S.Bf = FormDe(l, S.Bf.Modo); S.Bf.Msg = msg; }
        }
        if (S.View != "login") Render(); else Persistencia.Guardar(S);
    }

    /// <summary>Una carga sobre la báscula: llega, se estabiliza, se controla, se registra y sale.</summary>
    private async Task Pesar(Etiqueta l, int yo)
    {
        bool Vivo() => yo == _gen;
        var sinEtq = l.SinEtq; var c = sinEtq ? "Sin etiqueta" : CortoDe(l); var W = l.Bruto;
        var grua = !sinEtq && Esc.Grua; var fuera = !sinEtq && Esc.Fuera && l.Prod == "rs";
        if (grua) Esc.Grua = false; if (fuera) Esc.Fuera = false; if (grua || fuera) RenderPanel();
        var desvio = grua ? 1850 : fuera ? -960 : 0;
        B.Carga = l; B.ProdPic = l.Prod; B.Pic = "vacia"; B.Chip = "espera";
        B.Res = sinEtq ? new("warn", "Llega una carga sin etiqueta", "La cámara no encuentra código") : new("info", $"Llega {c}", "La cámara lee la etiqueta");
        Marcar(l, "bascula"); BfSeguir(l); Refrescar();
        await Task.Delay(60); if (!Vivo()) return;
        B.Pic = grua ? "grua" : fuera ? "fuera" : "sobre"; PintarBal();
        await Task.Delay(700); if (!Vivo()) return;
        B.Meta = W * 1.035 + desvio; B.Ruido = 40; Fijar("inestable", "warn", $"{c} · estabilizando", "No toque la carga");
        await Task.Delay(800); if (!Vivo()) return;
        B.Meta = W + desvio; B.Ruido = 12;
        await Task.Delay(800); if (!Vivo()) return;
        B.Ruido = 3;
        await Task.Delay(700); if (!Vivo()) return;
        B.Ruido = 0; B.Lect = B.Meta;
        if (sinEtq)
        {
            // Sin etiqueta no se registra sola: el pesador la ingresa en la pantalla Báscula y graba
            B.Esperando = true; Fijar("estable", "warn", $"Sin etiqueta · {D.Kg(W)} kg", "Ingrésela a mano: toque Abrir");
            if (S.View == "bas" && S.Bf is { Dirty: false })
            {
                S.Bf = FormNuevo("sub"); S.Bf.Bruto = W; S.Bf.Tomado = true; S.Bf.Msg = new("warn", "Carga sin etiqueta: elija qué es, revise los datos y toque Grabar"); Render();
            }
            B.Liberar = new TaskCompletionSource<Etiqueta>();
            var rec = await B.Liberar.Task; if (!Vivo()) return;
            B.Esperando = false; B.Liberar = null;
            Fijar("estable", "ok", $"{rec.Codigo} · {D.Kg(rec.Neto)} kg", $"Ingresada a mano · registrada {rec.Hp}");
            await Task.Delay(1600); if (!Vivo()) return;
            B.Pic = "sale"; B.Meta = 0; B.Ruido = 30; Fijar("inestable", "muted", $"{rec.Codigo} sale de la báscula", "Quedó registrada");
            await Task.Delay(900); if (!Vivo()) return;
            B.Ruido = 0; B.Lect = 0; B.Meta = 0; B.Carga = null; B.Pic = "vacia";
            Fijar("cero", "muted", "Báscula libre", "Esperando la próxima carga");
            return;
        }
        var extra = "";
        if (grua || fuera)
        {
            if (grua) Fijar("bloqueado", "err", "Grúa sobre la báscula", $"Marca {D.Kg(W + desvio)} kg. Retroceda para pesar.");
            else Fijar("bloqueado", "err", "Carro fuera del área", "Se pasa de la plataforma. Acomódelo.");
            await Task.Delay(2800); if (!Vivo()) return;
            B.Pic = "sobre"; B.Meta = W; B.Ruido = 25;
            Fijar("inestable", "warn", grua ? "La grúa salió · estabilizando" : "Carro acomodado · estabilizando", "No toque la carga");
            await Task.Delay(900); if (!Vivo()) return;
            B.Ruido = 5;
            await Task.Delay(700); if (!Vivo()) return;
            B.Ruido = 0; B.Lect = B.Meta;
            extra = grua ? "grúa encima: se esperó que saliera" : "carro fuera del área: se acomodó";
        }
        var r = Evaluar(l, W);
        Fijar("estable", r);
        await Task.Delay(1000); if (!Vivo()) return;
        Registrar(l, r, extra);
        Fijar("estable", r.K, r.T1, $"Registrado {l.Hp} · {r.Corto}");
        await Task.Delay(1600); if (!Vivo()) return;
        B.Pic = "sale"; B.Meta = 0; B.Ruido = 30; Fijar("inestable", "muted", $"{c} sale de la báscula", "Quedó registrada");
        await Task.Delay(900); if (!Vivo()) return;
        B.Ruido = 0; B.Lect = 0; B.Meta = 0; B.Carga = null; B.Pic = "vacia";
        Fijar("cero", "muted", "Báscula libre", "Esperando la próxima carga");
    }

    /// <summary>Nódulos en la junta: vacía no vuelve a cero y no registra hasta que el pesador los saca.</summary>
    private async Task SinCero(int yo)
    {
        B.Pic = "cero"; B.Meta = 6; B.Lect = 6; B.Ruido = 0;
        Fijar("sincero", "err", "Sin cero: vacía marca 6 kg", "Nódulos en la junta. Así no registra.");
        await Task.Delay(4800); if (yo != _gen) return;
        B.Pic = "vacia"; B.Meta = 0; B.Lect = 0;
        Fijar("cero", "ok", "Cero recuperado", "El pesador retiró los nódulos");
        await Task.Delay(1800); if (yo != _gen) return;
        Fijar("cero", "muted", "Báscula libre", "Esperando la próxima carga");
    }

    private void Encolar(Etiqueta l, int ms = 3500) { l.Llega = DateTime.Now.AddMilliseconds(ms); _cola.Enqueue(l); Proximo(); }

    private async void Proximo()
    {
        if (_ocupada || _cola.Count == 0 || S.View == "login") return;
        _ocupada = true; var yo = _gen; var l = _cola.Dequeue();
        var falta = (l.Llega - DateTime.Now).TotalMilliseconds;
        if (falta > 0) await Task.Delay((int)falta);
        if (yo != _gen) return;
        await Pesar(l, yo); if (yo != _gen) return;
        if (Esc.Cero) { Esc.Cero = false; RenderPanel(); await SinCero(yo); if (yo != _gen) return; }
        _ocupada = false;
        await Task.Delay(400); if (yo != _gen) return;
        Proximo();
    }

    /* ---------- Pantalla Báscula: los datos de las planillas y del sistema, editables ---------- */
    private Etiqueta? RegDe(int? id) => id == null ? null : S.Labels.FirstOrDefault(x => x.Id == id);

    private Form FormNuevo(string prod)
    {
        var f = new Form { Modo = "nuevo", Prod = prod };
        if (prod == "cat") { var G = GrupoAct(); var n = Siguiente(G) ?? 1; f.G = G.G; f.N = n; f.Lado = LadoDe(G, n); f.Mu = "N"; }
        if (prod == "lam") { f.T = S.Lam.Tipo; f.Num = S.Lam.Sig[S.Lam.Tipo]; f.Tara = 12; }
        if (prod == "rs") { f.G = S.Rs.Grupo; f.Nr = Math.Min(12, S.Rs.Lista.Count(x => x.G == S.Rs.Grupo) + 1); f.Carro = ""; f.Tara = null; }
        if (prod == "sub") { f.St = "dg"; f.Cant = 1; f.Bote = ""; }
        return f;
    }

    private static Form FormDe(Etiqueta l, string modo)
    {
        var f = new Form
        {
            Modo = modo, Id = l.Id, Prod = l.Prod, Bruto = l.Estado is "pesada" or "sintara" ? l.Bruto : null,
            Tara = l.Prod == "lam" ? l.Tara : l.Prod == "rs" ? (l.SinTara ? null : l.Tara) : null,
        };
        if (l.Prod == "cat") { f.G = l.Ref.G; f.N = l.Ref.M ? 0 : l.Ref.N; f.Lado = l.Lado; f.Mu = l.Ref.M ? "S" : "N"; }
        if (l.Prod == "lam") { f.T = l.Ref.T; f.Num = l.Ref.N; }
        if (l.Prod == "rs") { f.G = l.Ref.G; f.Nr = l.Ref.N; f.Carro = l.Ref.Carro; }
        if (l.Prod == "sub") { f.St = l.Ref.St; f.Cant = Math.Max(1, l.Ref.Cant); f.Bote = l.Ref.Bote; }
        return f;
    }

    /// <summary>Cuando llega una carga con etiqueta y la pantalla Báscula está abierta sin cambios, muestra sus datos.</summary>
    private void BfSeguir(Etiqueta l) { if (S.View == "bas" && S.Bf is { Dirty: false } && !l.SinEtq && l.Id > 0) S.Bf = FormDe(l, "carga"); }
    private static int? NetoBF(Form f) => f.Bruto == null ? null : f.Prod is "lam" or "rs" ? (f.Tara == null ? null : f.Bruto - f.Tara) : f.Bruto;

    private bool Fallo(string txt) { S.Bf!.Msg = new("err", txt); Render(); return false; }

    private static string? ValidarBF(Form f, bool pesable)
    {
        if (f.Prod is "cat" or "rs" && f.G <= 0) return "Falta el grupo";
        if (f.Prod == "cat" && f.Mu != "S" && (f.N < 1 || f.N > 42)) return "La cuba va de 1 a 42";
        if (f.Prod == "lam" && f.Num <= 0) return "Falta el número del pallet";
        if (f.Prod == "rs" && (f.Nr < 1 || f.Nr > 12)) return "El carro del grupo va de 1 a 12";
        if (f.Prod == "rs" && f.Carro.Length == 0) return "Falta el número pintado del carro";
        if (f.Prod == "sub" && f.Cant < 1) return "Faltan las cajas o paquetes";
        if (f.Prod == "sub" && f.St == "hm" && f.Bote.Length == 0) return "Falta el número de bote";
        if (pesable && !(f.Bruto > 0)) return "Falta el peso: toque Tomar peso o el recuadro del peso";
        if (pesable && f.Prod is "lam" or "rs" && f.Tara != null && f.Bruto - f.Tara <= 0) return "La tara es mayor que el peso";
        return null;
    }

    /// <summary>Pesaje sin etiqueta: se crea el registro con lo que puso el pesador.</summary>
    private bool CrearManual(Form f)
    {
        var h = Hora(); Etiqueta l;
        if (f.Prod == "cat")
        {
            var G = GrupoDe(f.G);
            if (f.Mu == "S") { if (G.MuestraH != null) return Fallo($"La muestra del grupo {f.G} ya está registrada"); l = EtqMuestra(f.G, h, S.User); G.MuestraH = h; }
            else { if (G.Celdas.ContainsKey(f.N)) return Fallo($"La celda {f.N} del grupo {f.G} ya tiene registro: edítela desde Ver datos"); l = EtqCat(f.G, f.N, f.Lado.Length > 0 ? f.Lado : LadoDe(G, f.N), h, f.Bruto!.Value, S.User); }
        }
        else if (f.Prod == "lam")
        {
            var T = D.LamDe(f.T);
            if (S.Labels.Any(x => x.Prod == "lam" && x.Ref.T == f.T && x.Ref.N == f.Num)) return Fallo($"El Nº {f.Num} de {T.Corto} ya tiene registro");
            l = EtqLam(T, f.Num, h, f.Bruto!.Value - (f.Tara ?? 12), S.User);
            if (f.Num >= S.Lam.Sig[f.T]) S.Lam.Sig[f.T] = f.Num + 1;
        }
        else if (f.Prod == "rs")
        {
            if (S.Rs.Lista.Any(x => x.G == f.G && (x.Carro == f.Carro || x.N == f.Nr))) return Fallo($"El carro C{f.Carro} o el Nº {f.Nr} ya está en el grupo {f.G}");
            l = EtqRs(f.G, f.Carro, f.Nr, h, f.Tara != null ? f.Bruto!.Value - f.Tara.Value : 0, f.Tara, S.User);
        }
        else l = EtqSub(D.SubDe(f.St), f.Cant, f.St == "hm" ? f.Bote : "", h, f.Bruto!.Value, S.User);
        l.Bruto = f.Bruto!.Value;
        if (f.Prod is "lam" or "rs") l.Tara = f.Tara ?? 0;
        if (f.Prod == "rs") l.SinTara = f.Tara == null;
        Agregar(l); l.M = S.Min; l.Manual = true; l.Nota = string.Join(" · ", new[] { l.Nota, "sin etiqueta: ingresada en la báscula" }.Where(x => x.Length > 0));
        if (f.Prod == "cat" && !l.Ref.M) GrupoDe(f.G).Celdas[f.N] = new Celda { E = "camino", H = h, Id = l.Id };
        if (f.Prod == "rs") S.Rs.Lista.Add(new RsItem { G = f.G, Carro = f.Carro, N = f.Nr, Id = l.Id });
        var r = Evaluar(l, f.Bruto!.Value);
        S.Bf = FormDe(l, "editar"); Registrar(l, r, "ingreso manual", true);
        S.Bf = FormDe(l, "editar");
        S.Bf.Msg = new(r.K == "ok" ? "ok" : "warn", $"Grabado {l.Hp} · {l.Codigo} · {D.Kg(l.Estado == "pesada" ? l.Neto : l.Bruto)} kg{(r.K == "ok" ? "" : " · " + r.T2)}");
        if (B.Esperando && B.Liberar != null) B.Liberar.TrySetResult(l);
        Render(); return true;
    }

    /// <summary>Corregir un registro: con o sin etiqueta, antes o después de pesar.</summary>
    private bool AplicarBF(Etiqueta l, Form f, bool pesable)
    {
        var antes = l.Codigo;
        if (l.Prod == "cat" && !l.Ref.M && (f.G != l.Ref.G || f.N != l.Ref.N || f.Lado != l.Lado))
        {
            var G2 = GrupoDe(f.G);
            if (G2.Celdas.TryGetValue(f.N, out var ya) && ya.Id != l.Id) return Fallo($"La celda {f.N} del grupo {f.G} ya tiene registro");
            if (S.Grupos.TryGetValue(l.Ref.G, out var G1) && G1.Celdas.TryGetValue(l.Ref.N, out var c1) && c1.Id == l.Id) G1.Celdas.Remove(l.Ref.N);
            G2.Celdas[f.N] = new Celda { E = l.Estado, P = l.Neto, H = l.Hora, Id = l.Id };
            l.Ref = new Ref { G = f.G, N = f.N }; l.Lado = f.Lado; l.Codigo = $"{f.N}{f.Lado}{f.G}"; l.Detalle = $"Celda {f.N}, lado {LadoTxt(f.Lado)}, grupo {f.G}";
            l.Cajas = new() { new[] { "CELDA", f.N.ToString() }, new[] { "LADO", f.Lado }, new[] { "GRUPO", f.G.ToString() } };
            l.Qr = $"VEN|CAT|C{D.Pad2(f.N)}|L{f.Lado}|G{f.G}|M-N"; l.Pie = $"Cátodo comercial · lado {LadoTxt(f.Lado)} · {D.FECHA} {l.Hora}";
        }
        if (l.Prod == "lam")
        {
            var T = D.LamDe(f.T);
            if ((f.T != l.Ref.T || f.Num != l.Ref.N) && S.Labels.Any(x => x != l && x.Prod == "lam" && x.Ref.T == f.T && x.Ref.N == f.Num)) return Fallo($"El Nº {f.Num} de {T.Corto} ya tiene registro");
            l.Ref = new Ref { T = f.T, N = f.Num }; l.Codigo = CodLam(T, f.Num); l.Detalle = T.Nombre; l.TipoNombre = T.Nombre; l.Tara = f.Tara ?? 12;
            l.Cajas = new() { new[] { "DÍA", D.DIA }, new[] { "MES", D.MES }, new[] { "Nº", f.Num.ToString() } };
        }
        if (l.Prod == "rs")
        {
            if (S.Rs.Lista.Any(x => x.Id != l.Id && x.G == f.G && (x.Carro == f.Carro || x.N == f.Nr))) return Fallo($"El carro C{f.Carro} o el Nº {f.Nr} ya está en el grupo {f.G}");
            var x2 = S.Rs.Lista.FirstOrDefault(y => y.Id == l.Id); if (x2 != null) { x2.G = f.G; x2.Carro = f.Carro; x2.N = f.Nr; }
            l.Ref = new Ref { G = f.G, Carro = f.Carro, N = f.Nr }; l.Codigo = $"G{f.G} · C{f.Carro}"; l.Detalle = $"Restos de ánodos · carro {f.Nr} del grupo {f.G}";
            l.Tara = f.Tara ?? 0; l.SinTara = f.Tara == null;
            l.Cajas = new() { new[] { "GRUPO", f.G.ToString() }, new[] { "CARRO", f.Carro }, new[] { "Nº", f.Nr.ToString() } };
            l.Pie = f.Tara != null ? $"Tara carro + rack {D.Kg(f.Tara.Value)} kg · {D.FECHA} {l.Hora}" : $"Sin tara registrada · {D.FECHA} {l.Hora}";
        }
        if (l.Prod == "sub")
        {
            var T = D.SubDe(f.St); var bote = f.St == "hm" ? f.Bote : "";
            l.Ref = new Ref { St = f.St, Cant = f.Cant, Bote = bote }; l.Codigo = CodSub(T, bote);
            l.Detalle = T.Nombre + (f.Cant > 1 ? $" · {f.Cant} {(T.Caja ? "cajas" : "paquetes")}" : "");
        }
        l.Editado = true;
        if (pesable)
        {
            var corr = f.Bruto != l.Bruto; l.Bruto = f.Bruto!.Value;
            var r = Evaluar(l, l.Bruto);
            if (r.Estado == "pesada") l.Neto = l.Prod is "lam" or "rs" ? l.Bruto - l.Tara : l.Bruto;
            Marcar(l, r.Estado);
            l.Ctrl = r.Ctrl + (corr ? " · peso digitado" : "") + $" · corregido {Hora()}";
        }
        Persistencia.Guardar(S);
        S.Bf = FormDe(l, "editar");
        S.Bf.Msg = new("ok", $"Cambios grabados · {l.Codigo}{(antes != l.Codigo ? $" (antes {antes})" : "")}{(pesable && l.Estado == "pesada" ? $" · neto {D.Kg(l.Neto)} kg" : "")}");
        Render(); Avisar($"Registro corregido: {l.Codigo}", "ok"); return true;
    }

    internal void GrabarBF()
    {
        var f = S.Bf; if (f == null) return;
        var l = RegDe(f.Id); var pesable = l == null || l.Estado is "pesada" or "sintara";
        var e = ValidarBF(f, pesable); if (e != null) { Fallo(e); return; }
        if (l == null) { CrearManual(f); return; }
        if (!f.Dirty) { f.Msg = new("info", "No hay cambios que grabar"); Render(); return; }
        AplicarBF(l, f, pesable);
    }

    internal void TomarPeso()
    {
        var f = S.Bf; if (f == null) return;
        var l = RegDe(f.Id);
        if (l != null && l.Estado is not ("pesada" or "sintara")) { f.Msg = new("info", $"{l.Codigo} se registra sola cuando el peso se estabiliza"); Render(); return; }
        if (B.Chip is "bloqueado" or "sincero") { f.Msg = new("err", $"Báscula bloqueada: {B.Res.T1}"); Render(); return; }
        if (B.Chip is "inestable" or "espera") { f.Msg = new("warn", "Espere que el peso se estabilice"); Render(); return; }
        if (Math.Round(B.Lect) < 1) { f.Msg = new("warn", "La báscula está vacía: suba la carga o escriba el peso"); Render(); return; }
        f.Bruto = (int)Math.Round(B.Lect); f.Ed.Remove("bruto"); f.Tomado = true; f.Dirty = true;
        f.Msg = new("ok", $"Peso tomado de la báscula: {D.Kg(f.Bruto.Value)} kg"); Render();
    }

    internal void AbrirBas()
    {
        S.Md = null; S.View = "bas";
        if (B.Esperando) { S.Bf = FormNuevo("sub"); S.Bf.Bruto = (int)Math.Round(B.Lect); S.Bf.Tomado = true; S.Bf.Msg = new("warn", "Carga sin etiqueta: elija qué es, revise los datos y toque Grabar"); }
        else S.Bf = B.Carga is { Id: > 0 } ? FormDe(B.Carga, "carga") : FormNuevo(S.Prod is "rs" or "lam" ? S.Prod : "cat");
        Render();
    }
    internal void CerrarBas() { S.Md = null; S.View = "main"; S.Bf = null; Render(); }
    internal void Editar(int id) { var l = RegDe(id); if (l == null) return; S.Md = null; S.View = "bas"; S.Bf = FormDe(l, "editar"); Render(); }
    private void BfProd(string v)
    {
        var f = S.Bf!; if (f.Modo != "nuevo" || f.Prod == v) return;
        var n = FormNuevo(v); if (f.Tomado) { n.Bruto = f.Bruto; n.Tomado = true; }
        S.Bf = n; Render();
    }
    private void BfCampo(string k)
    {
        if (k is "lado" or "mu" or "t" or "st") { S.Md = new Modal { Tipo = "bfop", Campo = k }; Render(); return; }
        var max = k switch { "num" => 4, "carro" => 3, "tara" or "bruto" => 5, "bote" => 3, _ => 2 };
        S.Md = new Modal { Tipo = "bfkp", Campo = k, Max = max }; Render();
    }
    private void OkBfkp()
    {
        var k = S.Md!.Campo; var v = S.Md.Val; var f = S.Bf!; if (v.Length == 0 || !int.TryParse(v, out var n)) return;
        switch (k)
        {
            case "g": f.G = n; break;
            case "n": f.N = n; if (f.Prod == "cat" && !f.Ed.Contains("lado")) f.Lado = LadoDe(GrupoDe(f.G), n); break;
            case "num": f.Num = n; break;
            case "nr": f.Nr = n; break;
            case "carro": f.Carro = n.ToString(); if (f.Prod == "rs" && !f.Ed.Contains("tara")) f.Tara = D.TaraCarro.TryGetValue(f.Carro, out var t) ? t : null; break;
            case "tara": f.Tara = n; break;
            case "bruto": f.Bruto = n; f.Tomado = false; break;
            case "cant": f.Cant = n; break;
            case "bote": f.Bote = n.ToString(); break;
        }
        f.Ed.Add(k); f.Dirty = true; f.Msg = null; Cerrar();
    }
    private void SetBfop(string v)
    {
        var k = S.Md!.Campo; var f = S.Bf!;
        switch (k)
        {
            case "lado": f.Lado = v; break;
            case "mu": f.Mu = v; if (v == "N" && f.Lado.Length == 0) { var G = GrupoDe(f.G); f.N = f.N > 0 ? f.N : Siguiente(G) ?? 1; f.Lado = LadoDe(G, f.N); } break;
            case "t": f.T = v; if (f.Modo == "nuevo") { f.Num = S.Lam.Sig[v]; f.Tara = 12; } break;
            case "st": f.St = v; if (v != "hm") f.Bote = ""; break;
        }
        f.Ed.Add(k); f.Dirty = true; f.Msg = null; Cerrar();
    }
    private void LimpiarBF()
    {
        var n = FormNuevo(S.Bf?.Prod ?? "cat");
        if (B.Esperando) { n = FormNuevo("sub"); n.Bruto = (int)Math.Round(B.Lect); n.Tomado = true; }
        S.Bf = n; Render();
    }
    private void Turno(string v) { S.Turno = v; if (S.Bf != null) S.Bf.Msg = new("info", $"Turno {v}: lo que se pese desde ahora queda en el turno {v}"); Render(); }
    private void ControlCero()
    {
        if (B.Carga != null || (Math.Round(B.Lect) > 0 && B.Pic != "cero")) { Avisar("Hay carga sobre la báscula: el control de cero se hace con la báscula vacía.", "warn"); return; }
        if (B.Pic == "cero") { S.Cero = new CeroInfo { H = Hora(), Ok = false }; if (S.Bf != null) S.Bf.Msg = new("err", $"Control de cero: falla. Vacía marca {D.Kg((int)Math.Round(B.Lect))} kg"); Render(); return; }
        S.Cero = new CeroInfo { H = Hora(), Ok = true }; if (S.Bf != null) S.Bf.Msg = new("ok", $"Control de cero OK a las {S.Cero.H}: vacía marca 0 kg"); Render();
    }

    /* ---------- Panel de prueba (F2): situaciones para mostrar el control de la báscula ---------- */
    internal void Escenario(string k)
    {
        if (k == "caja")
        {
            Encolar(new Etiqueta { SinEtq = true, Prod = "sub", Codigo = "Sin etiqueta", Bruto = D.CajasEj[_cajaN++ % D.CajasEj.Length], Estado = "camino" }, 900);
            Avisar("Caja sin etiqueta: llega a la báscula en un momento. Toque Abrir en la báscula para ingresarla.", "info");
            _panelAbierto = false; RenderPanel(); return;
        }
        if (k == "cero" && !Esc.Cero && !_ocupada)
        {
            _ocupada = true; var yo = _gen;
            _ = Correr();
            async Task Correr() { await SinCero(yo); if (yo != _gen) return; _ocupada = false; Proximo(); }
            _panelAbierto = false; RenderPanel(); return;
        }
        if (k == "grua") Esc.Grua = !Esc.Grua;
        if (k == "fuera") Esc.Fuera = !Esc.Fuera;
        if (k == "cero") Esc.Cero = !Esc.Cero;
        RenderPanel();
    }
}
