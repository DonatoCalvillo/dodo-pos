using FerreSys.Domain;

namespace FerreSys.Application;

public class GrossProfitSummary
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal TotalSold { get; init; }
    public decimal TotalCost { get; init; }

    public decimal NetProfit => TotalSold - TotalCost;

    public decimal MarginPercent =>
        TotalSold == 0m
            ? 0m
            : Math.Round(NetProfit / TotalSold * 100m, 2, MidpointRounding.AwayFromZero);
}

public class GetGrossProfitSummaryUseCase
{
    private readonly IProductRepository _productRepository;

    public GetGrossProfitSummaryUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public GrossProfitSummary Execute(DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date;
        var end = endDate.Date;
        if (end < start)
            (start, end) = (end, start);

        var totals = _productRepository.GetGrossProfitTotals(start, end);

        return new GrossProfitSummary
        {
            StartDate = start,
            EndDate = end,
            TotalSold = totals.TotalSold,
            TotalCost = totals.TotalCost
        };
    }
}
