using FerreSys.Domain;

namespace FerreSys.Application;

public class CloseCashDayUseCase
{
    private readonly IProductRepository _productRepository;

    public CloseCashDayUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public void Execute(DateTime businessDate, decimal countedCash)
    {
        if (countedCash < 0)
            throw new ArgumentException("El efectivo contado no puede ser negativo.");

        var existing = _productRepository.GetCashClose(businessDate.Date);
        if (existing == null)
            throw new InvalidOperationException("Guarda el fondo de caja antes de cerrar el día.");

        _productRepository.CloseCashDay(businessDate.Date, countedCash);
    }
}
