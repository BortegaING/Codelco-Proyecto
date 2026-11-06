using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace TerminalVentanas;

/// <summary>
/// Los colores de la pantalla: claro, oscuro, alto contraste o "auto" (igual que Windows).
/// Lo que elige el operador queda en tema.txt junto al registro del equipo; si no eligió nada, manda appsettings.json ("Tema").
/// </summary>
public static class Tema
{
    public static readonly string[] Todos = { "auto", "claro", "oscuro", "contraste" };

    /// <summary>Lo elegido: puede ser "auto".</summary>
    public static string Elegido { get; private set; } = "auto";

    /// <summary>Lo que se ve: "auto" ya resuelto a claro, oscuro o contraste.</summary>
    public static string Actual { get; private set; } = "claro";

    private static string Archivo => Path.Combine(Persistencia.Carpeta, "tema.txt");

    public static string Nombre(string t) => t switch { "claro" => "Claro", "oscuro" => "Oscuro", "contraste" => "Alto contraste", _ => "Como Windows" };

    /// <summary>Lee lo elegido en este equipo (o lo de appsettings) y pone sus colores.</summary>
    public static void Cargar(string porDefecto)
    {
        string? guardado = null;
        try { if (File.Exists(Archivo)) guardado = File.ReadAllText(Archivo).Trim().ToLowerInvariant(); } catch { /* sin disco: vale lo de appsettings */ }
        Elegido = Valido(guardado) ?? Valido(porDefecto?.Trim().ToLowerInvariant()) ?? "auto";
        Aplicar();
    }

    /// <summary>El operador eligió en la pantalla: se guarda y se ve en el momento.</summary>
    public static void Elegir(string t)
    {
        Elegido = Valido(t) ?? "auto";
        try { Directory.CreateDirectory(Persistencia.Carpeta); File.WriteAllText(Archivo, Elegido); } catch { /* sin disco: dura hasta cerrar */ }
        Aplicar();
    }

    /// <summary>Vuelve a mirar Windows (en "auto"). true si cambió lo que se ve.</summary>
    public static bool Aplicar()
    {
        var antes = Actual;
        Actual = Elegido == "auto" ? DeWindows() : Elegido;
        Ui.Paleta(Actual);
        return Actual != antes;
    }

    /// <summary>Lo que tiene Windows: un tema de contraste manda; si no, el modo oscuro o claro de las aplicaciones.</summary>
    public static string DeWindows()
    {
        if (SystemParameters.HighContrast) return "contraste";
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (k?.GetValue("AppsUseLightTheme") is int claro && claro == 0) return "oscuro";
        }
        catch { /* sin permiso al registro: claro */ }
        return "claro";
    }

    private static string? Valido(string? t) => t != null && Todos.Contains(t) ? t : null;
}
