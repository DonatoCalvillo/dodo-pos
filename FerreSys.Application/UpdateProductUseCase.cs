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
        // 1. Validar que el producto realmente exista en la BD
        var existingProduct = _productRepository.GetByBarcode(updatedProduct.Barcode);
        if (existingProduct == null)
        {
            throw new KeyNotFoundException($"No se encontró el producto con el código de barras '{updatedProduct.Barcode}' para actualizar.");
        }

        // 2. Guardar los cambios usando el repositorio de infraestructura
        _productRepository.Update(updatedProduct);
    }
}