using Amazon.S3;
using Amazon.S3.Model;

namespace LearningPortal.Services;

public record StoredFile(Stream Stream, string ContentType);

public interface IFileStorage
{
    Task<string> UploadAsync(IFormFile file);
    Task DeleteAsync(string key);
    Task<StoredFile?> OpenReadAsync(string key);
}

public class S3FileStorage : IFileStorage
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;

    public S3FileStorage(IAmazonS3 s3, IConfiguration config)
    {
        _s3 = s3;
        _bucket = config["Aws:BucketName"] ?? throw new InvalidOperationException("Aws:BucketName is not configured.");
    }

    public async Task<string> UploadAsync(IFormFile file)
    {
        string safeName = Path.GetFileName(file.FileName).Replace(' ', '-');
        string key = $"resources/{Guid.NewGuid():N}-{safeName}";   // unique key, so two uploads never collide

        await using var stream = file.OpenReadStream();
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = stream,
            ContentType = file.ContentType
        });

        return key;
    }

    public Task DeleteAsync(string key) => _s3.DeleteObjectAsync(_bucket, key);

    public async Task<StoredFile?> OpenReadAsync(string key)
    {
        try
        {
            var response = await _s3.GetObjectAsync(_bucket, key);
            string contentType = string.IsNullOrWhiteSpace(response.Headers.ContentType)
                ? "application/octet-stream"
                : response.Headers.ContentType;
            return new StoredFile(response.ResponseStream, contentType);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}