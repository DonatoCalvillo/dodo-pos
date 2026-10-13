using System.Drawing.Printing;
using FerreSys.Application;
using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI;

internal sealed class ReorderSuggestionPrintDocument : PrintDocument
{
    private readonly IReadOnlyList<ReorderSuggestionLine> _lines;
    private readonly DateTime _generatedAt;
    private int _rowIndex;
    private int _pageNumber;
    private int _totalPages = 1;

    private static readonly Font TitleFont = new("Tahoma", 14f, FontStyle.Bold);
    private static readonly Font SubtitleFont = new("Tahoma", 9f, FontStyle.Regular);
    private static readonly Font HeaderFont = new("Tahoma", 8.5f, FontStyle.Bold);
    private static readonly Font CellFont = new("Tahoma", 8.25f, FontStyle.Regular);
    private static readonly Font FooterFont = new("Tahoma", 8f, FontStyle.Regular);

    public ReorderSuggestionPrintDocument(ReorderSuggestionsSummary summary)
    {
        _lines = summary.Lines;
        _generatedAt = DateTime.Now;
        DocumentName = "Posly — Sugerido de Compra";
        DefaultPageSettings.Margins = new Margins(50, 50, 50, 55);
    }

    protected override void OnBeginPrint(PrintEventArgs e)
    {
        base.OnBeginPrint(e);
        _rowIndex = 0;
        _pageNumber = 0;
        _totalPages = EstimateTotalPages();
    }

    protected override void OnPrintPage(PrintPageEventArgs e)
    {
        base.OnPrintPage(e);
        if (e.Graphics is null) return;

        var g = e.Graphics;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        Rectangle margin = e.MarginBounds;
        float y = margin.Top;
        _pageNumber++;

        y = DrawHeader(g, margin, y);
        y = DrawColumnHeaders(g, margin, y);

        float rowHeight = 20f;
        float footerReserved = 48f;
        float bottomLimit = margin.Bottom - footerReserved;

        int startRow = _rowIndex;
        while (_rowIndex < _lines.Count)
        {
            if (y + rowHeight > bottomLimit)
                break;

            bool zebra = (_rowIndex - startRow) % 2 == 1;
            DrawRow(g, margin, y, rowHeight, _lines[_rowIndex], zebra);
            y += rowHeight;
            _rowIndex++;
        }

        DrawFooter(g, margin, e);

        e.HasMorePages = _rowIndex < _lines.Count;
        if (!e.HasMorePages)
            _rowIndex = 0;
    }

    private float DrawHeader(Graphics g, Rectangle margin, float y)
    {
        using var accent = new SolidBrush(Color.FromArgb(0, 70, 180));
        using var titleBrush = new SolidBrush(Color.FromArgb(30, 30, 30));
        using var muted = new SolidBrush(Color.FromArgb(90, 90, 90));

        g.FillRectangle(accent, margin.Left, y, 4, 36);
        g.DrawString("Posly — Sugerido de Compra", TitleFont, titleBrush, margin.Left + 12, y);
        y += 22;

        string meta =
            $"Generado: {_generatedAt:dd/MM/yyyy HH:mm}   ·   " +
            $"{_lines.Count} producto(s)   ·   " +
            $"Agotados: {_lines.Count(l => l.IsOutOfStock)}   ·   " +
            $"Bajo mínimo: {_lines.Count(l => !l.IsOutOfStock)}";
        g.DrawString(meta, SubtitleFont, muted, margin.Left + 12, y);
        y += 20;

        using var linePen = new Pen(Color.FromArgb(0, 70, 180), 1.5f);
        g.DrawLine(linePen, margin.Left, y, margin.Right, y);
        return y + 10;
    }

    private static float DrawColumnHeaders(Graphics g, Rectangle margin, float y)
    {
        float[] widths = ColumnWidths(margin.Width);
        string[] headers = ["Código", "Producto", "Unidad", "Stock", "Mínimo", "Deseado", "A pedir"];

        using var headerBg = new SolidBrush(Color.FromArgb(0, 70, 180));
        using var headerFg = new SolidBrush(Color.White);
        g.FillRectangle(headerBg, margin.Left, y, margin.Width, 22);

        float x = margin.Left;
        for (int i = 0; i < headers.Length; i++)
        {
            var align = i >= 3
                ? new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center }
                : new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
            var rect = new RectangleF(x + 4, y, widths[i] - 8, 22);
            g.DrawString(headers[i], HeaderFont, headerFg, rect, align);
            x += widths[i];
        }

