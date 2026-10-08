using FerreSys.Application;
using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI.Printing;

internal static class ThermalPrinterFactory
{
    public static IThermalPrinter Create(PeripheralSettings settings)
    {
        return settings.PrinterTransport switch
        {
            PrinterTransport.Windows => new WindowsRawPrinter(settings.WindowsPrinterName),
            PrinterTransport.Serial => new SerialThermalPrinter(settings.PrinterSerialPort, settings.PrinterBaudRate),
            PrinterTransport.Network => new NetworkThermalPrinter(settings.NetworkHost, settings.NetworkPort),
            _ => throw new InvalidOperationException("No hay impresora térmica configurada.")
        };
    }
}
