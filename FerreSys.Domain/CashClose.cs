namespace FerreSys.Domain;

public class CashClose
{
    public DateTime BusinessDate { get; }
    public decimal OpeningCash { get; }
    public decimal? CountedCash { get; }
    public DateTime? ClosedAt { get; }
    public decimal? CashSales { get; }
    public decimal? CardSales { get; }
    public decimal? CashEntries { get; }
    public decimal? CashExits { get; }
    public decimal? ExpectedCash { get; }
    public decimal? Difference { get; }

    public CashClose(
        DateTime businessDate,
        decimal openingCash,
        decimal? countedCash,
        DateTime? closedAt,
        decimal? cashSales = null,
        decimal? cardSales = null,
        decimal? cashEntries = null,
        decimal? cashExits = null,
        decimal? expectedCash = null,
        decimal? difference = null)
    {
        if (openingCash < 0)
            throw new ArgumentException("El fondo no puede ser negativo.");

        if (countedCash is < 0)
            throw new ArgumentException("El efectivo contado no puede ser negativo.");

        BusinessDate = businessDate.Date;
        OpeningCash = openingCash;
        CountedCash = countedCash;
        ClosedAt = closedAt;
        CashSales = cashSales;
        CardSales = cardSales;
        CashEntries = cashEntries;
        CashExits = cashExits;
        ExpectedCash = expectedCash;
        Difference = difference;
    }

    public bool HasReportSnapshot =>
        ClosedAt != null &&
        CountedCash != null &&
        ExpectedCash != null;
}

public class PaymentDayTotals
{
    public decimal CashAmount { get; }
    public int CashTicketCount { get; }
    public decimal CardAmount { get; }
    public int CardTicketCount { get; }
    public DateTime? LastSaleAt { get; }

    public PaymentDayTotals(
        decimal cashAmount,
        int cashTicketCount,
        decimal cardAmount,
        int cardTicketCount,
        DateTime? lastSaleAt)
    {
        CashAmount = cashAmount;
        CashTicketCount = cashTicketCount;
        CardAmount = cardAmount;
        CardTicketCount = cardTicketCount;
        LastSaleAt = lastSaleAt;
    }
}

public class CashCloseReportSnapshot
{
    public decimal CashSales { get; }
    public decimal CardSales { get; }
    public decimal CashEntries { get; }
    public decimal CashExits { get; }
    public decimal ExpectedCash { get; }
    public decimal CountedCash { get; }
    public decimal Difference { get; }

    public CashCloseReportSnapshot(
        decimal cashSales,
        decimal cardSales,
        decimal cashEntries,
        decimal cashExits,
        decimal expectedCash,
        decimal countedCash,
        decimal difference)
    {
        CashSales = cashSales;
        CardSales = cardSales;
        CashEntries = cashEntries;
        CashExits = cashExits;
        ExpectedCash = expectedCash;
        CountedCash = countedCash;
        Difference = difference;
    }
}
