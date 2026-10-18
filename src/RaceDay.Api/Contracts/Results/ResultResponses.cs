using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Contracts.Results;

// Selected projections, correlated counts and paging: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/querying/client-eval
// https://learn.microsoft.com/en-us/ef/core/querying/complex-query-operators
// https://learn.microsoft.com/en-us/ef/core/querying/pagination
// Accessed: 18 October 2026.
internal static class ResultResponses
{
    internal static readonly Expression<Func<Result, ResultResponse>> Details = x => new ResultResponse
    {
        ResultId = x.ResultId, EnrolmentId = x.EnrolmentId, RecordedByUserId = x.RecordedByUserId,
        ParticipantUserId = x.Enrolment.ParticipantUserId,
        ParticipantName = x.Enrolment.Participant.FirstName + " " + x.Enrolment.Participant.LastName,
        EventId = x.Enrolment.EventId, EventName = x.Enrolment.Event.EventName,
        EventDateTime = DateTime.SpecifyKind(x.Enrolment.Event.EventDateTime, DateTimeKind.Utc),
        EventCategoryId = x.Enrolment.EventCategoryId, CategoryName = x.Enrolment.Category.CategoryName,
        CategoryDistanceKm = x.Enrolment.Category.DistanceKm, BibNumber = x.Enrolment.BibNumber,
        ResultStatus = x.ResultStatus, FinishTimeSeconds = x.FinishTimeSeconds, OverallPosition = x.OverallPosition,
        TotalFinishers = x.Enrolment.Event.Enrolments.Count(e => e.Result != null && e.Result.ResultStatus == "Completed"),
        CategoryPosition = x.CategoryPosition, Notes = x.Notes,
        RecordedAt = DateTime.SpecifyKind(x.RecordedAt, DateTimeKind.Utc),
        UpdatedAt = x.UpdatedAt.HasValue ? DateTime.SpecifyKind(x.UpdatedAt.Value, DateTimeKind.Utc) : null
    };

    internal static async Task<ResultPageResponse> Page(IQueryable<Result> query, ResultListRequest request,
        bool eventList, CancellationToken cancellationToken)
    {
        if (request.ResultStatus is { } status) query = query.Where(x => x.ResultStatus == status);
        int count = await query.CountAsync(cancellationToken);
        var ordered = eventList
            ? query.OrderBy(x => x.ResultStatus != "Completed").ThenBy(x => x.OverallPosition == null)
                .ThenBy(x => x.OverallPosition).ThenBy(x => x.FinishTimeSeconds == null).ThenBy(x => x.FinishTimeSeconds).ThenBy(x => x.ResultId)
            : query.OrderByDescending(x => x.Enrolment.Event.EventDateTime).ThenByDescending(x => x.ResultId);
        var items = await ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(Details).ToListAsync(cancellationToken);
        return new ResultPageResponse
        {
            Page = request.Page, PageSize = request.PageSize, TotalCount = count,
            TotalPages = (int)(((long)count + request.PageSize - 1) / request.PageSize), Items = items
        };
    }
}
