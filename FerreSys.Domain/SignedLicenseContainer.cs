namespace FerreSys.Domain;

public sealed class SignedLicenseContainer
{
    public string JsonPayload { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}
