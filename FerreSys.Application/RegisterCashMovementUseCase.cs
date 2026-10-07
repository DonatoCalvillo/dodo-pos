using FerreSys.Domain;

namespace FerreSys.Application;

public class RegisterCashMovementUseCase
{
    private readonly IProductRepository _productRepository;

    public RegisterCashMovementUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public CashMovement Execute(DateTime businessDate, CashMovementKind kind, decimal amount, string concept)
    {
        var date = businessDate.Date;
        var close = _productRepository.GetCashClose(date);
        if (close?.ClosedAt != null)
            throw new InvalidOperationException("El día ya está cerrado. No se pueden registrar movimientos.");

        var movement = new CashMovement(
            Guid.NewGuid(),
            date,
            kind,
            amount,
            concept,
            DateTime.Now);

        _productRepository.SaveCashMovement(movement);
        return movement;
    }
}
