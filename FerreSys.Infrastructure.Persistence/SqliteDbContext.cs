using Microsoft.Data.Sqlite;

namespace FerreSys.Infrastructure.Persistence;

public class SqliteDbContext
{
    private readonly string _connectionString;

    public string DataFolder { get; }

    public string DatabasePath { get; }

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

        DatabasePath = Path.Combine(appDataFolder, "ferresys.db");
        _connectionString = $"Data Source={DatabasePath};";

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

    public void RestoreFrom(string backupPath)
    {
        if (string.IsNullOrWhiteSpace(backupPath) || !File.Exists(backupPath))
            throw new FileNotFoundException("No se encontró el respaldo.", backupPath);

        string fullBackup = Path.GetFullPath(backupPath);
        string fullDb = Path.GetFullPath(DatabasePath);
        if (string.Equals(fullBackup, fullDb, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El archivo elegido es la base en uso.");

        using (var connection = CreateConnection())
        {
            connection.Open();
            using var checkpoint = connection.CreateCommand();
            checkpoint.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
            checkpoint.ExecuteNonQuery();
        }

        SqliteConnection.ClearAllPools();
        File.Copy(fullBackup, fullDb, overwrite: true);
        TryDelete(fullDb + "-wal");
        TryDelete(fullDb + "-shm");
    }

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
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
            DesiredStock REAL NOT NULL DEFAULT 0,
            IsActive INTEGER NOT NULL DEFAULT 1,
            ImageBlob BLOB
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

        CREATE TABLE IF NOT EXISTS SaleReturns (
            Id TEXT PRIMARY KEY,
            SaleId TEXT NOT NULL,
            CreatedAt TEXT NOT NULL,
            Amount REAL NOT NULL,
            RestoreStock INTEGER NOT NULL,
            Notes TEXT NOT NULL DEFAULT '',
            FOREIGN KEY(SaleId) REFERENCES Sales(Id)
        );
        CREATE INDEX IF NOT EXISTS IX_SaleReturns_SaleId ON SaleReturns(SaleId);

        CREATE TABLE IF NOT EXISTS SaleReturnLines (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            ReturnId TEXT NOT NULL,
            ProductId TEXT NOT NULL,
            Barcode TEXT NOT NULL,
            ProductName TEXT NOT NULL,
            Quantity REAL NOT NULL,
            UnitPrice REAL NOT NULL,
            CostPrice REAL NOT NULL,
            LineAmount REAL NOT NULL,
            FOREIGN KEY(ReturnId) REFERENCES SaleReturns(Id)
        );
        CREATE INDEX IF NOT EXISTS IX_SaleReturnLines_ReturnId ON SaleReturnLines(ReturnId);

        CREATE TABLE IF NOT EXISTS AppSettings (
            Key TEXT PRIMARY KEY,
            Value TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Users (
            Id TEXT PRIMARY KEY,
            EmployeeNumber TEXT NOT NULL UNIQUE,
            Username TEXT NOT NULL COLLATE NOCASE UNIQUE,
            PasswordSalt BLOB NOT NULL,
            PasswordHash BLOB NOT NULL,
            FirstName TEXT NOT NULL,
            LastName TEXT NOT NULL,
            BirthDate TEXT NOT NULL,
            JobTitle TEXT NOT NULL DEFAULT '',
            Phone TEXT NOT NULL DEFAULT '',
            Email TEXT NOT NULL DEFAULT '',
            HireDate TEXT,
            Notes TEXT NOT NULL DEFAULT '',
            PhotoBlob BLOB,
            IsActive INTEGER NOT NULL DEFAULT 1,
            Permissions INTEGER NOT NULL
        );
    ";

        using var command = new SqliteCommand(createTablesQuery, connection);
        command.ExecuteNonQuery();

        EnsureSalesPaymentColumns(connection);
        EnsureSaleDetailPriceColumns(connection);
        EnsureProductUnitColumn(connection);
        EnsureProductReorderColumns(connection);
        EnsureProductIsActiveColumn(connection);
        EnsureProductImageColumn(connection);
        EnsureProductPricingColumns(connection);
        EnsureSaleDiscountColumn(connection);
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

    private static void EnsureProductIsActiveColumn(SqliteConnection connection)
    {
        using var pragma = new SqliteCommand("PRAGMA table_info(Products);", connection);
        using var reader = pragma.ExecuteReader();
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
            columns.Add(reader.GetString(1));
        reader.Close();

        if (!columns.Contains("IsActive"))
        {
            using var alter = new SqliteCommand(
                "ALTER TABLE Products ADD COLUMN IsActive INTEGER NOT NULL DEFAULT 1;",
                connection);
            alter.ExecuteNonQuery();
        }

        using var index = new SqliteCommand(
            "CREATE INDEX IF NOT EXISTS IX_Products_IsActive ON Products (IsActive);",
            connection);
        index.ExecuteNonQuery();
    }

    private static void EnsureProductImageColumn(SqliteConnection connection)
    {
        using var pragma = new SqliteCommand("PRAGMA table_info(Products);", connection);
        using var reader = pragma.ExecuteReader();
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
            columns.Add(reader.GetString(1));
        reader.Close();

        if (columns.Contains("ImageBlob"))
            return;

        using var alter = new SqliteCommand(
            "ALTER TABLE Products ADD COLUMN ImageBlob BLOB;",
            connection);
        alter.ExecuteNonQuery();
    }

    private static void EnsureProductPricingColumns(SqliteConnection connection)
    {
        using var pragma = new SqliteCommand("PRAGMA table_info(Products);", connection);
        using var reader = pragma.ExecuteReader();
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
            columns.Add(reader.GetString(1));
        reader.Close();

        void AddColumnIfMissing(string name, string definition)
        {
            if (columns.Contains(name))
                return;
            using var alter = new SqliteCommand(
                $"ALTER TABLE Products ADD COLUMN {name} {definition};",
                connection);
            alter.ExecuteNonQuery();
        }

        AddColumnIfMissing("DiscountKind", "TEXT NOT NULL DEFAULT 'None'");
        AddColumnIfMissing("DiscountValue", "REAL NOT NULL DEFAULT 0");
        AddColumnIfMissing("DiscountStartsOn", "TEXT");
        AddColumnIfMissing("DiscountEndsOn", "TEXT");
        AddColumnIfMissing("WholesalePrice", "REAL");
        AddColumnIfMissing("WholesaleMinQuantity", "REAL");
    }

    private static void EnsureSaleDiscountColumn(SqliteConnection connection)
    {
        using var pragma = new SqliteCommand("PRAGMA table_info(Sales);", connection);
        using var reader = pragma.ExecuteReader();
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
            columns.Add(reader.GetString(1));
        reader.Close();

        if (columns.Contains("DiscountAmount"))
            return;

        using var alter = new SqliteCommand(
            "ALTER TABLE Sales ADD COLUMN DiscountAmount REAL NOT NULL DEFAULT 0;",
            connection);
        alter.ExecuteNonQuery();
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