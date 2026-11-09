// Lanzador de la Terminal Ventanas: el TerminalVentanas.exe que queda AFUERA, en la carpeta del release.
// El programa real necesita a su lado las DLL de .NET (self-contained), así que vive en la subcarpeta "programa".
// Está hecho con .NET Framework 4.8, que ya viene en Windows 10 y 11. Lo compila app/construir.py con el csc.exe de Windows.
//
// Cómo abre el programa: hace lo mismo que programa\TerminalVentanas.exe, pero dentro de este proceso. Carga hostfxr.dll
// (de Microsoft, firmada) y le pide que corra TerminalVentanas.dll. Así no se ejecuta el TerminalVentanas.exe de adentro,
// que no tiene firma: Smart App Control de Windows lo bloquea ("Una directiva de Control de aplicaciones bloqueó este archivo").
// Si no encuentra hostfxr.dll, abre el .exe de adentro como antes.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("Terminal Rotulado Ventanas")]
[assembly: System.Reflection.AssemblyProduct("Terminal Rotulado Ventanas")]
[assembly: System.Reflection.AssemblyCompany("Ingmétrica")]

static class Lanzador
{
    const string Titulo = "Terminal Rotulado Ventanas";

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr LoadLibraryW(string ruta);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    static extern IntPtr GetProcAddress(IntPtr modulo, string nombre);

    // int hostfxr_main_startupinfo(int argc, const wchar_t** argv, const wchar_t* host_path, const wchar_t* dotnet_root, const wchar_t* app_path)
    // Es la misma llamada que hace el TerminalVentanas.exe de adentro (el apphost de .NET) para un programa self-contained.
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    delegate int MainStartupInfo(int argc, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] argv,
                                 string hostPath, string dotnetRoot, string appPath);

    [STAThread]
    static int Main(string[] args)
    {
        string aqui = AppDomain.CurrentDomain.BaseDirectory;
        string exe = Buscar(aqui);
        if (exe == null)
        {
            MessageBox.Show("No se encontró el programa (carpeta \"programa\" con TerminalVentanas.exe) al lado de este archivo.\n\nCopie la carpeta completa del zip, sin separar los archivos.",
                Titulo, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
        string carpeta = Path.GetDirectoryName(exe);
        string dll = Path.Combine(carpeta, "TerminalVentanas.dll");
        string hostfxr = Path.Combine(carpeta, "hostfxr.dll");
        if (Environment.Is64BitProcess && File.Exists(dll) && File.Exists(hostfxr))
            return Adentro(carpeta, exe, dll, hostfxr, args);

        var psi = new ProcessStartInfo(exe)
        {
            WorkingDirectory = carpeta,
            UseShellExecute = false,
            Arguments = string.Join(" ", args.Select(a => a.Contains(" ") ? "\"" + a + "\"" : a).ToArray()),
        };
        Process.Start(psi);
        return 0;
    }

    // Corre TerminalVentanas.dll con el .NET 8 de la carpeta, en este mismo proceso. Vuelve cuando se cierra la app.
    static int Adentro(string carpeta, string exe, string dll, string hostfxr, string[] args)
    {
        IntPtr modulo = LoadLibraryW(hostfxr);
        IntPtr funcion = modulo == IntPtr.Zero ? IntPtr.Zero : GetProcAddress(modulo, "hostfxr_main_startupinfo");
        if (funcion == IntPtr.Zero)
        {
            MessageBox.Show("Windows no dejó cargar los archivos de .NET de la carpeta \"programa\" (error " + Marshal.GetLastWin32Error() + ").\n\nCopie de nuevo la carpeta completa del zip.",
                Titulo, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
        Environment.CurrentDirectory = carpeta;
        var argv = new[] { exe }.Concat(args).ToArray();  // como lo recibe el .exe de adentro: primero su propia ruta
        var main = (MainStartupInfo)Marshal.GetDelegateForFunctionPointer(funcion, typeof(MainStartupInfo));
        return main(argv.Length, argv, exe, carpeta, dll);
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
