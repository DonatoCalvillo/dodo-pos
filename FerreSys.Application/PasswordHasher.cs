using System.Security.Cryptography;

namespace FerreSys.Application;

public static class PasswordHasher
{
    public const int Iterations = 100_000;
    private const int SaltLength = 16;
    private const int HashLength = 32;

    public static (byte[] Salt, byte[] Hash) Hash(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("La contraseña es obligatoria.");

        byte[] salt = RandomNumberGenerator.GetBytes(SaltLength);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashLength);
        return (salt, hash);
    }

    public static bool Verify(string password, byte[] salt, byte[] hash)
    {
        if (string.IsNullOrEmpty(password) || salt is not { Length: > 0 } || hash is not { Length: > 0 })
            return false;

        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            hash.Length);
        return CryptographicOperations.FixedTimeEquals(actual, hash);
    }
}
