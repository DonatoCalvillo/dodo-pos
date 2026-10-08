using FerreSys.Domain;

namespace FerreSys.Application;

public class UpdateProductUseCase
{
    private readonly IProductRepository _productRepository;

    public UpdateProductUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public void Execute(Product updatedProduct)
    {
        var existingProduct = _productRepository.GetByBarcode(updatedProduct.Barcode);
        if (existingProduct == null)
        {
            throw new KeyNotFoundException($"No se encontró el producto con el código de barras '{updatedProduct.Barcode}' para actualizar.");
        }

        var movement = StockMovementFactory.CreateIfChanged(
            updatedProduct,
            StockMovementKind.AjusteAuditoria,
            existingProduct.Stock,
            updatedProduct.Stock,
            notes: "Edición de producto");

        _productRepository.UpdateWithStockMovement(updatedProduct, movement);
    }
}
