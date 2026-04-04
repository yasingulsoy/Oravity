using Minio;
using Minio.DataModel.Args;

namespace Oravity.Infrastructure.Storage;

/// <summary>
/// MinIO (S3 uyumlu) dosya depolama — geliştirme ve üretim için object storage.
/// </summary>
public class MinioFileStorageService : IFileStorageService
{
    private readonly IMinioClient _minio;
    private const string BucketName = "oravity-files";

    public MinioFileStorageService(IMinioClient minio)
    {
        _minio = minio;
    }

    public async Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        var bucketExists = await _minio.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(BucketName), ct);

        if (!bucketExists)
            await _minio.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(BucketName), ct);

        var objectName =
            $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}_{fileName}";

        long objectSize;
        if (stream.CanSeek)
        {
            objectSize = stream.Length;
        }
        else
        {
            await using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            stream = ms;
            objectSize = ms.Length;
            stream.Position = 0;
        }

        await _minio.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(BucketName)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(objectSize)
                .WithContentType(contentType),
            ct);

        return objectName;
    }

    public async Task<Stream> DownloadAsync(string filePath, CancellationToken ct = default)
    {
        var ms = new MemoryStream();
        await _minio.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(BucketName)
                .WithObject(filePath)
                .WithCallbackStream(s => s.CopyTo(ms)),
            ct);
        ms.Position = 0;
        return ms;
    }

    public async Task DeleteAsync(string filePath, CancellationToken ct = default)
    {
        await _minio.RemoveObjectAsync(
            new RemoveObjectArgs()
                .WithBucket(BucketName)
                .WithObject(filePath),
            ct);
    }

    /// <summary>Ön imzalı (presigned) GET URL — süre dolunca geçersiz olur.</summary>
    public async Task<string> GetPublicUrlAsync(string filePath, TimeSpan? expiry = null)
    {
        var seconds = (int)(expiry ?? TimeSpan.FromHours(1)).TotalSeconds;
        if (seconds < 1) seconds = 3600;

        return await _minio.PresignedGetObjectAsync(
            new PresignedGetObjectArgs()
                .WithBucket(BucketName)
                .WithObject(filePath)
                .WithExpiry(seconds));
    }
}
