using FerreSys.Domain;
using Microsoft.Data.Sqlite;

namespace FerreSys.Infrastructure.Persistence;

public sealed class SqliteLoyaltyRepository : ILoyaltyRepository
{
    private readonly SqliteDbContext _context;

    public SqliteLoyaltyRepository(SqliteDbContext context)
    {
        _context = context;
    }

    public IReadOnlyList<CustomerBalance> ListCustomers()
    {
        const string sql = """
            SELECT c.Id, c.Name, c.Phone, c.Email, c.Notes, c.IsActive, c.CreatedAt,
                   c.Rfc, c.LegalName, c.TaxRegime, c.TaxZipCode, c.CfdiUse,
                   COALESCE(SUM(m.Points), 0)
            FROM Customers c
            LEFT JOIN LoyaltyMovements m ON m.CustomerId = c.Id
            GROUP BY c.Id, c.Name, c.Phone, c.Email, c.Notes, c.IsActive, c.CreatedAt,
                     c.Rfc, c.LegalName, c.TaxRegime, c.TaxZipCode, c.CfdiUse
            ORDER BY c.Name COLLATE NOCASE;
            """;

        return QueryBalances(sql, null);
    }

    public Customer? GetCustomer(Guid id)
    {
        using var connection = _context.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, Phone, Email, Notes, IsActive, CreatedAt, Rfc, LegalName, TaxRegime, TaxZipCode, CfdiUse
            FROM Customers
            WHERE Id = @Id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@Id", id.ToString());
        using var reader = command.ExecuteReader();
        return reader.Read() ? MapCustomer(reader) : null;
    }

    public Customer? FindByPhone(string phone)
    {
        using var connection = _context.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, Phone, Email, Notes, IsActive, CreatedAt, Rfc, LegalName, TaxRegime, TaxZipCode, CfdiUse
            FROM Customers
            WHERE Phone = @Phone
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@Phone", phone);
        using var reader = command.ExecuteReader();
        return reader.Read() ? MapCustomer(reader) : null;
    }

    public IReadOnlyList<CustomerBalance> SearchActive(string term, int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(term) || limit <= 0)
            return [];

        const string sql = """
            SELECT c.Id, c.Name, c.Phone, c.Email, c.Notes, c.IsActive, c.CreatedAt,
                   c.Rfc, c.LegalName, c.TaxRegime, c.TaxZipCode, c.CfdiUse,
                   COALESCE(SUM(m.Points), 0)
            FROM Customers c
            LEFT JOIN LoyaltyMovements m ON m.CustomerId = c.Id
            WHERE c.IsActive = 1
              AND (c.Name LIKE @Term OR c.Phone LIKE @Term)
            GROUP BY c.Id, c.Name, c.Phone, c.Email, c.Notes, c.IsActive, c.CreatedAt,
                     c.Rfc, c.LegalName, c.TaxRegime, c.TaxZipCode, c.CfdiUse
            ORDER BY c.Name COLLATE NOCASE
            LIMIT @Limit;
            """;

