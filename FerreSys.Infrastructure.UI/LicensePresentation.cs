using FerreSys.Application;
using FerreSys.Domain;
using FerreSys.Licensing;

namespace FerreSys.Infrastructure.UI;

internal static class LicensePresentation
{
    public static string StatusBar(LocalSubscriptionService subscription)
    {
        LicensePayload? license = PaidLicense(subscription);
        if (license != null)
        {
            if (license.Plan == LicensePlan.Lifetime)
                return "Vitalicia";
            return $"{LicensePlanInfo.DisplayName(license.Plan)} · vence {license.ExpiresAt.ToLocalTime():dd/MM/yyyy}";
        }

        TrialEvaluation trial = subscription.ReadTrial();
        if (trial.IsActive && trial.ExpiresAt.HasValue)
            return $"Prueba · vence {trial.ExpiresAt.Value.ToLocalTime():dd/MM/yyyy}";

        return "Prueba terminada";
    }

    public static string Title(LocalSubscriptionService subscription)
    {
        LicensePayload? license = PaidLicense(subscription);
        if (license != null)
            return LicensePlanInfo.DisplayName(license.Plan);

        return subscription.ReadTrial().IsActive ? "Prueba gratis" : "Prueba terminada";
    }

    public static string Detail(LocalSubscriptionService subscription)
    {
        LicensePayload? license = PaidLicense(subscription);
        if (license != null)
        {
            string expiry = license.Plan == LicensePlan.Lifetime
                ? "Sin vencimiento"
                : $"Vence: {license.ExpiresAt.ToLocalTime():dd/MM/yyyy}  (gracia {license.GraceDays} días)";
            return $"{license.CustomerName}\n{expiry}";
        }

        TrialEvaluation trial = subscription.ReadTrial();
        if (trial.IsActive && trial.ExpiresAt.HasValue)
        {
            return
                $"Hasta {FreeTierLimits.ProductLimit} productos.\n" +
                $"Vence: {trial.ExpiresAt.Value.ToLocalTime():dd/MM/yyyy}";
        }

        return string.IsNullOrWhiteSpace(trial.Error)
            ? TrialClock.EndedMessage
            : trial.Error;
    }

    private static LicensePayload? PaidLicense(LocalSubscriptionService subscription) =>
        subscription.GetCurrentTier() == SubscriptionTier.Premium ? subscription.CurrentLicense : null;
}
