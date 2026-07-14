using FerreSys.Domain;

namespace FerreSys.Application;

public class SaleItemDto
{
    public string Barcode { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class ProcessSaleUseCase
{
    private readonly IProductRepository _productRepository;

    public ProcessSaleUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public void Execute(List<SaleItemDto> items)
    {
        if (items == null || items.Count == 0)
            throw new ArgumentException("No hay productos en el carrito para realizar la venta.");

        var productsToUpdate = new List<(Product Product, int Quantity)>();
        var saleItems = new List<SaleItem>();

        // 1. Validar stock y preparar datos
        foreach (var item in items)
        {
            var product = _productRepository.GetByBarcode(item.Barcode);
            if (product == null)
                throw new KeyNotFoundException($"El producto con código '{item.Barcode}' no existe.");

            product.RecordSale(item.Quantity);
            productsToUpdate.Add((product, item.Quantity));

            // Creamos el registro del renglón de la venta
            saleItems.Add(new SaleItem(
                product.Id,
                product.Barcode,
                product.Name,
                item.Quantity,
                product.RetailPrice
            ));
        }

        // 2. Persistir actualización de stock
        foreach (var (product, _) in productsToUpdate)
        {
            _productRepository.Update(product);
        }

        // 3. Guardar el registro histórico de la venta
        var sale = new Sale(Guid.NewGuid(), DateTime.Now, saleItems);
        _productRepository.SaveSale(sale);
    }
}