        return QueryBalances(sql, command =>
        {
            command.Parameters.AddWithValue("@Term", $"%{term.Trim()}%");
            command.Parameters.AddWithValue("@Limit", limit);
        });
    }

    public int GetBalance(Guid customerId)
    {
        using var connection = _context.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(SUM(Points), 0) FROM LoyaltyMovements WHERE CustomerId = @Id;";
        command.Parameters.AddWithValue("@Id", customerId.ToString());
        object? value = command.ExecuteScalar();
        return value == null || value is DBNull ? 0 : Convert.ToInt32(value);
    }

    public void SaveCustomer(Customer customer)
    {
        using var connection = _context.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Customers (Id, Name, Phone, Email, Notes, IsActive, CreatedAt, Rfc, LegalName, TaxRegime, TaxZipCode, CfdiUse)
            VALUES (@Id, @Name, @Phone, @Email, @Notes, @IsActive, @CreatedAt, @Rfc, @LegalName, @TaxRegime, @TaxZipCode, @CfdiUse)
            ON CONFLICT(Id) DO UPDATE SET
                Name = excluded.Name,
                Phone = excluded.Phone,
                Email = excluded.Email,
                Notes = excluded.Notes,
                IsActive = excluded.IsActive,
                Rfc = excluded.Rfc,
                LegalName = excluded.LegalName,
                TaxRegime = excluded.TaxRegime,
                TaxZipCode = excluded.TaxZipCode,
                CfdiUse = excluded.CfdiUse;
            """;
        command.Parameters.AddWithValue("@Id", customer.Id.ToString());
        command.Parameters.AddWithValue("@Name", customer.Name);
        command.Parameters.AddWithValue("@Phone", customer.Phone);
        command.Parameters.AddWithValue("@Email", customer.Email);
        command.Parameters.AddWithValue("@Notes", customer.Notes);
        command.Parameters.AddWithValue("@IsActive", customer.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("@CreatedAt", customer.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@Rfc", customer.Rfc);
        command.Parameters.AddWithValue("@LegalName", customer.LegalName);
        command.Parameters.AddWithValue("@TaxRegime", customer.TaxRegime);
        command.Parameters.AddWithValue("@TaxZipCode", customer.TaxZipCode);
        command.Parameters.AddWithValue("@CfdiUse", customer.CfdiUse);
        try
        {
            command.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException("Ese teléfono ya está registrado.");
        }
    }

    public IReadOnlyList<LoyaltyReward> ListRewards()
    {
        var rewards = new List<LoyaltyReward>();
        using var connection = _context.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, Description, PointsCost, Kind, Value, IsActive
            FROM LoyaltyRewards
            ORDER BY PointsCost, Name COLLATE NOCASE;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
            rewards.Add(MapReward(reader));
        return rewards;
    }

    public LoyaltyReward? GetReward(Guid id)
    {
        using var connection = _context.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, Description, PointsCost, Kind, Value, IsActive
            FROM LoyaltyRewards
            WHERE Id = @Id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@Id", id.ToString());
        using var reader = command.ExecuteReader();
        return reader.Read() ? MapReward(reader) : null;
    }

    public void SaveReward(LoyaltyReward reward)
    {
        using var connection = _context.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO LoyaltyRewards (Id, Name, Description, PointsCost, Kind, Value, IsActive)
            VALUES (@Id, @Name, @Description, @PointsCost, @Kind, @Value, @IsActive)
            ON CONFLICT(Id) DO UPDATE SET
                Name = excluded.Name,
                Description = excluded.Description,
                PointsCost = excluded.PointsCost,
                Kind = excluded.Kind,
                Value = excluded.Value,
                IsActive = excluded.IsActive;
            """;
        command.Parameters.AddWithValue("@Id", reward.Id.ToString());
        command.Parameters.AddWithValue("@Name", reward.Name);
        command.Parameters.AddWithValue("@Description", reward.Description);
        command.Parameters.AddWithValue("@PointsCost", reward.PointsCost);
        command.Parameters.AddWithValue("@Kind", reward.Kind.ToString());
        command.Parameters.AddWithValue("@Value", reward.Value);
        command.Parameters.AddWithValue("@IsActive", reward.IsActive ? 1 : 0);
        command.ExecuteNonQuery();
    }

    public LoyaltyReversalTotals GetReversalTotals(Guid saleId)
    {
        using var connection = _context.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                COALESCE(SUM(CASE WHEN Points < 0 THEN -Points ELSE 0 END), 0),
                COALESCE(SUM(CASE WHEN Points > 0 THEN Points ELSE 0 END), 0)
            FROM LoyaltyMovements
            WHERE SaleId = @SaleId AND Kind = @Kind;
            """;
        command.Parameters.AddWithValue("@SaleId", saleId.ToString());
        command.Parameters.AddWithValue("@Kind", nameof(LoyaltyMovementKind.Reversal));
        using var reader = command.ExecuteReader();
        if (!reader.Read())
            return default;

        return new LoyaltyReversalTotals(Convert.ToInt32(reader.GetValue(0)), Convert.ToInt32(reader.GetValue(1)));
    }

    internal static void InsertMovement(SqliteConnection connection, SqliteTransaction transaction, LoyaltyMovement movement)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO LoyaltyMovements (Id, CustomerId, SaleId, OccurredAt, Kind, Points, RewardId, Note)
            VALUES (@Id, @CustomerId, @SaleId, @OccurredAt, @Kind, @Points, @RewardId, @Note);
            """;
        command.Parameters.AddWithValue("@Id", movement.Id.ToString());
        command.Parameters.AddWithValue("@CustomerId", movement.CustomerId.ToString());
        command.Parameters.AddWithValue("@SaleId", movement.SaleId?.ToString() ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OccurredAt", movement.OccurredAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@Kind", movement.Kind.ToString());
        command.Parameters.AddWithValue("@Points", movement.Points);
        command.Parameters.AddWithValue("@RewardId", movement.RewardId?.ToString() ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Note", movement.Note);
        command.ExecuteNonQuery();
    }

    private List<CustomerBalance> QueryBalances(string sql, Action<SqliteCommand>? bind)
    {
        var rows = new List<CustomerBalance>();
        using var connection = _context.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        bind?.Invoke(command);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            Customer customer = MapCustomer(reader);
            int points = Convert.ToInt32(reader.GetValue(12));
            rows.Add(new CustomerBalance(customer, points));
        }

        return rows;
    }

    private static Customer MapCustomer(SqliteDataReader reader) =>
        new(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            !reader.IsDBNull(5) && Convert.ToInt32(reader.GetValue(5)) != 0,
            DateTime.Parse(reader.GetString(6)),
            reader.FieldCount > 7 && !reader.IsDBNull(7) ? reader.GetString(7) : string.Empty,
            reader.FieldCount > 8 && !reader.IsDBNull(8) ? reader.GetString(8) : string.Empty,
            reader.FieldCount > 9 && !reader.IsDBNull(9) ? reader.GetString(9) : string.Empty,
            reader.FieldCount > 10 && !reader.IsDBNull(10) ? reader.GetString(10) : string.Empty,
            reader.FieldCount > 11 && !reader.IsDBNull(11) ? reader.GetString(11) : string.Empty);

    private static LoyaltyReward MapReward(SqliteDataReader reader)
    {
        string kindText = reader.IsDBNull(4) ? nameof(LoyaltyRewardKind.Benefit) : reader.GetString(4);
        if (!Enum.TryParse(kindText, ignoreCase: true, out LoyaltyRewardKind kind))
            kind = LoyaltyRewardKind.Benefit;

        return new LoyaltyReward(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            Convert.ToInt32(reader.GetValue(3)),
            kind,
            reader.IsDBNull(5) ? 0m : Convert.ToDecimal(reader.GetValue(5)),
            !reader.IsDBNull(6) && Convert.ToInt32(reader.GetValue(6)) != 0);
    }
}
