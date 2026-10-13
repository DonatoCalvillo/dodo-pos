using System.Globalization;

namespace FerreSys.Licensing;

public sealed record TrialEvaluation(bool IsActive, DateTime? ExpiresAt, string Error);

public sealed class TrialClock
{
    public const int TrialDays = 14;
    public const string EndedMessage = "La prueba de 14 días terminó.";

    private readonly string _startedPath;
    private readonly string _lastSeenPath;
    private readonly Func<DateTime> _utcNow;

    public TrialClock(string folder, Func<DateTime>? utcNow = null)
        : this(
            Path.Combine(folder, "trial.started"),
            Path.Combine(folder, "trial.lastseen"),
            utcNow)
    {
    }

    public TrialClock(LicenseFileStore store, Func<DateTime>? utcNow = null)
        : this(store.TrialStartedPath, store.TrialLastSeenPath, utcNow)
    {
    }

    public TrialClock(string startedPath, string lastSeenPath, Func<DateTime>? utcNow = null)
    {
        _startedPath = startedPath;
        _lastSeenPath = lastSeenPath;
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
    }

    public TrialEvaluation Evaluate(bool recordLastSeen = true)
    {
        DateTime now = _utcNow().ToUniversalTime();
        if (!TryReadOrCreateStarted(now, out DateTime started, out string startError))
            return new TrialEvaluation(false, null, startError);

        DateTime expiresAt = started.AddDays(TrialDays);
        DateTime? lastSeen = ReadStamp(_lastSeenPath);
        if (lastSeen.HasValue && now < lastSeen.Value)
            return new TrialEvaluation(false, expiresAt, LicenseValidator.ClockRollbackMessage);

        if (recordLastSeen)
            WriteStamp(_lastSeenPath, now);

        if (now > expiresAt)
            return new TrialEvaluation(false, expiresAt, EndedMessage);

        return new TrialEvaluation(true, expiresAt, string.Empty);
    }

    private bool TryReadOrCreateStarted(DateTime now, out DateTime started, out string error)
    {
        if (!File.Exists(_startedPath))
        {
            WriteStamp(_startedPath, now);
            started = now;
            error = string.Empty;
            return true;
        }

        DateTime? parsed = ReadStamp(_startedPath);
        if (!parsed.HasValue)
        {
            started = default;
            error = "No se pudo leer el inicio de la prueba.";
            return false;
        }

        started = parsed.Value;
        error = string.Empty;
        return true;
    }

    private static DateTime? ReadStamp(string path)
    {
        if (!File.Exists(path))
            return null;

        string text = File.ReadAllText(path).Trim();
        if (!DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsed))
            return null;

        return parsed.ToUniversalTime();
    }

    private static void WriteStamp(string path, DateTime utc)
    {
        string? folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folder))
            Directory.CreateDirectory(folder);

        File.WriteAllText(path, utc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
    }
}
