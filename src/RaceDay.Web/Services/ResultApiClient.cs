using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using RaceDay.Web.Models.Results;

namespace RaceDay.Web.Services;

// Isolated private HTTP requests, JSON and API problems: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-10.0
// Contract: docs/API-Endpoint-Plan.md, section 11.
// Accessed: 18 November 2026.
public sealed class ResultApiClient(HttpClient client)
{
    internal async Task<ResultPage> Mine(ResultSearch search, WebLogin login, CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = search.Page.ToString(CultureInfo.InvariantCulture), ["pageSize"] = "10"
        };
        if (search.ResultStatus is not null) query["resultStatus"] = search.ResultStatus;
        using var response = await Send(QueryHelpers.AddQueryString("api/results/mine", query), login, cancellationToken);
        var page = await Read<ResultPage>(response, cancellationToken);
        if (page is null || page.Page != search.Page || page.PageSize != 10 || page.TotalCount < 0 ||
            page.TotalPages != (int)(((long)page.TotalCount + 9) / 10) || page.Items is null ||
            page.Items.Count != Math.Min(10L, Math.Max(0L, page.TotalCount - ((long)page.Page - 1) * 10)) ||
            page.Items.Any(item => !Owned(item, login)) || page.Items.Select(item => item.ResultId).Distinct().Count() != page.Items.Count)
            throw new ApiUnavailableException("The API returned inconsistent personal results.");
        return page;
    }

    internal async Task<ResultDetails> Details(int enrolmentId, WebLogin login, CancellationToken cancellationToken)
    {
        using var response = await Send($"api/enrolments/{enrolmentId.ToString(CultureInfo.InvariantCulture)}/result", login, cancellationToken);
        var result = await Read<ResultDetails>(response, cancellationToken);
        if (!Owned(result, login) || result!.EnrolmentId != enrolmentId)
            throw new ApiUnavailableException("The API did not return this user's result.");
        return result;
    }

    private static bool Owned(ResultDetails? result, WebLogin login) =>
        result is { ResultId: > 0, EnrolmentId: > 0, EventId: > 0, EventCategoryId: > 0, TotalFinishers: >= 0 } &&
        result.ParticipantUserId == login.UserId && !string.IsNullOrWhiteSpace(result.EventName) &&
        !string.IsNullOrWhiteSpace(result.CategoryName) && (result.ResultStatus == "Completed"
            ? result.FinishTimeSeconds is > 0 && result.TotalFinishers > 0 && result.OverallPosition is null or > 0 && result.CategoryPosition is null or > 0
            : result.ResultStatus is "DidNotFinish" or "DidNotStart" or "Disqualified" && result.FinishTimeSeconds is null &&
              result.OverallPosition is null && result.CategoryPosition is null);

    private async Task<HttpResponseMessage> Send(string route, WebLogin login, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, route);
        request.Headers.Add("Cookie", login.ApiCookie);
        HttpResponseMessage response;
        try { response = await client.SendAsync(request, cancellationToken); }
        catch (HttpRequestException error) { throw new ApiUnavailableException("The results API could not be reached.", error); }
        catch (OperationCanceledException error) when (!cancellationToken.IsCancellationRequested)
        { throw new ApiUnavailableException("The results API did not respond in time.", error); }
        if (response.StatusCode == HttpStatusCode.OK) return response;
        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized) throw new ApiSessionEndedException();
            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
                throw new ResultRequestException((int)response.StatusCode);
            throw new ApiUnavailableException("The results API returned an unexpected response.");
        }
    }

    private static async Task<T?> Read<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try { return await response.Content.ReadFromJsonAsync<T>(cancellationToken); }
        catch (JsonException error) { throw new ApiUnavailableException("The results response was unreadable.", error); }
    }
}

internal sealed class ResultRequestException(int statusCode) : Exception("The results request was rejected.")
{
    internal int StatusCode { get; } = statusCode;
}
