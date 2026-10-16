using FerreSys.Domain;

namespace FerreSys.Application;

public class SaleItemDto
{
    public string Barcode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}

public class ProcessSaleUseCase
{
    private readonly IProductRepository _productRepository;
    private readonly ILoyaltyRepository _loyalty;
    private readonly IAppSettingsStore _settings;

    public ProcessSaleUseCase(
        IProductRepository productRepository,
        ILoyaltyRepository loyalty,
        IAppSettingsStore settings)
    {
        _productRepository = productRepository;
        _loyalty = loyalty;
        _settings = settings;
    }

    public Sale Execute(
        List<SaleItemDto> items,
        string paymentMethod,
        string cardReference,
        decimal amountReceived = 0,
        decimal cardAmount = 0,
        decimal discountAmount = 0,
        LoyaltyCheckoutRequest? loyalty = null)
    {
        if (items == null || items.Count == 0)
            throw new ArgumentException("No hay productos en el carrito para realizar la venta.");

        var productsToUpdate = new List<Product>();
        var saleItems = new List<SaleItem>();
        var movements = new List<StockMovement>();
        var saleId = Guid.NewGuid();
        var occurredAt = DateTime.Now;

        foreach (var item in items)
        {
            var product = _productRepository.GetByBarcode(item.Barcode);
            if (product == null)
                throw new KeyNotFoundException($"El producto con código '{item.Barcode}' no existe.");

            decimal before = product.Stock;
            product.RecordSale(item.Quantity);
            productsToUpdate.Add(product);

            saleItems.Add(new SaleItem(
                product.Id,
                product.Barcode,
                product.Name,
                item.Quantity,
                product.ResolveUnitPrice(item.Quantity, DateOnly.FromDateTime(occurredAt)),
                product.CostPrice,
                product.Unit
            ));

            movements.Add(StockMovementFactory.Create(
                product,
                StockMovementKind.Venta,
                before,
                -item.Quantity,
                product.Stock,
                saleId,
                notes: string.Empty,
                occurredAt: occurredAt));
        }

        decimal subtotal = saleItems.Sum(item => item.Subtotal);
        LoyaltySaleInfo? loyaltyInfo = loyalty == null
            ? null
            : BuildLoyalty(loyalty, subtotal, discountAmount);

        var sale = new Sale(
            saleId,
            occurredAt,
            saleItems,
            paymentMethod,
            cardReference,
            amountReceived,
            cardAmount,
            discountAmount,
            loyaltyInfo);

        _productRepository.SaveSaleWithInventory(sale, productsToUpdate, movements);
        return sale;
    }

    private LoyaltySaleInfo BuildLoyalty(LoyaltyCheckoutRequest request, decimal subtotal, decimal discountAmount)
    {
        LoyaltyProgramSettings settings = _settings.GetLoyaltySettings();
        if (!settings.Enabled)
            throw new InvalidOperationException("El programa de fidelidad está apagado.");

        Customer customer = _loyalty.GetCustomer(request.CustomerId)
            ?? throw new InvalidOperationException("El cliente de fidelidad no existe.");
        if (!customer.IsActive)
            throw new InvalidOperationException("El cliente de fidelidad no está activo.");

        int balance = _loyalty.GetBalance(customer.Id);
        LoyaltyReward? reward = null;
        int redeemed = 0;
        decimal loyaltyDiscount = 0m;
        if (request.RewardId is Guid rewardId)
        {
            reward = _loyalty.GetReward(rewardId)
                ?? throw new InvalidOperationException("El premio ya no existe.");
            if (!reward.IsActive)
                throw new InvalidOperationException("El premio no está activo.");
            if (balance < reward.PointsCost)
                throw new InvalidOperationException("El cliente no tiene puntos suficientes para ese premio.");

            loyaltyDiscount = reward.DiscountFor(subtotal);
            decimal sent = decimal.Round(request.LoyaltyDiscountAmount, 2, MidpointRounding.AwayFromZero);
            if (sent != loyaltyDiscount)
                throw new InvalidOperationException("El descuento del premio cambió. Vuelve a cobrar la venta.");
            if (loyaltyDiscount > 0m && loyaltyDiscount >= subtotal)
                throw new InvalidOperationException("El descuento del premio debe ser menor al subtotal.");

            redeemed = reward.PointsCost;
        }
        else if (request.LoyaltyDiscountAmount != 0m)
        {
            throw new InvalidOperationException("Hay un descuento de premio sin premio seleccionado.");
        }

        if (discountAmount < loyaltyDiscount)
            throw new InvalidOperationException("El descuento de la venta no cubre el premio.");

        decimal paid = subtotal - discountAmount;
        int earned = settings.CalculatePoints(subtotal, paid);
        int balanceAfter = balance - redeemed + earned;
        return new LoyaltySaleInfo(
            customer.Id,
            customer.Name,
            earned,
            reward?.Id,
            reward?.Name ?? string.Empty,
            redeemed,
            loyaltyDiscount,
            balanceAfter);
    }
}
