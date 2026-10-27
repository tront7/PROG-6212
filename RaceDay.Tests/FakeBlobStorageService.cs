using Microsoft.AspNetCore.Http;
using RaceDay.Api.Services;

namespace RaceDay.Tests;

/// <summary>Stand-in for Azure Blob Storage so tests never need a real storage account.</summary>
public class FakeBlobStorageService : IBlobStorageService
{
    public bool IsConfigured => true;

    public Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        var url = $"https://fake-blob.test/{folder}/{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        return Task.FromResult(url);
    }
}
