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

    public static Bitmap NewSale(int size = 20) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(60, 60, 60), 1.4f);
        using var back = new SolidBrush(Color.FromArgb(200, 210, 225));
        using var front = new SolidBrush(Color.FromArgb(70, 130, 200));
        g.FillRectangle(back, 3, 5, 10, 11);
        g.DrawRectangle(pen, 3, 5, 10, 11);
        g.FillRectangle(front, 7, 3, 10, 11);
        g.DrawRectangle(pen, 7, 3, 10, 11);
        using var plus = new Pen(Color.FromArgb(0, 120, 70), 2f);
        g.DrawLine(plus, 16, 14, 16, 19);
        g.DrawLine(plus, 13.5f, 16.5f, 18.5f, 16.5f);
    });

    public static Bitmap CloseSale(int size = 20) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(60, 60, 60), 1.4f);
        using var tab = new SolidBrush(Color.FromArgb(180, 190, 205));
        g.FillRectangle(tab, 3, 4, 11, 12);
        g.DrawRectangle(pen, 3, 4, 11, 12);
        g.DrawLine(pen, 3, 8, 14, 8);
        using var x = new Pen(Color.FromArgb(160, 50, 50), 2f);
        g.DrawLine(x, 12, 12, 18, 18);
        g.DrawLine(x, 18, 12, 12, 18);
    });

    public static Bitmap CashClose(int size = 24) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(60, 60, 60), 1.6f);
        using var body = new SolidBrush(Color.FromArgb(70, 140, 90));
        g.FillRectangle(body, 3, 9, 18, 11);
        g.DrawRectangle(pen, 3, 9, 18, 11);
        g.DrawLine(pen, 3, 14, 21, 14);
        using var coin = new SolidBrush(Color.FromArgb(220, 180, 60));
        g.FillEllipse(coin, 8, 2, 8, 8);
        g.DrawEllipse(pen, 8, 2, 8, 8);
    });

    public static Bitmap CashMovements(int size = 24) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(60, 60, 60), 1.5f);
        using var inArrow = new Pen(Color.FromArgb(40, 140, 70), 2.2f);
        using var outArrow = new Pen(Color.FromArgb(170, 60, 50), 2.2f);
        g.DrawLine(inArrow, 4, 7, 11, 7);
        g.DrawLine(inArrow, 8, 4, 11, 7);
        g.DrawLine(inArrow, 8, 10, 11, 7);
        g.DrawLine(outArrow, 13, 17, 20, 17);
        g.DrawLine(outArrow, 17, 14, 20, 17);
        g.DrawLine(outArrow, 17, 20, 20, 17);
        g.DrawRectangle(pen, 3, 3, 18, 18);
    });

    public static Bitmap GrossProfit(int size = 24) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(60, 60, 60), 1.6f);
        using var bar = new SolidBrush(Color.FromArgb(40, 140, 70));
        g.FillRectangle(bar, 5, 14, 4, 6);
        g.FillRectangle(bar, 11, 10, 4, 10);
        g.FillRectangle(bar, 17, 5, 4, 15);
        g.DrawLine(pen, 3, 20, 21, 20);
        g.DrawLine(pen, 3, 4, 3, 20);
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
