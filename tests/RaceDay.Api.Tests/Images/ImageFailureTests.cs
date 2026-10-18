using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Storage;
using Xunit;

namespace RaceDay.Api.Tests.Images;

// Failure injection, SQL transactions and competing requests: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors
// https://learn.microsoft.com/en-us/ef/core/saving/transactions
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// Accessed: 18 October 2026.
public sealed partial class ImageTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedUpload_KeepsThePreviousSqlNameAndObject(bool banner)
    {
        using var good = Factory();
        using var client = good.Client();
        await Login(client, banner);
        using var original = await Upload(client, banner);
        Assert.Equal(HttpStatusCode.OK, original.StatusCode);
        string name = (await SavedName(banner))!;
        using var broken = Factory();
        broken.ExtraServices = services =>
        {
            services.RemoveAll<IImageStorage>();
            services.AddSingleton<IImageStorage>(sp => new FailingStorage(new BlobImageStorage(sp.GetRequiredService<IConfiguration>(), sp.GetRequiredService<TimeProvider>()), upload: true));
        };
        using var other = broken.Client();
        await Login(other, banner);
        using var response = await Upload(other, banner);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(name, await SavedName(banner));
        Assert.Equal(name, Assert.Single(await Objects()));
        Assert.DoesNotContain("AccountKey", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedRemoval_KeepsTheNameAndCanBeRetried(bool banner)
    {
        using var good = Factory();
        using var client = good.Client();
        await Login(client, banner);
        using var original = await Upload(client, banner);
        Assert.Equal(HttpStatusCode.OK, original.StatusCode);
        string name = (await SavedName(banner))!;
        using var broken = Factory();
        broken.ExtraServices = services =>
        {
            services.RemoveAll<IImageStorage>();
            services.AddSingleton<IImageStorage>(sp => new FailingStorage(new BlobImageStorage(sp.GetRequiredService<IConfiguration>(), sp.GetRequiredService<TimeProvider>()), delete: true));
        };
        using var other = broken.Client();
        await Login(other, banner);
        using var response = await other.DeleteAsync(Path(banner));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(name, await SavedName(banner));
        Assert.True((await storage.Container.GetBlobClient(name).ExistsAsync()).Value);
        using var retry = await client.DeleteAsync(Path(banner));
        Assert.Equal(HttpStatusCode.NoContent, retry.StatusCode);
        Assert.Null(await SavedName(banner));
        Assert.Empty(await Objects());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedSqlSave_RemovesTheNewUploadAndKeepsTheOldImage(bool banner)
    {
        using var good = Factory();
        using var client = good.Client();
        await Login(client, banner);
        using var original = await Upload(client, banner);
        Assert.Equal(HttpStatusCode.OK, original.StatusCode);
        string name = (await SavedName(banner))!;
        using var broken = Factory();
        broken.ExtraServices = services => services.AddDbContext<RaceDayDbContext>(options => options.AddInterceptors(new FailImageSave()));
        using var other = broken.Client();
        await Login(other, banner);
        using var response = await Upload(other, banner);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(name, await SavedName(banner));
        Assert.Equal(name, Assert.Single(await Objects()));
    }

    [Fact]
    public async Task ConcurrentPictureUploads_LeaveOneSavedObjectAndNoLostReplacement()
    {
        using var factory = Factory();
        using var first = factory.Client();
        using var second = factory.Client();
        await Login(first, false);
        await Login(second, false);
        var responses = await Task.WhenAll(Upload(first, false), Upload(second, false, "jpg"));
        try
        {
            Assert.All(responses, x => Assert.Equal(HttpStatusCode.OK, x.StatusCode));
            Assert.Equal(await SavedName(false), Assert.Single(await Objects()));
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    [Fact]
    public async Task BannerUploadAndEventDeletion_CannotLeaveAnOrphanedBanner()
    {
        using var factory = Factory();
        using var first = factory.Client();
        using var second = factory.Client();
        await Login(first, true);
        await Login(second, true);
        var item = await AddEvent();
        var upload = Upload(first, true, eventId: item.EventId);
        var removal = second.DeleteAsync($"/api/events/{item.EventId}");
        using var created = await upload;
        using var removed = await removal;
        Assert.Contains(created.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.NotFound });
        Assert.Equal(created.StatusCode == HttpStatusCode.OK ? HttpStatusCode.Conflict : HttpStatusCode.NoContent, removed.StatusCode);
        await using var context = database.CreateContext();
        var saved = await context.Events.FindAsync(item.EventId);
        if (created.StatusCode == HttpStatusCode.OK) Assert.Equal(saved!.BannerImageBlobName, Assert.Single(await Objects()));
        else { Assert.Null(saved); Assert.Empty(await Objects()); }
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-connection")]
    public async Task MissingOrInvalidStorageSetting_Returns503WithoutChangingSql(string connection)
    {
        using var factory = Factory();
        factory.Settings["ImageStorage:ConnectionString"] = connection;
        using var client = factory.Client();
        await Login(client, false);
        using var response = await Upload(client, false);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Null(await SavedName(false));
    }

    private sealed class FailingStorage(IImageStorage inner, bool upload = false, bool delete = false) : IImageStorage
    {
        public Task Upload(string name, Stream content, string type, CancellationToken cancellationToken) => upload
            ? throw new ImageStorageException("Simulated upload failure.") : inner.Upload(name, content, type, cancellationToken);
        public Task Delete(string name, CancellationToken cancellationToken) => delete
            ? throw new ImageStorageException("Simulated delete failure.") : inner.Delete(name, cancellationToken);
        public string? ReadUrl(string? name) => inner.ReadUrl(name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Removal_CanBeRetriedIfSqlFailsAfterTheBlobWasRemoved(bool banner)
    {
        using var good = Factory();
        using var client = good.Client();
        await Login(client, banner);
        using var original = await Upload(client, banner);
        Assert.Equal(HttpStatusCode.OK, original.StatusCode);
        string name = (await SavedName(banner))!;
        using var broken = Factory();
        broken.ExtraServices = services => services.AddDbContext<RaceDayDbContext>(options => options.AddInterceptors(new FailImageSave()));
        using var other = broken.Client();
        await Login(other, banner);
        using var failed = await other.DeleteAsync(Path(banner));
        Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
        Assert.Equal(name, await SavedName(banner));
        Assert.False((await storage.Container.GetBlobClient(name).ExistsAsync()).Value);
        using var retry = await client.DeleteAsync(Path(banner));
        Assert.Equal(HttpStatusCode.NoContent, retry.StatusCode);
        Assert.Null(await SavedName(banner));
        Assert.Empty(await Objects());
    }

    private sealed class FailImageSave : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries().Any(x =>
                (x.Entity is User && x.Property(nameof(User.ProfilePictureBlobName)).IsModified) ||
                (x.Entity is Event && x.Property(nameof(Event.BannerImageBlobName)).IsModified)))
                throw new InvalidOperationException("Simulated SQL save failure.");
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
