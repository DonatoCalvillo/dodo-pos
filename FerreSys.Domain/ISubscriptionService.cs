namespace FerreSys.Domain;

public interface ISubscriptionService
{
    SubscriptionTier GetCurrentTier();
}