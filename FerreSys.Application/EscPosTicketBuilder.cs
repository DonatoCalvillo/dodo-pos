using System.Text;

namespace FerreSys.Application;

public static class EscPosTicketBuilder
{
    private static readonly Encoding Latin1 = Encoding.Latin1;

    public static byte[] Build(string ticketText, bool openDrawer, int drawerPin) =>
        Build(
            new TicketDocument { Pieces = [TicketPiece.Lines(ticketText)] },
            logoRaster: null,
            openDrawer,
            drawerPin);

    public static byte[] Build(TicketDocument document, byte[]? logoRaster, bool openDrawer, int drawerPin)
    {
        ArgumentNullException.ThrowIfNull(document);
        using var stream = new MemoryStream();
        stream.WriteByte(0x1B);
        stream.WriteByte(0x40);
        stream.Write([0x1B, 0x74, 0x10]);

        bool header = true;
        foreach (TicketPiece piece in document.Pieces)
        {
            if (piece.IsLogo)
            {
                if (logoRaster is { Length: > 0 })
                {
                    stream.Write([0x1B, 0x61, 0x01]);
                    stream.Write(logoRaster);
                    stream.WriteByte(0x0A);
                }

                continue;
            }

            if (!string.IsNullOrEmpty(piece.Text))
                WriteLines(stream, piece.Text, ref header);
        }

        if (openDrawer)
            stream.Write(DrawerPulse(drawerPin));

        stream.Write([0x1B, 0x64, 0x03]);
        stream.Write([0x1D, 0x56, 0x42, 0x00]);

        return stream.ToArray();
    }

    public static byte[] DrawerPulse(int drawerPin)
    {
        byte pin = drawerPin == 1 ? (byte)1 : (byte)0;
        return [0x1B, 0x70, pin, 0x19, 0xFA];
    }

    private static void WriteLines(Stream stream, string ticketText, ref bool header)
    {
        string[] lines = ticketText.Replace("\r\n", "\n").Split('\n');
        foreach (string raw in lines)
        {
            if (raw.Length == 0)
                continue;

            if (header && raw.StartsWith('-'))
            {
                header = false;
                stream.Write([0x1B, 0x45, 0x00]);
                stream.Write([0x1B, 0x61, 0x00]);
            }
            else if (header)
            {
                stream.Write([0x1B, 0x61, 0x01]);
                stream.Write([0x1B, 0x45, 0x01]);
            }
            else if (raw.StartsWith('='))
            {
                stream.Write([0x1B, 0x61, 0x01]);
                stream.Write([0x1B, 0x45, 0x00]);
            }

            stream.Write(Latin1.GetBytes(raw));
            stream.WriteByte(0x0A);
        }
    }
}
