namespace FerreSys.Domain;

public interface ICloudBackupStore
{
    /// <summary>
    /// Uploads an encrypted package. Returns true when the local file can be deleted.
    /// </summary>
    Task<bool> UploadAsync(string localFilePath, string objectKey, CancellationToken cancellationToken = default);
}
