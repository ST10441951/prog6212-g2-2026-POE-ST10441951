using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

// Parameterised SQL and transaction-held update locks: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/querying/sql-queries
// https://learn.microsoft.com/en-us/sql/t-sql/queries/hints-transact-sql-table
// Accessed: 18 October 2026.
internal static class EventWriteLock
{
    // Call inside a transaction before reading entries or changing event/category availability.
    // One writer per event prevents two requests from taking the last category place together.
    internal static IQueryable<Event> Query(RaceDayDbContext database, int eventId)
    {
        if (database.Database.CurrentTransaction is null)
            throw new InvalidOperationException("An event write lock requires a transaction.");
        return database.Events.FromSqlInterpolated(
            $"SELECT * FROM dbo.Events WITH (UPDLOCK, HOLDLOCK) WHERE EventId = {eventId}");
    }
}
