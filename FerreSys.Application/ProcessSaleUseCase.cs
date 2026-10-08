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

    public ProcessSaleUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public Sale Execute(
        List<SaleItemDto> items,
        string paymentMethod,
        string cardReference,
        decimal amountReceived = 0,
        decimal cardAmount = 0)
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
                product.RetailPrice,
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

        var sale = new Sale(
            saleId,
            occurredAt,
            saleItems,
            paymentMethod,
            cardReference,
            amountReceived,
            cardAmount);

        _productRepository.SaveSaleWithInventory(sale, productsToUpdate, movements);
        return sale;
    }
}
