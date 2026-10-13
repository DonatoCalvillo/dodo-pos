using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FerreSys.Domain;

namespace FerreSys.Licensing;

public sealed record LicenseValidationResult(bool IsValid, LicensePayload? Payload, string Error);

public sealed class LicenseValidator
{
    public const string NotFoundMessage = "Archivo de licencia no encontrado.";
    public const string ClockRollbackMessage =
        "El reloj de esta computadora está atrasado respecto a la última validación.";

    private readonly string _publicKeyXml;
    private readonly Func<string> _hardwareId;
    private readonly Func<DateTime> _utcNow;
    private readonly string? _lastSeenPath;

    public LicenseValidator(
        string publicKeyXml,
        Func<string> hardwareId,
        Func<DateTime>? utcNow = null,
        string? lastSeenPath = null)
    {
        _publicKeyXml = publicKeyXml;
        _hardwareId = hardwareId;
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
        _lastSeenPath = lastSeenPath;
    }

    public static LicenseValidator ForInstalledApp(LicenseFileStore store) =>
        new(EmbeddedPublicKey.Xml, HardwareFingerprint.GetDeviceId, lastSeenPath: store.LastSeenPath);

    public LicenseValidationResult ValidateFile(string licenseFilePath, bool recordLastSeen = true)
    {
        if (!File.Exists(licenseFilePath))
            return new LicenseValidationResult(false, null, NotFoundMessage);

        try
        {
            string fileContent = File.ReadAllText(licenseFilePath);
            SignedLicenseContainer? signedData = JsonSerializer.Deserialize<SignedLicenseContainer>(fileContent, LicenseJson.Options);
            if (signedData == null)
                return new LicenseValidationResult(false, null, "Formato inválido.");

            return ValidateContainer(signedData, recordLastSeen);
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            return new LicenseValidationResult(false, null, "Formato inválido.");
        }
        catch (Exception ex)
        {
            return new LicenseValidationResult(false, null, $"Error al validar: {ex.Message}");
        }
    }

    public LicenseValidationResult ValidateContainer(SignedLicenseContainer? signedData, bool recordLastSeen = true)
    {
        if (signedData == null
            || string.IsNullOrWhiteSpace(signedData.JsonPayload)
            || string.IsNullOrWhiteSpace(signedData.Signature))
        {
            return new LicenseValidationResult(false, null, "Formato inválido.");
        }

        try
        {
            using var rsa = RSA.Create();
            rsa.FromXmlString(_publicKeyXml);

            byte[] dataBytes = Encoding.UTF8.GetBytes(signedData.JsonPayload);
            byte[] signatureBytes = Convert.FromBase64String(signedData.Signature);
            bool isSignatureValid = rsa.VerifyData(
                dataBytes,
                signatureBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            if (!isSignatureValid)
                return new LicenseValidationResult(false, null, "Licencia alterada o apócrifa.");

            LicensePayload? payload = JsonSerializer.Deserialize<LicensePayload>(signedData.JsonPayload, LicenseJson.Options);
            if (payload == null)
                return new LicenseValidationResult(false, null, "Payload ilegible.");

            if (!string.Equals(payload.HardwareId, _hardwareId(), StringComparison.Ordinal))
                return new LicenseValidationResult(false, null, "Esta licencia no corresponde a esta computadora.");

            DateTime now = _utcNow();
            DateTime? lastSeen = ReadLastSeen();
            if (lastSeen.HasValue && now < lastSeen.Value)
                return new LicenseValidationResult(false, null, ClockRollbackMessage);

            if (LicensePlanInfo.Expires(payload.Plan) && now > payload.ExpiresAt.AddDays(payload.GraceDays))
                return new LicenseValidationResult(false, null, "Su suscripción ha expirado.");

            if (recordLastSeen)
                WriteLastSeen(now);

            return new LicenseValidationResult(true, payload, "Licencia Válida");
        }
        catch (FormatException)
        {
            return new LicenseValidationResult(false, null, "Formato inválido.");
        }
        catch (Exception ex)
        {
            return new LicenseValidationResult(false, null, $"Error al validar: {ex.Message}");
        }
    }

    private DateTime? ReadLastSeen()
    {
        if (string.IsNullOrEmpty(_lastSeenPath) || !File.Exists(_lastSeenPath))
            return null;

        string text = File.ReadAllText(_lastSeenPath).Trim();
        if (!DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsed))
            return null;

        return parsed.ToUniversalTime();
    }

    private void WriteLastSeen(DateTime utcNow)
    {
        if (string.IsNullOrEmpty(_lastSeenPath))
            return;

        string? folder = Path.GetDirectoryName(_lastSeenPath);
        if (!string.IsNullOrEmpty(folder))
            Directory.CreateDirectory(folder);

        File.WriteAllText(
            _lastSeenPath,
            utcNow.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
    }
}
