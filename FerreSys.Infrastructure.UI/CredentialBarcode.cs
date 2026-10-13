using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ZXing;
using ZXing.Common;

namespace FerreSys.Infrastructure.UI;

internal static class CredentialBarcode
{
    public static byte[] Png(string content)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.CODE_128,
            Options = new EncodingOptions
            {
                Width = 380,
                Height = 90,
                Margin = 0,
                PureBarcode = false
            }
        };

        var pixels = writer.Write(content);
        using var bitmap = new System.Drawing.Bitmap(pixels.Width, pixels.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        System.Drawing.Imaging.BitmapData data = bitmap.LockBits(
            new Rectangle(0, 0, pixels.Width, pixels.Height),
            ImageLockMode.WriteOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        int sourceStride = pixels.Width * 4;
        for (int y = 0; y < pixels.Height; y++)
        {
            Marshal.Copy(
                pixels.Pixels,
                y * sourceStride,
                IntPtr.Add(data.Scan0, y * data.Stride),
                sourceStride);
        }
        bitmap.UnlockBits(data);

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
}
