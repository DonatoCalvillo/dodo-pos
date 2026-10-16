using FerreSys.Domain;
using Microsoft.Data.Sqlite;

namespace FerreSys.Infrastructure.Persistence;

public sealed class SqliteSupplierRepository : ISupplierRepository
{
    private readonly SqliteDbContext _context;

    public SqliteSupplierRepository(SqliteDbContext context)
    {
        _context = context;
    }

    public IReadOnlyList<Supplier> List()
    {
        var suppliers = new List<Supplier>();
        using var connection = _context.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, ContactName, Phone, Email, Address, Notes, IsActive, CreatedAt
            FROM Suppliers
            ORDER BY Name COLLATE NOCASE;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
            suppliers.Add(Map(reader));
        return suppliers;
    }

    public Supplier? Get(Guid id)
    {
        using var connection = _context.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, ContactName, Phone, Email, Address, Notes, IsActive, CreatedAt
            FROM Suppliers
            WHERE Id = @Id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@Id", id.ToString());
        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public void Save(Supplier supplier)
    {
        using var connection = _context.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Suppliers (Id, Name, ContactName, Phone, Email, Address, Notes, IsActive, CreatedAt)
            VALUES (@Id, @Name, @ContactName, @Phone, @Email, @Address, @Notes, @IsActive, @CreatedAt)
            ON CONFLICT(Id) DO UPDATE SET
                Name = excluded.Name,
                ContactName = excluded.ContactName,
                Phone = excluded.Phone,
                Email = excluded.Email,
                Address = excluded.Address,
                Notes = excluded.Notes,
                IsActive = excluded.IsActive;
            """;
        command.Parameters.AddWithValue("@Id", supplier.Id.ToString());
        command.Parameters.AddWithValue("@Name", supplier.Name);
        command.Parameters.AddWithValue("@ContactName", supplier.ContactName);
        command.Parameters.AddWithValue("@Phone", supplier.Phone);
        command.Parameters.AddWithValue("@Email", supplier.Email);
        command.Parameters.AddWithValue("@Address", supplier.Address);
        command.Parameters.AddWithValue("@Notes", supplier.Notes);
        command.Parameters.AddWithValue("@IsActive", supplier.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("@CreatedAt", supplier.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.ExecuteNonQuery();
    }

    private static Supplier Map(SqliteDataReader reader)
    {
        return new Supplier(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetInt32(7) == 1,
            DateTime.Parse(reader.GetString(8)));
    }
}
