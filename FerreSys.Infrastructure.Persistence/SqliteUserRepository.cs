using FerreSys.Domain;
using Microsoft.Data.Sqlite;

namespace FerreSys.Infrastructure.Persistence;

public class SqliteUserRepository : IUserRepository
{
    private const string SelectSql =
        """
        SELECT Id, EmployeeNumber, Username, PasswordSalt, PasswordHash, FirstName, LastName, BirthDate,
               JobTitle, Phone, Email, HireDate, Notes, PhotoBlob, IsActive, Permissions
        FROM Users
        """;

    private readonly SqliteDbContext _context;

    public SqliteUserRepository(SqliteDbContext context)
    {
        _context = context;
    }

    public User? GetById(Guid id) => QueryOne(" WHERE Id = @Id LIMIT 1;", ("@Id", id.ToString()));

    public User? GetByUsername(string username) =>
        QueryOne(" WHERE Username = @Username COLLATE NOCASE LIMIT 1;", ("@Username", username.Trim()));

    public User? GetByEmployeeNumber(string employeeNumber) =>
        QueryOne(
            " WHERE EmployeeNumber = @EmployeeNumber COLLATE NOCASE LIMIT 1;",
            ("@EmployeeNumber", employeeNumber.Trim().ToUpperInvariant()));

    public IReadOnlyList<User> List()
    {
        var users = new List<User>();
        using var connection = _context.CreateConnection();
        connection.Open();
        using var command = new SqliteCommand(SelectSql + " ORDER BY EmployeeNumber;", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
            users.Add(Map(reader));
        return users;
    }

    public void Save(User user)
    {
        using var connection = _context.CreateConnection();
        connection.Open();
        using var command = new SqliteCommand(
            """
            INSERT INTO Users (
                Id, EmployeeNumber, Username, PasswordSalt, PasswordHash, FirstName, LastName, BirthDate,
                JobTitle, Phone, Email, HireDate, Notes, PhotoBlob, IsActive, Permissions)
            VALUES (
                @Id, @EmployeeNumber, @Username, @PasswordSalt, @PasswordHash, @FirstName, @LastName, @BirthDate,
                @JobTitle, @Phone, @Email, @HireDate, @Notes, @Photo, @IsActive, @Permissions)
            ON CONFLICT(Id) DO UPDATE SET
                EmployeeNumber = excluded.EmployeeNumber,
                Username = excluded.Username,
                PasswordSalt = excluded.PasswordSalt,
                PasswordHash = excluded.PasswordHash,
                FirstName = excluded.FirstName,
                LastName = excluded.LastName,
                BirthDate = excluded.BirthDate,
                JobTitle = excluded.JobTitle,
                Phone = excluded.Phone,
                Email = excluded.Email,
                HireDate = excluded.HireDate,
                Notes = excluded.Notes,
                PhotoBlob = excluded.PhotoBlob,
                IsActive = excluded.IsActive,
                Permissions = excluded.Permissions;
            """,
            connection);
        Bind(command, user);
        command.ExecuteNonQuery();
    }

    public string NextEmployeeNumber()
    {
        int max = 0;
        foreach (User user in List())
        {
            if (User.TryParseEmployeeSequence(user.EmployeeNumber, out int sequence))
                max = Math.Max(max, sequence);
        }

        return User.FormatEmployeeNumber(max + 1);
    }

    private User? QueryOne(string where, (string Name, object Value) parameter)
    {
        using var connection = _context.CreateConnection();
        connection.Open();
        using var command = new SqliteCommand(SelectSql + where, connection);
        command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    private static void Bind(SqliteCommand command, User user)
    {
        command.Parameters.AddWithValue("@Id", user.Id.ToString());
        command.Parameters.AddWithValue("@EmployeeNumber", user.EmployeeNumber);
        command.Parameters.AddWithValue("@Username", user.Username);
        command.Parameters.AddWithValue("@PasswordSalt", user.PasswordSalt);
        command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
        command.Parameters.AddWithValue("@FirstName", user.FirstName);
        command.Parameters.AddWithValue("@LastName", user.LastName);
        command.Parameters.AddWithValue("@BirthDate", user.BirthDate.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("@JobTitle", user.JobTitle);
        command.Parameters.AddWithValue("@Phone", user.Phone);
        command.Parameters.AddWithValue("@Email", user.Email);
        command.Parameters.AddWithValue("@HireDate", user.HireDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Notes", user.Notes);
        command.Parameters.AddWithValue("@Photo", user.Photo ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@IsActive", user.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("@Permissions", (int)user.Permissions);
    }

    private static User Map(SqliteDataReader reader)
    {
        string? hire = reader.IsDBNull(11) ? null : reader.GetString(11);
        byte[]? photo = reader.IsDBNull(13) ? null : (byte[])reader["PhotoBlob"];
        return new User(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            reader.GetString(2),
            (byte[])reader["PasswordSalt"],
            (byte[])reader["PasswordHash"],
            reader.GetString(5),
            reader.GetString(6),
            DateOnly.ParseExact(reader.GetString(7), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            reader.GetString(8),
            reader.GetString(9),
            reader.GetString(10),
            hire == null ? null : DateOnly.ParseExact(hire, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            reader.GetString(12),
            photo,
            reader.GetInt32(14) != 0,
            (UserPermission)reader.GetInt32(15));
    }
}
