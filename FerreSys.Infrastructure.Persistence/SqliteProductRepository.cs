using System.Data;
using Microsoft.Data.Sqlite;
using FerreSys.Domain;

namespace FerreSys.Infrastructure.Persistence;

public class SqliteProductRepository : IProductRepository
{
    private const string ProductSelect =
        "SELECT Id, Barcode, Name, CostPrice, RetailPrice, Stock, Unit, MinStock, DesiredStock FROM Products";

    private readonly SqliteDbContext _context;

    public SqliteProductRepository(SqliteDbContext context)
    {
        _context = context;
    }

    public void Save(Product product)
    {
        using var connection = _context.CreateConnection();
        connection.Open();

        string query = @"
            INSERT INTO Products (Id, Barcode, Name, CostPrice, RetailPrice, Stock, Unit, MinStock, DesiredStock)
            VALUES (@Id, @Barcode, @Name, @CostPrice, @RetailPrice, @Stock, @Unit, @MinStock, @DesiredStock);";

        using var command = new SqliteCommand(query, connection);
        BindProductParameters(command, product);
        command.ExecuteNonQuery();
    }

    public Product? GetByBarcode(string barcode)
    {
        using var connection = _context.CreateConnection();
        connection.Open();

        string query = ProductSelect + " WHERE Barcode = @Barcode LIMIT 1;";
        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@Barcode", barcode);

        using var reader = command.ExecuteReader();
        if (reader.Read())
            return MapProduct(reader);

        return null;
    }

    public List<Product> SearchByName(string term)
    {
        var products = new List<Product>();
        using var connection = _context.CreateConnection();
        connection.Open();

        // Filtro Fuzzy simple usando LIKE. El operador % busca coincidencias en cualquier parte del texto.
        string query = ProductSelect + " WHERE Name LIKE @Term;";
        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@Term", $"%{term}%");

        using var reader = command.ExecuteReader();
        while (reader.Read())
            products.Add(MapProduct(reader));

        return products;
    }

