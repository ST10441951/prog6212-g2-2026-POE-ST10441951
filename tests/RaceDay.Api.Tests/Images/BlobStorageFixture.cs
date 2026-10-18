using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Xunit;

namespace RaceDay.Api.Tests.Images;

// Isolated storage containers and the local emulator: Microsoft (2026).
// https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite
// https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-container-create
// Accessed: 18 October 2026.
public sealed class BlobStorageFixture : IAsyncLifetime
{
    internal string Connection { get; } = Environment.GetEnvironmentVariable("RACEDAY_TEST_STORAGE_CONNECTION") ?? "UseDevelopmentStorage=true";
    internal BlobContainerClient Container { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var options = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2025_11_05);
        Container = new BlobServiceClient(Connection, options).GetBlobContainerClient($"raceday-tests-{Guid.NewGuid():N}");
        await Container.CreateIfNotExistsAsync(PublicAccessType.None);
    }

    public async Task DisposeAsync()
    {
        if (Container is null) return;
        if (!Container.Name.StartsWith("raceday-tests-", StringComparison.Ordinal))
            throw new InvalidOperationException("Only this fixture's temporary image container may be removed.");
        await Container.DeleteIfExistsAsync();
    }
}
