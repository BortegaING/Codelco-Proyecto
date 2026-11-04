using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TerminalVentanas;

/// <summary>Manda la etiqueta (ZPL) a la Zebra, o la guarda como archivo en modo seco.</summary>
public static class Impresion
{
    public static string Enviar(ConfigImpresora cfg, string codigo, string zpl)
    {
        if (!string.Equals(cfg.Modo, "zebra", StringComparison.OrdinalIgnoreCase))
        {
            Directory.CreateDirectory(cfg.CarpetaZpl);
            var limpio = new string(codigo.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
            var ruta = Path.Combine(cfg.CarpetaZpl, $"{DateTime.Now:yyyyMMdd-HHmmss}-{(limpio.Length > 0 ? limpio : "etiqueta")}.zpl");
            File.WriteAllText(ruta, zpl, new UTF8Encoding(false));
            return ruta;
        }
        CrudoAImpresora(cfg.Nombre, Encoding.UTF8.GetBytes(zpl), $"Etiqueta {codigo}");
        return cfg.Nombre;
    }

    // Impresión en crudo por el spooler de Windows: la Zebra recibe el ZPL tal cual
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private sealed class DocInfo
    {
        public string? pDocName;
        public string? pOutputFile;
        public string? pDataType;
    }

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);
    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);
    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int StartDocPrinter(IntPtr hPrinter, int level, [In] DocInfo di);
    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);
    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);
    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);
    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, byte[] pBytes, int dwCount, out int dwWritten);

    private static void CrudoAImpresora(string impresora, byte[] datos, string documento)
    {
        if (!OpenPrinter(impresora, out var h, IntPtr.Zero))
            throw new InvalidOperationException($"No se encontró la impresora \"{impresora}\" (error {Marshal.GetLastWin32Error()}).");
        try
        {
            if (StartDocPrinter(h, 1, new DocInfo { pDocName = documento, pDataType = "RAW" }) == 0)
                throw new InvalidOperationException($"La impresora no aceptó el trabajo (error {Marshal.GetLastWin32Error()}).");
            try
            {
                StartPagePrinter(h);
                if (!WritePrinter(h, datos, datos.Length, out var escritos) || escritos != datos.Length)
                    throw new InvalidOperationException($"No se pudo mandar la etiqueta completa (error {Marshal.GetLastWin32Error()}).");
                EndPagePrinter(h);
            }
            finally { EndDocPrinter(h); }
        }
        finally { ClosePrinter(h); }
    }
}