    public int GetTotalCount()
    {
        using var connection = _context.CreateConnection();
        connection.Open();

        string query = "SELECT COUNT(*) FROM Products;";
        using var command = new SqliteCommand(query, connection);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void Update(Product product)
    {
        using var connection = _context.CreateConnection();
        connection.Open();

        string query = @"
            UPDATE Products 
            SET Name = @Name, CostPrice = @CostPrice, RetailPrice = @RetailPrice, Stock = @Stock,
                Unit = @Unit, MinStock = @MinStock, DesiredStock = @DesiredStock
            WHERE Id = @Id;";

        using var command = new SqliteCommand(query, connection);
        BindProductParameters(command, product);
        command.ExecuteNonQuery();
    }

    public void SaveSale(Sale sale)
    {
        using var connection = _context.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Insertar en cabecera de Ventas
            string saleQuery = "INSERT INTO Sales (Id, CreatedAt, Total, PaymentMethod, CardReference, AmountReceived, CardAmount) VALUES (@Id, @CreatedAt, @Total, @PaymentMethod, @CardReference, @AmountReceived, @CardAmount);";
            using (var cmdSale = new SqliteCommand(saleQuery, connection, transaction))
            {
                cmdSale.Parameters.AddWithValue("@Id", sale.Id.ToString());
                cmdSale.Parameters.AddWithValue("@CreatedAt", sale.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                cmdSale.Parameters.AddWithValue("@Total", sale.Total);
                cmdSale.Parameters.AddWithValue("@PaymentMethod", sale.PaymentMethod);
                cmdSale.Parameters.AddWithValue("@CardReference", sale.CardReference);
                cmdSale.Parameters.AddWithValue("@AmountReceived", sale.AmountReceived);
                cmdSale.Parameters.AddWithValue("@CardAmount", sale.CardAmount);
                cmdSale.ExecuteNonQuery();
            }
            // Insertar los detalles
            string detailQuery = @"
            INSERT INTO SaleDetails (SaleId, ProductId, Barcode, ProductName, Quantity, PriceSold, UnitPrice, CostPrice, Unit)
            VALUES (@SaleId, @ProductId, @Barcode, @ProductName, @Quantity, @PriceSold, @UnitPrice, @CostPrice, @Unit);";

            foreach (var item in sale.Items)
            {
                using var cmdDetail = new SqliteCommand(detailQuery, connection, transaction);
                cmdDetail.Parameters.AddWithValue("@SaleId", sale.Id.ToString());
                cmdDetail.Parameters.AddWithValue("@ProductId", item.ProductId.ToString());
                cmdDetail.Parameters.AddWithValue("@Barcode", item.Barcode);
                cmdDetail.Parameters.AddWithValue("@ProductName", item.ProductName);
                cmdDetail.Parameters.AddWithValue("@Quantity", item.Quantity);
                cmdDetail.Parameters.AddWithValue("@PriceSold", item.UnitPrice);
                cmdDetail.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                cmdDetail.Parameters.AddWithValue("@CostPrice", item.CostPrice);
                cmdDetail.Parameters.AddWithValue("@Unit", item.Unit.ToString());
                cmdDetail.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public List<Sale> GetSalesHistory()
    {
        var sales = new List<Sale>();
        using var connection = _context.CreateConnection();
        connection.Open();

        string query = "SELECT Id, CreatedAt, Total, PaymentMethod, CardReference, AmountReceived, CardAmount FROM Sales ORDER BY CreatedAt DESC;";
        using var command = new SqliteCommand(query, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            // En un MVP, cargamos la cabecera de la venta. 
            // Inicializamos la lista de ítems vacía para no ralentizar la consulta principal.
            sales.Add(MapSaleHeader(reader));
        }
        return sales;
    }

    public List<Sale> GetSalesHistory(string? filterType = null)
    {
        var sales = new List<Sale>();
        using var connection = _context.CreateConnection();
        connection.Open();

        // Filtro base: por defecto trae todo
        string query = "SELECT Id, CreatedAt, Total, PaymentMethod, CardReference, AmountReceived, CardAmount FROM Sales";

        if (filterType == "TODAY")
        {
            // Filtra solo los registros del día de hoy en formato SQLite (YYYY-MM-DD)
            query += " WHERE date(CreatedAt) = date('now', 'localtime')";
        }
        else if (filterType == "MONTH")
        {
            // Filtra los registros del mes actual
            query += " WHERE strftime('%Y-%m', CreatedAt) = strftime('%Y-%m', 'now', 'localtime')";
        }

        query += " ORDER BY CreatedAt DESC;";

        using var command = new SqliteCommand(query, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            // Cabecera con Total de BD; el desglose se carga al seleccionar el ticket (GetSaleDetails)
            sales.Add(MapSaleHeader(reader));
        }
        return sales;
    }

    public List<SaleItem> GetSaleDetails(Guid saleId)
    {
        var items = new List<SaleItem>();
        using var connection = _context.CreateConnection();
        connection.Open();

        string query = @"
            SELECT ProductId, Barcode, ProductName, Quantity, COALESCE(UnitPrice, PriceSold), CostPrice, Unit
            FROM SaleDetails WHERE SaleId = @SaleId;";
        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@SaleId", saleId.ToString());

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new SaleItem(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                Convert.ToDecimal(reader.GetValue(3)),
                reader.GetDecimal(4),
                reader.IsDBNull(5) ? 0m : reader.GetDecimal(5),
                reader.IsDBNull(6) ? UnitOfMeasure.Pieza : UnitOfMeasureExtensions.ParseOrDefault(reader.GetString(6))
            ));
        }
        return items;
    }

    public void SaveBulk(List<Product> products)
    {
        using var connection = _context.CreateConnection();
        connection.Open();

        // INICIAMOS LA TRANSACCIÓN (Garantiza el Todo o Nada)
        using var transaction = connection.BeginTransaction();

        try
        {
            string query = @"
            INSERT INTO Products (Id, Barcode, Name, CostPrice, RetailPrice, Stock, Unit, MinStock, DesiredStock)
            VALUES (@Id, @Barcode, @Name, @CostPrice, @RetailPrice, @Stock, @Unit, @MinStock, @DesiredStock);";

            foreach (var product in products)
            {
                using var command = new SqliteCommand(query, connection, transaction);
                BindProductParameters(command, product);
                command.ExecuteNonQuery();
            }

            // Si todos se insertaron con éxito, guardamos los cambios de forma permanente
            transaction.Commit();
        }
        catch
        {
            // Si CUALQUIERA falló a nivel base de datos, deshacemos todo lo que se hizo antes
            transaction.Rollback();
            throw; // Re-lanzamos la excepción para que la UI se entere
        }
    }

    public List<Sale> GetSalesHistoryAdvanced(DateTime startDate, DateTime endDate, string ticketIdFilter)
    {
        var sales = new List<Sale>();
        using var connection = _context.CreateConnection();
        connection.Open();

        // Filtramos usando la función date() de SQLite para comparar rangos de forma limpia (YYYY-MM-DD)
        string query = @"
        SELECT Id, CreatedAt, Total, PaymentMethod, CardReference, AmountReceived, CardAmount
        FROM Sales 
        WHERE date(CreatedAt) >= date(@StartDate) 
          AND date(CreatedAt) <= date(@EndDate)";

        // Si el usuario escribió algo en el buscador de ID, añadimos la condición LIKE
        if (!string.IsNullOrWhiteSpace(ticketIdFilter))
        {
            query += " AND Id LIKE @TicketId";
        }

        query += " ORDER BY CreatedAt DESC;";

        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd"));

        if (!string.IsNullOrWhiteSpace(ticketIdFilter))
        {
            // Añadimos comodines % para que busque coincidencias parciales (ej: los primeros 8 caracteres)
            command.Parameters.AddWithValue("@TicketId", "%" + ticketIdFilter.Trim() + "%");
        }

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            // Cabecera con Total de BD; el desglose se carga al seleccionar el ticket (GetSaleDetails)
            sales.Add(MapSaleHeader(reader));
        }
        return sales;
    }

    public CashClose? GetCashClose(DateTime businessDate)
    {
        using var connection = _context.CreateConnection();
        connection.Open();

        string query = @"
            SELECT BusinessDate, OpeningCash, CountedCash, ClosedAt,
                   CashSales, CardSales, CashEntries, CashExits, ExpectedCash, Difference
            FROM CashCloses
            WHERE BusinessDate = @BusinessDate;";

        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@BusinessDate", businessDate.ToString("yyyy-MM-dd"));

        using var reader = command.ExecuteReader();
        if (!reader.Read())
            return null;

        decimal? counted = reader.IsDBNull(2) ? null : ReadMoney(reader, 2);
        DateTime? closedAt = reader.IsDBNull(3) ? null : DateTime.Parse(reader.GetString(3));

        return new CashClose(
            DateTime.Parse(reader.GetString(0)),
            ReadMoney(reader, 1),
            counted,
            closedAt,
            reader.IsDBNull(4) ? null : ReadMoney(reader, 4),
            reader.IsDBNull(5) ? null : ReadMoney(reader, 5),
            reader.IsDBNull(6) ? null : ReadMoney(reader, 6),
            reader.IsDBNull(7) ? null : ReadMoney(reader, 7),
            reader.IsDBNull(8) ? null : ReadMoney(reader, 8),
            reader.IsDBNull(9) ? null : ReadMoney(reader, 9));
    }

    public void SaveOpeningCash(DateTime businessDate, decimal openingCash)
    {
        using var connection = _context.CreateConnection();
        connection.Open();

        string query = @"
            INSERT INTO CashCloses (BusinessDate, OpeningCash, CountedCash, ClosedAt)
            VALUES (@BusinessDate, @OpeningCash, NULL, NULL)
            ON CONFLICT(BusinessDate) DO UPDATE SET OpeningCash = excluded.OpeningCash;";

        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@BusinessDate", businessDate.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("@OpeningCash", openingCash);
        command.ExecuteNonQuery();
    }

    public void CloseCashDay(DateTime businessDate, CashCloseReportSnapshot report)
    {
        using var connection = _context.CreateConnection();
        connection.Open();

        string query = @"
            UPDATE CashCloses
            SET CountedCash = @CountedCash,
                ClosedAt = @ClosedAt,
                CashSales = @CashSales,
                CardSales = @CardSales,
                CashEntries = @CashEntries,
                CashExits = @CashExits,
                ExpectedCash = @ExpectedCash,
                Difference = @Difference
            WHERE BusinessDate = @BusinessDate;";

        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@BusinessDate", businessDate.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("@CountedCash", report.CountedCash);
        command.Parameters.AddWithValue("@ClosedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@CashSales", report.CashSales);
        command.Parameters.AddWithValue("@CardSales", report.CardSales);
        command.Parameters.AddWithValue("@CashEntries", report.CashEntries);
        command.Parameters.AddWithValue("@CashExits", report.CashExits);
        command.Parameters.AddWithValue("@ExpectedCash", report.ExpectedCash);
        command.Parameters.AddWithValue("@Difference", report.Difference);

        if (command.ExecuteNonQuery() == 0)
            throw new InvalidOperationException("Guarda el fondo de caja antes de cerrar el día.");
    }

    public PaymentDayTotals GetPaymentTotals(DateTime businessDate)
    {
        using var connection = _context.CreateConnection();
        connection.Open();

        string query = @"
            SELECT
                COALESCE(SUM(
                    CASE
                        WHEN PaymentMethod = 'EFECTIVO' THEN Total
                        WHEN PaymentMethod = 'MIXTO' THEN Total - COALESCE(CardAmount, 0)
                        ELSE 0
                    END), 0),
                COALESCE(SUM(
                    CASE
                        WHEN PaymentMethod = 'EFECTIVO' THEN 1
                        WHEN PaymentMethod = 'MIXTO' AND (Total - COALESCE(CardAmount, 0)) > 0 THEN 1
                        ELSE 0
                    END), 0),
                COALESCE(SUM(
                    CASE
                        WHEN PaymentMethod = 'TARJETA' THEN Total
                        WHEN PaymentMethod = 'MIXTO' THEN COALESCE(CardAmount, 0)
                        ELSE 0
                    END), 0),
                COALESCE(SUM(
                    CASE
                        WHEN PaymentMethod = 'TARJETA' THEN 1
                        WHEN PaymentMethod = 'MIXTO' AND COALESCE(CardAmount, 0) > 0 THEN 1
                        ELSE 0
                    END), 0),
                MAX(CreatedAt)
            FROM Sales
            WHERE date(CreatedAt) = date(@BusinessDate);";

        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@BusinessDate", businessDate.ToString("yyyy-MM-dd"));

        using var reader = command.ExecuteReader();
        reader.Read();

        DateTime? lastSaleAt = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4));

