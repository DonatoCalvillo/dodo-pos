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
}
