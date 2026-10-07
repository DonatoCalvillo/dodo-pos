using FerreSys.Domain;

namespace FerreSys.Application;

public class SaveOpeningCashUseCase
{
    private readonly IProductRepository _productRepository;

    public SaveOpeningCashUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public void Execute(DateTime businessDate, decimal openingCash)
    {
        if (openingCash < 0)
            throw new ArgumentException("El fondo no puede ser negativo.");

        var date = businessDate.Date;
        var existing = _productRepository.GetCashClose(date);
        if (existing?.ClosedAt != null)
            throw new InvalidOperationException("El día ya está cerrado. No se puede cambiar el fondo.");

        _productRepository.SaveOpeningCash(date, openingCash);
    }
}
