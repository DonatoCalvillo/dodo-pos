using System.Data;
using Microsoft.Data.Sqlite;
using FerreSys.Domain;

namespace FerreSys.Infrastructure.Persistence;

public class SqliteProductRepository : IProductRepository
{
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
            INSERT INTO Products (Id, Barcode, Name, CostPrice, RetailPrice, Stock)
            VALUES (@Id, @Barcode, @Name, @CostPrice, @RetailPrice, @Stock);";

        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@Id", product.Id.ToString());
        command.Parameters.AddWithValue("@Barcode", product.Barcode);
        command.Parameters.AddWithValue("@Name", product.Name);
        command.Parameters.AddWithValue("@CostPrice", product.CostPrice);
        command.Parameters.AddWithValue("@RetailPrice", product.RetailPrice);
        command.Parameters.AddWithValue("@Stock", product.Stock);

        command.ExecuteNonQuery();
    }

    public Product? GetByBarcode(string barcode)
    {
        using var connection = _context.CreateConnection();
        connection.Open();

        string query = "SELECT Id, Barcode, Name, CostPrice, RetailPrice, Stock FROM Products WHERE Barcode = @Barcode LIMIT 1;";
        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@Barcode", barcode);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new Product(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetDecimal(3),
                reader.GetDecimal(4),
                reader.GetInt32(5)
            );
        }

        return null;
    }

    public List<Product> SearchByName(string term)
    {
        var products = new List<Product>();
        using var connection = _context.CreateConnection();
        connection.Open();

        // Filtro Fuzzy simple usando LIKE. El operador % busca coincidencias en cualquier parte del texto.
        string query = "SELECT Id, Barcode, Name, CostPrice, RetailPrice, Stock FROM Products WHERE Name LIKE @Term;";
        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@Term", $"%{term}%");

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            products.Add(new Product(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetDecimal(3),
                reader.GetDecimal(4),
                reader.GetInt32(5)
            ));
        }

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
            SET Name = @Name, CostPrice = @CostPrice, RetailPrice = @RetailPrice, Stock = @Stock 
            WHERE Id = @Id;";

        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@Id", product.Id.ToString());
        command.Parameters.AddWithValue("@Name", product.Name);
        command.Parameters.AddWithValue("@CostPrice", product.CostPrice);
        command.Parameters.AddWithValue("@RetailPrice", product.RetailPrice);
        command.Parameters.AddWithValue("@Stock", product.Stock);

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
            string saleQuery = "INSERT INTO Sales (Id, CreatedAt, Total) VALUES (@Id, @CreatedAt, @Total);";
            using (var cmdSale = new SqliteCommand(saleQuery, connection, transaction))
            {
                cmdSale.Parameters.AddWithValue("@Id", sale.Id.ToString());
                cmdSale.Parameters.AddWithValue("@CreatedAt", sale.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                cmdSale.Parameters.AddWithValue("@Total", sale.Total);
                cmdSale.ExecuteNonQuery();
            }

            // Insertar los detalles
            string detailQuery = @"
            INSERT INTO SaleDetails (SaleId, ProductId, Barcode, ProductName, Quantity, PriceSold) 
            VALUES (@SaleId, @ProductId, @Barcode, @ProductName, @Quantity, @PriceSold);";

            foreach (var item in sale.Items)
            {
                using var cmdDetail = new SqliteCommand(detailQuery, connection, transaction);
                cmdDetail.Parameters.AddWithValue("@SaleId", sale.Id.ToString());
                cmdDetail.Parameters.AddWithValue("@ProductId", item.ProductId.ToString());
                cmdDetail.Parameters.AddWithValue("@Barcode", item.Barcode);
                cmdDetail.Parameters.AddWithValue("@ProductName", item.ProductName);
                cmdDetail.Parameters.AddWithValue("@Quantity", item.Quantity);
                cmdDetail.Parameters.AddWithValue("@PriceSold", item.PriceSold);
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

        string query = "SELECT Id, CreatedAt, Total FROM Sales ORDER BY CreatedAt DESC;";
        using var command = new SqliteCommand(query, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            // En un MVP, cargamos la cabecera de la venta. 
            // Inicializamos la lista de ítems vacía para no ralentizar la consulta principal.
            sales.Add(new Sale(
                Guid.Parse(reader.GetString(0)),
                DateTime.Parse(reader.GetString(1)),
                new List<SaleItem> { new SaleItem(Guid.Empty, "", "Resumen", 1, reader.GetDecimal(2)) }
            ));
        }
        return sales;
    }

    public List<Sale> GetSalesHistory(string? filterType = null)
    {
        var sales = new List<Sale>();
        using var connection = _context.CreateConnection();
        connection.Open();

        // Filtro base: por defecto trae todo
        string query = "SELECT Id, CreatedAt, Total FROM Sales";

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
            sales.Add(new Sale(
                Guid.Parse(reader.GetString(0)),
                DateTime.Parse(reader.GetString(1)),
                new List<SaleItem> { new SaleItem(Guid.Empty, "", "Resumen", 1, reader.GetDecimal(2)) }
            ));
        }
        return sales;
    }

    public List<SaleItem> GetSaleDetails(Guid saleId)
    {
        var items = new List<SaleItem>();
        using var connection = _context.CreateConnection();
        connection.Open();

        string query = "SELECT ProductId, Barcode, ProductName, Quantity, PriceSold FROM SaleDetails WHERE SaleId = @SaleId;";
        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@SaleId", saleId.ToString());

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new SaleItem(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3),
                reader.GetDecimal(4)
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
            INSERT INTO Products (Id, Barcode, Name, CostPrice, RetailPrice, Stock)
            VALUES (@Id, @Barcode, @Name, @CostPrice, @RetailPrice, @Stock);";

            foreach (var product in products)
            {
                using var command = new SqliteCommand(query, connection, transaction);
                command.Parameters.AddWithValue("@Id", product.Id.ToString());
                command.Parameters.AddWithValue("@Barcode", product.Barcode);
                command.Parameters.AddWithValue("@Name", product.Name);
                command.Parameters.AddWithValue("@CostPrice", product.CostPrice);
                command.Parameters.AddWithValue("@RetailPrice", product.RetailPrice);
                command.Parameters.AddWithValue("@Stock", product.Stock);

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
}