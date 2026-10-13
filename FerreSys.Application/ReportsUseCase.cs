using FerreSys.Domain;

namespace FerreSys.Application;

public class ReportsUseCase
{
    public const int MaxRangeDays = 366;

    private readonly IReportReader _reader;

    public ReportsUseCase(IReportReader reader)
    {
        _reader = reader;
    }

    public SalesSummaryReport SalesSummary(DateTime start, DateTime end, ReportGrouping grouping)
    {
        (start, end) = Normalize(start, end);
        return _reader.GetSalesSummary(start, end, grouping);
    }

    public SalesDetailReport SalesDetail(DateTime start, DateTime end, ReportGrouping grouping)
    {
        (start, end) = Normalize(start, end);
        return _reader.GetSalesDetail(start, end, grouping);
    }

    public IReadOnlyList<CashCloseReportDay> CashClose(DateTime start, DateTime end)
    {
        (start, end) = Normalize(start, end);
        return _reader.GetCashClose(start, end);
    }

    public InventoryReport Inventory() => _reader.GetInventory();

    public BestSellersReport BestSellers(
        DateTime start,
        DateTime end,
        int topN,
        BestSellerRankBy rankBy)
    {
        (start, end) = Normalize(start, end);
        return _reader.GetBestSellers(start, end, topN, rankBy);
    }

    private static (DateTime Start, DateTime End) Normalize(DateTime start, DateTime end)
    {
        start = start.Date;
        end = end.Date;
        if (end < start)
            throw new ArgumentException("La fecha final no puede ser anterior a la inicial.");

        if ((end - start).TotalDays > MaxRangeDays)
            throw new ArgumentException($"El periodo no puede ser mayor a {MaxRangeDays} días.");

        return (start, end);
    }
}
