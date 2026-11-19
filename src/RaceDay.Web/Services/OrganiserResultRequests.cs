using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using RaceDay.Web.Models.Entries;
using RaceDay.Web.Models.Organiser;
using RaceDay.Web.Models.Results;

namespace RaceDay.Web.Services;

// Private HTTP requests, explicit JSON and elapsed-time conversion: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/dotnet/standard/base-types/parsing-numeric
// Contract: docs/API-Endpoint-Plan.md, section 11.
// Accessed: 19 November 2026.
public sealed partial class OrganiserApiClient
{
    internal async Task<ResultPage> EventResults(int eventId, EventResultSearch search, WebLogin login, CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string?> { ["page"] = search.Page.ToString(CultureInfo.InvariantCulture), ["pageSize"] = "10" };
        if (search.ResultStatus is not null) query["resultStatus"] = search.ResultStatus;
        if (search.EventCategoryId is { } categoryId) query["eventCategoryId"] = categoryId.ToString(CultureInfo.InvariantCulture);
        using var request = new HttpRequestMessage(HttpMethod.Get, QueryHelpers.AddQueryString(EventRoute(eventId) + "/results", query));
        using var response = await EventRequest(request, HttpStatusCode.OK, login, cancellationToken);
        var page = await Read<ResultPage>(response, cancellationToken);
        if (page is null || page.Page != search.Page || page.PageSize != 10 || page.TotalCount < 0 ||
            page.TotalPages != (int)(((long)page.TotalCount + 9) / 10) || page.Items is null ||
            page.Items.Count != Math.Min(10L, Math.Max(0L, page.TotalCount - ((long)page.Page - 1) * 10)) ||
            page.Items.Any(item => !OfficialResult(item) || item.EventId != eventId) ||
            page.Items.Select(item => item.ResultId).Distinct().Count() != page.Items.Count ||
            page.Items.Select(item => item.EnrolmentId).Distinct().Count() != page.Items.Count)
            throw new ApiUnavailableException("The API returned inconsistent event results.");
        return page;
    }

    internal async Task<ResultDetails> OfficialResult(EntryDetails entry, WebLogin login, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, EntryRoute(entry.EnrolmentId) + "/result");
        using var response = await EventRequest(request, HttpStatusCode.OK, login, cancellationToken);
        var result = await Read<ResultDetails>(response, cancellationToken);
        if (!MatchesEntry(result, entry)) throw new ApiUnavailableException("The API did not confirm this entry's result.");
        return result!;
    }

    internal async Task<ResultDetails> SaveOfficialResult(ResultForm form, WebLogin login, CancellationToken cancellationToken)
    {
        int? seconds = FinishSeconds(form.FinishTime);
        var entry = form.Entry!;
        using var request = new HttpRequestMessage(form.Original is null ? HttpMethod.Post : HttpMethod.Put, EntryRoute(entry.EnrolmentId) + "/result")
        {
            Content = JsonContent.Create(new { form.ResultStatus, finishTimeSeconds = seconds, form.OverallPosition, form.CategoryPosition, form.Notes })
        };
        using var response = await EventRequest(request, form.Original is null ? HttpStatusCode.Created : HttpStatusCode.OK, login, cancellationToken);
        var saved = await Read<ResultDetails>(response, cancellationToken);
        string? notes = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim();
        if (!MatchesEntry(saved, entry) || !string.Equals(saved!.ResultStatus, form.ResultStatus.Trim(), StringComparison.OrdinalIgnoreCase) ||
            saved.FinishTimeSeconds != seconds || saved.OverallPosition != form.OverallPosition || saved.CategoryPosition != form.CategoryPosition || saved.Notes != notes ||
            (form.Original is { } original
                ? saved.ResultId != original.ResultId || saved.RecordedByUserId != original.RecordedByUserId || saved.RecordedAt != original.RecordedAt || saved.UpdatedAt is null
                : saved.RecordedByUserId != login.UserId || saved.UpdatedAt is not null))
            throw new ApiUnavailableException("The API did not confirm the saved result.");
        return saved;
    }

    private static bool MatchesEntry(ResultDetails? result, EntryDetails entry) => OfficialResult(result) &&
        result!.EnrolmentId == entry.EnrolmentId && result.EventId == entry.EventId &&
        result.ParticipantUserId == entry.ParticipantUserId && result.EventCategoryId == entry.EventCategoryId;

    private static bool OfficialResult(ResultDetails? result) =>
        result is { ResultId: > 0, EnrolmentId: > 0, EventId: > 0, EventCategoryId: > 0, ParticipantUserId: > 0, RecordedByUserId: > 0, TotalFinishers: >= 0 } &&
        !string.IsNullOrWhiteSpace(result.ParticipantName) && !string.IsNullOrWhiteSpace(result.EventName) &&
        !string.IsNullOrWhiteSpace(result.CategoryName) && result.RecordedAt != default && (result.ResultStatus == "Completed"
            ? result.FinishTimeSeconds is > 0 && result.TotalFinishers > 0 && result.OverallPosition is null or > 0 && result.CategoryPosition is null or > 0
            : result.ResultStatus is "DidNotFinish" or "DidNotStart" or "Disqualified" && result.FinishTimeSeconds is null && result.OverallPosition is null && result.CategoryPosition is null);

    private static int? FinishSeconds(string? time)
    {
        if (string.IsNullOrWhiteSpace(time)) return null;
        if (Regex.IsMatch(time.Trim(), @"^[0-9]{1,6}:[0-5][0-9]:[0-5][0-9]$", RegexOptions.CultureInvariant))
        {
            var parts = time.Trim().Split(':');
            long seconds = long.Parse(parts[0], CultureInfo.InvariantCulture) * 3600 + int.Parse(parts[1], CultureInfo.InvariantCulture) * 60 + int.Parse(parts[2], CultureInfo.InvariantCulture);
            if (seconds <= int.MaxValue) return (int)seconds;
        }
        throw new EventWriteException(400, "Enter an elapsed time as HH:MM:SS.", new() { ["FinishTime"] = ["Use HH:MM:SS within the supported time range, for example 01:02:30."] });
    }
}
