using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Contracts.Results;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Api.Tests.Results;

// Filtered lists, projected counts and paging: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/querying/complex-query-operators
// https://learn.microsoft.com/en-us/ef/core/querying/pagination
// Accessed: 18 October 2026.
public sealed partial class ResultTests
{
    [Fact]
    public async Task TotalFinishers_CountsCompletedResultsAcrossAllCategoriesAndChangesAfterCorrections()
    {
        using var factory = Factory();
        using var client = factory.Client();
        var firstEntry = await AddEntry();
        await AddResult(firstEntry, position: 42);
        var firstCategory = await Category(firstEntry.EventCategoryId);
        var secondCategory = new EventCategory
        {
            EventId = firstEntry.EventId, CategoryName = "5 km Run", DistanceKm = 5, EntryFee = 100,
            IsAvailable = true, CreatedAt = Now.UtcDateTime.AddDays(-20)
        };
        await using (var edit = database.CreateContext())
        {
            edit.EventCategories.Add(secondCategory);
            await edit.SaveChangesAsync();
        }
        var secondEntry = await AddEntry(participant: 4, category: secondCategory);
        await AddResult(secondEntry, position: 99);
        foreach (string status in new[] { "DidNotFinish", "Disqualified", "DidNotStart" })
            await AddResult(await AddEntry(participant: await AddParticipant(), category: firstCategory), status);
        await AddResult(await AddEntry());
        await Login(client, "naledi.mokoena@example.com");
        var mine = (await client.GetFromJsonAsync<ResultPageResponse>($"/api/results/mine?eventId={firstEntry.EventId}&pageSize=1"))!;
        Assert.Equal(2, Assert.Single(mine.Items).TotalFinishers);
        Assert.Equal(1, mine.TotalCount);
        await Login(client);
        var filtered = (await client.GetFromJsonAsync<ResultPageResponse>($"/api/events/{firstEntry.EventId}/results?resultStatus=DidNotFinish&eventCategoryId={firstCategory.EventCategoryId}"))!;
        Assert.Equal(1, filtered.TotalCount);
        Assert.Equal(2, Assert.Single(filtered.Items).TotalFinishers);
        var page = (await client.GetFromJsonAsync<ResultPageResponse>($"/api/events/{firstEntry.EventId}/results?pageSize=1"))!;
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(5, page.TotalPages);
        Assert.Equal(42, Assert.Single(page.Items).OverallPosition);
        Assert.Equal(2, page.Items[0].TotalFinishers);
        using var correction = await client.PutAsJsonAsync(Path(secondEntry), Details("DidNotFinish"));
        Assert.Equal(HttpStatusCode.OK, correction.StatusCode);
        Assert.Equal(1, (await correction.Content.ReadFromJsonAsync<ResultResponse>())!.TotalFinishers);
        await Login(client, "naledi.mokoena@example.com");
        var refreshed = (await client.GetFromJsonAsync<ResultResponse>(Path(firstEntry)))!;
        Assert.Equal(1, refreshed.TotalFinishers);
        Assert.Equal(42, refreshed.OverallPosition);
    }

