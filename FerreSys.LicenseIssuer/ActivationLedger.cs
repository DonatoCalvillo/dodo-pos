using System.Text.Json;
using FerreSys.Licensing;

namespace FerreSys.LicenseIssuer;

internal sealed class ActivationRecord
{
    public string Code { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? HardwareId { get; set; }
    public string? LicenseId { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool Revoked { get; set; }
    public DateTime CreatedAt { get; set; }
}

internal sealed class ActivationLedger
{
    public List<ActivationRecord> Records { get; set; } = [];
}

internal sealed class ActivationLedgerStore
{
    private readonly string _path;
    private readonly object _gate = new();

    public ActivationLedgerStore(string path)
    {
        _path = path;
    }

    public ActivationLedger Load()
    {
        lock (_gate)
        {
            if (!File.Exists(_path))
                return new ActivationLedger();

            string json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<ActivationLedger>(json, LicenseJson.Options) ?? new ActivationLedger();
        }
    }

    public void Save(ActivationLedger ledger)
    {
        lock (_gate)
        {
            string? folder = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);

            File.WriteAllText(_path, JsonSerializer.Serialize(ledger, LicenseJson.Options));
        }
    }
}
