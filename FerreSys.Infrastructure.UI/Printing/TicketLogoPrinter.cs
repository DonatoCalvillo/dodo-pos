using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI.Printing;

internal static class TicketLogoPrinter
{
    public static byte[]? TryEncode(IAppSettingsStore settings, BusinessTicketProfile profile)
    {
        if (!profile.ShowsLogo)
            return null;

        return EscPosRaster.TryEncode(settings.ResolveTicketLogoPath(), profile.PaperWidth);
    }
}
