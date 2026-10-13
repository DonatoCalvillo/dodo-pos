using FerreSys.Domain;

namespace FerreSys.Application;

public sealed class UserDraft
{
    public Guid? Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string? Password { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateOnly BirthDate { get; init; }
    public string JobTitle { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateOnly? HireDate { get; init; }
    public string Notes { get; init; } = string.Empty;
    public byte[]? Photo { get; init; }
    public bool IsActive { get; init; } = true;
    public UserPermission Permissions { get; init; }
}

public class SaveUserUseCase
{
    private readonly IUserRepository _users;

    public SaveUserUseCase(IUserRepository users)
    {
        _users = users;
    }

    public User Execute(UserDraft draft)
    {
        string username = draft.Username.Trim();
        if (username.Length < 3 || username.Length > 32 || username.Any(ch => !char.IsLetterOrDigit(ch)))
            throw new InvalidOperationException("El usuario debe tener entre 3 y 32 letras o números, sin espacios.");

        if (!string.IsNullOrWhiteSpace(draft.Email)
            && (draft.Email.Count(ch => ch == '@') != 1 || draft.Email.StartsWith('@') || draft.Email.EndsWith('@')))
            throw new InvalidOperationException("El correo no es válido.");

        User? existing = draft.Id is Guid id ? _users.GetById(id) : null;
        if (draft.Id != null && existing == null)
            throw new InvalidOperationException("No se encontró el empleado.");

        User? sameName = _users.GetByUsername(username);
        if (sameName != null && (existing == null || sameName.Id != existing.Id))
            throw new InvalidOperationException($"El usuario \"{username}\" ya está registrado.");

        if (existing == null && string.Equals(username, User.AdminUsername, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El usuario admin está reservado.");

        bool isAdmin = existing?.IsBuiltInAdmin == true;
        if (isAdmin && !string.Equals(username, existing!.Username, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("No se puede cambiar el usuario del administrador.");

        string? password = string.IsNullOrWhiteSpace(draft.Password) ? null : draft.Password;
        if (existing == null && password == null)
            throw new InvalidOperationException("La contraseña es obligatoria para un empleado nuevo.");
        if (password != null && password.Length < 4)
            throw new InvalidOperationException("La contraseña debe tener al menos 4 caracteres.");
        if (isAdmin)
            password = null;

        byte[] salt;
        byte[] hash;
        if (password == null)
        {
            salt = existing!.PasswordSalt;
            hash = existing.PasswordHash;
        }
        else
        {
            (salt, hash) = PasswordHasher.Hash(password);
        }

        UserPermission permissions = isAdmin ? UserPermission.All : draft.Permissions;
        bool active = isAdmin || draft.IsActive;
        string employeeNumber = existing?.EmployeeNumber ?? _users.NextEmployeeNumber();
        Guid userId = existing?.Id ?? Guid.NewGuid();

        var user = new User(
            userId,
            employeeNumber,
            isAdmin ? existing!.Username : username,
            salt,
            hash,
            draft.FirstName,
            draft.LastName,
            draft.BirthDate,
            draft.JobTitle,
            draft.Phone,
            draft.Email,
            draft.HireDate,
            draft.Notes,
            draft.Photo,
            active,
            permissions);

        _users.Save(user);
        return user;
    }
}
