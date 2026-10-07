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

    void SaveSale(Sale sale);

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

    void SaveCashMovement(CashMovement movement);

    List<CashMovement> GetCashMovements(DateTime businessDate);

    CashMovementDayTotals GetCashMovementTotals(DateTime businessDate);
}