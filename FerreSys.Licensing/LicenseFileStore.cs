using System.Text.Json;
using FerreSys.Domain;

namespace FerreSys.Licensing;

public sealed class LicenseFileStore
{
    public LicenseFileStore(string? folder = null)
    {
        Folder = folder ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FerreSys");
    }

    public string Folder { get; }

    public string LicensePath => Path.Combine(Folder, "license.lic");

    public string LastSeenPath => Path.Combine(Folder, "license.lastseen");

    public string TrialStartedPath => Path.Combine(Folder, "trial.started");

    public string TrialLastSeenPath => Path.Combine(Folder, "trial.lastseen");

    public void Save(SignedLicenseContainer container)
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(LicensePath, JsonSerializer.Serialize(container, LicenseJson.Options));
    }
}
