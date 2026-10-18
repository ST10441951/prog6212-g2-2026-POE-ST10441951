using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Contracts.Enrolments;

// SQL projections, UTC response values and paging: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/querying/client-eval
// https://learn.microsoft.com/en-us/ef/core/querying/pagination
// https://learn.microsoft.com/en-us/dotnet/api/system.datetime.specifykind
// Accessed: 18 October 2026.
internal static class EnrolmentResponses
{
    internal static readonly Expression<Func<Enrolment, EnrolmentResponse>> Details = x => new EnrolmentResponse
    {
        EnrolmentId = x.EnrolmentId, ParticipantUserId = x.ParticipantUserId,
        ParticipantName = x.Participant.FirstName + " " + x.Participant.LastName,
        EventId = x.EventId, EventName = x.Event.EventName,
        EventDateTime = DateTime.SpecifyKind(x.Event.EventDateTime, DateTimeKind.Utc),
        EventCategoryId = x.EventCategoryId, CategoryName = x.Category.CategoryName, CategoryDistanceKm = x.Category.DistanceKm,
        EnrolmentDate = DateTime.SpecifyKind(x.EnrolmentDate, DateTimeKind.Utc), Status = x.Status, BibNumber = x.BibNumber,
        UpdatedAt = x.UpdatedAt.HasValue ? DateTime.SpecifyKind(x.UpdatedAt.Value, DateTimeKind.Utc) : null,
        Result = x.Result == null ? null : new EnrolmentResultResponse
        {
            ResultId = x.Result.ResultId, ResultStatus = x.Result.ResultStatus, FinishTimeSeconds = x.Result.FinishTimeSeconds,
            OverallPosition = x.Result.OverallPosition, CategoryPosition = x.Result.CategoryPosition
        }
    };

    internal static async Task<EnrolmentPageResponse> Page(IQueryable<Enrolment> query, EnrolmentListRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Status is { } status)
        {
            string filter = status.ToUpperInvariant();
            query = query.Where(x => x.Status.ToUpper() == filter);
        }
        int count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.EnrolmentDate).ThenByDescending(x => x.EnrolmentId)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).Select(Details).ToListAsync(cancellationToken);
        return new EnrolmentPageResponse
        {
            Page = request.Page, PageSize = request.PageSize, TotalCount = count,
            TotalPages = (int)(((long)count + request.PageSize - 1) / request.PageSize), Items = items
        };
    }
}
