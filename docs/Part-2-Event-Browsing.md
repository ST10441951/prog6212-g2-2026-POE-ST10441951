# RaceDay Part 2 Event Browsing

## 1. Endpoints

| Method | Route | Response |
|---|---|---|
| GET | `/api/events` | 200 with matching upcoming events and paging totals |
| GET | `/api/events/{eventId}` | 200 with one visible event, categories and routes |

Both endpoints follow the Part 1 plan. Event creation, updates, deletion, the owned-event list and dashboard are now covered in the [event-management notes](Part-2-Event-Management.md).

## 2. Filters and paging

The upcoming list includes Open and Closed events whose UTC start is later than now. Draft, Cancelled, Completed and past events are excluded, including when their owner is signed in.

| Query value | Rule |
|---|---|
| `page` | Starts at 1; defaults to 1 |
| `pageSize` | From 1 to 100; defaults to 10 |
| `eventType` | Running, Walking or Cycling |
| `province` | One of the nine full South African province names |
| `fromDate` | Optional first UTC event date, inclusive; YYYY-MM-DD |
| `toDate` | Optional final UTC event date, inclusive; YYYY-MM-DD |

Text filters ignore case and surrounding spaces. Invalid types, provinces, dates and paging values return 400 with validation errors. A supplied text filter cannot be blank. The final date cannot be earlier than the first date. Page combinations that exceed the supported SQL offset are rejected.

Filters are combined before counting and paging. Events are ordered by start time, then event ID so events with the same start remain in a stable order. The response includes `page`, `pageSize`, `totalCount`, `totalPages` and `items`. Empty results and pages beyond the last match return 200 with an empty items array (Microsoft, 2026a; 2026b).

## 3. Event visibility

Open, Closed and Completed event details are public, including historical events. Draft and Cancelled events return 404 unless the signed-in Organiser owns the event. Missing and hidden events use the same response.

Public details show available categories and their routes. The owning Organiser also sees unavailable categories. A category without a route returns null for its route. Category and route reads use the existing SQL relationships (Microsoft, 2026c).

Event details use an optional session. Login is not required for public information. When a cookie is supplied, it must pass the existing session, account-status and role checks before owner access is granted. Expired, logged-out, inactive or changed-role sessions cannot reveal hidden information. Request headers and query values cannot select the owner (Microsoft, 2026d).

Responses use selected fields rather than serialising database entities. They exclude user contact information, credentials, enrolments and storage object names. Banner image URLs remain null until image storage is implemented. Responses use no-store because the visible categories can depend on the caller.

## 4. Swagger checks

The existing `RaceDayPart2` sample events were checked without changing their records.

1. Execute GET `/api/events` without login. It returns the upcoming cycling event and Soweto walk.
2. Set `eventType` to Cycling and execute again. Only the Cape Peninsula Cycle Challenge is returned.
3. Execute GET `/api/events/{eventId}` with ID 2. It returns the event details, available categories and route information.
4. The Completed event at ID 1 remains readable directly but is excluded from the upcoming list.

The results depend on the current date. These checks were completed on 18 October 2026. Swagger lists all six filters and explains when an owner session is needed.

| Evidence | File |
|---|---|
| Filtered upcoming events | [Swagger event browsing](evidence/swagger-event-browsing.jpg) |
| Event details and categories | [Swagger event details](evidence/swagger-event-details.jpg) |

## 5. Test results

At the event-browsing revision, all 233 tests passed locally and on GitHub with none skipped: seven startup, 63 database, 51 authentication, 57 profile and password, and 55 event-browsing checks. The build had no warnings or errors, and the model needed no migration changes. The event-management work adds 121 tests and brings the local total to 354, all included in the API workflow.

The new checks cover event statuses, current-time boundaries, all event types and provinces, combined filters, inclusive dates, extreme dates, paging, invalid input, historical details, category visibility, routes, private events and owner sessions. Requests use the real API pipeline and temporary SQL Server databases (Microsoft, 2026e).

The [event-browsing build for 64250d3](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39120804257) passed all 233 tests. The [API workflow](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/workflows/part2-build.yml) runs the full solution on every push. The README build badge shows its latest main-branch result.

## References

Microsoft (2026a) 'Pagination', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/querying/pagination (Accessed: 18 October 2026).

Microsoft (2026b) 'Model validation in ASP.NET Core MVC', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026c) 'Eager Loading of Related Data', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/querying/related-data/eager (Accessed: 18 October 2026).

Microsoft (2026d) 'Simple authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/simple?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026e) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 October 2026).
