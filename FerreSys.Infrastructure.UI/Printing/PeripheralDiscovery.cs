using System.Drawing.Printing;
using System.IO.Ports;

namespace FerreSys.Infrastructure.UI.Printing;

internal static class PeripheralDiscovery
{
    public static readonly int[] BaudRates = [9600, 19200, 38400, 57600, 115200];

    public static IReadOnlyList<string> InstalledPrinters()
    {
        return PrinterSettings.InstalledPrinters
            .Cast<string>()
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<string> SerialPorts()
    {
        return SerialPort.GetPortNames()
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
