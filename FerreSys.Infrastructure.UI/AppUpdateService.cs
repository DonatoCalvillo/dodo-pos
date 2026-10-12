using System.Diagnostics;
using Velopack;

namespace FerreSys.Infrastructure.UI;

internal static class AppUpdateService
{
    public static async Task CheckSilentlyAsync()
    {
        if (string.IsNullOrWhiteSpace(UpdateSettings.FeedUrl))
            return;

        try
        {
            var manager = new UpdateManager(UpdateSettings.FeedUrl);
            var update = await manager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (update == null)
                return;

            await manager.DownloadUpdatesAsync(update).ConfigureAwait(false);
            manager.WaitExitThenApplyUpdates(update);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error al actualizar: {ex.Message}");
        }
    }
}
