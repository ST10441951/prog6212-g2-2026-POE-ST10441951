using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Contracts.Events;
using Xunit;

namespace RaceDay.Api.Tests.Results;

// Competing result/event writes share the transaction-held event lock: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/saving/transactions
// https://learn.microsoft.com/en-us/sql/t-sql/queries/hints-transact-sql-table
// Accessed: 18 October 2026.
public sealed partial class ResultTests
{
    [Fact]
    public async Task ConcurrentCreation_SavesOneOfficialResultPerEntry()
    {
        using var factory = Factory();
        using var first = factory.Client();
        using var second = factory.Client();
        await Login(first);
        await Login(second);
        var entry = await AddEntry();
        var responses = await Task.WhenAll(first.PostAsJsonAsync(Path(entry), Details()), second.PostAsJsonAsync(Path(entry), Details()));
        try
        {
            Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Created);
            Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);
            await using var context = database.CreateContext();
            Assert.Equal(1, await context.Results.CountAsync());
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    [Fact]
    public async Task CapturingAResultAndCancellingItsEvent_CannotBothSucceed()
    {
        using var factory = Factory();
        using var first = factory.Client();
        using var second = factory.Client();
        await Login(first);
        await Login(second);
        var entry = await AddEntry(eventStatus: "Open");
        var item = (await first.GetFromJsonAsync<EventDetailsResponse>($"/api/events/{entry.EventId}"))!;
        var cancelRequest = new
        {
            item.EventName, item.EventType, item.Description, item.DistanceKm, item.EventDateTime, item.EntryClosingDateTime,
            item.VenueName, item.AddressLine1, item.City, item.Province, item.Latitude, item.Longitude, status = "Cancelled"
        };
        var captureTask = first.PostAsJsonAsync(Path(entry), Details());
        var cancelTask = second.PutAsJsonAsync($"/api/events/{entry.EventId}", cancelRequest);
        using var captured = await captureTask;
        using var cancelled = await cancelTask;
        Assert.Contains(captured.StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.Conflict });
        Assert.Equal(captured.StatusCode == HttpStatusCode.Created ? HttpStatusCode.Conflict : HttpStatusCode.OK, cancelled.StatusCode);
        await using var context = database.CreateContext();
        Assert.Equal(captured.StatusCode == HttpStatusCode.Created ? 1 : 0, await context.Results.CountAsync());
        Assert.Equal(captured.StatusCode == HttpStatusCode.Created ? "Open" : "Cancelled", (await context.Events.FindAsync(entry.EventId))!.Status);
        Assert.Equal("Confirmed", (await context.Enrolments.FindAsync(entry.EnrolmentId))!.Status);
    }
}
