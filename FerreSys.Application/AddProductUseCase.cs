using FerreSys.Domain;

namespace FerreSys.Application;

public class AddProductUseCase
{
    private readonly IProductRepository _productRepository;
    private readonly ISubscriptionService _subscriptionService;
    private const int FreeTierLimit = 200;

    // Inyectamos el repositorio a través del constructor (Inversión de Dependencias)
    public AddProductUseCase(IProductRepository productRepository, ISubscriptionService subscriptionService)
    {
        _productRepository = productRepository;
        _subscriptionService = subscriptionService;
    }

    public void Execute(Product newProduct)
    {
        // 1. Validar si el código de barras ya existe para evitar duplicados
        var existingProduct = _productRepository.GetByBarcode(newProduct.Barcode);
        if (existingProduct != null)
        {
            throw new InvalidOperationException($"El código de barras '{newProduct.Barcode}' ya está registrado con el producto: {existingProduct.Name}.");
        }

        // 2. CRUCIAL MVP: Validar el límite de la versión gratuita
        var currentTier = _subscriptionService.GetCurrentTier();

        if (currentTier == SubscriptionTier.Free)
        {
            // Si es gratis, contamos los productos actuales
            int currentTotal = _productRepository.GetTotalCount();
            if (currentTotal >= FreeTierLimit)
            {
                throw new InvalidOperationException(
                    "Has alcanzado el límite de 200 productos de la versión gratuita.\n\n" +
                    "Para registrar más productos de forma ilimitada y activar respaldos automáticos en la nube, " +
                    "adquiere la Licencia Premium de FerreSys."
                );
            }
        }

        // 3. Si pasa las validaciones, se guarda en el repositorio
        _productRepository.Save(newProduct);
    }
}