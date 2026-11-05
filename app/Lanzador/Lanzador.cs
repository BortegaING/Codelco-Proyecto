// Lanzador de la Terminal Ventanas: el TerminalVentanas.exe que queda AFUERA, en la carpeta del release.
// El programa real necesita a su lado las DLL de .NET (self-contained), así que vive en la subcarpeta "programa".
// Este .exe solo lo busca y lo abre. Está hecho con .NET Framework 4.8, que ya viene en Windows 10 y 11:
// no necesita nada más y no deja archivos sueltos. Lo compila app/construir.py con el csc.exe de Windows.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("Terminal Rotulado Ventanas")]
[assembly: System.Reflection.AssemblyProduct("Terminal Rotulado Ventanas")]
[assembly: System.Reflection.AssemblyCompany("Ingmétrica")]

static class Lanzador
{
    [STAThread]
    static int Main(string[] args)
    {
        string aqui = AppDomain.CurrentDomain.BaseDirectory;
        string exe = Buscar(aqui);
        if (exe == null)
        {
            MessageBox.Show("No se encontró el programa (carpeta \"programa\" con TerminalVentanas.exe) al lado de este archivo.\n\nCopie la carpeta completa del zip, sin separar los archivos.",
                "Terminal Rotulado Ventanas", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
        var psi = new ProcessStartInfo(exe)
        {
            WorkingDirectory = Path.GetDirectoryName(exe),
            UseShellExecute = false,
            Arguments = string.Join(" ", args.Select(a => a.Contains(" ") ? "\"" + a + "\"" : a).ToArray()),
        };
        Process.Start(psi);
        return 0;
    }

    // Primero "programa\TerminalVentanas.exe"; si no, cualquier subcarpeta que lo tenga (la de nombre mayor, por si hay dos versiones)
    static string Buscar(string aqui)
    {
        string p = Path.Combine(Path.Combine(aqui, "programa"), "TerminalVentanas.exe");
        if (File.Exists(p)) return p;
        foreach (var d in Directory.GetDirectories(aqui).OrderByDescending(x => x, StringComparer.OrdinalIgnoreCase))
        {
            p = Path.Combine(d, "TerminalVentanas.exe");
            if (File.Exists(p)) return p;
        }
        return null;
    }
}
