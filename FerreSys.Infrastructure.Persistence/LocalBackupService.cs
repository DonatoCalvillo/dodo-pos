using FerreSys.Domain;

namespace FerreSys.Infrastructure.Persistence;

public sealed class LocalBackupService
{
    public const int RetentionDays = 7;

    private readonly SqliteDbContext _dbContext;
    private readonly IAppSettingsStore _settings;

    public LocalBackupService(SqliteDbContext dbContext, IAppSettingsStore settings)
    {
        _dbContext = dbContext;
        _settings = settings;
    }

    public string Folder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Posly_Backups");

    public bool IsDue(DateTime utcNow)
    {
        LocalBackupSettings settings = _settings.GetLocalBackupSettings();
        if (settings.IntervalHours <= 0)
            return false;

        if (settings.LastBackupUtc is not DateTime last)
            return true;

        return utcNow >= last.AddHours(settings.IntervalHours);
    }

    public void BackupIfDue()
    {
        if (IsDue(DateTime.UtcNow))
            CreateBackup();
    }

    public void BackupOnClose()
    {
        if (_settings.GetLocalBackupSettings().IntervalHours <= 0)
            CreateBackup();
        else
            BackupIfDue();
    }

    public void CreateBackup()
    {
        PruneExpired();
        string fileName = $"posly_{DateTime.Now:yyyyMMdd_HHmmss}.db";
        _dbContext.BackupTo(Path.Combine(Folder, fileName));
        LocalBackupSettings current = _settings.GetLocalBackupSettings();
        _settings.SaveLocalBackupSettings(new LocalBackupSettings
        {
            IntervalHours = current.IntervalHours,
            LastBackupUtc = DateTime.UtcNow
        });
    }

    public IReadOnlyList<LocalBackupCopy> ListRecent()
    {
        PruneExpired();
        if (!Directory.Exists(Folder))
            return [];

        return Directory.GetFiles(Folder, "posly_*.db")
            .Select(path => new FileInfo(path))
            .Where(file => file.LastWriteTime >= DateTime.Now.AddDays(-RetentionDays))
            .OrderByDescending(file => file.LastWriteTime)
            .Select(file => new LocalBackupCopy
            {
                Path = file.FullName,
                CreatedLocal = file.LastWriteTime,
                SizeBytes = file.Length
            })
            .ToList();
    }

    public void Restore(string backupPath) => _dbContext.RestoreFrom(backupPath);

    private void PruneExpired()
    {
        if (!Directory.Exists(Folder))
            return;

        DateTime cutoff = DateTime.Now.AddDays(-RetentionDays);
        foreach (string path in Directory.GetFiles(Folder, "posly_*.db"))
        {
            var info = new FileInfo(path);
            if (info.LastWriteTime >= cutoff)
                continue;

            try
            {
                info.Delete();
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
