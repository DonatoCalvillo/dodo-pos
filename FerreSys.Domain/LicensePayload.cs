namespace FerreSys.Domain;

public sealed class LicensePayload
{
    public string LicenseId { get; set; } = Guid.NewGuid().ToString();
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string PlanName { get; set; } = "Premium";
    public string HardwareId { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int GraceDays { get; set; } = 5;
}
