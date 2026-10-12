using System.Text.Json.Serialization;
using FerreSys.Domain;

namespace FerreSys.Licensing;

public sealed class ActivateLicenseRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("activation_code")]
    public string ActivationCode { get; set; } = string.Empty;

    [JsonPropertyName("hardware_id")]
    public string HardwareId { get; set; } = string.Empty;
}

public sealed class RenewLicenseRequest
{
    [JsonPropertyName("license_id")]
    public string LicenseId { get; set; } = string.Empty;

    [JsonPropertyName("hardware_id")]
    public string HardwareId { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}

public sealed class LicenseApiError
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;
}

public sealed record LicenseApiResult(bool Ok, SignedLicenseContainer? License, string Error);
