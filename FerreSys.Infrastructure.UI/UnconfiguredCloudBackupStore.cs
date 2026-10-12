using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI;

public sealed class UnconfiguredCloudBackupStore : ICloudBackupStore
{
    public Task<bool> UploadAsync(string localFilePath, string objectKey, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }
}
