using Microsoft.Data.Sqlite;

namespace FerreSys.Infrastructure.Persistence;

public class SqliteDbContext
{
    private readonly string _connectionString;

    public SqliteDbContext()
    {
        // El archivo se guardará en la misma carpeta del ejecutable con el nombre "ferresys.db"
        string databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ferresys.db");
        _connectionString = $"Data Source={databasePath};";

        InitializeDatabase();
    }

    public SqliteConnection CreateConnection()
    {
        return new SqliteConnection(_connectionString);
    }

    private void InitializeDatabase()
    {
        using var connection = CreateConnection();
        connection.Open();

        string createTablesQuery = @"
        CREATE TABLE IF NOT EXISTS Products (
            Id TEXT PRIMARY KEY,
            Barcode TEXT NOT NULL UNIQUE,
            Name TEXT NOT NULL,
            CostPrice REAL NOT NULL,
            RetailPrice REAL NOT NULL,
            Stock INTEGER NOT NULL
        );
        CREATE INDEX IF NOT EXISTS idx_products_barcode ON Products(Barcode);

        CREATE TABLE IF NOT EXISTS Sales (
            Id TEXT PRIMARY KEY,
            CreatedAt TEXT NOT NULL,
            Total REAL NOT NULL
        );

        CREATE TABLE IF NOT EXISTS SaleDetails (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            SaleId TEXT NOT NULL,
            ProductId TEXT NOT NULL,
            Barcode TEXT NOT NULL,
            ProductName TEXT NOT NULL,
            Quantity INTEGER NOT NULL,
            PriceSold REAL NOT NULL,
            FOREIGN KEY(SaleId) REFERENCES Sales(Id)
        );
    ";

        using var command = new SqliteCommand(createTablesQuery, connection);
        command.ExecuteNonQuery();
    }
}