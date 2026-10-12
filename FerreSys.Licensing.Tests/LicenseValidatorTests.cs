using System.Security.Cryptography;
using FerreSys.Domain;
using FerreSys.Licensing;
using Xunit;

namespace FerreSys.Licensing.Tests;

public sealed class LicenseValidatorTests : IDisposable
{
    private const string HardwareId = "A1B2C3D4E5F67890";
    private readonly string _directory;
    private readonly string _privateXml;
    private readonly string _publicXml;
    private readonly DateTime _now = new(2026, 10, 12, 18, 0, 0, DateTimeKind.Utc);

    public LicenseValidatorTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "ferresys-lic-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        using RSA rsa = RSA.Create(2048);
        _privateXml = rsa.ToXmlString(true);
        _publicXml = rsa.ToXmlString(false);
    }

    [Fact]
    public void ValidSignature_IsAccepted()
    {
        SignedLicenseContainer container = Sign(Sample(_now.AddDays(30)));
        LicenseValidationResult result = Validator().ValidateContainer(container);

        Assert.True(result.IsValid);
        Assert.Equal("Ferretería El MVP", result.Payload?.CustomerName);
    }

    [Fact]
    public void TamperedPayload_IsRejected()
    {
        SignedLicenseContainer container = Sign(Sample(_now.AddDays(30)));
        container.JsonPayload = container.JsonPayload.Replace("Premium", "Free", StringComparison.Ordinal);

        LicenseValidationResult result = Validator().ValidateContainer(container);

        Assert.False(result.IsValid);
        Assert.Equal("Licencia alterada o apócrifa.", result.Error);
    }

    [Fact]
    public void OtherHardware_IsRejected()
    {
        SignedLicenseContainer container = Sign(Sample(_now.AddDays(30)));
        LicenseValidationResult result = Validator("FFFFFFFFFFFFFFFF").ValidateContainer(container);

        Assert.False(result.IsValid);
        Assert.Equal("Esta licencia no corresponde a esta computadora.", result.Error);
    }

    [Fact]
    public void PastGrace_IsRejected()
    {
        SignedLicenseContainer container = Sign(Sample(_now.AddDays(-10)));
        LicenseValidationResult result = Validator().ValidateContainer(container);

        Assert.False(result.IsValid);
        Assert.Equal("Su suscripción ha expirado.", result.Error);
    }

    [Fact]
    public void InsideGrace_IsAccepted()
    {
        SignedLicenseContainer container = Sign(Sample(_now.AddDays(-4)));
        LicenseValidationResult result = Validator().ValidateContainer(container);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ClockBehindLastSeen_IsRejected()
    {
        string lastSeenPath = Path.Combine(_directory, "license.lastseen");
        File.WriteAllText(lastSeenPath, _now.AddDays(1).ToString("o"));
        SignedLicenseContainer container = Sign(Sample(_now.AddDays(30)));

        LicenseValidationResult result = Validator().ValidateContainer(container);

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidator.ClockRollbackMessage, result.Error);
        Assert.Equal(_now.AddDays(1).ToString("o"), File.ReadAllText(lastSeenPath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private LicenseValidator Validator(string? hardwareId = null) =>
        new(_publicXml, () => hardwareId ?? HardwareId, () => _now, Path.Combine(_directory, "license.lastseen"));

    private SignedLicenseContainer Sign(LicensePayload payload) =>
        LicenseSigner.Sign(payload, _privateXml);

    private LicensePayload Sample(DateTime expiresAt) => new()
    {
        LicenseId = "lic-1",
        CustomerName = "Ferretería El MVP",
        CustomerEmail = "ferreteria@example.com",
        PlanName = "Premium",
        HardwareId = HardwareId,
        IssuedAt = _now.AddDays(-20),
        ExpiresAt = expiresAt,
        GraceDays = 5
    };
}
