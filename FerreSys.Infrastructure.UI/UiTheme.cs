namespace FerreSys.Infrastructure.UI;

internal static class UiTheme
{
    public static readonly Color WindowBg = SystemColors.Control;
    public static readonly Color PanelBg = SystemColors.Control;
    public static readonly Color GroupTitle = Color.FromArgb(0, 70, 180);
    public static readonly Color Text = SystemColors.ControlText;
    public static readonly Color Total = Color.FromArgb(0, 60, 140);
    public static readonly Color StatusOk = Color.FromArgb(0, 140, 40);

    public static readonly Font Ui = new("Tahoma", 8.25f);
    public static readonly Font UiBold = new("Tahoma", 8.25f, FontStyle.Bold);
    public static readonly Font GroupFont = new("Tahoma", 8.25f, FontStyle.Bold);
    public static readonly Font Toolbar = new("Tahoma", 8.25f);
    public static readonly Font Search = new("Tahoma", 11f);
    public static readonly Font TotalFont = new("Tahoma", 16f, FontStyle.Bold);
}

internal static class UiIcons
{
    public static Bitmap Sales(int size = 24) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(60, 60, 60), 1.6f);
        using var brush = new SolidBrush(Color.FromArgb(70, 130, 200));
        g.FillRectangle(brush, 5, 4, 14, 16);
        g.DrawRectangle(pen, 5, 4, 14, 16);
        g.DrawLine(pen, 8, 8, 16, 8);
        g.DrawLine(pen, 8, 12, 16, 12);
        g.DrawLine(pen, 8, 16, 13, 16);
    });

    public static Bitmap Import(int size = 24) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(60, 60, 60), 1.6f);
        using var sheet = new SolidBrush(Color.White);
        using var arrow = new SolidBrush(Color.FromArgb(40, 150, 70));
        g.FillRectangle(sheet, 6, 3, 11, 15);
        g.DrawRectangle(pen, 6, 3, 11, 15);
        Point[] pts = [new(12, 20), new(8, 14), new(16, 14)];
        g.FillPolygon(arrow, pts);
    });

    public static Bitmap AddProduct(int size = 24) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(60, 60, 60), 1.6f);
        using var box = new SolidBrush(Color.FromArgb(220, 180, 80));
        g.FillRectangle(box, 4, 8, 16, 12);
        g.DrawRectangle(pen, 4, 8, 16, 12);
        g.DrawLine(pen, 4, 12, 20, 12);
        using var plus = new Pen(Color.FromArgb(0, 100, 180), 2.2f);
        g.DrawLine(plus, 18, 4, 18, 10);
        g.DrawLine(plus, 15, 7, 21, 7);
    });

    public static Bitmap Plus(int size = 20) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(0, 90, 160), 2.4f);
        g.DrawLine(pen, 10, 4, 10, 16);
        g.DrawLine(pen, 4, 10, 16, 10);
    });

    public static Bitmap Minus(int size = 20) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(120, 50, 50), 2.4f);
        g.DrawLine(pen, 4, 10, 16, 10);
    });

    public static Bitmap Clear(int size = 20) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(150, 40, 40), 2f);
        g.DrawLine(pen, 5, 5, 15, 15);
        g.DrawLine(pen, 15, 5, 5, 15);
    });

    public static Bitmap Checkout(int size = 22) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(60, 60, 60), 1.5f);
        using var card = new SolidBrush(Color.FromArgb(70, 130, 200));
        g.FillRectangle(card, 3, 6, 16, 11);
        g.DrawRectangle(pen, 3, 6, 16, 11);
        using var stripe = new SolidBrush(Color.FromArgb(40, 40, 40));
        g.FillRectangle(stripe, 3, 8, 16, 3);
    });

    public static Bitmap StatusDot(Color color, int size = 12) => Draw(size, g =>
    {
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 1, 1, size - 3, size - 3);
        using var pen = new Pen(Color.FromArgb(80, 80, 80), 1f);
        g.DrawEllipse(pen, 1, 1, size - 3, size - 3);
    });

    private static Bitmap Draw(int size, Action<Graphics> paint)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        paint(g);
        return bmp;
    }
}
