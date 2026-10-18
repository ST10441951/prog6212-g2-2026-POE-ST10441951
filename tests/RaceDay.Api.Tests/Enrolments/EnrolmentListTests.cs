using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Contracts.Enrolments;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Api.Tests.Enrolments;

// Paging, selected response fields and API checks: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/querying/pagination
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// Accessed: 18 October 2026.
public sealed partial class EnrolmentTests
{
    [Fact]
    public async Task MyList_FiltersPagesAndCountsOnlyTheSignedInParticipantsEntries()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var entries = new List<Enrolment>();
        EventCategory? firstCategory = null;
        for (int i = 0; i < 5; i++)
        {
            var category = await AddCategory();
            firstCategory ??= category;
            entries.Add(await AddEntry(category, status: i % 2 == 0 ? "Confirmed" : "Cancelled", daysAgo: i));
        }
        await AddEntry(firstCategory!, participant: 4, daysAgo: 0);
        using var response = await client.GetAsync("/api/enrolments/mine?page=2&pageSize=2&participantUserId=4");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = (await response.Content.ReadFromJsonAsync<EnrolmentPageResponse>())!;
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(2, page.Page);
        Assert.Equal(2, page.PageSize);
        Assert.Equal(new[] { entries[2].EnrolmentId, entries[3].EnrolmentId }, page.Items.Select(x => x.EnrolmentId));
        Assert.All(page.Items, x => Assert.Equal(3, x.ParticipantUserId));
        var cancelled = (await client.GetFromJsonAsync<EnrolmentPageResponse>("/api/enrolments/mine?status=%20cancelled%20"))!;
        Assert.Equal(2, cancelled.TotalCount);
        Assert.All(cancelled.Items, x => Assert.Equal("Cancelled", x.Status));
        var byEvent = (await client.GetFromJsonAsync<EnrolmentPageResponse>($"/api/enrolments/mine?eventId={firstCategory!.EventId}"))!;
        Assert.Equal(entries[0].EnrolmentId, Assert.Single(byEvent.Items).EnrolmentId);
        var beyond = (await client.GetFromJsonAsync<EnrolmentPageResponse>("/api/enrolments/mine?page=99"))!;
        Assert.Equal(5, beyond.TotalCount);
        Assert.Empty(beyond.Items);
        var none = (await client.GetFromJsonAsync<EnrolmentPageResponse>($"/api/enrolments/mine?eventId={int.MaxValue}"))!;
        Assert.Equal(0, none.TotalCount);
        Assert.Equal(0, none.TotalPages);
        Assert.Empty(none.Items);
        Assert.True(response.Headers.CacheControl?.NoStore);
        await AssertSafe(response);
    }

    [Fact]
    public async Task OrganiserList_FiltersOnlyItsOwnedEventAndIncludesNamesAndCancelledHistory()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client, "thabo.ndlovu@example.com");
        var first = await AddCategory("Cancelled");
        var second = await AddCategory(eventId: first.EventId, name: "5 km Run");
        var confirmed = await AddEntry(first);
        var cancelled = await AddEntry(second, participant: 4, status: "Cancelled");
        await AddEntry(await AddCategory(owner: 2));
        string path = $"/api/events/{first.EventId}/enrolments";
        using var response = await client.GetAsync(path + "?page=2&pageSize=1&organiserUserId=2");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = (await response.Content.ReadFromJsonAsync<EnrolmentPageResponse>())!;
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        var row = Assert.Single(page.Items);
        Assert.Equal(confirmed.EnrolmentId, row.EnrolmentId);
        Assert.Equal("Naledi Mokoena", row.ParticipantName);
        var byCategory = (await client.GetFromJsonAsync<EnrolmentPageResponse>(path + $"?eventCategoryId={second.EventCategoryId}"))!;
        Assert.Equal(cancelled.EnrolmentId, Assert.Single(byCategory.Items).EnrolmentId);
        var byStatus = (await client.GetFromJsonAsync<EnrolmentPageResponse>(path + "?status=%20CANCELLED%20"))!;
        Assert.Equal(cancelled.EnrolmentId, Assert.Single(byStatus.Items).EnrolmentId);
        var empty = (await client.GetFromJsonAsync<EnrolmentPageResponse>(path + "?status=Confirmed&page=2"))!;
        Assert.Equal(1, empty.TotalCount);
        Assert.Empty(empty.Items);
        await AssertSafe(response);
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
    [InlineData("status=Pending")]
    [InlineData("status=")]
    [InlineData("status=%20%20")]
    public async Task Lists_RejectInvalidPagingAndStatuses(string query)
    {
        using var factory = Factory();
        using var client = factory.Client();
        var category = await AddCategory();
        await Login(client);
        using var mine = await client.GetAsync("/api/enrolments/mine?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, mine.StatusCode);
        await Login(client, "thabo.ndlovu@example.com");
        using var owned = await client.GetAsync($"/api/events/{category.EventId}/enrolments?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, owned.StatusCode);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("text")]
    [InlineData("2147483648")]
    public async Task ListIds_RejectInvalidNumericFilters(string id)
    {
        using var factory = Factory();
        using var client = factory.Client();
        var category = await AddCategory();
        await Login(client);
        using var mine = await client.GetAsync("/api/enrolments/mine?eventId=" + id);
        Assert.Equal(HttpStatusCode.BadRequest, mine.StatusCode);
        await Login(client, "thabo.ndlovu@example.com");
        using var owned = await client.GetAsync($"/api/events/{category.EventId}/enrolments?eventCategoryId=" + id);
        Assert.Equal(HttpStatusCode.BadRequest, owned.StatusCode);
    }

    [Fact]
    public async Task EmptyLists_AreValidAndTheOrganiserCategoryFilterMustBelongToTheEvent()
    {
        using var factory = Factory();
        using var client = factory.Client();
        var category = await AddCategory();
        var foreign = await AddCategory();
        await Login(client);
        var mine = (await client.GetFromJsonAsync<EnrolmentPageResponse>("/api/enrolments/mine"))!;
        Assert.Equal(0, mine.TotalCount);
        Assert.Equal(0, mine.TotalPages);
        Assert.Empty(mine.Items);
        await Login(client, "thabo.ndlovu@example.com");
        var owned = (await client.GetFromJsonAsync<EnrolmentPageResponse>($"/api/events/{category.EventId}/enrolments"))!;
        Assert.Empty(owned.Items);
        Assert.Equal(0, owned.TotalCount);
        foreach (int id in new[] { foreign.EventCategoryId, int.MaxValue })
        {
            using var invalid = await client.GetAsync($"/api/events/{category.EventId}/enrolments?eventCategoryId={id}");
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChangedAccounts_CannotCreateEntriesUsingOldSessions(bool changeRole)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var category = await AddCategory();
        await using (var edit = database.CreateContext())
        {
            var user = (await edit.Users.FindAsync(3))!;
            if (changeRole) user.RoleId = 1; else user.IsActive = false;
            await edit.SaveChangesAsync();
        }
        using var response = await client.PostAsJsonAsync("/api/enrolments", EntryBody(category));
        Assert.Equal(changeRole ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden, response.StatusCode);
        using var replay = await client.GetAsync("/api/enrolments/mine");
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        await using var context = database.CreateContext();
        Assert.Empty(await context.Enrolments.ToListAsync());
    }

    [Fact]
    public async Task Swagger_DescribesAllEightOperationsAndExamplesUseOnlyEditableFields()
    {
        using var factory = Factory();
        using var client = factory.Client();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");
        foreach (var (path, method, success) in new[]
        {
            ("/api/enrolments", "post", "201"), ("/api/enrolments/mine", "get", "200"),
            ("/api/enrolments/{enrolmentId}", "get", "200"), ("/api/enrolments/{enrolmentId}", "delete", "204"),
            ("/api/enrolments/{enrolmentId}/cancel", "put", "200"), ("/api/enrolments/{enrolmentId}/reactivate", "put", "200"),
            ("/api/enrolments/{enrolmentId}/bib-number", "put", "200"), ("/api/events/{eventId}/enrolments", "get", "200")
        })
        {
            var operation = paths.GetProperty(path).GetProperty(method);
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("description").GetString()));
            Assert.True(operation.GetProperty("security")[0].TryGetProperty("SessionCookie", out _));
            foreach (string code in new[] { success, "401", "403" })
                Assert.True(operation.GetProperty("responses").TryGetProperty(code, out _));
        }
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        var create = schemas.GetProperty("CreateEnrolmentRequest").GetProperty("example");
        Assert.Equal(new[] { "eventCategoryId", "eventId" }, create.EnumerateObject().Select(x => x.Name).Order());
        Assert.True(create.GetProperty("eventId").GetInt32() > 0);
        Assert.True(create.GetProperty("eventCategoryId").GetInt32() > 0);
        Assert.Single(schemas.GetProperty("ReactivateEnrolmentRequest").GetProperty("example").EnumerateObject());
        var category = await AddCategory();
        await Login(client);
        using var created = await client.PostAsJsonAsync("/api/enrolments", EntryBody(category));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        int id = (await created.Content.ReadFromJsonAsync<EnrolmentResponse>())!.EnrolmentId;
        await Login(client, "thabo.ndlovu@example.com");
        using var assigned = await client.PutAsync($"/api/enrolments/{id}/bib-number", new StringContent(
            schemas.GetProperty("BibNumberRequest").GetProperty("example").GetRawText(), System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);
    }
}
