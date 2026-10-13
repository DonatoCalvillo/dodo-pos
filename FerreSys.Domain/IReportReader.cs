namespace FerreSys.Domain;

public enum ReportGrouping
{
    Day,
    Week,
    Month
}

public enum BestSellerRankBy
{
    Quantity,
    Amount,
    Profit
}

public enum InventoryValueMetric
{
    Cost,
    Retail
}

public sealed class SalesSeriesPoint
{
    public string Bucket { get; init; } = string.Empty;
    public DateTime BucketStart { get; init; }
    public int Tickets { get; init; }
    public decimal Gross { get; init; }
    public decimal Discounts { get; init; }
    public decimal Returns { get; init; }
    public decimal Net { get; init; }
    public decimal Cash { get; init; }
    public decimal Card { get; init; }
}

public sealed class SalesSummaryReport
{
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
    public int Tickets { get; init; }
    public decimal Gross { get; init; }
    public decimal Discounts { get; init; }
    public decimal Returns { get; init; }
    public decimal Net { get; init; }
    public decimal Cash { get; init; }
    public decimal Card { get; init; }
    public int MixedTickets { get; init; }
    public decimal MixedAmount { get; init; }
    public IReadOnlyList<SalesSeriesPoint> Series { get; init; } = [];
}

public sealed class SalesDetailLine
{
    public string Ticket { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
    public string Barcode { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Amount { get; init; }
    public decimal ReturnedQuantity { get; init; }
    public decimal ReturnedAmount { get; init; }
    public bool SaleOutsidePeriod { get; init; }
}

public sealed class SalesDetailReport
{
    public required SalesSummaryReport Summary { get; init; }
    public IReadOnlyList<SalesDetailLine> Lines { get; init; } = [];
}

public sealed class CashCloseReportDay
{
    public DateTime BusinessDate { get; init; }
    public decimal OpeningCash { get; init; }
    public decimal CashSales { get; init; }
    public decimal CardSales { get; init; }
    public decimal Entries { get; init; }
    public decimal Exits { get; init; }
    public decimal ReturnExits { get; init; }
    public decimal ExpectedCash { get; init; }
    public decimal? CountedCash { get; init; }
    public decimal? Difference { get; init; }
    public bool IsClosed { get; init; }
}

public sealed class InventoryReportLine
{
    public string Barcode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public decimal Stock { get; init; }
    public decimal MinStock { get; init; }
    public decimal DesiredStock { get; init; }
    public decimal CostPrice { get; init; }
    public decimal RetailPrice { get; init; }
    public decimal CostValue { get; init; }
    public decimal RetailValue { get; init; }
    public string Status { get; init; } = string.Empty;
}

public sealed class InventoryReport
{
    public int OkCount { get; init; }
    public int BelowMinimumCount { get; init; }
    public int OutOfStockCount { get; init; }
    public decimal CostValue { get; init; }
    public decimal RetailValue { get; init; }
    public IReadOnlyList<InventoryReportLine> Lines { get; init; } = [];
}

public sealed class BestSellerLine
{
    public string Barcode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal Amount { get; init; }
    public decimal Profit { get; init; }
}

public sealed class BestSellersReport
{
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
    public BestSellerRankBy RankBy { get; init; }
    public IReadOnlyList<BestSellerLine> Lines { get; init; } = [];
}

public interface IReportReader
{
    SalesSummaryReport GetSalesSummary(DateTime start, DateTime end, ReportGrouping grouping);
    SalesDetailReport GetSalesDetail(DateTime start, DateTime end, ReportGrouping grouping);
    IReadOnlyList<CashCloseReportDay> GetCashClose(DateTime start, DateTime end);
    InventoryReport GetInventory();
    BestSellersReport GetBestSellers(DateTime start, DateTime end, int topN, BestSellerRankBy rankBy);
}
