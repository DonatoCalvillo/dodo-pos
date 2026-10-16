using FerreSys.Domain;

namespace FerreSys.Application;

public class SaleReturnLineRequest
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
}

public class SaleReturnQuote
{
    public decimal Amount { get; }
    public bool ReturnsAllRemaining { get; }
    public IReadOnlyList<SaleReturnLine> Lines { get; }

    public SaleReturnQuote(decimal amount, bool returnsAllRemaining, IReadOnlyList<SaleReturnLine> lines)
    {
        Amount = amount;
        ReturnsAllRemaining = returnsAllRemaining;
        Lines = lines;
    }
}

public class ProcessSaleReturnUseCase
{
    private readonly IProductRepository _productRepository;
    private readonly ILoyaltyRepository _loyalty;

    public ProcessSaleReturnUseCase(IProductRepository productRepository, ILoyaltyRepository loyalty)
    {
        _productRepository = productRepository;
        _loyalty = loyalty;
    }

    public SaleReturnQuote Quote(Guid saleId, IReadOnlyList<SaleReturnLineRequest> requested)
    {
        return Build(saleId, requested).Quote;
    }

    public SaleReturn Execute(Guid saleId, IReadOnlyList<SaleReturnLineRequest> requested, bool restoreStock)
    {
        BuiltReturn built = Build(saleId, requested);
        var saleReturn = new SaleReturn(
            Guid.NewGuid(),
            saleId,
            DateTime.Now,
            built.Quote.Lines,
            restoreStock);

        var products = new List<Product>();
        var movements = new List<StockMovement>();
        if (restoreStock)
        {
            foreach (SaleReturnLine line in saleReturn.Lines)
            {
                Product? product = _productRepository.GetById(line.ProductId);
                if (product == null)
                {
                    throw new InvalidOperationException(
                        $"No se puede reponer '{line.ProductName}' porque el producto ya no existe.");
                }

                decimal before = product.Stock;
                product.AddStock(line.Quantity);
                products.Add(product);
                movements.Add(StockMovementFactory.Create(
                    product,
                    StockMovementKind.Devolucion,
                    before,
                    line.Quantity,
                    product.Stock,
                    saleId,
                    $"Devolución {saleReturn.Id.ToString()[..8].ToUpperInvariant()}"));
            }
        }

        string ticket = saleId.ToString()[..8].ToUpperInvariant();
        var cashMovement = new CashMovement(
            Guid.NewGuid(),
            DateTime.Today,
            CashMovementKind.Salida,
            saleReturn.Amount,
            $"Devolución ticket {ticket}",
            saleReturn.CreatedAt);

        LoyaltyReversalTotals prior = built.Sale.Loyalty == null
            ? default
            : _loyalty.GetReversalTotals(saleId);
        IReadOnlyList<LoyaltyMovement> reversals = LoyaltyReturnPlanner.Plan(
            built.Sale,
            saleReturn.Amount,
            built.Quote.ReturnsAllRemaining,
            prior);

        _productRepository.SaveSaleReturn(saleReturn, cashMovement, products, movements, reversals);
        return saleReturn;
    }

    private BuiltReturn Build(Guid saleId, IReadOnlyList<SaleReturnLineRequest> requested)
    {
        if (requested == null || requested.Count == 0)
            throw new ArgumentException("Selecciona al menos un producto para devolver.");

        Sale sale = _productRepository.FindSale(saleId)
            ?? throw new KeyNotFoundException("La venta no existe.");

        List<SaleItem> details = _productRepository.GetSaleDetails(saleId);
        if (details.Count == 0)
            throw new InvalidOperationException("El ticket no tiene productos.");

        Dictionary<Guid, SoldProduct> sold = details
            .GroupBy(item => item.ProductId)
            .ToDictionary(
                group => group.Key,
                group => new SoldProduct(
                    group.Key,
                    FirstText(group.Select(item => item.Barcode), group.Key.ToString()),
                    FirstText(group.Select(item => item.ProductName), "Producto"),
                    group.First().Unit,
                    group.Sum(item => item.Quantity),
                    group.Sum(item => item.Quantity * item.UnitPrice),
                    group.Sum(item => item.Quantity * item.CostPrice)));

        SaleReturnTotals prior = _productRepository.GetSaleReturnTotals(saleId);
        var requestedByProduct = requested
            .GroupBy(line => line.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(line => line.Quantity));

        var drafts = new List<DraftLine>();
        foreach ((Guid productId, decimal rawQuantity) in requestedByProduct)
        {
            if (!sold.TryGetValue(productId, out SoldProduct? product))
                throw new ArgumentException("El producto no pertenece a esta venta.");

            decimal quantity = RoundQuantity(rawQuantity, product.Unit);
            if (Math.Abs(quantity - rawQuantity) > 0.0000001m)
            {
                throw new ArgumentException(
                    $"La cantidad de '{product.Name}' no es válida para la unidad {product.Unit.DisplayName()}.");
            }

            if (quantity <= 0m)
                throw new ArgumentException("La cantidad a devolver debe ser mayor a cero.");

            decimal remaining = Remaining(product, prior);
            if (quantity > remaining)
            {
                throw new ArgumentException(
                    $"No se puede devolver más de lo pendiente de '{product.Name}'. Pendiente: {product.Unit.FormatQuantity(remaining)}.");
            }

            drafts.Add(new DraftLine(product, quantity, product.Gross / product.Quantity, product.Cost / product.Quantity));
        }

        bool returnsAllRemaining = sold.Values.All(product =>
        {
            decimal remaining = Remaining(product, prior);
            decimal quantity = requestedByProduct.TryGetValue(product.ProductId, out decimal raw)
                ? RoundQuantity(raw, product.Unit)
                : 0m;
            return quantity == remaining;
        });

        decimal[] lineAmounts = AllocateAmounts(sale, prior.Amount, drafts, returnsAllRemaining);
        var lines = new List<SaleReturnLine>(drafts.Count);
        for (int i = 0; i < drafts.Count; i++)
        {
            DraftLine draft = drafts[i];
            lines.Add(new SaleReturnLine(
                draft.Product.ProductId,
                draft.Product.Barcode,
                draft.Product.Name,
                draft.Quantity,
                decimal.Round(draft.UnitPrice, 2, MidpointRounding.AwayFromZero),
                decimal.Round(draft.UnitCost, 4, MidpointRounding.AwayFromZero),
                lineAmounts[i]));
        }

        decimal amount = decimal.Round(lines.Sum(line => line.LineAmount), 2, MidpointRounding.AwayFromZero);
        return new BuiltReturn(sale, new SaleReturnQuote(amount, returnsAllRemaining, lines));
    }