        return y + 24;
    }

    private static void DrawRow(Graphics g, Rectangle margin, float y, float rowHeight, ReorderSuggestionLine line, bool zebra)
    {
        if (zebra)
        {
            using var zebraBrush = new SolidBrush(Color.FromArgb(245, 248, 252));
            g.FillRectangle(zebraBrush, margin.Left, y, margin.Width, rowHeight);
        }

        if (line.IsOutOfStock)
        {
            using var danger = new SolidBrush(Color.FromArgb(40, 220, 80, 80));
            g.FillRectangle(danger, margin.Left, y, 4, rowHeight);
        }
        else if (line.AlertLevel == StockAlertLevel.Warning)
        {
            using var warn = new SolidBrush(Color.FromArgb(50, 230, 180, 40));
            g.FillRectangle(warn, margin.Left, y, 4, rowHeight);
        }

        float[] widths = ColumnWidths(margin.Width);
        string[] cells =
        [
            line.Barcode,
            line.Name,
            line.Unit.DisplayName(),
            line.Unit.FormatQuantity(line.Stock),
            line.Unit.FormatQuantity(line.MinStock),
            line.Unit.FormatQuantity(line.DesiredStock),
            line.Unit.FormatQuantity(line.SuggestedOrderQty)
        ];

        using var textBrush = new SolidBrush(Color.FromArgb(35, 35, 35));
        float x = margin.Left;
        for (int i = 0; i < cells.Length; i++)
        {
            var align = i >= 3
                ? new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter }
                : new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
            var rect = new RectangleF(x + 4, y, widths[i] - 8, rowHeight);
            g.DrawString(cells[i], CellFont, textBrush, rect, align);
            x += widths[i];
        }

        using var gridPen = new Pen(Color.FromArgb(220, 225, 230));
        g.DrawLine(gridPen, margin.Left, y + rowHeight, margin.Right, y + rowHeight);
    }

    private void DrawFooter(Graphics g, Rectangle margin, PrintPageEventArgs e)
    {
        float y = margin.Bottom - 36;
        using var linePen = new Pen(Color.FromArgb(180, 185, 190));
        g.DrawLine(linePen, margin.Left, y, margin.Right, y);
        y += 6;

        using var muted = new SolidBrush(Color.FromArgb(80, 80, 80));
        decimal totalAsk = _lines.Sum(l => l.SuggestedOrderQty);
        string totals = $"Total ítems: {_lines.Count}    ·    Suma a pedir: {totalAsk:N3}";
        g.DrawString(totals, FooterFont, muted, margin.Left, y);

        string page = $"Página {_pageNumber} de {_totalPages}";
        var pageSize = g.MeasureString(page, FooterFont);
        g.DrawString(page, FooterFont, muted, margin.Right - pageSize.Width, y);

        g.DrawString("Posly · Abastecimiento", FooterFont, muted, margin.Left, y + 14);
    }

    private int EstimateTotalPages()
    {
        if (_lines.Count == 0) return 1;

        // Approximate usable height for rows on letter with current margins/header/footer
        const float usableRowsHeight = 650f;
        const float rowHeight = 20f;
        int rowsPerPage = Math.Max(1, (int)(usableRowsHeight / rowHeight));
        return (int)Math.Ceiling(_lines.Count / (double)rowsPerPage);
    }

    private static float[] ColumnWidths(int totalWidth)
    {
        // Proportions: barcode, name, unit, stock, min, desired, ask
        float[] ratios = [0.14f, 0.34f, 0.10f, 0.10f, 0.10f, 0.11f, 0.11f];
        var widths = new float[ratios.Length];
        float used = 0;
        for (int i = 0; i < ratios.Length - 1; i++)
        {
            widths[i] = totalWidth * ratios[i];
            used += widths[i];
        }
        widths[^1] = totalWidth - used;
        return widths;
    }
}
