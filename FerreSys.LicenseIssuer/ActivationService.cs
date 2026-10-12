using System.Security.Cryptography;
using FerreSys.Domain;
using FerreSys.Licensing;

namespace FerreSys.LicenseIssuer;

internal sealed class ActivationService
{
    public const int BillingDays = 30;

    private readonly string _privateKeyXml;
    private readonly ActivationLedgerStore _ledger;

    public ActivationService(string privateKeyXml, ActivationLedgerStore ledger)
    {
        _privateKeyXml = privateKeyXml;
        _ledger = ledger;
    }

    public ActivationRecord CreateCode(string email, string customerName)
    {
        email = NormalizeEmail(email);
        customerName = customerName.Trim();
        if (email.Length == 0)
            throw new InvalidOperationException("El correo es obligatorio.");
        if (customerName.Length == 0)
            throw new InvalidOperationException("El nombre del cliente es obligatorio.");

        ActivationLedger ledger = _ledger.Load();
        var record = new ActivationRecord
        {
            Code = NewCode(ledger),
            Email = email,
            CustomerName = customerName,
            CreatedAt = DateTime.UtcNow
        };
        ledger.Records.Add(record);
        _ledger.Save(ledger);
        return record;
    }

    public (SignedLicenseContainer Container, string Code) IssueFile(string email, string customerName, string hardwareId, int days)
    {
        email = NormalizeEmail(email);
        customerName = customerName.Trim();
        hardwareId = hardwareId.Trim();
        if (email.Length == 0 || customerName.Length == 0 || hardwareId.Length == 0)
            throw new InvalidOperationException("Correo, nombre y hardware id son obligatorios.");
        if (days < 1)
            throw new InvalidOperationException("Los días de vigencia deben ser al menos 1.");

        DateTime issuedAt = DateTime.UtcNow;
        var payload = new LicensePayload
        {
            CustomerName = customerName,
            CustomerEmail = email,
            PlanName = "Premium",
            HardwareId = hardwareId,
            IssuedAt = issuedAt,
            ExpiresAt = issuedAt.AddDays(days),
            GraceDays = 5
        };

        ActivationLedger ledger = _ledger.Load();
        string code = NewCode(ledger);
        ledger.Records.Add(new ActivationRecord
        {
            Code = code,
            Email = email,
            CustomerName = customerName,
            HardwareId = hardwareId,
            LicenseId = payload.LicenseId,
            IssuedAt = payload.IssuedAt,
            ExpiresAt = payload.ExpiresAt,
            CreatedAt = issuedAt
        });
        _ledger.Save(ledger);
        return (LicenseSigner.Sign(payload, _privateKeyXml), code);
    }

    public void Revoke(string code)
    {
        string normalized = NormalizeCode(code);
        ActivationLedger ledger = _ledger.Load();
        ActivationRecord? record = ledger.Records.FirstOrDefault(item =>
            string.Equals(item.Code, normalized, StringComparison.OrdinalIgnoreCase));
        if (record == null)
            throw new InvalidOperationException("No existe ese código de activación.");

        record.Revoked = true;
        _ledger.Save(ledger);
    }

    public SignedLicenseContainer Activate(string email, string code, string hardwareId)
    {
        email = NormalizeEmail(email);
        hardwareId = hardwareId.Trim();
        string normalized = NormalizeCode(code);
        if (email.Length == 0 || normalized.Length == 0 || hardwareId.Length == 0)
            throw new InvalidOperationException("Correo, código y hardware id son obligatorios.");

        ActivationLedger ledger = _ledger.Load();
        ActivationRecord? record = ledger.Records.FirstOrDefault(item =>
            string.Equals(item.Code, normalized, StringComparison.OrdinalIgnoreCase));
        if (record == null || !string.Equals(record.Email, email, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Código de activación no válido para ese correo.");
        if (record.Revoked)
            throw new InvalidOperationException("Este código fue revocado.");
        if (!string.IsNullOrWhiteSpace(record.HardwareId)
            && !string.Equals(record.HardwareId, hardwareId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Este código ya está activado en otra computadora.");
        }

        DateTime issuedAt = record.IssuedAt ?? DateTime.UtcNow;
        DateTime expiresAt = record.ExpiresAt ?? issuedAt.AddDays(BillingDays);
        if (string.IsNullOrWhiteSpace(record.HardwareId))
        {
            issuedAt = DateTime.UtcNow;
            expiresAt = issuedAt.AddDays(BillingDays);
            record.HardwareId = hardwareId;
            record.LicenseId = Guid.NewGuid().ToString();
            record.IssuedAt = issuedAt;
            record.ExpiresAt = expiresAt;
            _ledger.Save(ledger);
        }

        var payload = new LicensePayload
        {
            LicenseId = record.LicenseId ?? Guid.NewGuid().ToString(),
            CustomerName = record.CustomerName,
            CustomerEmail = record.Email,
            PlanName = "Premium",
            HardwareId = hardwareId,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            GraceDays = 5
        };
        record.LicenseId = payload.LicenseId;
        _ledger.Save(ledger);
        return LicenseSigner.Sign(payload, _privateKeyXml);
    }

    public SignedLicenseContainer Renew(string licenseId, string hardwareId, string email)
    {
        licenseId = licenseId.Trim();
        hardwareId = hardwareId.Trim();
        email = NormalizeEmail(email);

        ActivationLedger ledger = _ledger.Load();
        ActivationRecord? record = ledger.Records.LastOrDefault(item =>
            string.Equals(item.LicenseId, licenseId, StringComparison.Ordinal));
        if (record == null)
            throw new InvalidOperationException("No hay una licencia activa con ese identificador.");
        if (record.Revoked)
            throw new InvalidOperationException("Esta licencia fue revocada.");
        if (!string.Equals(record.Email, email, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(record.HardwareId, hardwareId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("La renovación no corresponde a esta licencia.");
        }

        DateTime start = record.ExpiresAt.HasValue && record.ExpiresAt.Value > DateTime.UtcNow
            ? record.ExpiresAt.Value
            : DateTime.UtcNow;
        record.ExpiresAt = start.AddDays(BillingDays);
        record.IssuedAt = DateTime.UtcNow;
        _ledger.Save(ledger);

        var payload = new LicensePayload
        {
            LicenseId = record.LicenseId!,
            CustomerName = record.CustomerName,
            CustomerEmail = record.Email,
            PlanName = "Premium",
            HardwareId = hardwareId,
            IssuedAt = record.IssuedAt.Value,
            ExpiresAt = record.ExpiresAt.Value,
            GraceDays = 5
        };
        return LicenseSigner.Sign(payload, _privateKeyXml);
    }

    private static string NormalizeEmail(string email) => email.Trim();

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    private static string NewCode(ActivationLedger ledger)
    {
        while (true)
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(4);
            string hex = Convert.ToHexString(bytes);
            string code = $"FS-{hex[..4]}-{hex[4..]}";
            bool exists = ledger.Records.Any(item =>
                string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase));
            if (!exists)
                return code;
        }
    }
}
