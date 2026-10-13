using System.Security.Cryptography;
using FerreSys.Domain;
using FerreSys.Licensing;

namespace FerreSys.LicenseIssuer;

internal sealed class ActivationService
{
    public static readonly DateTime LifetimeExpiry = new(9999, 12, 31, 0, 0, 0, DateTimeKind.Utc);

    private readonly string _privateKeyXml;
    private readonly ActivationLedgerStore _ledger;

    public ActivationService(string privateKeyXml, ActivationLedgerStore ledger)
    {
        _privateKeyXml = privateKeyXml;
        _ledger = ledger;
    }

    public ActivationRecord CreateCode(string email, string customerName, LicensePlan plan, int? days)
    {
        email = NormalizeEmail(email);
        customerName = customerName.Trim();
        if (email.Length == 0)
            throw new InvalidOperationException("El correo es obligatorio.");
        if (customerName.Length == 0)
            throw new InvalidOperationException("El nombre del cliente es obligatorio.");

        int duration = ResolveDuration(plan, days);
        DateTime createdAt = DateTime.UtcNow;
        ActivationLedger ledger = _ledger.Load();
        var record = new ActivationRecord
        {
            Code = NewCode(ledger),
            Email = email,
            CustomerName = customerName,
            Plan = plan.ToString(),
            ExpiresAt = plan == LicensePlan.Lifetime ? null : createdAt.AddDays(duration),
            CreatedAt = createdAt
        };
        ledger.Records.Add(record);
        _ledger.Save(ledger);
        return record;
    }

    public (SignedLicenseContainer Container, string Code) IssueFile(
        string email,
        string customerName,
        string hardwareId,
        LicensePlan plan,
        int? days)
    {
        email = NormalizeEmail(email);
        customerName = customerName.Trim();
        hardwareId = hardwareId.Trim();
        if (email.Length == 0 || customerName.Length == 0 || hardwareId.Length == 0)
            throw new InvalidOperationException("Correo, nombre y hardware id son obligatorios.");

        int duration = ResolveDuration(plan, days);
        DateTime issuedAt = DateTime.UtcNow;
        var payload = BuildPayload(
            Guid.NewGuid().ToString(),
            customerName,
            email,
            plan,
            hardwareId,
            issuedAt,
            plan == LicensePlan.Lifetime ? null : issuedAt.AddDays(duration));

        ActivationLedger ledger = _ledger.Load();
        string code = NewCode(ledger);
        ledger.Records.Add(new ActivationRecord
        {
            Code = code,
            Email = email,
            CustomerName = customerName,
            Plan = plan.ToString(),
            HardwareId = hardwareId,
            LicenseId = payload.LicenseId,
            IssuedAt = payload.IssuedAt,
            ExpiresAt = plan == LicensePlan.Lifetime ? null : payload.ExpiresAt,
            CreatedAt = issuedAt
        });
        _ledger.Save(ledger);
        return (LicenseSigner.Sign(payload, _privateKeyXml), code);
    }

    public void Revoke(string code)
    {
        ActivationLedger ledger = _ledger.Load();
        ActivationRecord record = RequireRecord(ledger, code);
        record.Revoked = true;
        _ledger.Save(ledger);
    }

    public void Extend(string code, int? days)
    {
        ActivationLedger ledger = _ledger.Load();
        ActivationRecord record = RequireRecord(ledger, code);
        if (record.Revoked)
            throw new InvalidOperationException("Este código fue revocado.");

        LicensePlan plan = ParseStoredPlan(record.Plan);
        if (plan == LicensePlan.Lifetime)
            throw new InvalidOperationException("La licencia vitalicia no vence.");

        int duration = ResolveDuration(plan, days);
        DateTime start = record.ExpiresAt.HasValue && record.ExpiresAt.Value > DateTime.UtcNow
            ? record.ExpiresAt.Value
            : DateTime.UtcNow;
        record.ExpiresAt = start.AddDays(duration);
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

        LicensePlan plan = ParseStoredPlan(record.Plan);
        DateTime issuedAt = record.IssuedAt ?? DateTime.UtcNow;
        DateTime? expiresAt = record.ExpiresAt;
        if (string.IsNullOrWhiteSpace(record.HardwareId))
        {
            issuedAt = DateTime.UtcNow;
            if (plan != LicensePlan.Lifetime && expiresAt == null)
                expiresAt = issuedAt.AddDays(LicensePlanInfo.DefaultDays(plan));

            record.HardwareId = hardwareId;
            record.LicenseId = Guid.NewGuid().ToString();
            record.IssuedAt = issuedAt;
            record.ExpiresAt = expiresAt;
            record.Plan = plan.ToString();
            _ledger.Save(ledger);
        }

        LicensePayload payload = BuildPayload(
            record.LicenseId ?? Guid.NewGuid().ToString(),
            record.CustomerName,
            record.Email,
            plan,
            hardwareId,
            issuedAt,
            expiresAt);
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

        LicensePlan plan = ParseStoredPlan(record.Plan);
        LicensePayload payload = BuildPayload(
            record.LicenseId!,
            record.CustomerName,
            record.Email,
            plan,
            hardwareId,
            record.IssuedAt ?? DateTime.UtcNow,
            record.ExpiresAt);
        return LicenseSigner.Sign(payload, _privateKeyXml);
    }

    private static ActivationRecord RequireRecord(ActivationLedger ledger, string code)
    {
        string normalized = NormalizeCode(code);
        ActivationRecord? record = ledger.Records.FirstOrDefault(item =>
            string.Equals(item.Code, normalized, StringComparison.OrdinalIgnoreCase));
        if (record == null)
            throw new InvalidOperationException("No existe ese código de activación.");
        return record;
    }

    private static LicensePayload BuildPayload(
        string licenseId,
        string customerName,
        string email,
        LicensePlan plan,
        string hardwareId,
        DateTime issuedAt,
        DateTime? expiresAt)
    {
        return new LicensePayload
        {
            LicenseId = licenseId,
            CustomerName = customerName,
            CustomerEmail = email,
            Plan = plan,
            PlanName = LicensePlanInfo.DisplayName(plan),
            HardwareId = hardwareId,
            IssuedAt = issuedAt,
            ExpiresAt = plan == LicensePlan.Lifetime ? LifetimeExpiry : expiresAt ?? issuedAt,
            GraceDays = plan == LicensePlan.Lifetime ? 0 : 5
        };
    }

    private static int ResolveDuration(LicensePlan plan, int? days)
    {
        if (plan == LicensePlan.Lifetime)
            return 0;
        if (days is < 1)
            throw new InvalidOperationException("Los días de vigencia deben ser al menos 1.");
        return days ?? LicensePlanInfo.DefaultDays(plan);
    }

    private static LicensePlan ParseStoredPlan(string? plan)
    {
        if (string.IsNullOrWhiteSpace(plan))
            return LicensePlan.Monthly;
        if (!LicensePlanInfo.TryParse(plan, out LicensePlan parsed))
            return LicensePlan.Monthly;
        return parsed;
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