    [Fact]
    public async Task PersonalList_FiltersAndPagesOnlyItsOwnersResultsInEventDateOrder()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client, "naledi.mokoena@example.com");
        var entries = new List<Enrolment>();
        var ids = new List<int>();
        for (int i = 0; i < 5; i++)
        {
            var entry = await AddEntry();
            await using (var edit = database.CreateContext())
            {
                (await edit.Events.FindAsync(entry.EventId))!.EventDateTime = Now.UtcDateTime.AddDays(-2 - i);
                (await edit.Events.FindAsync(entry.EventId))!.EntryClosingDateTime = Now.UtcDateTime.AddDays(-3 - i);
                await edit.SaveChangesAsync();
            }
            entries.Add(entry);
            ids.Add((await AddResult(entry, i % 2 == 0 ? "Completed" : "DidNotFinish")).ResultId);
        }
        await AddResult(await AddEntry(participant: 4));
        using var response = await client.GetAsync("/api/results/mine?page=2&pageSize=2&participantUserId=4");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = (await response.Content.ReadFromJsonAsync<ResultPageResponse>())!;
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(new[] { ids[2], ids[3] }, page.Items.Select(x => x.ResultId));
        Assert.All(page.Items, x => Assert.Equal(3, x.ParticipantUserId));
        var filtered = (await client.GetFromJsonAsync<ResultPageResponse>("/api/results/mine?resultStatus=%20didnotfinish%20"))!;
        Assert.Equal(2, filtered.TotalCount);
        var byEvent = (await client.GetFromJsonAsync<ResultPageResponse>($"/api/results/mine?eventId={entries[0].EventId}"))!;
        Assert.Equal(ids[0], Assert.Single(byEvent.Items).ResultId);
        var later = (await client.GetFromJsonAsync<ResultPageResponse>("/api/results/mine?page=99"))!;
        Assert.Empty(later.Items);
        Assert.Equal(5, later.TotalCount);
        Assert.True(response.Headers.CacheControl?.NoStore);
        await AssertSafe(response);
    }

    [Fact]
    public async Task EventList_OrdersFinishersBeforeNonFinishersAndAllowsUnassignedPositionsAndTies()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var first = await AddEntry();
        var category = await Category(first.EventCategoryId);
        var positioned = await AddResult(first, position: 2, seconds: 3600);
        var unpositioned = await AddResult(await AddEntry(participant: 4, category: category), position: null, seconds: 1000);
        var slower = await AddResult(await AddEntry(participant: await AddParticipant(), category: category), position: null, seconds: 2000);
        var dnf = await AddResult(await AddEntry(participant: await AddParticipant(), category: category), "DidNotFinish");
        var disqualified = await AddResult(await AddEntry(participant: await AddParticipant(), category: category), "Disqualified");
        await AddResult(await AddEntry(owner: 2));
        var page = (await client.GetFromJsonAsync<ResultPageResponse>($"/api/events/{first.EventId}/results?organiserUserId=2"))!;
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(new[] { positioned.ResultId, unpositioned.ResultId, slower.ResultId, dnf.ResultId, disqualified.ResultId }, page.Items.Select(x => x.ResultId));
        Assert.All(page.Items, x => Assert.Equal(3, x.TotalFinishers));
        var secondPage = (await client.GetFromJsonAsync<ResultPageResponse>($"/api/events/{first.EventId}/results?page=2&pageSize=2"))!;
        Assert.Equal(new[] { slower.ResultId, dnf.ResultId }, secondPage.Items.Select(x => x.ResultId));
        var tieRequest = Details();
        tieRequest["overallPosition"] = 2;
        using var tie = await client.PutAsJsonAsync($"/api/enrolments/{unpositioned.EnrolmentId}/result", tieRequest);
        Assert.Equal(HttpStatusCode.OK, tie.StatusCode);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("page=-1")]
    [InlineData("page=1.5")]
    [InlineData("page=2147483648")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("pageSize=text")]
    [InlineData("page=2147483647&pageSize=100")]
    [InlineData("resultStatus=Pending")]
    [InlineData("resultStatus=")]
    [InlineData("resultStatus=%20%20")]
    public async Task Lists_RejectInvalidPagingAndStatuses(string query)
    {
        using var factory = Factory();
        using var client = factory.Client();
        var entry = await AddEntry();
        await Login(client, "naledi.mokoena@example.com");
        using var mine = await client.GetAsync("/api/results/mine?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, mine.StatusCode);
        await Login(client);
        using var owned = await client.GetAsync($"/api/events/{entry.EventId}/results?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, owned.StatusCode);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("text")]
    [InlineData("2147483648")]
    public async Task Lists_RejectInvalidNumericFilters(string id)
    {
        using var factory = Factory();
        using var client = factory.Client();
        var entry = await AddEntry();
        await Login(client, "naledi.mokoena@example.com");
        using var mine = await client.GetAsync("/api/results/mine?eventId=" + id);
        Assert.Equal(HttpStatusCode.BadRequest, mine.StatusCode);
        await Login(client);
        using var owned = await client.GetAsync($"/api/events/{entry.EventId}/results?eventCategoryId=" + id);
        Assert.Equal(HttpStatusCode.BadRequest, owned.StatusCode);
    }

    [Fact]
    public async Task EmptyLists_AreValidAndCategoryFiltersMustBelongToTheEvent()
    {
        using var factory = Factory();
        using var client = factory.Client();
        var entry = await AddEntry();
        var other = await AddEntry();
        await Login(client, "naledi.mokoena@example.com");
        var mine = (await client.GetFromJsonAsync<ResultPageResponse>("/api/results/mine"))!;
        Assert.Equal(0, mine.TotalCount);
        Assert.Equal(0, mine.TotalPages);
        Assert.Empty(mine.Items);
        await Login(client);
        var owned = (await client.GetFromJsonAsync<ResultPageResponse>($"/api/events/{entry.EventId}/results"))!;
        Assert.Empty(owned.Items);
        Assert.Equal(0, owned.TotalCount);
        foreach (int categoryId in new[] { other.EventCategoryId, int.MaxValue })
        {
            using var invalid = await client.GetAsync($"/api/events/{entry.EventId}/results?eventCategoryId={categoryId}");
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        }
    }

    private async Task<EventCategory> Category(int id)
    {
        await using var context = database.CreateContext();
        return await context.EventCategories.AsNoTracking().SingleAsync(x => x.EventCategoryId == id);
    }
}
