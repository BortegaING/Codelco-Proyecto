using System.IO;
using System.Text.Json;

namespace TerminalVentanas;

/// <summary>Lo que se puede ajustar en cada VT1200 sin recompilar (appsettings.json junto al .exe).</summary>
public sealed class Config
{
    /// <summary>true: sin bordes y a pantalla completa, como el calibrador. F11 la alterna.</summary>
    public bool PantallaCompleta { get; set; } = true;

    /// <summary>Dónde se guarda el Excel del registro cuando se toca Excel.</summary>
    public string CarpetaExcel { get; set; } = @"C:\BasculaN9\Excel";

    /// <summary>Colores de la pantalla si el operador no eligió otros: "oscuro" (el original), "claro", "contraste" o "auto" (igual que Windows).</summary>
    public string Tema { get; set; } = "oscuro";

    /// <summary>Qué programa es: "origen" (la VT1200 que rotula los cátodos) o "caseta" (la pantalla de la Báscula N9 que identifica y pesa).</summary>
    public string Programa { get; set; } = "origen";

    /// <summary>Código de la etiqueta: "qr" (el de siempre) o "datamatrix" (DataMatrix GS1, la versión aparte).</summary>
    public string Codigo { get; set; } = "qr";

    /// <summary>GTIN de 14 dígitos para el IA (01) del DataMatrix GS1. Vacío hasta que Codelco lo asigne con su prefijo GS1.</summary>
    public string Gtin { get; set; } = "";

    /// <summary>Logo de Codelco en la etiqueta, abajo a la derecha (en pantalla y en la impresión).</summary>
    public bool Logo { get; set; } = true;

    public ConfigImpresora Impresora { get; set; } = new();

    public static Config Leer()
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(ruta)) return new Config();
        var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        return JsonSerializer.Deserialize<Config>(File.ReadAllText(ruta), opciones) ?? new Config();
    }
}

public sealed class ConfigImpresora
{
    /// <summary>"seco": guarda cada etiqueta como .zpl y no imprime (para pruebas, no gasta etiquetas). "zebra": imprime en la impresora.</summary>
    public string Modo { get; set; } = "seco";

    /// <summary>Nombre de la impresora en Windows (la Zebra por USB).</summary>
    public string Nombre { get; set; } = "ZDesigner ZT610R-203dpi ZPL";

    /// <summary>Ancho de impresión en puntos (203 dpi): 812 = 100 mm (Zebra ZT610R); 576 = 72 mm (Godex MX30, impresora portátil). La etiqueta se escala a ese ancho.</summary>
    public int AnchoPuntos { get; set; } = 812;

    /// <summary>Papel en blanco al final de cada etiqueta, en puntos (8 = 1 mm). En papel continuo deja el QR antes del borde de corte:
    /// la Godex MX30 corta unos 15 mm por encima del cabezal, así que usa 160 (20 mm). La Zebra con etiquetas troqueladas usa 0.</summary>
    public int LargoExtraPuntos { get; set; } = 0;

    /// <summary>Dónde quedan los .zpl en modo seco.</summary>
    public string CarpetaZpl { get; set; } = @"C:\BasculaN9\Etiquetas";
}
