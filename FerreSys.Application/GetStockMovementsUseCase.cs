using FerreSys.Domain;

namespace FerreSys.Application;

public class GetStockMovementsUseCase
{
    private readonly IProductRepository _productRepository;

    public GetStockMovementsUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public List<StockMovement> Execute(
        Guid? productId = null,
        DateTime? fromInclusive = null,
        DateTime? toInclusive = null,
        StockMovementKind? kind = null)
    {
        DateTime? toEndOfDay = toInclusive.HasValue
            ? toInclusive.Value.Date.AddDays(1).AddTicks(-1)
            : null;

        DateTime? fromStart = fromInclusive?.Date;

        return _productRepository.GetStockMovements(productId, fromStart, toEndOfDay, kind);
    }
}
