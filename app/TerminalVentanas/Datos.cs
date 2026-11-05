using System.Globalization;

namespace TerminalVentanas;

/// <summary>Datos de la visita y reglas de la báscula. Los pesos son los del 22-10-2026, grupo 21, y la hoja de láminas del 21-10.</summary>
public static class D
{
    public const string FECHA = "22-10-2026", DIA = "22", MES = "10", FQ = "20261022";
    private static readonly CultureInfo Cl = new("es-CL");

    /// <summary>Pesos reales del Detalle pesaje de producción del Sistema de Cátodos, grupo 21.</summary>
    public static readonly Dictionary<int, int> PesoCelda = new()
    {
        [1] = 4796, [2] = 4855, [3] = 4722, [4] = 4687, [5] = 4860, [6] = 4815, [7] = 4818, [8] = 4843, [9] = 4742, [10] = 4831, [11] = 4869,
        [12] = 4724, [13] = 4610, [14] = 4725, [15] = 4714, [16] = 4680, [17] = 4800, [18] = 4789, [19] = 4790, [20] = 4644, [21] = 4775,
        [42] = 4310, [41] = 4646, [40] = 4714, [39] = 4445, [38] = 4365, [37] = 4791, [36] = 4756, [35] = 4796, [34] = 4641, [33] = 4612,
        [32] = 4651, [31] = 4640, [30] = 4626, [29] = 4316,
    };
    public const int PesoMuestra = 854;
    /// <summary>Netos de los 11 carros de restos del grupo 21, en orden (cuaderno del pesador).</summary>
    public static readonly int[] RestosNetos = { 3413, 3878, 3488, 3718, 3868, 3464, 3506, 3274, 3394, 3912, 2046 };
    /// <summary>Tara de carro + rack por número pintado: valores de ejemplo (la lista real la tiene el pesador).</summary>
    public static readonly Dictionary<string, int> TaraCarro = new() { ["7"] = 2120, ["11"] = 2180, ["15"] = 2165, ["22"] = 2150, ["26"] = 2210, ["30"] = 2135, ["33"] = 2190, ["41"] = 2175 };
    /// <summary>Netos de la hoja de láminas del 21-10, para simular la báscula con los números nuevos.</summary>
    public static readonly int[] LamEj = { 3458, 3743, 3701, 3485, 3620, 3724, 3547, 3477, 3814, 3629, 3262 };

    public sealed record TipoLam(string Id, string Nombre, string Corto, string Q, (int N, int Kg)[] Prev, int[] Ej);
    /// <summary>Los bloques de la "Hoja de láminas despegue": H. INIC. (aprobadas), C.O, L.VENTA y L.RECHAZO S/O.</summary>
    public static readonly TipoLam[] Lam =
    {
        new("aprob", "Láminas aprobadas N.E.", "Aprobadas N.E.", "LAPNE", new[] { (270, 3477), (271, 3814), (272, 3629), (273, 3262) }, LamEj),
        new("co", "Láminas C.O", "C.O", "LCO", new[] { (36, 2261), (37, 2152) }, new[] { 2261, 2152 }),
        new("venta", "Láminas venta", "Venta", "LVTA", Array.Empty<(int, int)>(), LamEj),
        new("rso", "Láminas rechazo S/O", "Rechazo S/O", "LRSO", new[] { (63, 1885), (64, 1949), (65, 1852) }, new[] { 1885, 1949, 1852 }),
    };
    public static TipoLam LamDe(string id) => Lam.First(t => t.Id == id);

    public sealed record TipoSub(string Id, string Nombre, string Corto, bool Caja = false, bool Bote = false);
    /// <summary>Lo que no lleva etiqueta y se ingresa en la báscula: filas del formulario de subproductos en PM y botes de hojas madres.</summary>
    public static readonly TipoSub[] Sub =
    {
        new("dg", "Despuntes de guillotina", "D.G", Caja: true),
        new("do", "Despuntes de orejas", "D.O", Caja: true),
        new("lvm", "Láminas venta MFCI", "Lám. venta MFCI"),
        new("lrsm", "Láminas rechazo S/O MFCI", "Lám. rech. S/O MFCI"),
        new("lrco", "Láminas rechazo C/O", "Lám. rech. C/O"),
        new("cus", "Cu sucio", "Cu sucio"),
        new("hm", "Restos de hojas madres (bote)", "Hojas madres", Bote: true),
    };
    public static TipoSub SubDe(string id) => Sub.First(t => t.Id == id);
    /// <summary>Pesos de cajas de despuntes del 21-10 (D.G 329, 400 y 312; D.O 1.192) para la caja sin etiqueta del panel de prueba.</summary>
    public static readonly int[] CajasEj = { 329, 400, 312, 1192 };

    /// <summary>Orden de pesaje del 22-10: celdas 1 a 21 subiendo y después desde la 42 bajando.</summary>
    public static readonly int[] Orden = Enumerable.Range(1, 21).Concat(Enumerable.Range(22, 21).Reverse()).ToArray();
    public static readonly Dictionary<string, string> LadoTxt = new() { ["T"] = "total", ["P"] = "parcial" };
    public static readonly Dictionary<string, string> EstTxt = new() { ["camino"] = "En camino", ["bascula"] = "En la báscula", ["pesada"] = "Pesada", ["sintara"] = "Sin tara" };
    /// <summary>Lo esperado por producto (las reglas de la simulación). C.O, rechazo S/O y la muestra todavía no tienen rango.</summary>
    public static readonly Dictionary<string, (int Min, int Max)> Rango = new() { ["cat"] = (4200, 5000), ["aprob"] = (3200, 3900), ["rs"] = (2800, 4000) };

    public static string Kg(int v) => v.ToString("#,0", Cl);
    public static string Hhmm(int m) => $"{(m / 60) % 24:00}:{m % 60:00}";
    public static string Pad2(int n) => n.ToString("00");
    public static string FmtRut(string r) => r.Length < 2 ? r : Miles(r[..^1]) + "-" + r[^1];

    /// <summary>Dígito verificador del RUT (módulo 11): 0 a 9 o K.</summary>
    public static char DvRut(string cuerpo)
    {
        int suma = 0, mult = 2;
        for (var i = cuerpo.Length - 1; i >= 0; i--) { suma += (cuerpo[i] - '0') * mult; mult = mult == 7 ? 2 : mult + 1; }
        var r = 11 - suma % 11;
        return r == 11 ? '0' : r == 10 ? 'K' : (char)('0' + r);
    }

    /// <summary>RUT completo (7 u 8 números más el dígito) y con el dígito verificador que corresponde.</summary>
    public static bool RutValido(string r) => r.Length is >= 8 and <= 9 && r[..^1].All(char.IsDigit) && char.ToUpperInvariant(r[^1]) == DvRut(r[..^1]);
    private static string Miles(string s) => long.TryParse(s, out var n) ? n.ToString("#,0", Cl) : s;
}
