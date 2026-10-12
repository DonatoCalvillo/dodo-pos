using System.Text.Json;

namespace FerreSys.Licensing;

public static class LicenseJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false
    };
}
