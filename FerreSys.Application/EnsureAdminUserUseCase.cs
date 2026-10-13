using FerreSys.Domain;

namespace FerreSys.Application;

public class EnsureAdminUserUseCase
{
    private readonly IUserRepository _users;

    public EnsureAdminUserUseCase(IUserRepository users)
    {
        _users = users;
    }

    public void Execute()
    {
        (byte[] salt, byte[] hash) = PasswordHasher.Hash(User.AdminPassword);
        User? existing = _users.GetByUsername(User.AdminUsername) ?? _users.GetById(User.AdminId);
        if (existing == null)
        {
            _users.Save(new User(
                User.AdminId,
                User.AdminEmployeeNumber,
                User.AdminUsername,
                salt,
                hash,
                "Administrador",
                "Sistema",
                new DateOnly(1990, 1, 1),
                "Administrador",
                string.Empty,
                string.Empty,
                null,
                string.Empty,
                null,
                true,
                UserPermission.All));
            return;
        }

        existing.ReplacePassword(salt, hash);
        existing.UpdateProfile(
            existing.FirstName,
            existing.LastName,
            existing.BirthDate,
            existing.JobTitle,
            existing.Phone,
            existing.Email,
            existing.HireDate,
            existing.Notes,
            existing.Photo,
            isActive: true,
            UserPermission.All);
        _users.Save(existing);
    }
}