        return new PaymentDayTotals(
            ReadMoney(reader, 0),
            Convert.ToInt32(reader.GetValue(1)),
            ReadMoney(reader, 2),
            Convert.ToInt32(reader.GetValue(3)),
            lastSaleAt);
    }

    public GrossProfitTotals GetGrossProfitTotals(DateTime startDate, DateTime endDate)
    {
        using var connection = _context.CreateConnection();
        connection.Open();

        string query = @"
            SELECT
                COALESCE(SUM(d.Quantity * COALESCE(d.UnitPrice, d.PriceSold)), 0),
                COALESCE(SUM(d.Quantity * COALESCE(d.CostPrice, 0)), 0)
            FROM SaleDetails d
            INNER JOIN Sales s ON s.Id = d.SaleId
            WHERE date(s.CreatedAt) >= date(@StartDate)
              AND date(s.CreatedAt) <= date(@EndDate);";

        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd"));

        using var reader = command.ExecuteReader();
        reader.Read();

        return new GrossProfitTotals(
            ReadMoney(reader, 0),
            ReadMoney(reader, 1));
    }

    public void SaveCashMovement(CashMovement movement)
    {
        using var connection = _context.CreateConnection();
        connection.Open();

        string query = @"
            INSERT INTO CashMovements (Id, BusinessDate, Kind, Amount, Concept, CreatedAt)
            VALUES (@Id, @BusinessDate, @Kind, @Amount, @Concept, @CreatedAt);";

        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@Id", movement.Id.ToString());
        command.Parameters.AddWithValue("@BusinessDate", movement.BusinessDate.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("@Kind", movement.KindCode);
        command.Parameters.AddWithValue("@Amount", movement.Amount);
        command.Parameters.AddWithValue("@Concept", movement.Concept);
        command.Parameters.AddWithValue("@CreatedAt", movement.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.ExecuteNonQuery();
    }

    public List<CashMovement> GetCashMovements(DateTime businessDate)
    {
        using var connection = _context.CreateConnection();
        connection.Open();

        string query = @"
            SELECT Id, BusinessDate, Kind, Amount, Concept, CreatedAt
            FROM CashMovements
            WHERE BusinessDate = @BusinessDate
            ORDER BY CreatedAt DESC, Id DESC;";

        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@BusinessDate", businessDate.ToString("yyyy-MM-dd"));

        var movements = new List<CashMovement>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            movements.Add(new CashMovement(
                Guid.Parse(reader.GetString(0)),
                DateTime.Parse(reader.GetString(1)),
                CashMovement.ParseKind(reader.GetString(2)),
                ReadMoney(reader, 3),
                reader.GetString(4),
                DateTime.Parse(reader.GetString(5))));
        }

        return movements;
    }

    public CashMovementDayTotals GetCashMovementTotals(DateTime businessDate)
    {
        using var connection = _context.CreateConnection();
        connection.Open();

        string query = @"
            SELECT
                COALESCE(SUM(CASE WHEN Kind = 'ENTRADA' THEN Amount ELSE 0 END), 0),
                COALESCE(SUM(CASE WHEN Kind = 'SALIDA' THEN Amount ELSE 0 END), 0)
            FROM CashMovements
            WHERE BusinessDate = @BusinessDate;";

        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@BusinessDate", businessDate.ToString("yyyy-MM-dd"));

        using var reader = command.ExecuteReader();
        reader.Read();

        return new CashMovementDayTotals(ReadMoney(reader, 0), ReadMoney(reader, 1));
    }

    public List<ReorderSuggestionLine> GetReorderSuggestions()
    {
        var lines = new List<ReorderSuggestionLine>();
        using var connection = _context.CreateConnection();
        connection.Open();

        string query = ProductSelect + @"
            WHERE Stock <= 0
               OR (MinStock > 0 AND Stock <= MinStock)
            ORDER BY
                CASE WHEN Stock <= 0 THEN 0 ELSE 1 END,
                Name COLLATE NOCASE;";

        using var command = new SqliteCommand(query, connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var product = MapProduct(reader);
            lines.Add(new ReorderSuggestionLine(
                product.Barcode,
                product.Name,
                product.Unit,
                product.Stock,
                product.MinStock,
                product.DesiredStock));
        }

        return lines;
    }

    private static Sale MapSaleHeader(SqliteDataReader reader)
    {
        decimal total = reader.GetDecimal(2);
        string method = reader.IsDBNull(3) ? "EFECTIVO" : reader.GetString(3);
        string cardReference = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
        decimal amountReceived = reader.IsDBNull(5) ? 0m : reader.GetDecimal(5);
        decimal cardAmount = reader.FieldCount > 6 && !reader.IsDBNull(6) ? reader.GetDecimal(6) : 0m;

        return new Sale(
            Guid.Parse(reader.GetString(0)),
            DateTime.Parse(reader.GetString(1)),
            new List<SaleItem> { new SaleItem(Guid.Empty, "", "Resumen", 1, total, 0m) },
            method,
            cardReference,
            amountReceived,
            cardAmount);
    }

    private static Product MapProduct(SqliteDataReader reader)
    {
        var unit = reader.FieldCount > 6 && !reader.IsDBNull(6)
            ? UnitOfMeasureExtensions.ParseOrDefault(reader.GetString(6))
            : UnitOfMeasure.Pieza;
        decimal stock = decimal.Round(
            Convert.ToDecimal(reader.GetValue(5)),
            unit.MaxDecimalPlaces(),
            MidpointRounding.AwayFromZero);

        decimal minStock = 0m;
        if (reader.FieldCount > 7 && !reader.IsDBNull(7))
        {
            minStock = decimal.Round(
                Convert.ToDecimal(reader.GetValue(7)),
                unit.MaxDecimalPlaces(),
                MidpointRounding.AwayFromZero);
        }

        decimal desiredStock = 0m;
        if (reader.FieldCount > 8 && !reader.IsDBNull(8))
        {
            desiredStock = decimal.Round(
                Convert.ToDecimal(reader.GetValue(8)),
                unit.MaxDecimalPlaces(),
                MidpointRounding.AwayFromZero);
        }

        return new Product(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetDecimal(3),
            reader.GetDecimal(4),
            stock,
            unit,
            minStock,
            desiredStock);
    }

    private static void BindProductParameters(SqliteCommand command, Product product)
    {
        command.Parameters.AddWithValue("@Id", product.Id.ToString());
        command.Parameters.AddWithValue("@Barcode", product.Barcode);
        command.Parameters.AddWithValue("@Name", product.Name);
        command.Parameters.AddWithValue("@CostPrice", product.CostPrice);
        command.Parameters.AddWithValue("@RetailPrice", product.RetailPrice);
        command.Parameters.AddWithValue("@Stock", product.Stock);
        command.Parameters.AddWithValue("@Unit", product.Unit.ToString());
        command.Parameters.AddWithValue("@MinStock", product.MinStock);
        command.Parameters.AddWithValue("@DesiredStock", product.DesiredStock);
    }

    private static decimal ReadMoney(SqliteDataReader reader, int ordinal) =>
        Math.Round(Convert.ToDecimal(reader.GetValue(ordinal)), 2, MidpointRounding.AwayFromZero);
}
