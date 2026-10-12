using FerreSys.Domain;

namespace FerreSys.Application;

public class DeactivateProductUseCase
{
    private readonly IProductRepository _productRepository;

    public DeactivateProductUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public void Execute(Guid productId)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("El producto a ocultar no es válido.");

        _productRepository.Deactivate(productId);
    }
}
