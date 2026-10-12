namespace FerreSys.LicenseIssuer;

internal static class IssuerPaths
{
    public static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".ferresys",
        "licensing");

    public static string PrivateKeyPath => Path.Combine(Root, "private.xml");

    public static string LedgerPath => Path.Combine(Root, "ledger.json");
}