    private static decimal[] AllocateAmounts(
        Sale sale,
        decimal alreadyReturned,
        List<DraftLine> drafts,
        bool returnsAllRemaining)
    {
        var lineAmounts = new decimal[drafts.Count];
        decimal cap = decimal.Round(sale.Total - alreadyReturned, 2, MidpointRounding.AwayFromZero);
        if (cap <= 0m)
            throw new InvalidOperationException("Esta venta ya no tiene monto pendiente por devolver.");

        if (returnsAllRemaining)
        {
            decimal gross = drafts.Sum(draft => draft.Quantity * draft.UnitPrice);
            decimal assigned = 0m;
            for (int i = 0; i < drafts.Count - 1; i++)
            {
                decimal share = gross == 0m
                    ? 0m
                    : decimal.Round(cap * (drafts[i].Quantity * drafts[i].UnitPrice) / gross, 2, MidpointRounding.AwayFromZero);
                lineAmounts[i] = share;
                assigned += share;
            }

            lineAmounts[^1] = decimal.Round(cap - assigned, 2, MidpointRounding.AwayFromZero);
            CorrectNegativeRemainder(lineAmounts);
            return lineAmounts;
        }

        if (sale.Subtotal <= 0m)
            throw new InvalidOperationException("No se pudo calcular el monto de la devolución.");

        for (int i = 0; i < drafts.Count; i++)
        {
            decimal gross = drafts[i].Quantity * drafts[i].UnitPrice;
            lineAmounts[i] = decimal.Round(gross * sale.Total / sale.Subtotal, 2, MidpointRounding.AwayFromZero);
        }

        decimal amount = lineAmounts.Sum();
        if (amount > cap)
        {
            decimal overflow = amount - cap;
            for (int i = lineAmounts.Length - 1; i >= 0 && overflow > 0m; i--)
            {
                decimal cut = Math.Min(lineAmounts[i], overflow);
                lineAmounts[i] -= cut;
                overflow -= cut;
            }
        }

        if (lineAmounts.Sum() <= 0m)
            throw new InvalidOperationException("El monto a devolver debe ser mayor a cero.");

        return lineAmounts;
    }

    private static void CorrectNegativeRemainder(decimal[] lineAmounts)
    {
        if (lineAmounts[^1] >= 0m)
            return;

        decimal debt = -lineAmounts[^1];
        lineAmounts[^1] = 0m;
        for (int i = 0; i < lineAmounts.Length - 1 && debt > 0m; i++)
        {
            decimal cut = Math.Min(lineAmounts[i], debt);
            lineAmounts[i] -= cut;
            debt -= cut;
        }
    }

    private static decimal Remaining(SoldProduct product, SaleReturnTotals prior)
    {
        decimal sold = RoundQuantity(product.Quantity, product.Unit);
        decimal returned = RoundQuantity(prior.QuantityFor(product.ProductId), product.Unit);
        return sold - returned;
    }

    private static decimal RoundQuantity(decimal quantity, UnitOfMeasure unit) =>
        decimal.Round(quantity, unit.MaxDecimalPlaces(), MidpointRounding.AwayFromZero);

    private static string FirstText(IEnumerable<string> values, string fallback)
    {
        foreach (string value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return fallback;
    }

    private sealed class BuiltReturn
    {
        public Sale Sale { get; }
        public SaleReturnQuote Quote { get; }

        public BuiltReturn(Sale sale, SaleReturnQuote quote)
        {
            Sale = sale;
            Quote = quote;
        }
    }

    private sealed class SoldProduct
    {
        public Guid ProductId { get; }
        public string Barcode { get; }
        public string Name { get; }
        public UnitOfMeasure Unit { get; }
        public decimal Quantity { get; }
        public decimal Gross { get; }
        public decimal Cost { get; }

        public SoldProduct(
            Guid productId,
            string barcode,
            string name,
            UnitOfMeasure unit,
            decimal quantity,
            decimal gross,
            decimal cost)
        {
            ProductId = productId;
            Barcode = barcode;
            Name = name;
            Unit = unit;
            Quantity = quantity;
            Gross = gross;
            Cost = cost;
        }
    }

    private sealed class DraftLine
    {
        public SoldProduct Product { get; }
        public decimal Quantity { get; }
        public decimal UnitPrice { get; }
        public decimal UnitCost { get; }

        public DraftLine(SoldProduct product, decimal quantity, decimal unitPrice, decimal unitCost)
        {
            Product = product;
            Quantity = quantity;
            UnitPrice = unitPrice;
            UnitCost = unitCost;
        }
    }
}
