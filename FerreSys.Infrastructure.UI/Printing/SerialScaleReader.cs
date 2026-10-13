using System.IO.Ports;
using System.Text;

namespace FerreSys.Infrastructure.UI.Printing;

internal sealed class SerialScaleReader : IDisposable
{
    private readonly StringBuilder _buffer = new();
    private SerialPort? _port;

    public event EventHandler<ScaleReading>? WeightReceived;

    public void Start(string portName, int baudRate)
    {
        Stop();
        _port = new SerialPort(portName, baudRate > 0 ? baudRate : 9600, Parity.None, 8, StopBits.One)
        {
            Encoding = Encoding.ASCII,
            NewLine = "\r",
            ReadTimeout = 500,
            Handshake = Handshake.None
        };
        _port.DataReceived += OnDataReceived;
        _port.Open();
    }

    public void Stop()
    {
        if (_port == null)
            return;

        try
        {
            _port.DataReceived -= OnDataReceived;
            if (_port.IsOpen)
                _port.Close();
        }
        catch (Exception)
        {
            // El puerto puede haberse desconectado.
        }
        finally
        {
            _port.Dispose();
            _port = null;
            lock (_buffer)
                _buffer.Clear();
        }
    }

    public void Dispose() => Stop();

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (sender is not SerialPort port)
            return;

        string chunk;
        try
        {
            chunk = port.ReadExisting();
        }
        catch (Exception)
        {
            return;
        }

        if (string.IsNullOrEmpty(chunk))
            return;

        lock (_buffer)
        {
            _buffer.Append(chunk);
            string text = _buffer.ToString();
            int index;
            while ((index = text.IndexOfAny(['\r', '\n'])) >= 0)
            {
                string line = text[..index].Trim();
                text = text[(index + 1)..].TrimStart('\r', '\n');
                if (ScaleWeightParser.TryParse(line, out ScaleReading reading))
                    WeightReceived?.Invoke(this, reading);
            }

            _buffer.Clear();
            _buffer.Append(text);
        }
    }
}
