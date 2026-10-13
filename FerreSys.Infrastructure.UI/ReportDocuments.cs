using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI;

internal static class ReportDocuments
{
    public static ReportDocument SalesSummary(SalesSummaryReport report, IReadOnlySet<string> series) =>
        new()
        {
            FileName = "resumen-ventas",
            Title = "Resumen de ventas",
            Period = ReportText.Period(report.Start, report.End),
            Totals = SummaryTotals(report),
            Columns = ["Periodo", "Tickets", "Bruto", "Descuentos", "Devoluciones", "Neto", "Efectivo", "Tarjeta"],
            Rows = report.Series.Select(point => new[]
            {
                point.Bucket,
                point.Tickets.ToString(),
                ReportText.Money(point.Gross),
                ReportText.Money(point.Discounts),
                ReportText.Money(point.Returns),
                ReportText.Money(point.Net),
                ReportText.Money(point.Cash),
                ReportText.Money(point.Card)
            }).ToList(),
            Charts = ReportCharts.Sales(report, series)
        };

    public static ReportDocument SalesDetail(SalesDetailReport report, IReadOnlySet<string> series) =>
        new()
        {
            FileName = "resumen-ventas-detalle",
            Title = "Resumen de ventas con detalle",
            Period = ReportText.Period(report.Summary.Start, report.Summary.End),
            Totals = SummaryTotals(report.Summary),
            Columns = ["Ticket", "Fecha", "Código", "Producto", "Cantidad", "Precio", "Importe", "Cant. devuelta", "Importe devuelto"],
            Rows = report.Lines.Select(line => new[]
            {
                line.SaleOutsidePeriod ? line.Ticket + " *" : line.Ticket,
                ReportText.Day(line.OccurredAt),
                line.Barcode,
                line.ProductName,
                ReportText.Qty(line.Quantity),
                ReportText.Money(line.UnitPrice),
                ReportText.Money(line.Amount),
                ReportText.Qty(line.ReturnedQuantity),
                ReportText.Money(line.ReturnedAmount)
            }).ToList(),
            Charts = ReportCharts.Sales(report.Summary, series)
        };

    public static ReportDocument Cash(IReadOnlyList<CashCloseReportDay> days, DateTime start, DateTime end, ReportGrouping grouping, IReadOnlySet<string> series)
    {
        decimal cash = days.Sum(day => day.CashSales);
        decimal card = days.Sum(day => day.CardSales);
        decimal entries = days.Sum(day => day.Entries);
        decimal exits = days.Sum(day => day.Exits);
        decimal returns = days.Sum(day => day.ReturnExits);
        return new ReportDocument
        {
            FileName = "corte-de-caja",
            Title = "Corte de caja",
            Period = ReportText.Period(start, end),
            Totals =
            [
                ("Días", days.Count.ToString()),
                ("Ventas en efectivo", ReportText.Money(cash)),
                ("Ventas con tarjeta", ReportText.Money(card)),
                ("Entradas", ReportText.Money(entries)),
                ("Salidas", ReportText.Money(exits)),
                ("Salidas por devolución", ReportText.Money(returns))
            ],
            Columns = ["Fecha", "Apertura", "Efectivo", "Tarjeta", "Entradas", "Salidas", "Devoluciones", "Esperado", "Contado", "Diferencia", "Estado"],
            Rows = days.Select(day => new[]
            {
                ReportText.Date(day.BusinessDate),
                ReportText.Money(day.OpeningCash),
                ReportText.Money(day.CashSales),
                ReportText.Money(day.CardSales),
                ReportText.Money(day.Entries),
                ReportText.Money(day.Exits),
                ReportText.Money(day.ReturnExits),
                ReportText.Money(day.ExpectedCash),
                day.CountedCash is decimal counted ? ReportText.Money(counted) : "",
                day.Difference is decimal difference ? ReportText.Money(difference) : "",
                day.IsClosed ? "Cerrado" : "Abierto"
            }).ToList(),
            Charts = ReportCharts.Cash(days, grouping, series)
        };
    }

    public static ReportDocument Inventory(InventoryReport report, InventoryValueMetric metric, int topN, IReadOnlySet<string> series) =>
        new()
        {
            FileName = "inventario",
            Title = "Inventario",
            Period = "Existencia actual",
            Totals =
            [
                ("Productos", report.Lines.Count.ToString()),
                ("OK", report.OkCount.ToString()),
                ("Bajo mínimo", report.BelowMinimumCount.ToString()),
                ("En cero", report.OutOfStockCount.ToString()),
                ("Valor a costo", ReportText.Money(report.CostValue)),
                ("Valor a precio", ReportText.Money(report.RetailValue))
            ],
            Columns = ["Código", "Producto", "Unidad", "Existencia", "Mínimo", "Deseado", "Costo", "Precio", "Valor costo", "Valor precio", "Estado"],
            Rows = report.Lines.Select(line => new[]
            {
                line.Barcode,
                line.Name,
                line.Unit,
                ReportText.Qty(line.Stock),
                ReportText.Qty(line.MinStock),
                ReportText.Qty(line.DesiredStock),
                ReportText.Money(line.CostPrice),
                ReportText.Money(line.RetailPrice),
                ReportText.Money(line.CostValue),
                ReportText.Money(line.RetailValue),
                line.Status
            }).ToList(),
            Charts = ReportCharts.Inventory(report, metric, topN, series)
        };

    public static ReportDocument BestSellers(BestSellersReport report, IReadOnlySet<string> series) =>
        new()
        {
            FileName = "mas-vendidos",
            Title = "Productos más vendidos",
            Period = ReportText.Period(report.Start, report.End),
            Totals =
            [
                ("Productos", report.Lines.Count.ToString()),
                ("Cantidad neta", ReportText.Qty(report.Lines.Sum(line => line.Quantity))),
                ("Importe neto", ReportText.Money(report.Lines.Sum(line => line.Amount))),
                ("Utilidad neta", ReportText.Money(report.Lines.Sum(line => line.Profit)))
            ],
            Columns = ["Código", "Producto", "Cantidad neta", "Importe neto", "Utilidad neta"],
            Rows = report.Lines.Select(line => new[]
            {
                line.Barcode,
                line.Name,
                ReportText.Qty(line.Quantity),
                ReportText.Money(line.Amount),
                ReportText.Money(line.Profit)
            }).ToList(),
            Charts = ReportCharts.BestSellers(report, series)
        };

    private static IReadOnlyList<(string Label, string Value)> SummaryTotals(SalesSummaryReport report) =>
    [
        ("Tickets", report.Tickets.ToString()),
        ("Bruto", ReportText.Money(report.Gross)),
        ("Descuentos", ReportText.Money(report.Discounts)),
        ("Devoluciones", ReportText.Money(report.Returns)),
        ("Neto", ReportText.Money(report.Net)),
        ("Efectivo", ReportText.Money(report.Cash)),
        ("Tarjeta", ReportText.Money(report.Card)),
        ("Tickets mixtos", report.MixedTickets.ToString()),
        ("Importe mixto", ReportText.Money(report.MixedAmount))
    ];
}
