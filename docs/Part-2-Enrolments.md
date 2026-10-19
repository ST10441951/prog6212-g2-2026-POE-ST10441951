# RaceDay Part 2 - Enrolments

## 1. Endpoints

Participant actions use the session identity. Rosters and bib numbers require the event's Organiser. Entry details allow its Participant or event owner. Missing sessions return 401; wrong roles and unrelated users return 403 (Microsoft, 2026a).

| Method | Route | Success |
|---|---|---|
| POST | `/api/enrolments` | 201 with entry and Location |
| GET | `/api/enrolments/mine` | 200 with personal entries |
| GET | `/api/enrolments/{enrolmentId}` | 200 with private details |
| PUT | `/api/enrolments/{enrolmentId}/cancel` | 200 with Cancelled entry |
| PUT | `/api/enrolments/{enrolmentId}/reactivate` | 200 with Confirmed entry |
| DELETE | `/api/enrolments/{enrolmentId}` | 204 |
| GET | `/api/events/{eventId}/enrolments` | 200 with owned-event roster |
| PUT | `/api/enrolments/{enrolmentId}/bib-number` | 200 with saved bib |

Lists default to page 1 and size 10, maximum 100, newest entry first. Personal filters: status/event ID. Roster filters: status/category ID. The category must belong to the event. Invalid filters return 400; empty pages return 200 (Microsoft, 2026b).

## 2. Entry rules

POST accepts only `eventId` and `eventCategoryId`. The API assigns identity, status and timestamps. Invalid/unknown fields return 400, missing/mismatched records 404 and non-JSON writes 415 (Microsoft, 2026c).

- Event must be Open, closing time in the future and category available.
- Only Confirmed entries use capacity. Null capacity means unlimited.
- One saved entry per Participant/event; duplicates, including Cancelled history, return 409.
- Age limits are inclusive, using completed years on the UTC event date. Missing required birth dates or out-of-range ages return 400.
- A 29 February birthday advances on 1 March in non-leap years (Microsoft, 2026d).

## 3. Status changes and deletion

| Action | Rule |
|---|---|
| Cancel | Own Confirmed entry, Open event, entries not closed, no result. Keep ID, entry time, category and bib. |
| Reactivate | Own Cancelled entry without a result; repeat entry checks. Keep the row; changing category clears its bib. |
| DELETE | Own Confirmed entry while entries are open and no result exists. Remove only the entry; later GET returns 404. |

Cancelled and historical entries are retained. State conflicts return 409.

## 4. Bib numbers and concurrent writes

Bibs are trimmed and uppercased: 1-20 letters/digits/hyphens, starting with a letter or digit. Numbers are unique within the event, including Cancelled entries. Invalid values return 400 and duplicates 409. Another event may reuse the number; repeated assignment to the same entry returns 200.

Serializable transactions and the shared event update lock protect capacity, availability and status checks. SQL unique constraints also reject duplicate entries and bibs (Microsoft, 2026e; 2026f; 2026g).

## 5. Checks and evidence

The 113 checks cover all eight operations, privacy, ages, capacity, statuses, bibs and competing requests (Microsoft, 2026h). See the [rubric review](Part-2-Rubric-Review.md).

Swagger and HTTP checks on 18 October created an entry, assigned a bib, cancelled/reactivated the same row and read the roster. DELETE returned 204, then GET returned 404. Temporary records were removed.

| Evidence | File |
|---|---|
| Entry and Location | [Enrolment creation](evidence/swagger-enrolment-create.jpg) |
| Same entry reactivated | [Reactivation](evidence/swagger-enrolment-reactivate.jpg) |
| Owned roster | [Event enrolments](evidence/swagger-event-enrolments.jpg) |

## References

Microsoft (2026a) 'Role-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026b) 'Pagination', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/querying/pagination (Accessed: 18 October 2026).

Microsoft (2026c) 'Model validation in ASP.NET Core MVC', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026d) 'How to use DateOnly and TimeOnly', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/standard/datetime/how-to-use-dateonly-timeonly (Accessed: 18 October 2026).

Microsoft (2026e) 'Using Transactions', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/saving/transactions (Accessed: 18 October 2026).

Microsoft (2026f) 'SQL Queries', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/querying/sql-queries (Accessed: 18 October 2026).

Microsoft (2026g) 'Table Hints (Transact-SQL)', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/sql/t-sql/queries/hints-transact-sql-table (Accessed: 18 October 2026).

Microsoft (2026h) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 October 2026).
