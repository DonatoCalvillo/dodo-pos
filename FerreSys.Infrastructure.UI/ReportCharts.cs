using System.Globalization;
using FerreSys.Domain;
using ScottPlot;

namespace FerreSys.Infrastructure.UI;

internal static class ReportCharts
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-MX");

    public static IReadOnlyList<Plot> Sales(SalesSummaryReport report, IReadOnlySet<string> series)
    {
        var definitions = new (string Key, string Name, Func<SalesSeriesPoint, decimal> Value)[]
        {
            ("gross", "Bruto", point => point.Gross),
            ("discounts", "Descuentos", point => point.Discounts),
            ("returns", "Devoluciones", point => point.Returns),
            ("net", "Neto", point => point.Net),
            ("cash", "Efectivo", point => point.Cash),
            ("card", "Tarjeta", point => point.Card)
        };

        return [TimeLines(
            "Ventas del periodo",
            report.Series.Select(point => (point.BucketStart, point.Bucket)).ToList(),
            definitions
                .Where(item => series.Contains(item.Key))
                .Select(item => (item.Name, report.Series.Select(point => (double)item.Value(point)).ToArray()))
                .ToList())];
    }

    public static IReadOnlyList<Plot> Cash(IReadOnlyList<CashCloseReportDay> days, ReportGrouping grouping, IReadOnlySet<string> series)
    {
        var buckets = days
            .GroupBy(day => Bucket(day.BusinessDate, grouping))
            .OrderBy(group => group.Key.Start)
            .ToList();

        var definitions = new (string Key, string Name, Func<IGrouping<(DateTime Start, string Label), CashCloseReportDay>, decimal> Value)[]
        {
            ("opening", "Apertura", group => group.OrderBy(day => day.BusinessDate).First().OpeningCash),
            ("cashSales", "Ventas en efectivo", group => group.Sum(day => day.CashSales)),
            ("entries", "Entradas", group => group.Sum(day => day.Entries)),
            ("exits", "Salidas", group => group.Sum(day => day.Exits)),
            ("expected", "Esperado", group => group.OrderBy(day => day.BusinessDate).Last().ExpectedCash),
            ("counted", "Contado", group => group.OrderBy(day => day.BusinessDate).Last().CountedCash ?? 0m)
        };

        return [TimeLines(
            "Corte de caja",
            buckets.Select(group => (group.Key.Start, group.Key.Label)).ToList(),
            definitions
                .Where(item => series.Contains(item.Key))
                .Select(item => (item.Name, buckets.Select(group => (double)item.Value(group)).ToArray()))
                .ToList())];
    }

    public static IReadOnlyList<Plot> Inventory(InventoryReport report, InventoryValueMetric metric, int topN, IReadOnlySet<string> series)
    {
        var plots = new List<Plot>();
        if (series.Contains("status"))
        {
            plots.Add(Bars(
                "Estado del inventario",
                ["OK", "Bajo mínimo", "En cero"],
                [report.OkCount, report.BelowMinimumCount, report.OutOfStockCount]));
        }

        if (series.Contains("top"))
        {
            bool cost = metric == InventoryValueMetric.Cost;
            var top = report.Lines
                .OrderByDescending(line => cost ? line.CostValue : line.RetailValue)
                .Take(topN)
                .ToList();
            plots.Add(Bars(
                cost ? "Mayor valor a costo" : "Mayor valor a precio",
                top.Select(line => line.Name).ToArray(),
                top.Select(line => (double)(cost ? line.CostValue : line.RetailValue)).ToArray()));
        }

        return plots.Count == 0 ? [Empty("Inventario")] : plots;
    }

    public static IReadOnlyList<Plot> BestSellers(BestSellersReport report, IReadOnlySet<string> series)
    {
        var chosen = new (string Key, string Name, Func<BestSellerLine, decimal> Value)[]
        {
            ("quantity", "Cantidad", line => line.Quantity),
            ("amount", "Importe", line => line.Amount),
            ("profit", "Utilidad", line => line.Profit)
        }.Where(item => series.Contains(item.Key)).Take(2).ToList();

        if (report.Lines.Count == 0 || chosen.Count == 0)
            return [Empty("Productos más vendidos")];

        var plot = new Plot();
        plot.Title("Productos más vendidos");
        string[] labels = report.Lines.Select(line => line.Name).ToArray();
        double[] positions = Enumerable.Range(0, labels.Length).Select(index => (double)index).ToArray();
        var bars = plot.Add.Bars(positions, report.Lines.Select(line => (double)chosen[0].Value(line)).ToArray());
        bars.LegendText = chosen[0].Name;
        if (chosen.Count == 2)
        {
            var right = plot.Axes.AddRightAxis();
            var second = plot.Add.Scatter(
                positions,
                report.Lines.Select(line => (double)chosen[1].Value(line)).ToArray());
            second.LegendText = chosen[1].Name;
            second.Axes.YAxis = right;
        }

        ApplyCategoryTicks(plot, labels);
        plot.ShowLegend();
        return [plot];
    }

    public static byte[] Png(Plot plot)
    {
        plot.Font.Automatic();
        return plot.GetImageBytes(1100, 420, ImageFormat.Png);
    }

    private static Plot TimeLines(
        string title,
        IReadOnlyList<(DateTime Start, string Label)> axis,
        IReadOnlyList<(string Name, double[] Values)> series)
    {
        if (axis.Count == 0 || series.Count == 0)
            return Empty(title);

        var plot = new Plot();
        plot.Title(title);
        double[] xs = axis.Select(item => item.Start.ToOADate()).ToArray();
        foreach ((string name, double[] values) in series)
        {
            var line = plot.Add.Scatter(xs, values);
            line.LegendText = name;
        }

        plot.Axes.DateTimeTicksBottom();
        plot.ShowLegend();
        return plot;
    }

    private static Plot Bars(string title, string[] labels, double[] values)
    {
        var plot = new Plot();
        plot.Title(title);
        if (labels.Length == 0)
            return plot;

        double[] positions = Enumerable.Range(0, labels.Length).Select(index => (double)index).ToArray();
        plot.Add.Bars(positions, values);
        ApplyCategoryTicks(plot, labels);
        return plot;
    }

    private static void ApplyCategoryTicks(Plot plot, string[] labels)
    {
        var ticks = labels.Select((label, index) => new Tick(index, Trim(label))).ToArray();
        plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);
        plot.Axes.Bottom.TickLabelStyle.Rotation = labels.Length > 6 ? 35 : 0;
        plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleRight;
        plot.Axes.Margins(bottom: labels.Length > 6 ? 0.25 : 0.08, top: 0.15);
    }

    private static Plot Empty(string title)
    {
        var plot = new Plot();
        plot.Title(title);
        var text = plot.Add.Text("Sin datos para graficar", 0, 0);
        text.LabelAlignment = Alignment.MiddleCenter;
        return plot;
    }

    private static (DateTime Start, string Label) Bucket(DateTime date, ReportGrouping grouping)
    {
        if (grouping == ReportGrouping.Month)
            return (new DateTime(date.Year, date.Month, 1), date.ToString("yyyy-MM"));

        if (grouping == ReportGrouping.Week)
        {
            int diff = ((int)date.DayOfWeek + 6) % 7;
            DateTime start = date.Date.AddDays(-diff);
            return (start, start.ToString("dd/MM", Es));
        }

        return (date.Date, date.ToString("dd/MM/yyyy", Es));
    }

    private static string Trim(string label) => label.Length <= 22 ? label : label[..21] + "…";
}
