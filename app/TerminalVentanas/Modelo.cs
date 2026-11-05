using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TerminalVentanas;

/// <summary>Lo que identifica a una carga: grupo y celda, tipo y número de lámina, grupo y carro, o tipo de subproducto.</summary>
public sealed class Ref
{
    public int G, N, Cant = 1;
    public bool M;
    public string T = "", Carro = "", St = "", Bote = "";
}

/// <summary>Una etiqueta impresa o una carga ingresada a mano: es una fila del registro del día.</summary>
public sealed class Etiqueta
{
    public int Id, M, Neto, Tara, Bruto;
    public string Hora = "", Prod = "", Codigo = "", Detalle = "", Lado = "", Estado = "camino", Oper = "", Tag = "", Qr = "", Pie = "",
        TipoNombre = "", Nota = "", Ctrl = "", Hp = "", Turno = "";
    public bool SinTara, Manual, Editado;
    public List<string[]> Cajas = new();
    public Ref Ref = new();
    /// <summary>Carga de prueba sin etiqueta (panel F2): no es parte del registro hasta que el pesador la ingresa.</summary>
    [JsonIgnore] public bool SinEtq;
    [JsonIgnore] public DateTime Llega;
}

public sealed class Celda { public string E = "camino", H = ""; public int P, Id; }

public sealed class Grupo
{
    public int G;
    public string LadoA = "T", LadoB = "T";
    public Dictionary<int, Celda> Celdas = new();
    public string? MuestraH;
}

public sealed class LamEstado
{
    public string Tipo = "aprob";
    public Dictionary<string, int> Sig = new() { ["aprob"] = 274, ["co"] = 38, ["venta"] = 1, ["rso"] = 66 };
    public int Ej;
}

public sealed class RsItem { public int G, N, Id; public string Carro = ""; }
public sealed class RsEstado { public int Grupo = 21; public string Carro = ""; public List<RsItem> Lista = new(); }
public sealed class CeroInfo { public string H = ""; public bool Ok; }

public sealed record Aviso(string K, string Txt, string? Volver = null);

/// <summary>Lo que se muestra arriba en la pantalla principal para el producto elegido.</summary>
public sealed class Modelo
{
    public string Lbl = "", Codigo = "", ImpTxt = "";
    public string? Corto;
    public bool Vacio, ImpDis;
    public Etiqueta Sticker = new();
    public Func<System.Windows.UIElement>? Trabajo;
    public Aviso Estado = new("ok", "");
    public Modelo Copia() => (Modelo)MemberwiseClone();
}

/// <summary>Una ventana sobre la pantalla: elegir grupo, lado, teclado de un dato, registro, etc.</summary>
public sealed class Modal { public string Tipo = "", Val = "", Campo = ""; public int Max = 2; }

/// <summary>El formulario de la pantalla Báscula.</summary>
public sealed class Form
{
    public string Modo = "nuevo", Prod = "cat", Lado = "T", Mu = "N", T = "aprob", Carro = "", St = "dg", Bote = "";
    public int? Id, Bruto, Tara;
    public int G = 21, N = 1, Num = 1, Nr = 1, Cant = 1;
    public HashSet<string> Ed = new();
    public bool Dirty, Tomado;
    public Aviso? Msg;
}

/// <summary>Todo lo que el terminal recuerda. Lo durable se guarda en el equipo después de cada cambio.</summary>
public sealed class Estado
{
    public string View = "login", Rut = "111111111", User = "", Prod = "cat", Turno = "A";
    public int Grupo = 21, Min = 10 * 60 + 5, UltPes = 9 * 60 + 58, Uid;
    public int? Sel = 5;
    public bool Muestra;
    public Dictionary<int, Grupo> Grupos = new();
    public LamEstado Lam = new();
    public RsEstado Rs = new();
    public List<Etiqueta> Labels = new();
    public CeroInfo? Cero = new() { H = "09:35", Ok = true };

    [JsonIgnore] public Aviso? Msg;
    [JsonIgnore] public string? Fase;
    [JsonIgnore] public Modelo? Snap;
    [JsonIgnore] public Modal? Md;
    [JsonIgnore] public Form? Bf;
}

/// <summary>El registro del día en un archivo JSON del perfil del usuario: sobrevive a cierres y reinicios.</summary>
public static class Persistencia
{
    public static string Carpeta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IngMetrica", "TerminalVentanas");
    private static string Ruta => Path.Combine(Carpeta, "registro.json");
    private static readonly JsonSerializerOptions Op = new() { IncludeFields = true, WriteIndented = false };

    public static void Guardar(Estado s)
    {
        try
        {
            Directory.CreateDirectory(Carpeta);
            var tmp = Ruta + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(s, Op), new UTF8Encoding(false));
            File.Move(tmp, Ruta, true);
        }
        catch { /* sin disco o sin permiso: el terminal sigue funcionando en memoria */ }
    }

    public static Estado? Recuperar()
    {
        try { return File.Exists(Ruta) ? JsonSerializer.Deserialize<Estado>(File.ReadAllText(Ruta), Op) : null; }
        catch { return null; }
    }

    public static void Olvidar()
    {
        try { if (File.Exists(Ruta)) File.Delete(Ruta); } catch { }
    }
}
