using FerreSys.Domain;

namespace FerreSys.Application;

public class AddProductUseCase
{
    private readonly IProductRepository _productRepository;
    private readonly ISubscriptionService _subscriptionService;

    public AddProductUseCase(IProductRepository productRepository, ISubscriptionService subscriptionService)
    {
        _productRepository = productRepository;
        _subscriptionService = subscriptionService;
    }

    public void Execute(Product newProduct)
    {
        var existingProduct = _productRepository.GetByBarcode(newProduct.Barcode);
        if (existingProduct != null)
        {
            throw new InvalidOperationException($"El código de barras '{newProduct.Barcode}' ya está registrado con el producto: {existingProduct.Name}.");
        }

        var currentTier = _subscriptionService.GetCurrentTier();

        if (currentTier == SubscriptionTier.Free)
        {
            int currentTotal = _productRepository.GetTotalCount();
            if (currentTotal >= FreeTierLimits.ProductLimit)
            {
                throw new InvalidOperationException(
                    $"Has alcanzado el límite de {FreeTierLimits.ProductLimit} productos de la versión gratuita.\n\n" +
                    FreeTierLimits.PremiumUpsell
                );
            }
        }

        var movement = StockMovementFactory.Create(
            newProduct,
            StockMovementKind.CargaInicial,
            quantityBefore: 0m,
            quantityDelta: newProduct.Stock,
            quantityAfter: newProduct.Stock,
            notes: "Alta de producto");

        _productRepository.SaveWithStockMovement(newProduct, movement);
    }
}
