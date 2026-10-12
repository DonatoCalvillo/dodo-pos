namespace FerreSys.Domain;

public interface ISubscriptionService
{
    SubscriptionTier GetCurrentTier();

    /// <summary>
    /// Identificador de la licencia Premium vigente. Null en el plan gratuito.
    /// </summary>
    string? PremiumLicenseId { get; }
}