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

    public void Execute(
        List<SaleItemDto> items,
        string paymentMethod,
        string cardReference,
        decimal amountReceived = 0,
        decimal cardAmount = 0)
    {
        if (items == null || items.Count == 0)
            throw new ArgumentException("No hay productos en el carrito para realizar la venta.");

        var productsToUpdate = new List<(Product Product, decimal Quantity)>();
        var saleItems = new List<SaleItem>();

        foreach (var item in items)
        {
            var product = _productRepository.GetByBarcode(item.Barcode);
            if (product == null)
                throw new KeyNotFoundException($"El producto con código '{item.Barcode}' no existe.");

            product.RecordSale(item.Quantity);
            productsToUpdate.Add((product, item.Quantity));

            saleItems.Add(new SaleItem(
                product.Id,
                product.Barcode,
                product.Name,
                item.Quantity,
                product.RetailPrice,
                product.CostPrice,
                product.Unit
            ));
        }

        foreach (var (product, _) in productsToUpdate)
        {
            _productRepository.Update(product);
        }

        var sale = new Sale(
            Guid.NewGuid(),
            DateTime.Now,
            saleItems,
            paymentMethod,
            cardReference,
            amountReceived,
            cardAmount);
        _productRepository.SaveSale(sale);
    }
}
