namespace FerreSys.Domain;

public enum LicensePlan
{
    Monthly = 0,
    Annual = 1,
    Lifetime = 2
}

public static class LicensePlanInfo
{
    public const string LegacyPremiumName = "Premium";

    public static bool Expires(LicensePlan plan) => plan != LicensePlan.Lifetime;

    public static int DefaultDays(LicensePlan plan) => plan switch
    {
        LicensePlan.Annual => 365,
        LicensePlan.Lifetime => 0,
        _ => 30
    };

    public static string DisplayName(LicensePlan plan) => plan switch
    {
        LicensePlan.Annual => "Anual",
        LicensePlan.Lifetime => "Vitalicia",
        _ => "Mensual"
    };

    public static bool TryParse(string? text, out LicensePlan plan)
    {
        plan = text?.Trim().ToLowerInvariant() switch
        {
            "monthly" or "mensual" or "premium" => LicensePlan.Monthly,
            "annual" or "anual" => LicensePlan.Annual,
            "lifetime" or "vitalicia" or "vitalicio" => LicensePlan.Lifetime,
            _ => LicensePlan.Monthly
        };
        return text is not null && text.Trim().Length > 0 && text.Trim().ToLowerInvariant() is
            "monthly" or "mensual" or "premium" or
            "annual" or "anual" or
            "lifetime" or "vitalicia" or "vitalicio";
    }
}
