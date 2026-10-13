using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace FerreSys.Infrastructure.UI;

internal static class ProductImages
{
    public const int ThumbnailMaxEdge = 160;
    public const int RowHeight = 48;
    public const int ColumnWidth = 52;

    private static readonly Image PlaceholderImage = CreatePlaceholder();

    public static Image Placeholder => PlaceholderImage;

    public static void AddPhotoColumn(DataGridView grid)
    {
        grid.RowTemplate.Height = RowHeight;
        grid.Columns.Insert(0, new DataGridViewImageColumn
        {
            Name = "Foto",
            HeaderText = "Foto",
            ImageLayout = DataGridViewImageCellLayout.Zoom,
            Width = ColumnWidth,
            MinimumWidth = ColumnWidth,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            Resizable = DataGridViewTriState.False
        });
    }

    public static Image ForGrid(byte[]? jpeg) =>
        jpeg is { Length: > 0 } ? Decode(jpeg) : PlaceholderImage;

    public static byte[] EncodeThumbnail(string filePath)
    {
        using var original = Image.FromFile(filePath);
        int width = original.Width;
        int height = original.Height;
        if (width < 1 || height < 1)
            throw new ArgumentException("La imagen no tiene un tamaño válido.");

        if (width > ThumbnailMaxEdge || height > ThumbnailMaxEdge)
        {
            float scale = Math.Min(ThumbnailMaxEdge / (float)width, ThumbnailMaxEdge / (float)height);
            width = Math.Max(1, (int)Math.Round(width * scale));
            height = Math.Max(1, (int)Math.Round(height * scale));
        }

        using var bitmap = new Bitmap(width, height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.Clear(Color.White);
            graphics.DrawImage(original, 0, 0, width, height);
        }

        using var stream = new MemoryStream();
        ImageCodecInfo codec = ImageCodecInfo.GetImageEncoders()
            .First(encoder => encoder.FormatID == ImageFormat.Jpeg.Guid);
        using var parameters = new EncoderParameters(1);
        parameters.Param[0] = new EncoderParameter(Encoder.Quality, 75L);
        bitmap.Save(stream, codec, parameters);
        return stream.ToArray();
    }

    public static void ClearRows(DataGridView grid)
    {
        if (grid.Columns.Contains("Foto"))
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.Cells["Foto"].Value is Image image && !ReferenceEquals(image, PlaceholderImage))
                    image.Dispose();
            }
        }

        grid.Rows.Clear();
    }

    private static Image Decode(byte[] jpeg)
    {
        using var stream = new MemoryStream(jpeg);
        using var decoded = Image.FromStream(stream);
        return new Bitmap(decoded);
    }

    private static Image CreatePlaceholder()
    {
        var bitmap = new Bitmap(RowHeight, RowHeight);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.FromArgb(230, 230, 230));
        using var pen = new Pen(Color.FromArgb(180, 180, 180));
        graphics.DrawRectangle(pen, 0, 0, bitmap.Width - 1, bitmap.Height - 1);
        using var font = new Font("Segoe UI", 18, FontStyle.Bold, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(Color.FromArgb(120, 120, 120));
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        graphics.DrawString("?", font, brush, new RectangleF(0, 0, bitmap.Width, bitmap.Height), format);
        return bitmap;
    }
}
