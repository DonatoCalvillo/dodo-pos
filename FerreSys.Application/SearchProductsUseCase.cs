using FerreSys.Domain;

namespace FerreSys.Application;

public class SearchProductsUseCase
{
    private readonly IProductRepository _productRepository;

    public SearchProductsUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public List<Product> Execute(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new List<Product>();
        }

        // Si el término de búsqueda parece un código de barras (por ejemplo, puro número largo)
        // intentamos buscar primero por coincidencia exacta para el lector de barras.
        if (searchTerm.Length >= 5 && long.TryParse(searchTerm, out _))
        {
            var product = _productRepository.GetByBarcode(searchTerm);
            if (product != null)
            {
                return new List<Product> { product };
            }
        }

        // Si no es un código de barras exacto, hace la búsqueda distributiva (Fuzzy) por nombre
        return _productRepository.SearchByName(searchTerm);
    }
}