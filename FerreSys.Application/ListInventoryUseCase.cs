using FerreSys.Domain;

namespace FerreSys.Application;

public class ListInventoryUseCase
{
    private readonly IProductRepository _productRepository;

    public ListInventoryUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public List<Product> Execute() => _productRepository.ListActive();
}
