using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Contracts.Events;
using Xunit;

namespace RaceDay.Api.Tests.Enrolments;

// Competing HTTP requests and SQL lock behaviour: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/saving/transactions
// https://learn.microsoft.com/en-us/sql/t-sql/queries/hints-transact-sql-table
// Accessed: 18 October 2026.
public sealed partial class EnrolmentTests
{
    [Fact]
    public async Task ConcurrentEntries_CannotTakeTheSameLastPlace()
    {
        using var factory = Factory();
        using var first = factory.Client();
        using var second = factory.Client();
        await Login(first);
        await Login(second, "sipho.dlamini@example.com");
        var category = await AddCategory(capacity: 1);
        var responses = await Task.WhenAll(first.PostAsJsonAsync("/api/enrolments", EntryBody(category)), second.PostAsJsonAsync("/api/enrolments", EntryBody(category)));
        try
        {
            Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Created);
            Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);
            await using var context = database.CreateContext();
            Assert.Equal(1, await context.Enrolments.CountAsync(x => x.Status == "Confirmed"));
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    [Fact]
    public async Task ConcurrentDuplicateEntries_SaveOnlyOneRow()
    {
        using var factory = Factory();
        using var first = factory.Client();
        using var second = factory.Client();
        await Login(first);
        await Login(second);
        var category = await AddCategory();
        var responses = await Task.WhenAll(first.PostAsJsonAsync("/api/enrolments", EntryBody(category)), second.PostAsJsonAsync("/api/enrolments", EntryBody(category)));
        try
        {
            Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Created);
            Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);
            await using var context = database.CreateContext();
            Assert.Equal(1, await context.Enrolments.CountAsync());
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    [Fact]
    public async Task ConcurrentReactivation_AllocatesOnlyOnePlaceAndKeepsBothHistoricalIds()
    {
        using var factory = Factory();
        using var first = factory.Client();
        using var second = factory.Client();
        await Login(first);
        await Login(second, "sipho.dlamini@example.com");
        var category = await AddCategory(capacity: 1);
        var firstEntry = await AddEntry(category, status: "Cancelled");
        var secondEntry = await AddEntry(category, participant: 4, status: "Cancelled");
        var responses = await Task.WhenAll(
            first.PutAsJsonAsync($"/api/enrolments/{firstEntry.EnrolmentId}/reactivate", new { eventCategoryId = category.EventCategoryId }),
            second.PutAsJsonAsync($"/api/enrolments/{secondEntry.EnrolmentId}/reactivate", new { eventCategoryId = category.EventCategoryId }));
        try
        {
            Assert.Single(responses, x => x.StatusCode == HttpStatusCode.OK);
            Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);
            await using var context = database.CreateContext();
            Assert.Equal(1, await context.Enrolments.CountAsync(x => x.Status == "Confirmed"));
            Assert.Equal(1, await context.Enrolments.CountAsync(x => x.Status == "Cancelled"));
            Assert.NotNull(await context.Enrolments.FindAsync(firstEntry.EnrolmentId));
            Assert.NotNull(await context.Enrolments.FindAsync(secondEntry.EnrolmentId));
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    [Fact]
    public async Task EntryAndCapacityReduction_CannotSaveMoreConfirmedEntriesThanCapacity()
    {
        using var factory = Factory();
        using var participant = factory.Client();
        using var organiser = factory.Client();
        await Login(participant, "sipho.dlamini@example.com");
        await Login(organiser, "thabo.ndlovu@example.com");
        var category = await AddCategory(capacity: 2);
        await AddEntry(category);
        var entryTask = participant.PostAsJsonAsync("/api/enrolments", EntryBody(category));
        var updateTask = organiser.PutAsJsonAsync($"/api/events/{category.EventId}/categories/{category.EventCategoryId}", new
        {
            categoryName = category.CategoryName, distanceKm = category.DistanceKm, minimumAge = (int?)null,
            maximumAge = (int?)null, entryFee = category.EntryFee, capacity = 1, isAvailable = true
        });
        using var entry = await entryTask;
        using var update = await updateTask;
        Assert.Contains(entry.StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.Conflict });
        Assert.Equal(entry.StatusCode == HttpStatusCode.Created ? HttpStatusCode.Conflict : HttpStatusCode.OK, update.StatusCode);
        await using var context = database.CreateContext();
        int count = await context.Enrolments.CountAsync(x => x.Status == "Confirmed");
        int capacity = (await context.EventCategories.FindAsync(category.EventCategoryId))!.Capacity!.Value;
        Assert.True(count <= capacity);
    }

    [Fact]
    public async Task EntryAndClosingTheEvent_AreSavedInAConsistentOrder()
    {
        using var factory = Factory();
        using var participant = factory.Client();
        using var organiser = factory.Client();
        await Login(participant);
        await Login(organiser, "thabo.ndlovu@example.com");
        var category = await AddCategory();
        var item = (await organiser.GetFromJsonAsync<EventDetailsResponse>($"/api/events/{category.EventId}"))!;
        var close = new
        {
            item.EventName, item.EventType, item.Description, item.DistanceKm, item.EventDateTime, item.EntryClosingDateTime,
            item.VenueName, item.AddressLine1, item.City, item.Province, item.Latitude, item.Longitude, status = "Closed"
        };
        var entryTask = participant.PostAsJsonAsync("/api/enrolments", EntryBody(category));
        var closeTask = organiser.PutAsJsonAsync($"/api/events/{category.EventId}", close);
        using var entry = await entryTask;
        using var closed = await closeTask;
        Assert.Contains(entry.StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.Conflict });
        Assert.Equal(HttpStatusCode.OK, closed.StatusCode);
        await using var context = database.CreateContext();
        Assert.Equal("Closed", (await context.Events.FindAsync(category.EventId))!.Status);
        Assert.Equal(entry.StatusCode == HttpStatusCode.Created ? 1 : 0, await context.Enrolments.CountAsync());
        using var laterEntry = await participant.PostAsJsonAsync("/api/enrolments", EntryBody(category));
        Assert.Equal(HttpStatusCode.Conflict, laterEntry.StatusCode);
    }

    [Fact]
    public async Task ConcurrentBibAssignments_KeepOneNumberPerEvent()
    {
        using var factory = Factory();
        using var first = factory.Client();
        using var second = factory.Client();
        await Login(first, "thabo.ndlovu@example.com");
        await Login(second, "thabo.ndlovu@example.com");
        var category = await AddCategory();
        var one = await AddEntry(category);
        var two = await AddEntry(category, participant: 4);
        var responses = await Task.WhenAll(
            first.PutAsJsonAsync($"/api/enrolments/{one.EnrolmentId}/bib-number", new { bibNumber = "A100" }),
            second.PutAsJsonAsync($"/api/enrolments/{two.EnrolmentId}/bib-number", new { bibNumber = "a100" }));
        try
        {
            Assert.Single(responses, x => x.StatusCode == HttpStatusCode.OK);
            Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);
            await using var context = database.CreateContext();
            Assert.Equal(1, await context.Enrolments.CountAsync(x => x.BibNumber == "A100"));
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }
}
