using FerreSys.Domain;

namespace FerreSys.Application;

public sealed class SaveLoyaltySettingsUseCase
{
    private readonly IAppSettingsStore _settings;

    public SaveLoyaltySettingsUseCase(IAppSettingsStore settings)
    {
        _settings = settings;
    }

    public void Execute(LoyaltyProgramSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.EarnMode == LoyaltyEarnMode.PercentOfSale)
        {
            if (settings.EarnPercent <= 0m || settings.EarnPercent > 1000m)
                throw new InvalidOperationException("El porcentaje de puntos debe ser mayor a 0 y hasta 1000.");
        }
        else
        {
            if (settings.PointsPerBlock <= 0)
                throw new InvalidOperationException("Los puntos por bloque deben ser al menos 1.");
            if (settings.AmountPerBlock <= 0m)
                throw new InvalidOperationException("El monto de cada bloque debe ser mayor a cero.");
        }

        _settings.SaveLoyaltySettings(settings);
    }
}
