namespace FerreSys.Infrastructure.UI;

internal static class Branding
{
    private static readonly Stream IconStream = Open("FerreSys.Infrastructure.UI.Assets.dodo.ico");

    public static Image Logo { get; } = LoadLogo();
    public static Image Mark { get; } = Scale(Logo, 16);
    public static Icon AppIcon { get; } = new Icon(IconStream);

    private static Image LoadLogo()
    {
        using Stream stream = Open("FerreSys.Infrastructure.UI.Assets.dodo_dark.png");
        using var source = Image.FromStream(stream);
        return new Bitmap(source);
    }

    private static Bitmap Scale(Image source, int size)
    {
        var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        graphics.DrawImage(source, 0, 0, size, size);
        return bitmap;
    }

    private static Stream Open(string name)
    {
        Stream? stream = typeof(Branding).Assembly.GetManifestResourceStream(name);
        if (stream == null)
            throw new InvalidOperationException($"No se encontró el recurso {name}.");
        return stream;
    }
}
