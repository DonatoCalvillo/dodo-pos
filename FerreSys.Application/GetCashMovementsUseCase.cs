using FerreSys.Domain;

namespace FerreSys.Application;

public class GetCashMovementsUseCase
{
    private readonly IProductRepository _productRepository;

    public GetCashMovementsUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public List<CashMovement> Execute(DateTime businessDate) =>
        _productRepository.GetCashMovements(businessDate.Date);
}
