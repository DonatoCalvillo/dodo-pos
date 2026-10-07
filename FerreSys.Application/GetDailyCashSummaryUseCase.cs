using FerreSys.Domain;

namespace FerreSys.Application;

public class DailyCashSummary
{
    public DateTime BusinessDate { get; init; }
    public bool HasOpeningCash { get; init; }
    public decimal OpeningCash { get; init; }
    public decimal CashSales { get; init; }
    public decimal CardSales { get; init; }
    public int CashTicketCount { get; init; }
    public int CardTicketCount { get; init; }
    public decimal CashEntries { get; init; }
    public decimal CashExits { get; init; }
    public decimal? CountedCash { get; init; }
    public DateTime? ClosedAt { get; init; }
    public DateTime? LastSaleAt { get; init; }

    public decimal TotalSales => CashSales + CardSales;
    public decimal CashMovementsNet => CashEntries - CashExits;
    public decimal ExpectedCash => OpeningCash + CashSales + CashEntries - CashExits;
    public decimal? Difference => CountedCash is decimal counted ? counted - ExpectedCash : null;

    public bool IsStale =>
        ClosedAt is DateTime closed &&
        LastSaleAt is DateTime last &&
        last > closed;
}

public class GetDailyCashSummaryUseCase
{
    private readonly IProductRepository _productRepository;

    public GetDailyCashSummaryUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public DailyCashSummary Execute(DateTime businessDate)
    {
        var date = businessDate.Date;
        var close = _productRepository.GetCashClose(date);
        var totals = _productRepository.GetPaymentTotals(date);
        var movements = _productRepository.GetCashMovementTotals(date);

        return new DailyCashSummary
        {
            BusinessDate = date,
            HasOpeningCash = close != null,
            OpeningCash = close?.OpeningCash ?? 0m,
            CashSales = totals.CashAmount,
            CardSales = totals.CardAmount,
            CashTicketCount = totals.CashTicketCount,
            CardTicketCount = totals.CardTicketCount,
            CashEntries = movements.Entries,
            CashExits = movements.Exits,
            CountedCash = close?.CountedCash,
            ClosedAt = close?.ClosedAt,
            LastSaleAt = totals.LastSaleAt
        };
    }
}
