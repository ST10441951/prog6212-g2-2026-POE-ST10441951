using System.Text.RegularExpressions;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace RaceDay.Api.Storage;

// Private containers, streams, conditions and signed read URLs: Microsoft (2026).
// https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-upload
// https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-delete
// https://learn.microsoft.com/en-us/azure/storage/blobs/sas-service-create-dotnet
// Accessed: 18 October 2026.
/// <summary>Azure Blob Storage, also usable with the local Azurite emulator.</summary>
public sealed partial class BlobImageStorage : IImageStorage
{
    private readonly BlobContainerClient? container;
    private readonly TimeProvider clock;

    /// <summary>Read settings from configuration without exposing account credentials to clients.</summary>
    public BlobImageStorage(IConfiguration settings, TimeProvider clock)
    {
        this.clock = clock;
        string? connection = settings["ImageStorage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connection)) return;
        try
        {
            var options = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2025_11_05);
            options.Retry.MaxRetries = 2;
            options.Retry.NetworkTimeout = TimeSpan.FromSeconds(10);
            container = new BlobServiceClient(connection, options)
                .GetBlobContainerClient(settings["ImageStorage:ContainerName"] ?? "raceday-images");
            if (!container.CanGenerateSasUri) throw new ArgumentException("An account-key connection is required.");
        }
        catch (Exception error) when (error is ArgumentException or FormatException)
        {
            container = null;
        }
    }

    /// <inheritdoc />
    public async Task Upload(string blobName, Stream content, string contentType, CancellationToken cancellationToken)
    {
        var target = RequiredContainer();
        try
        {
            await target.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
            if ((await target.GetAccessPolicyAsync(cancellationToken: cancellationToken)).Value.BlobPublicAccess != PublicAccessType.None)
                throw new ImageStorageException("Use a private image container.");
            await target.GetBlobClient(blobName).UploadAsync(content, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType, CacheControl = "private, max-age=300" },
                Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All }
            }, cancellationToken);
        }
        catch (Exception error) when (error is RequestFailedException or IOException)
        {
            throw new ImageStorageException("Image storage is unavailable.", error);
        }
    }

    /// <inheritdoc />
    public async Task Delete(string blobName, CancellationToken cancellationToken)
    {
        var target = RequiredContainer();
        try
        {
            await target.GetBlobClient(blobName).DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots,
                cancellationToken: cancellationToken);
        }
        catch (Exception error) when (error is RequestFailedException or IOException)
        {
            throw new ImageStorageException("Image storage is unavailable.", error);
        }
    }

    /// <inheritdoc />
    public string? ReadUrl(string? blobName)
    {
        if (container is null || blobName is null || !GeneratedName().IsMatch(blobName)) return null;
        var blob = container.GetBlobClient(blobName);
        var signature = new BlobSasBuilder
        {
            BlobContainerName = container.Name, BlobName = blobName, Resource = "b",
            StartsOn = clock.GetUtcNow().AddMinutes(-5), ExpiresOn = clock.GetUtcNow().AddHours(1),
            Protocol = blob.Uri.Scheme == "https" ? SasProtocol.Https : SasProtocol.HttpsAndHttp
        };
        signature.SetPermissions(BlobSasPermissions.Read);
        return blob.GenerateSasUri(signature).AbsoluteUri;
    }

    private BlobContainerClient RequiredContainer() => container
        ?? throw new ImageStorageException("Set ImageStorage:ConnectionString before uploading images.");

    [GeneratedRegex(@"\A(?:profile-pictures/user|event-banners/event)-[1-9][0-9]*/[a-f0-9]{32}\.(?:jpg|png)\z", RegexOptions.CultureInvariant)]
    private static partial Regex GeneratedName();
}
