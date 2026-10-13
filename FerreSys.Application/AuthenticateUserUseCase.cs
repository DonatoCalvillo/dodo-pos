using FerreSys.Domain;

namespace FerreSys.Application;

public class AuthenticateUserUseCase
{
    private readonly IUserRepository _users;

    public AuthenticateUserUseCase(IUserRepository users)
    {
        _users = users;
    }

    public User ExecuteCredentials(string username, string password)
    {
        User? user = _users.GetByUsername(username.Trim());
        if (user == null || !PasswordHasher.Verify(password, user.PasswordSalt, user.PasswordHash))
            throw new InvalidOperationException("Usuario o contraseña incorrectos.");

        EnsureActive(user);
        return user;
    }

    public User ExecuteBarcode(string code)
    {
        string normalized = code.Trim().ToUpperInvariant();
        User? user = _users.GetByEmployeeNumber(normalized);
        if (user == null)
            throw new InvalidOperationException("La credencial no corresponde a un empleado.");

        EnsureActive(user);
        return user;
    }

    private static void EnsureActive(User user)
    {
        if (!user.IsActive)
            throw new InvalidOperationException("Este empleado está inactivo.");
    }
}
