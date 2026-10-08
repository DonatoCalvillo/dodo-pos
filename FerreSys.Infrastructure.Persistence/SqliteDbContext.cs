using Microsoft.Data.Sqlite;

namespace FerreSys.Infrastructure.Persistence;

public class SqliteDbContext
{
    private readonly string _connectionString;

    public string DataFolder { get; }

    public SqliteDbContext()
    {
        // Obtenemos la ruta de AppData/Local del usuario (ej: C:\Users\Nombre\AppData\Local\FerreSys)
        string appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FerreSys"
        );
        DataFolder = appDataFolder;

        // Creamos la carpeta automáticamente si no existe
        if (!Directory.Exists(appDataFolder))
        {
            Directory.CreateDirectory(appDataFolder);
        }

        string databasePath = Path.Combine(appDataFolder, "ferresys.db");
        _connectionString = $"Data Source={databasePath};";

        InitializeDatabase();
    }
    public SqliteConnection CreateConnection()
    {
        return new SqliteConnection(_connectionString);
    }

    public void BackupTo(string destinationPath)
    {
        string? directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using var source = CreateConnection();
        source.Open();

        using var destination = new SqliteConnection($"Data Source={destinationPath}");
        destination.Open();
        source.BackupDatabase(destination);
    }

    private void InitializeDatabase()
    {
        using var connection = CreateConnection();
        connection.Open();

        using (var wal = new SqliteCommand("PRAGMA journal_mode=WAL;", connection))
            wal.ExecuteNonQuery();

        string createTablesQuery = @"
        CREATE TABLE IF NOT EXISTS Products (
            Id TEXT PRIMARY KEY,
            Barcode TEXT NOT NULL UNIQUE,
            Name TEXT NOT NULL,
            CostPrice REAL NOT NULL,
            RetailPrice REAL NOT NULL,
            Stock REAL NOT NULL,
            Unit TEXT NOT NULL DEFAULT 'Pieza',
            MinStock REAL NOT NULL DEFAULT 0,
            DesiredStock REAL NOT NULL DEFAULT 0
        );
        CREATE INDEX IF NOT EXISTS idx_products_barcode ON Products(Barcode);

        CREATE TABLE IF NOT EXISTS Sales (
            Id TEXT PRIMARY KEY,
            CreatedAt TEXT NOT NULL,
            Total REAL NOT NULL,
            PaymentMethod TEXT NOT NULL DEFAULT 'EFECTIVO',
            CardReference TEXT NOT NULL DEFAULT '',
            AmountReceived REAL NOT NULL DEFAULT 0,
            CardAmount REAL NOT NULL DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS SaleDetails (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            SaleId TEXT NOT NULL,
            ProductId TEXT NOT NULL,
            Barcode TEXT NOT NULL,
            ProductName TEXT NOT NULL,
            Quantity REAL NOT NULL,
            PriceSold REAL NOT NULL,
            UnitPrice REAL,
            CostPrice REAL,
            Unit TEXT NOT NULL DEFAULT 'Pieza',
            FOREIGN KEY(SaleId) REFERENCES Sales(Id)
        );

        CREATE TABLE IF NOT EXISTS CashCloses (
            BusinessDate TEXT PRIMARY KEY,
            OpeningCash REAL NOT NULL,
            CountedCash REAL,
            ClosedAt TEXT,
            CashSales REAL,
            CardSales REAL,
            CashEntries REAL,
            CashExits REAL,
            ExpectedCash REAL,
            Difference REAL
        );

        CREATE TABLE IF NOT EXISTS CashMovements (
            Id TEXT PRIMARY KEY,
            BusinessDate TEXT NOT NULL,
            Kind TEXT NOT NULL,
            Amount REAL NOT NULL,
            Concept TEXT NOT NULL,
            CreatedAt TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_CashMovements_BusinessDate ON CashMovements(BusinessDate);

        CREATE TABLE IF NOT EXISTS StockMovements (
            Id TEXT PRIMARY KEY,
            ProductId TEXT NOT NULL,
            Barcode TEXT NOT NULL,
            ProductName TEXT NOT NULL,
            OccurredAt TEXT NOT NULL,
            Kind TEXT NOT NULL,
            QuantityBefore REAL NOT NULL,
            QuantityDelta REAL NOT NULL,
            QuantityAfter REAL NOT NULL,
            Actor TEXT NOT NULL,
            ReferenceId TEXT,
            Notes TEXT NOT NULL DEFAULT ''
        );
        CREATE INDEX IF NOT EXISTS IX_StockMovements_ProductId_OccurredAt
            ON StockMovements(ProductId, OccurredAt);
        CREATE INDEX IF NOT EXISTS IX_StockMovements_OccurredAt
            ON StockMovements(OccurredAt);

        CREATE TABLE IF NOT EXISTS AppSettings (
            Key TEXT PRIMARY KEY,
            Value TEXT NOT NULL
        );
    ";

        using var command = new SqliteCommand(createTablesQuery, connection);
        command.ExecuteNonQuery();

        EnsureSalesPaymentColumns(connection);
        EnsureSaleDetailPriceColumns(connection);
        EnsureProductUnitColumn(connection);
        EnsureProductReorderColumns(connection);
        EnsureSaleDetailUnitColumn(connection);
        EnsureCashCloseReportColumns(connection);
    }

    private static void EnsureCashCloseReportColumns(SqliteConnection connection)
    {
        using var pragma = new SqliteCommand("PRAGMA table_info(CashCloses);", connection);
        using var reader = pragma.ExecuteReader();
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
            columns.Add(reader.GetString(1));
        reader.Close();

        void AddColumnIfMissing(string name)
        {
            if (columns.Contains(name))
                return;
            using var alter = new SqliteCommand(
                $"ALTER TABLE CashCloses ADD COLUMN {name} REAL;",
                connection);
            alter.ExecuteNonQuery();
        }

        AddColumnIfMissing("CashSales");
        AddColumnIfMissing("CardSales");
        AddColumnIfMissing("CashEntries");
        AddColumnIfMissing("CashExits");
        AddColumnIfMissing("ExpectedCash");
        AddColumnIfMissing("Difference");
    }

    private static void EnsureSalesPaymentColumns(SqliteConnection connection)
    {
        // CREATE TABLE IF NOT EXISTS no agrega columnas nuevas a bases ya existentes
        using var pragma = new SqliteCommand("PRAGMA table_info(Sales);", connection);
        using var reader = pragma.ExecuteReader();
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
            columns.Add(reader.GetString(1));
        reader.Close();

        if (!columns.Contains("PaymentMethod"))
        {
            using var alter = new SqliteCommand(
                "ALTER TABLE Sales ADD COLUMN PaymentMethod TEXT NOT NULL DEFAULT 'EFECTIVO';",
                connection);
            alter.ExecuteNonQuery();
        }

        if (!columns.Contains("CardReference"))
        {
            using var alter = new SqliteCommand(
                "ALTER TABLE Sales ADD COLUMN CardReference TEXT NOT NULL DEFAULT '';",
                connection);
            alter.ExecuteNonQuery();
        }

        if (!columns.Contains("AmountReceived"))
        {
            using var alter = new SqliteCommand(
                "ALTER TABLE Sales ADD COLUMN AmountReceived REAL NOT NULL DEFAULT 0;",
                connection);
            alter.ExecuteNonQuery();
        }

        if (!columns.Contains("CardAmount"))
        {
            using var alter = new SqliteCommand(
                "ALTER TABLE Sales ADD COLUMN CardAmount REAL NOT NULL DEFAULT 0;",
                connection);
            alter.ExecuteNonQuery();
        }
    }

    private static void EnsureSaleDetailPriceColumns(SqliteConnection connection)
    {
        using var pragma = new SqliteCommand("PRAGMA table_info(SaleDetails);", connection);
        using var reader = pragma.ExecuteReader();
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
            columns.Add(reader.GetString(1));
        reader.Close();

        if (!columns.Contains("UnitPrice"))
        {
            using var alter = new SqliteCommand(
                "ALTER TABLE SaleDetails ADD COLUMN UnitPrice REAL;",
                connection);
            alter.ExecuteNonQuery();
        }

        if (!columns.Contains("CostPrice"))
        {
            using var alter = new SqliteCommand(
                "ALTER TABLE SaleDetails ADD COLUMN CostPrice REAL;",
                connection);
            alter.ExecuteNonQuery();
        }

        using var backfill = new SqliteCommand(
            "UPDATE SaleDetails SET UnitPrice = PriceSold WHERE UnitPrice IS NULL;",
            connection);
        backfill.ExecuteNonQuery();
    }

    private static void EnsureProductUnitColumn(SqliteConnection connection)
    {
        using var pragma = new SqliteCommand("PRAGMA table_info(Products);", connection);
        using var reader = pragma.ExecuteReader();
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
            columns.Add(reader.GetString(1));
        reader.Close();

        if (!columns.Contains("Unit"))
        {
            using var alter = new SqliteCommand(
                "ALTER TABLE Products ADD COLUMN Unit TEXT NOT NULL DEFAULT 'Pieza';",
                connection);
            alter.ExecuteNonQuery();
        }
    }

    private static void EnsureProductReorderColumns(SqliteConnection connection)
    {
        using var pragma = new SqliteCommand("PRAGMA table_info(Products);", connection);
        using var reader = pragma.ExecuteReader();
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
            columns.Add(reader.GetString(1));
        reader.Close();

        if (!columns.Contains("MinStock"))
        {
            using var alter = new SqliteCommand(
                "ALTER TABLE Products ADD COLUMN MinStock REAL NOT NULL DEFAULT 0;",
                connection);
            alter.ExecuteNonQuery();
        }

        if (!columns.Contains("DesiredStock"))
        {
            using var alter = new SqliteCommand(
                "ALTER TABLE Products ADD COLUMN DesiredStock REAL NOT NULL DEFAULT 0;",
                connection);
            alter.ExecuteNonQuery();
        }
    }

    private static void EnsureSaleDetailUnitColumn(SqliteConnection connection)
    {
        using var pragma = new SqliteCommand("PRAGMA table_info(SaleDetails);", connection);
        using var reader = pragma.ExecuteReader();
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
            columns.Add(reader.GetString(1));
        reader.Close();

        if (!columns.Contains("Unit"))
        {
            using var alter = new SqliteCommand(
                "ALTER TABLE SaleDetails ADD COLUMN Unit TEXT NOT NULL DEFAULT 'Pieza';",
                connection);
            alter.ExecuteNonQuery();
        }
    }
}