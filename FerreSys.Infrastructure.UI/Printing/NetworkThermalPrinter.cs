using System.Net.Sockets;
using FerreSys.Application;

namespace FerreSys.Infrastructure.UI.Printing;

internal sealed class NetworkThermalPrinter : IThermalPrinter
{
    private readonly string _host;
    private readonly int _port;

    public NetworkThermalPrinter(string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Escribe la IP o el nombre de la impresora de red.", nameof(host));
        if (port is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(port), "El puerto de red debe estar entre 1 y 65535.");
        _host = host.Trim();
        _port = port;
    }

    public void Write(byte[] payload)
    {
        using var client = new TcpClient();
        var connect = client.ConnectAsync(_host, _port);
        if (!connect.Wait(TimeSpan.FromSeconds(4)))
            throw new TimeoutException($"No hubo respuesta de {_host}:{_port}.");
        connect.GetAwaiter().GetResult();

        using NetworkStream stream = client.GetStream();
        stream.Write(payload, 0, payload.Length);
        stream.Flush();
    }
}
