namespace FerreSys.Domain;

public sealed class LocalBackupSettings
{
    public static readonly int[] AllowedIntervals = [0, 1, 6, 12, 24];

    public int IntervalHours { get; init; }

    public DateTime? LastBackupUtc { get; init; }

    public static int Normalize(int hours) =>
        AllowedIntervals.Contains(hours) ? hours : 0;
}

public sealed class LocalBackupCopy
{
    public required string Path { get; init; }

    public required DateTime CreatedLocal { get; init; }

    public required long SizeBytes { get; init; }
}
