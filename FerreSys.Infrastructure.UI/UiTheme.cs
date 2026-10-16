using System.Drawing.Drawing2D;

namespace FerreSys.Infrastructure.UI;

internal static class UiTheme
{
    public static readonly Color WindowBg = Color.FromArgb(241, 245, 249);      // Slate 100
    public static readonly Color PanelBg = Color.FromArgb(255, 255, 255);       // Pure White Card
    public static readonly Color HeaderBg = Color.FromArgb(15, 23, 42);         // Slate 900
    public static readonly Color BrandPrimary = Color.FromArgb(0, 149, 217);    // Posly Cyan
    public static readonly Color BrandPrimaryHover = Color.FromArgb(2, 132, 199);
    public static readonly Color GroupTitle = Color.FromArgb(15, 23, 42);       // Slate 900
    public static readonly Color Text = Color.FromArgb(30, 41, 59);             // Slate 800
    public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);     // Slate 500
    public static readonly Color Border = Color.FromArgb(226, 232, 240);        // Slate 200
    public static readonly Color Total = Color.FromArgb(56, 189, 248);          // Sky 400 (for dark display)
    public static readonly Color CheckoutGreen = Color.FromArgb(16, 185, 129);  // Emerald 500
    public static readonly Color CheckoutGreenHover = Color.FromArgb(5, 150, 105);
    public static readonly Color StatusOk = Color.FromArgb(22, 163, 74);
    public static readonly Color StatusWarn = Color.FromArgb(217, 119, 6);
    public static readonly Color StatusDanger = Color.FromArgb(220, 38, 38);
    public static readonly Color StockWarn = Color.FromArgb(254, 249, 195);
    public static readonly Color StockDanger = Color.FromArgb(254, 226, 226);

    public static readonly Font Ui = new("Segoe UI", 9.25f, FontStyle.Regular);
    public static readonly Font UiBold = new("Segoe UI Semibold", 9.25f, FontStyle.Bold);
    public static readonly Font GroupFont = new("Segoe UI Semibold", 9.75f, FontStyle.Bold);
    public static readonly Font Toolbar = new("Segoe UI Semibold", 9f, FontStyle.Bold);
    public static readonly Font Search = new("Segoe UI", 12f, FontStyle.Regular);
    public static readonly Font TotalFont = new("Segoe UI", 26f, FontStyle.Bold);
}

