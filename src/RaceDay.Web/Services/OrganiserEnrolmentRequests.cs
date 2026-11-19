using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using RaceDay.Web.Models.Entries;
using RaceDay.Web.Models.Organiser;

namespace RaceDay.Web.Services;

// Private roster reads, paging and explicit bib writes: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-10.0
// Contract: docs/API-Endpoint-Plan.md, section 10.
// Accessed: 18 November 2026.
public sealed partial class OrganiserApiClient
{
    internal async Task<EntryPage> EventRoster(int eventId, RosterSearch search, WebLogin login, CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string?> { ["page"] = search.Page.ToString(CultureInfo.InvariantCulture), ["pageSize"] = "10" };
        if (search.Status is not null) query["status"] = search.Status;
        if (search.EventCategoryId is { } categoryId) query["eventCategoryId"] = categoryId.ToString(CultureInfo.InvariantCulture);
        using var request = new HttpRequestMessage(HttpMethod.Get, QueryHelpers.AddQueryString(EventRoute(eventId) + "/enrolments", query));
        using var response = await EventRequest(request, HttpStatusCode.OK, login, cancellationToken);
        var page = await Read<EntryPage>(response, cancellationToken);
        if (page is null || page.Page != search.Page || page.PageSize != 10 || page.TotalCount < 0 ||
            page.TotalPages != (int)(((long)page.TotalCount + 9) / 10) || page.Items is null ||
            page.Items.Count != Math.Min(10L, Math.Max(0L, page.TotalCount - ((long)page.Page - 1) * 10)) ||
            page.Items.Any(item => !RosterEntry(item) || item.EventId != eventId) ||
            page.Items.Select(item => item.EnrolmentId).Distinct().Count() != page.Items.Count)
            throw new ApiUnavailableException("The API returned inconsistent event enrolments.");
        return page;
    }

    internal async Task<EntryDetails> RosterDetails(int eventId, int enrolmentId, WebLogin login, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, EntryRoute(enrolmentId));
        using var response = await EventRequest(request, HttpStatusCode.OK, login, cancellationToken);
        var entry = await Read<EntryDetails>(response, cancellationToken);
        if (!RosterEntry(entry) || entry!.EnrolmentId != enrolmentId)
            throw new ApiUnavailableException("The API returned inconsistent enrolment details.");
        if (entry.EventId != eventId) throw new EventWriteException(404, "This enrolment does not belong to the selected event.");
        return entry;
    }

    internal async Task<EntryDetails> SaveBibNumber(EntryDetails original, BibNumberForm model, WebLogin login, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, EntryRoute(original.EnrolmentId) + "/bib-number")
        { Content = JsonContent.Create(new { model.BibNumber }) };
        using var response = await EventRequest(request, HttpStatusCode.OK, login, cancellationToken);
        var saved = await Read<EntryDetails>(response, cancellationToken);
        // Confirm the API's documented uppercase value and unchanged entry identity/history.
        if (!RosterEntry(saved) || saved!.EnrolmentId != original.EnrolmentId || saved.EventId != original.EventId ||
            saved.ParticipantUserId != original.ParticipantUserId || saved.EventCategoryId != original.EventCategoryId ||
            saved.Status != original.Status || saved.EnrolmentDate != original.EnrolmentDate ||
            saved.BibNumber != model.BibNumber.Trim().ToUpperInvariant())
            throw new ApiUnavailableException("The API did not confirm this bib assignment.");
        return saved;
    }

    private static string EntryRoute(int enrolmentId) => $"api/enrolments/{enrolmentId.ToString(CultureInfo.InvariantCulture)}";
    private static bool RosterEntry(EntryDetails? entry) => entry is { EnrolmentId: > 0, ParticipantUserId: > 0, EventId: > 0, EventCategoryId: > 0 } &&
        !string.IsNullOrWhiteSpace(entry.ParticipantName) && !string.IsNullOrWhiteSpace(entry.EventName) &&
        !string.IsNullOrWhiteSpace(entry.CategoryName) && entry.Status is "Confirmed" or "Cancelled";
}
