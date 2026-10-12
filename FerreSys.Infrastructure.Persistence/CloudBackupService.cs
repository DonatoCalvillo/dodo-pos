using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using FerreSys.Domain;

namespace FerreSys.Infrastructure.Persistence;

public sealed class CloudBackupService
{
    private const int KeyBytes = 32;
    private const int SaltBytes = 16;
    private const int NonceBytes = 12;
    private const int TagBytes = 16;
    private const int Pbkdf2Iterations = 100_000;

    private readonly SqliteDbContext _dbContext;
    private readonly ICloudBackupStore _store;

    public CloudBackupService(SqliteDbContext dbContext, ICloudBackupStore store)
    {
        _dbContext = dbContext;
        _store = store;
    }

    public string PendingFolder => Path.Combine(_dbContext.DataFolder, "pending-backups");

    public void TryEnqueuePackage(string? licenseId)
    {
        if (string.IsNullOrWhiteSpace(licenseId))
            return;

        string license = licenseId.Trim();

        string tempDir = Path.Combine(Path.GetTempPath(), "FerreSysBackups", Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(PendingFolder);

            string safeLicense = SanitizeLicense(license);
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
            string tempDb = Path.Combine(tempDir, "ferresys.db");
            string tempZip = Path.Combine(tempDir, "ferresys.zip");
            string packagePath = Path.Combine(PendingFolder, $"{safeLicense}__{stamp}.zip.enc");

            _dbContext.BackupTo(tempDb);
            using (var zip = ZipFile.Open(tempZip, ZipArchiveMode.Create))
                zip.CreateEntryFromFile(tempDb, "ferresys.db", CompressionLevel.Optimal);

            EncryptFile(tempZip, packagePath, license);
        }
        catch (Exception ex)
        {
            Log(ex.Message);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }
    }

    public async Task EnqueueAndUploadAsync(string? licenseId)
    {
        TryEnqueuePackage(licenseId);
        await TryUploadPendingAsync().ConfigureAwait(false);
    }

    public async Task TryUploadPendingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(PendingFolder))
                return;

            foreach (string file in Directory.GetFiles(PendingFolder, "*.zip.enc"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string? objectKey = ObjectKeyFromFileName(Path.GetFileName(file));
                if (objectKey == null)
                    continue;

                bool uploaded = await _store.UploadAsync(file, objectKey, cancellationToken).ConfigureAwait(false);
                if (uploaded)
                    File.Delete(file);
            }
        }
        catch (Exception ex)
        {
            Log(ex.Message);
        }
    }

    private void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(_dbContext.DataFolder);
            string path = Path.Combine(_dbContext.DataFolder, "backup_errors.log");
            File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}{Environment.NewLine}");
        }
        catch
        {
            // El mostrador no debe enterarse de un fallo de bitácora.
        }
    }

    internal static string SanitizeLicense(string license)
    {
        var chars = license.Trim().Where(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_').ToArray();
        string safe = new string(chars);
        if (safe.Length == 0)
            safe = "licencia";
        if (safe.Length > 40)
            safe = safe[..40];
        return safe;
    }

    internal static string? ObjectKeyFromFileName(string fileName)
    {
        const string suffix = ".zip.enc";
        int split = fileName.IndexOf("__", StringComparison.Ordinal);
        if (split <= 0 || !fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            return null;

        string safe = fileName[..split];
        string stamp = fileName[(split + 2)..^suffix.Length];
        if (string.IsNullOrEmpty(safe) || string.IsNullOrEmpty(stamp))
            return null;

        return $"{safe}/ferresys_backup_{stamp}.zip.enc";
    }

    private static void EncryptFile(string sourcePath, string destinationPath, string license)
    {
        byte[] plain = File.ReadAllBytes(sourcePath);
        byte[] salt = RandomNumberGenerator.GetBytes(SaltBytes);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceBytes);
        byte[] key = DeriveKey(license, salt);
        byte[] cipher = new byte[plain.Length];
        byte[] tag = new byte[TagBytes];

        using (var aes = new AesGcm(key, TagBytes))
            aes.Encrypt(nonce, plain, cipher, tag);

        using var output = File.Create(destinationPath);
        output.Write(Encoding.ASCII.GetBytes("FSBK"));
        output.WriteByte(1);
        output.Write(salt);
        output.Write(nonce);
        output.Write(tag);
        output.Write(cipher);
    }

    private static byte[] DeriveKey(string license, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(license, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, KeyBytes);
}
