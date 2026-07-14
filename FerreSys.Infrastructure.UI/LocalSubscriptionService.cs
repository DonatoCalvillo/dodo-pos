using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI;

public class LocalSubscriptionService : ISubscriptionService
{
    // Por ahora, devolvemos siempre Free. 
    // En el futuro, aquí leeremos un archivo de licencia o llamaremos a la API.
    public SubscriptionTier GetCurrentTier()
    {
        return SubscriptionTier.Free;
    }
}