internal static class UiIcons
{
    public static Bitmap Sales(int size = 24) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(2, 132, 199), 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var brush = new SolidBrush(Color.FromArgb(224, 242, 254));
        g.FillRectangle(brush, 5, 3, 14, 18);
        g.DrawRectangle(pen, 5, 3, 14, 18);
        g.DrawLine(pen, 8, 8, 16, 8);
        g.DrawLine(pen, 8, 12, 16, 12);
        g.DrawLine(pen, 8, 16, 13, 16);
    });

    public static Bitmap Import(int size = 24) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(71, 85, 105), 1.6f);
        using var sheet = new SolidBrush(Color.FromArgb(241, 245, 249));
        using var arrow = new SolidBrush(Color.FromArgb(22, 163, 74));
        g.FillRectangle(sheet, 6, 3, 11, 15);
        g.DrawRectangle(pen, 6, 3, 11, 15);
        Point[] pts = [new(12, 20), new(8, 14), new(16, 14)];
        g.FillPolygon(arrow, pts);
    });

    public static Bitmap AddProduct(int size = 24) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(71, 85, 105), 1.6f);
        using var box = new SolidBrush(Color.FromArgb(254, 243, 199));
        g.FillRectangle(box, 4, 8, 16, 12);
        g.DrawRectangle(pen, 4, 8, 16, 12);
        g.DrawLine(pen, 4, 12, 20, 12);
        using var plus = new Pen(Color.FromArgb(2, 132, 199), 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(plus, 18, 4, 18, 10);
        g.DrawLine(plus, 15, 7, 21, 7);
    });

    public static Bitmap Plus(int size = 20) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(2, 132, 199), 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(pen, 10, 4, 10, 16);
        g.DrawLine(pen, 4, 10, 16, 10);
    });

    public static Bitmap Minus(int size = 20) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(71, 85, 105), 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(pen, 4, 10, 16, 10);
    });

    public static Bitmap Clear(int size = 20) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(220, 38, 38), 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(pen, 5, 5, 15, 15);
        g.DrawLine(pen, 15, 5, 5, 15);
    });

    public static Bitmap NewSale(int size = 20) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(2, 132, 199), 1.6f);
        using var back = new SolidBrush(Color.FromArgb(224, 242, 254));
        g.FillRectangle(back, 4, 4, 12, 12);
        g.DrawRectangle(pen, 4, 4, 12, 12);
        using var plus = new Pen(Color.FromArgb(22, 163, 74), 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(plus, 10, 7, 10, 13);
        g.DrawLine(plus, 7, 10, 13, 10);
    });

    public static Bitmap CloseSale(int size = 20) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(100, 116, 139), 1.6f);
        g.DrawRectangle(pen, 4, 4, 12, 12);
        using var x = new Pen(Color.FromArgb(220, 38, 38), 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(x, 7, 7, 13, 13);
        g.DrawLine(x, 13, 7, 7, 13);
    });

    public static Bitmap CashClose(int size = 24) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(22, 163, 74), 1.6f);
        using var body = new SolidBrush(Color.FromArgb(220, 252, 231));
        g.FillRectangle(body, 3, 9, 18, 11);
        g.DrawRectangle(pen, 3, 9, 18, 11);
        g.DrawLine(pen, 3, 14, 21, 14);
        using var coin = new SolidBrush(Color.FromArgb(250, 204, 21));
        g.FillEllipse(coin, 8, 2, 8, 8);
        g.DrawEllipse(pen, 8, 2, 8, 8);
    });

    public static Bitmap CashMovements(int size = 24) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(100, 116, 139), 1.5f);
        using var inArrow = new Pen(Color.FromArgb(22, 163, 74), 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var outArrow = new Pen(Color.FromArgb(220, 38, 38), 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(inArrow, 4, 7, 11, 7);
        g.DrawLine(inArrow, 8, 4, 11, 7);
        g.DrawLine(inArrow, 8, 10, 11, 7);
        g.DrawLine(outArrow, 13, 17, 20, 17);
        g.DrawLine(outArrow, 17, 14, 20, 17);
        g.DrawLine(outArrow, 17, 20, 20, 17);
        g.DrawRectangle(pen, 3, 3, 18, 18);
    });

    public static Bitmap Kardex(int size = 24) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(71, 85, 105), 1.6f);
        using var paper = new SolidBrush(Color.FromArgb(241, 245, 249));
        g.FillRectangle(paper, 4, 3, 16, 18);
        g.DrawRectangle(pen, 4, 3, 16, 18);
        g.DrawLine(pen, 7, 7, 17, 7);
        g.DrawLine(pen, 7, 11, 17, 11);
        g.DrawLine(pen, 7, 15, 14, 15);
        using var mark = new SolidBrush(Color.FromArgb(0, 149, 217));
        g.FillRectangle(mark, 7, 17, 4, 2);
    });

    public static Bitmap GrossProfit(int size = 24) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(71, 85, 105), 1.6f);
        using var bar = new SolidBrush(Color.FromArgb(16, 185, 129));
        g.FillRectangle(bar, 5, 14, 4, 6);
        g.FillRectangle(bar, 11, 10, 4, 10);
        g.FillRectangle(bar, 17, 5, 4, 15);
        g.DrawLine(pen, 3, 20, 21, 20);
        g.DrawLine(pen, 3, 4, 3, 20);
    });

    public static Bitmap Reorder(int size = 24) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(71, 85, 105), 1.5f);
        using var box = new SolidBrush(Color.FromArgb(254, 243, 199));
        g.FillRectangle(box, 4, 10, 16, 10);
        g.DrawRectangle(pen, 4, 10, 16, 10);
        g.DrawLine(pen, 4, 14, 20, 14);
        using var arrow = new Pen(Color.FromArgb(0, 149, 217), 2.1f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(arrow, 12, 3, 12, 9);
        g.DrawLine(arrow, 9, 6, 12, 9);
        g.DrawLine(arrow, 15, 6, 12, 9);
    });

    public static Bitmap Checkout(int size = 22) => Draw(size, g =>
    {
        using var pen = new Pen(Color.White, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawRectangle(pen, 2, 5, 18, 12);
        g.DrawLine(pen, 2, 9, 20, 9);
        g.DrawLine(pen, 5, 13, 9, 13);
    });

    public static Bitmap Business(int size = 24) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(71, 85, 105), 1.5f);
        using var roof = new SolidBrush(Color.FromArgb(0, 149, 217));
        using var wall = new SolidBrush(Color.FromArgb(241, 245, 249));
        Point[] roofPts = [new(3, 11), new(12, 3), new(21, 11)];
        g.FillPolygon(roof, roofPts);
        g.DrawPolygon(pen, roofPts);
        g.FillRectangle(wall, 6, 11, 12, 9);
        g.DrawRectangle(pen, 6, 11, 12, 9);
        using var door = new SolidBrush(Color.FromArgb(71, 85, 105));
        g.FillRectangle(door, 10, 14, 4, 6);
    });

    public static Bitmap Printer(int size = 24) => Draw(size, g =>
    {
        using var pen = new Pen(Color.FromArgb(71, 85, 105), 1.5f);
        using var body = new SolidBrush(Color.FromArgb(224, 242, 254));
        g.FillRectangle(body, 4, 6, 16, 10);
        g.DrawRectangle(pen, 4, 6, 16, 10);
        g.DrawRectangle(pen, 7, 3, 10, 4);
        g.DrawLine(pen, 6, 18, 18, 18);
        g.DrawLine(pen, 8, 16, 8, 18);
        g.DrawLine(pen, 16, 16, 16, 18);
    });

    public static Bitmap StatusDot(Color color, int size = 12) => Draw(size, g =>
    {
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 2, 2, size - 4, size - 4);
    });

    private static Bitmap Draw(int size, Action<Graphics> paint)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        paint(g);
        return bmp;
    }
}
