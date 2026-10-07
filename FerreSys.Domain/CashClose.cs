namespace FerreSys.Domain;

public class CashClose
{
    public DateTime BusinessDate { get; }
    public decimal OpeningCash { get; }
    public decimal? CountedCash { get; }
    public DateTime? ClosedAt { get; }

    public CashClose(DateTime businessDate, decimal openingCash, decimal? countedCash, DateTime? closedAt)
    {
        if (openingCash < 0)
            throw new ArgumentException("El fondo no puede ser negativo.");

        if (countedCash is < 0)
            throw new ArgumentException("El efectivo contado no puede ser negativo.");

        BusinessDate = businessDate.Date;
        OpeningCash = openingCash;
        CountedCash = countedCash;
        ClosedAt = closedAt;
    }
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
