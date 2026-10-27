using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public interface IBlobStorageService
{
    /// <summary>False when no Azure connection string has been configured.</summary>
    bool IsConfigured { get; }

    /// <summary>Uploads an image to Azure Blob Storage and returns its public URL.</summary>
    Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
}

/// <summary>
/// Stores uploaded images in an Azure Blob Storage container. The container is
/// created on first use with public read access on blobs, so the stored URL can
/// be used directly in an &lt;img&gt; tag by the MVC front end.
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient? _container;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _containerReady;

    public BlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureBlobStorage:ConnectionString"];
        var containerName = configuration["AzureBlobStorage:ContainerName"];
        if (string.IsNullOrWhiteSpace(containerName)) containerName = "raceday-images";

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            _container = new BlobContainerClient(connectionString, containerName);
        }
    }

    public bool IsConfigured => _container is not null;

    public async Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        if (_container is null)
            throw new InvalidOperationException("Azure Blob Storage is not configured.");

        await EnsureContainerAsync(cancellationToken);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blobName = $"{folder}/{Guid.NewGuid():N}{extension}";
        var blob = _container.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
        };
        await blob.UploadAsync(stream, options, cancellationToken);

        return blob.Uri.ToString();
    }

    private async Task EnsureContainerAsync(CancellationToken cancellationToken)
    {
        if (_containerReady || _container is null) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_containerReady) return;
            await _container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
            _containerReady = true;
        }
        finally
        {
            _initLock.Release();
        }
    }
}
