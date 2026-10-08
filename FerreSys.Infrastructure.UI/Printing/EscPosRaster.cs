using System.Drawing.Imaging;
using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI.Printing;

internal static class EscPosRaster
{
    public static byte[]? TryEncode(string? path, TicketPaperWidth paper)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        try
        {
            return Encode(path, paper);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static byte[] Encode(string path, TicketPaperWidth paper)
    {
        int maxDots = paper == TicketPaperWidth.Mm80 ? 576 : 384;
        using var stream = new MemoryStream(File.ReadAllBytes(path));
        using var source = Image.FromStream(stream);

        int targetWidth = Math.Min(maxDots, Math.Max(8, source.Width));
        int targetHeight = Math.Max(1, source.Height * targetWidth / Math.Max(1, source.Width));
        if (targetHeight > 240)
        {
            targetHeight = 240;
            targetWidth = Math.Max(8, source.Width * targetHeight / Math.Max(1, source.Height));
            targetWidth = Math.Min(maxDots, targetWidth);
        }

        targetWidth = Math.Max(8, targetWidth / 8 * 8);

        using var bmp = new Bitmap(targetWidth, targetHeight, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(bmp))
        {
            graphics.Clear(Color.White);
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(source, new Rectangle(0, 0, targetWidth, targetHeight));
        }

        int bytesPerRow = targetWidth / 8;
        var data = new byte[bytesPerRow * targetHeight];
        for (int y = 0; y < targetHeight; y++)
        {
            for (int x = 0; x < targetWidth; x++)
            {
                Color color = bmp.GetPixel(x, y);
                int luminance = (color.R * 299 + color.G * 587 + color.B * 114) / 1000;
                if (luminance < 160)
                    data[y * bytesPerRow + (x / 8)] |= (byte)(0x80 >> (x % 8));
            }
        }

        byte xL = (byte)(bytesPerRow & 0xFF);
        byte xH = (byte)((bytesPerRow >> 8) & 0xFF);
        byte yL = (byte)(targetHeight & 0xFF);
        byte yH = (byte)((targetHeight >> 8) & 0xFF);
        var result = new byte[8 + data.Length];
        result[0] = 0x1D;
        result[1] = 0x76;
        result[2] = 0x30;
        result[3] = 0x00;
        result[4] = xL;
        result[5] = xH;
        result[6] = yL;
        result[7] = yH;
        Buffer.BlockCopy(data, 0, result, 8, data.Length);
        return result;
    }
}
