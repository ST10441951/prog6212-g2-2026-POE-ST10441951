# RaceDay Part 2 Event Management

## 1. Endpoints

All five endpoints require a signed-in Organiser. Missing or expired sessions return 401. Participants and inactive accounts return 403. Updates and deletion also return 403 when another Organiser owns the event (Microsoft, 2026a).

| Method | Route | Success |
|---|---|---|
| GET | `/api/events/mine` | 200 with owned event summaries and paging totals |
| POST | `/api/events` | 201 with the new Draft and its Location |
| PUT | `/api/events/{eventId}` | 200 with the saved owned event |
| DELETE | `/api/events/{eventId}` | 204 with no response body |
| GET | `/api/organiser/dashboard` | 200 with owned event and enrolment totals |

The owner comes from the session. Requests cannot assign ownership, IDs, image fields or timestamps. Existing public event reads keep the same response fields.

## 2. Event details and validation

The request contains the planned name, type, description, distance, start and closing times, venue, address, city and province. Coordinates are optional and must be supplied together.

Names, descriptions and location fields use the Part 1 length limits and cannot be blank. Type is Running, Walking or Cycling. Province must be one of the nine full South African names. Distances range from 0.01 to 9999.99 kilometres. Coordinates stay within latitude and longitude limits.

Distances allow two decimal places, coordinates six, and dates use whole seconds. These limits match the existing SQL types rather than silently rounding submitted data. Offset times are stored and returned in UTC. Creation requires future start and closing times, with closing earlier than the start. Invalid fields and unexpected JSON properties return 400; non-JSON writes return 415 (Microsoft, 2026b).

The API assigns Draft status and the creation timestamp. The owner can follow the new event's Location immediately, even though the Draft is hidden from public readers. Updates replace the editable fields and set `UpdatedAt` (Microsoft, 2026c).

## 3. Status and history rules

| Current status | Permitted requested statuses |
|---|---|
| Draft | Draft, Open, Cancelled |
| Open | Open, Closed, Cancelled, Completed |
| Closed | Closed, Open, Cancelled, Completed |
| Cancelled | Cancelled |
| Completed | Completed |

- Opening or reopening requires an available category and future start and closing times.
- Completion is allowed only once the event start has passed.
- A changed schedule must have future start and closing times.
- Any saved enrolment, including Cancelled entries, prevents changes to event dates, type and distance.
- Cancelled and Completed events also keep those historical fields when no entries exist. Descriptive corrections remain allowed.
- An event with recorded results cannot be cancelled.
- Cancellation keeps categories, entries and results rather than deleting them.
- Deletion requires no categories or enrolments. An existing banner must be removed first.

Status or history conflicts return 409. SQL checks and the update run in one serializable transaction so a conflicting write cannot slip between validation and saving. Foreign keys also protect event deletion (Microsoft, 2026d).

## 4. Owned list and dashboard

The owned list includes every status and historical event. It accepts `page`, `pageSize` and an optional status filter. Defaults are page 1 and 10 items, with a maximum page size of 100. Status ignores case and surrounding spaces. Ordering uses event date then ID; counts are taken before paging. Invalid filters return 400 and empty pages return 200 (Microsoft, 2026e).

The dashboard includes only the signed-in Organiser's events. It returns all owned event dates and totals for saved enrolments. Confirmed and Cancelled rows are counted separately, including historical records. Upcoming events are future Open or Closed events. Results do not multiply enrolment counts. An Organiser with no events receives zero totals and an empty event list (Microsoft, 2026f).

No account contact details, passwords, storage object names or Participant records are included. Responses use no-store.

## 5. Checks

At the event-management revision, all 354 tests passed locally and on GitHub with none skipped. This includes 121 Organiser event-management checks. The build had no warnings or errors, and the model needed no migration changes.

The new checks cover all five endpoints, both roles, expired sessions, ownership, required and invalid fields, SQL precision, UTC offsets, creation locations, all 25 status pairs, opening readiness, saved history, deletion conflicts, filters, paging, dashboard counts and usable Swagger examples. Tests use temporary SQL Server databases (Microsoft, 2026g).

Swagger checks on 18 October 2026 use the existing Organiser demonstration login and a separate temporary event. Creation returns 201 with a readable Draft Location. The owned list and dashboard include the event, and PUT can cancel it. A separate HTTP cleanup request returned 204 from DELETE, followed by 404 from GET. The original four users, three events, six categories, six routes, four enrolments and two results remain.

| Evidence | File |
|---|---|
| Created Draft and Location | [Swagger event creation](evidence/swagger-event-create.jpg) |
| Owned event and enrolment totals | [Swagger Organiser dashboard](evidence/swagger-organiser-dashboard.jpg) |

The [event-management build](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39124637480) passed all 354 tests. The [planning workflow](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39124637429) also passed. The subsequent [category work](Part-2-Categories.md) adds the category writes needed to open a new Draft through the API. The category revision passed 447 tests locally and on GitHub. The subsequent [route work](Part-2-Routes.md) brings the local total to 532.

## References

Microsoft (2026a) 'Role-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026b) 'Model validation in ASP.NET Core MVC', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026c) 'Basic SaveChanges', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/saving/basic (Accessed: 18 October 2026).

Microsoft (2026d) 'Using Transactions', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/saving/transactions (Accessed: 18 October 2026).

Microsoft (2026e) 'Pagination', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/querying/pagination (Accessed: 18 October 2026).

Microsoft (2026f) 'Complex Query Operators', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/querying/complex-query-operators (Accessed: 18 October 2026).

Microsoft (2026g) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 October 2026).
