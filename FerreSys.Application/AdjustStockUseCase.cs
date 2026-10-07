using FerreSys.Domain;

namespace FerreSys.Application;

public class AdjustStockUseCase
{
    private readonly IProductRepository _productRepository;

    public AdjustStockUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public void Execute(string barcode, decimal newStock)
    {
        if (newStock < 0)
        {
            throw new ArgumentException("El stock de inventario no puede ser una cantidad negativa.");
        }

        // 1. Buscamos el producto por su identificador único de negocio
        var product = _productRepository.GetByBarcode(barcode);
        if (product == null)
        {
            throw new KeyNotFoundException($"No se encontró el producto con código '{barcode}' para ajustar stock.");
        }

        // 2. Usamos las reglas del dominio para reconstruir la entidad con el nuevo stock
        var updatedProduct = new Product(
            product.Id,
            product.Barcode,
            product.Name,
            product.CostPrice,
            product.RetailPrice,
            newStock,
            product.Unit
        );

        // 3. Mandamos a actualizar mediante el repositorio
        _productRepository.Update(updatedProduct);
    }
}
