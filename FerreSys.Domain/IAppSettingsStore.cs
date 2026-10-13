namespace FerreSys.Domain;

public interface IAppSettingsStore
{
    BusinessTicketProfile GetTicketProfile();
    void SaveTicketProfile(BusinessTicketProfile profile);
    string? ResolveTicketLogoPath();
    void SaveTicketLogo(string sourcePath);
    void RemoveTicketLogo();
    PeripheralSettings GetPeripheralSettings();
    void SavePeripheralSettings(PeripheralSettings settings);

    bool IsSeedOfferCompleted();
    void MarkSeedOfferCompleted();

    string? GetLicenseKey();
    void SaveLicenseKey(string licenseKey);

    LocalBackupSettings GetLocalBackupSettings();
    void SaveLocalBackupSettings(LocalBackupSettings settings);

    ReportPreferences GetReportPreferences();
    void SaveReportPreferences(ReportPreferences preferences);
}
