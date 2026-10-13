namespace FerreSys.Domain;

public enum PrinterTransport
{
    None,
    Windows,
    Serial,
    Network
}

public enum ScannerMode
{
    Keyboard,
    Serial
}

public sealed class PeripheralSettings
{
    public PrinterTransport PrinterTransport { get; init; } = PrinterTransport.None;
    public string WindowsPrinterName { get; init; } = string.Empty;
    public string PrinterSerialPort { get; init; } = string.Empty;
    public int PrinterBaudRate { get; init; } = 9600;
    public string NetworkHost { get; init; } = string.Empty;
    public int NetworkPort { get; init; } = 9100;
    public bool OpenDrawerOnCash { get; init; } = true;
    public int DrawerPin { get; init; }
    public ScannerMode ScannerMode { get; init; } = ScannerMode.Keyboard;
    public string ScannerSerialPort { get; init; } = string.Empty;
    public int ScannerBaudRate { get; init; } = 9600;
    public bool ScannerAddToCart { get; init; } = true;
    public bool ScaleEnabled { get; init; }
    public string ScaleSerialPort { get; init; } = string.Empty;
    public int ScaleBaudRate { get; init; } = 9600;

    public bool HasPrinterConfigured => PrinterTransport switch
    {
        PrinterTransport.Windows => !string.IsNullOrWhiteSpace(WindowsPrinterName),
        PrinterTransport.Serial => !string.IsNullOrWhiteSpace(PrinterSerialPort),
        PrinterTransport.Network => !string.IsNullOrWhiteSpace(NetworkHost) && NetworkPort is > 0 and <= 65535,
        _ => false
    };

    public bool HasScaleConfigured =>
        ScaleEnabled
        && !string.IsNullOrWhiteSpace(ScaleSerialPort)
        && ScaleBaudRate > 0;

    public static PeripheralSettings Default() => new();
}
