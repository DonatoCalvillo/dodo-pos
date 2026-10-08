using System.IO.Ports;
using FerreSys.Application;

namespace FerreSys.Infrastructure.UI.Printing;

internal sealed class SerialThermalPrinter : IThermalPrinter
{
    private readonly string _portName;
    private readonly int _baudRate;

    public SerialThermalPrinter(string portName, int baudRate)
    {
        if (string.IsNullOrWhiteSpace(portName))
            throw new ArgumentException("Selecciona un puerto COM para la impresora.", nameof(portName));
        _portName = portName.Trim();
        _baudRate = baudRate > 0 ? baudRate : 9600;
    }

    public void Write(byte[] payload)
    {
        using var port = new SerialPort(_portName, _baudRate, Parity.None, 8, StopBits.One)
        {
            WriteTimeout = 4000,
            Handshake = Handshake.None
        };
        port.Open();
        port.Write(payload, 0, payload.Length);
    }
}
