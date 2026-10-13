using FerreSys.Domain;
using FerreSys.Licensing;

namespace FerreSys.Infrastructure.UI;

public sealed class LocalSubscriptionService : ISubscriptionService
{
    private readonly LicenseFileStore _store;
    private readonly LicenseValidator _validator;
    private readonly object _gate = new();
    private LicensePayload? _payload;

    public LocalSubscriptionService()
        : this(new LicenseFileStore(), null)
    {
    }

    public LocalSubscriptionService(LicenseFileStore store, LicenseValidator? validator)
    {
        _store = store;
        _validator = validator ?? LicenseValidator.ForInstalledApp(store);
        Reload();
    }

    public LicensePayload? CurrentLicense
    {
        get
        {
            lock (_gate)
                return _payload;
        }
    }

    public string LastError { get; private set; } = string.Empty;

    public string? PremiumLicenseId
    {
        get
        {
            lock (_gate)
                return IsPaid(_payload) ? _payload!.LicenseId : null;
        }
    }

    public SubscriptionTier GetCurrentTier()
    {
        lock (_gate)
            return IsPaid(_payload) ? SubscriptionTier.Premium : SubscriptionTier.Free;
    }

    public TrialEvaluation ReadTrial(bool recordLastSeen = false)
    {
        return new TrialClock(_store).Evaluate(recordLastSeen);
    }

    public void Reload()
    {
        LicenseValidationResult result = _validator.ValidateFile(_store.LicensePath);
        lock (_gate)
        {
            if (!result.IsValid)
            {
                _payload = null;
                LastError = result.Error == LicenseValidator.NotFoundMessage ? string.Empty : result.Error;
                return;
            }

            _payload = result.Payload;
            LastError = string.Empty;
        }
    }

    public bool TryInstall(SignedLicenseContainer container, out string error)
    {
        LicenseValidationResult check = _validator.ValidateContainer(container, recordLastSeen: false);
        if (!check.IsValid || check.Payload == null)
        {
            error = check.Error;
            return false;
        }

        _store.Save(container);
        Reload();
        error = string.Empty;
        return true;
    }

    public async Task TryRenewAsync(LicenseApiClient client, CancellationToken cancellationToken = default)
    {
        LicensePayload? current = CurrentLicense;
        if (current == null || string.IsNullOrWhiteSpace(current.LicenseId))
            return;

        LicenseApiResult result = await client.RenewAsync(
            current.LicenseId,
            current.HardwareId,
            current.CustomerEmail,
            cancellationToken).ConfigureAwait(false);

        if (!result.Ok || result.License == null)
            return;

        TryInstall(result.License, out _);
    }

    private static bool IsPaid(LicensePayload? payload) => payload != null;
}
