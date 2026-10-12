namespace FerreSys.Domain;

public interface IProductRepository
{
    // Save a brand new product
    void Save(Product product);

    // Fetch a single product via barcode scanner
    Product? GetByBarcode(string barcode);

    // Fuzzy search for the smart counter dropdown
    List<Product> SearchByName(string term);

    // CRITICAL FOR MVP: Count total items to enforce the 200-product free limit
    int GetTotalCount();

    // Update stock levels after a transaction
    void Update(Product product);

    /// <summary>
    /// Hides a product from the counter without deleting the row.
    /// </summary>
    void Deactivate(Guid productId);

    void SaveSale(Sale sale);

    /// <summary>
    /// Persists sale header/details, product stock updates and kardex lines in one transaction.
    /// </summary>
    void SaveSaleWithInventory(
        Sale sale,
        IReadOnlyList<Product> products,
        IReadOnlyList<StockMovement> movements);

    void SaveStockMovement(StockMovement movement);

    void SaveStockMovements(IReadOnlyList<StockMovement> movements);

    /// <summary>
    /// Inserts products and their initial-load kardex lines in one transaction.
    /// </summary>
    void SaveBulkWithStockMovements(List<Product> products, IReadOnlyList<StockMovement> movements);

    /// <summary>
    /// Updates a product and optionally writes a kardex line in one transaction.
    /// </summary>
    void UpdateWithStockMovement(Product product, StockMovement? movement);

    /// <summary>
    /// Inserts a product and optionally writes a kardex line in one transaction.
    /// </summary>
    void SaveWithStockMovement(Product product, StockMovement? movement);

    List<StockMovement> GetStockMovements(
        Guid? productId,
        DateTime? fromInclusive,
        DateTime? toInclusive,
        StockMovementKind? kind);

    List<Sale> GetSalesHistory();

    // Reemplaza el anterior por este que acepta filtros
    List<Sale> GetSalesHistory(string? filterType = null);
    // Nuevo método para traer el desglose
    List<SaleItem> GetSaleDetails(Guid saleId);

    void SaveBulk(List<Product> products);

    List<Sale> GetSalesHistoryAdvanced(DateTime startDate, DateTime endDate, string ticketIdFilter);

    CashClose? GetCashClose(DateTime businessDate);

    void SaveOpeningCash(DateTime businessDate, decimal openingCash);

    void CloseCashDay(DateTime businessDate, CashCloseReportSnapshot report);

    PaymentDayTotals GetPaymentTotals(DateTime businessDate);

    GrossProfitTotals GetGrossProfitTotals(DateTime startDate, DateTime endDate);

    void SaveCashMovement(CashMovement movement);

    List<CashMovement> GetCashMovements(DateTime businessDate);

    CashMovementDayTotals GetCashMovementTotals(DateTime businessDate);

    List<ReorderSuggestionLine> GetReorderSuggestions();
}