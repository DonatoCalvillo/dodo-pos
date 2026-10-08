using FerreSys.Domain;

namespace FerreSys.Application;

public class AdjustStockUseCase
{
    private readonly IProductRepository _productRepository;

    public AdjustStockUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public void Execute(string barcode, decimal newStock, string notes = "")
    {
        var product = _productRepository.GetByBarcode(barcode);
        if (product == null)
        {
            throw new KeyNotFoundException($"No se encontró el producto con código '{barcode}' para ajustar stock.");
        }

        var (before, delta, after) = product.AdjustStock(newStock);
        var movement = StockMovementFactory.CreateIfChanged(
            product,
            StockMovementKind.AjusteAuditoria,
            before,
            after,
            notes: string.IsNullOrWhiteSpace(notes) ? "Ajuste de auditoría" : notes);

        _productRepository.UpdateWithStockMovement(product, movement);
    }
}
