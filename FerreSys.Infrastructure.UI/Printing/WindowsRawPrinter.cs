using System.ComponentModel;
using System.Runtime.InteropServices;
using FerreSys.Application;

namespace FerreSys.Infrastructure.UI.Printing;

internal sealed class WindowsRawPrinter : IThermalPrinter
{
    private readonly string _printerName;

    public WindowsRawPrinter(string printerName)
    {
        if (string.IsNullOrWhiteSpace(printerName))
            throw new ArgumentException("Selecciona una impresora de Windows.", nameof(printerName));
        _printerName = printerName.Trim();
    }

    public void Write(byte[] payload)
    {
        if (!OpenPrinter(_printerName, out IntPtr handle, IntPtr.Zero))
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"No se pudo abrir la impresora '{_printerName}'.");

        try
        {
            var doc = new DOCINFOA
            {
                pDocName = "Posly Ticket",
                pDataType = "RAW"
            };

            if (!StartDocPrinter(handle, 1, doc))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "No se pudo iniciar el trabajo de impresión.");

            try
            {
                if (!StartPagePrinter(handle))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "No se pudo iniciar la página.");

                if (!WritePrinter(handle, payload, payload.Length, out _))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "No se pudieron enviar los datos ESC/POS.");

                EndPagePrinter(handle);
            }
            finally
            {
                EndDocPrinter(handle);
            }
        }
        finally
        {
            ClosePrinter(handle);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private sealed class DOCINFOA
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string pDocName = string.Empty;
        [MarshalAs(UnmanagedType.LPStr)]
        public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPStr)]
        public string pDataType = "RAW";
    }

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFOA di);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, byte[] pBytes, int dwCount, out int dwWritten);
}
