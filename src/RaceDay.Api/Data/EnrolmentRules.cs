using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

// Date-only ages and SQL counts: Microsoft (2026).
// https://learn.microsoft.com/en-us/dotnet/standard/datetime/how-to-use-dateonly-timeonly
// https://learn.microsoft.com/en-us/ef/core/querying/
// Rules follow docs/API-Endpoint-Plan.md, section 10.2.
// Accessed: 18 October 2026.
internal static class EnrolmentRules
{
    internal static bool EntriesOpen(Event item, DateTime now) => item.Status == "Open" && item.EntryClosingDateTime > now;

    internal static async Task<(int Status, string Message)?> CanEnter(RaceDayDbContext database, Event item,
        EventCategory category, int participantId, DateTime now, CancellationToken cancellationToken)
    {
        if (!EntriesOpen(item, now)) return (409, "This event is not Open or entries have closed");
        if (!category.IsAvailable) return (409, "This category is unavailable");
        if (category.MinimumAge.HasValue || category.MaximumAge.HasValue)
        {
            var birth = await database.Users.Where(x => x.UserId == participantId).Select(x => x.DateOfBirth)
                .SingleAsync(cancellationToken);
            if (!birth.HasValue) return (400, "A date of birth is required for this category");
            var eventDate = DateOnly.FromDateTime(item.EventDateTime);
            int age = eventDate.Year - birth.Value.Year;
            if (eventDate.Month < birth.Value.Month || (eventDate.Month == birth.Value.Month && eventDate.Day < birth.Value.Day)) age--;
            if (age < category.MinimumAge || age > category.MaximumAge)
                return (400, "Your age on the event date is outside the category limits");
        }
        if (category.Capacity is { } capacity &&
            await database.Enrolments.CountAsync(x => x.EventCategoryId == category.EventCategoryId && x.Status == "Confirmed", cancellationToken) >= capacity)
            return (409, "This category is full");
        return null;
    }
}
