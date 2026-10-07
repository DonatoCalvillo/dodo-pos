using FerreSys.Domain;

namespace FerreSys.Application;

public class CloseCashDayUseCase
{
    private readonly IProductRepository _productRepository;

    public CloseCashDayUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public CashCloseReportSnapshot Execute(DateTime businessDate, decimal countedCash)
    {
        if (countedCash < 0)
            throw new ArgumentException("El efectivo contado no puede ser negativo.");

        var date = businessDate.Date;
        var existing = _productRepository.GetCashClose(date);
        if (existing == null)
            throw new InvalidOperationException("Guarda el fondo de caja antes de cerrar el día.");

        var totals = _productRepository.GetPaymentTotals(date);
        var movements = _productRepository.GetCashMovementTotals(date);

        decimal expectedCash = existing.OpeningCash + totals.CashAmount + movements.Entries - movements.Exits;
        decimal difference = countedCash - expectedCash;

        var report = new CashCloseReportSnapshot(
            cashSales: totals.CashAmount,
            cardSales: totals.CardAmount,
            cashEntries: movements.Entries,
            cashExits: movements.Exits,
            expectedCash: expectedCash,
            countedCash: countedCash,
            difference: difference);

        _productRepository.CloseCashDay(date, report);
        return report;
    }
}
