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

    /// <summary>Dónde quedan los .zpl en modo seco.</summary>
    public string CarpetaZpl { get; set; } = @"C:\BasculaN9\Etiquetas";
}
