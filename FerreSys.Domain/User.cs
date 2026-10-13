namespace FerreSys.Domain;

public sealed class User
{
    public static readonly Guid AdminId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    public const string AdminUsername = "admin";
    public const string AdminPassword = "Admin123!";
    public const string AdminEmployeeNumber = "FS000001";
    public const string BarcodePrefix = "FS";

    public Guid Id { get; }
    public string EmployeeNumber { get; }
    public string Username { get; }
    public byte[] PasswordSalt { get; private set; }
    public byte[] PasswordHash { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public DateOnly BirthDate { get; private set; }
    public string JobTitle { get; private set; }
    public string Phone { get; private set; }
    public string Email { get; private set; }
    public DateOnly? HireDate { get; private set; }
    public string Notes { get; private set; }
    public byte[]? Photo { get; private set; }
    public bool IsActive { get; private set; }
    public UserPermission Permissions { get; private set; }

    public bool IsBuiltInAdmin =>
        Id == AdminId || string.Equals(Username, AdminUsername, StringComparison.OrdinalIgnoreCase);

    public string DisplayName
    {
        get
        {
            string name = $"{FirstName} {LastName}".Trim();
            return name.Length == 0 ? Username : name;
        }
    }

    public User(
        Guid id,
        string employeeNumber,
        string username,
        byte[] passwordSalt,
        byte[] passwordHash,
        string firstName,
        string lastName,
        DateOnly birthDate,
        string jobTitle,
        string phone,
        string email,
        DateOnly? hireDate,
        string notes,
        byte[]? photo,
        bool isActive,
        UserPermission permissions)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("El usuario necesita un identificador.");
        if (string.IsNullOrWhiteSpace(employeeNumber))
            throw new ArgumentException("El número de empleado es obligatorio.");
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("El usuario es obligatorio.");
        if (passwordSalt is not { Length: > 0 } || passwordHash is not { Length: > 0 })
            throw new ArgumentException("La contraseña almacenada no es válida.");
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Los apellidos son obligatorios.");
        if (birthDate > DateOnly.FromDateTime(DateTime.Today) || birthDate.Year < 1900)
            throw new ArgumentException("La fecha de nacimiento no es válida.");

        Id = id;
        EmployeeNumber = employeeNumber.Trim().ToUpperInvariant();
        Username = username.Trim();
        PasswordSalt = passwordSalt;
        PasswordHash = passwordHash;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        BirthDate = birthDate;
        JobTitle = jobTitle.Trim();
        Phone = phone.Trim();
        Email = email.Trim();
        HireDate = hireDate;
        Notes = notes.Trim();
        Photo = photo is { Length: > 0 } ? photo : null;
        IsActive = isActive;
        Permissions = permissions;
    }

    public bool Has(UserPermission permission) =>
        permission == UserPermission.None || (Permissions & permission) == permission;

    public void ReplacePassword(byte[] salt, byte[] hash)
    {
        if (salt is not { Length: > 0 } || hash is not { Length: > 0 })
            throw new ArgumentException("La contraseña almacenada no es válida.");

        PasswordSalt = salt;
        PasswordHash = hash;
    }

    public void UpdateProfile(
        string firstName,
        string lastName,
        DateOnly birthDate,
        string jobTitle,
        string phone,
        string email,
        DateOnly? hireDate,
        string notes,
        byte[]? photo,
        bool isActive,
        UserPermission permissions)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Los apellidos son obligatorios.");
        if (birthDate > DateOnly.FromDateTime(DateTime.Today) || birthDate.Year < 1900)
            throw new ArgumentException("La fecha de nacimiento no es válida.");

        if (IsBuiltInAdmin)
        {
            isActive = true;
            permissions = UserPermission.All;
        }

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        BirthDate = birthDate;
        JobTitle = jobTitle.Trim();
        Phone = phone.Trim();
        Email = email.Trim();
        HireDate = hireDate;
        Notes = notes.Trim();
        Photo = photo is { Length: > 0 } ? photo : null;
        IsActive = isActive;
        Permissions = permissions;
    }

    public static string FormatEmployeeNumber(int sequence)
    {
        if (sequence < 1)
            throw new ArgumentOutOfRangeException(nameof(sequence));

        return $"{BarcodePrefix}{sequence:D6}";
    }

    public static bool TryParseEmployeeSequence(string employeeNumber, out int sequence)
    {
        sequence = 0;
        if (string.IsNullOrWhiteSpace(employeeNumber))
            return false;

        string value = employeeNumber.Trim();
        if (!value.StartsWith(BarcodePrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        return int.TryParse(value[BarcodePrefix.Length..], out sequence) && sequence > 0;
    }

    public static bool LooksLikeEmployeeBarcode(string code)
    {
        return TryParseEmployeeSequence(code, out _);
    }
}
