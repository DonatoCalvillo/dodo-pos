using Velopack;
using Velopack.Sources;

namespace FerreSys.Infrastructure.UI;

internal enum StartupUpdateResult
{
    Continue,
    Restarting
}

internal static class AppUpdateService
{
    public static async Task<StartupUpdateResult> ApplyAvailableUpdateAsync(Action<string> report)
    {
        if (string.IsNullOrWhiteSpace(UpdateSettings.FeedUrl))
            return StartupUpdateResult.Continue;

        try
        {
            report("Buscando actualización...");
            var manager = new UpdateManager(new GithubSource(UpdateSettings.FeedUrl, null, false));
            UpdateInfo? update = await manager.CheckForUpdatesAsync();
            if (update == null)
                return StartupUpdateResult.Continue;

            report("Descargando actualización...");
            await manager.DownloadUpdatesAsync(update, progress => report($"Descargando actualización... {progress}%"));
            report("Instalando actualización...");
            manager.ApplyUpdatesAndRestart(update);
            return StartupUpdateResult.Restarting;
        }
        catch (Exception ex) when (ex.GetType().Name == "NotInstalledException")
        {
            return StartupUpdateResult.Continue;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            report("No se pudo buscar la actualización. Abriendo Posly...");
            await Task.Delay(1200);
            return StartupUpdateResult.Continue;
        }
    }
}
