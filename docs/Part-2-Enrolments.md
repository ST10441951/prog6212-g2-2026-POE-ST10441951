# RaceDay Part 2 - Enrolments

## 1. Endpoints and access

| Method | Route | Success |
|---|---|---|
| POST | `/api/enrolments` | 201 with the Confirmed entry and its Location |
| GET | `/api/enrolments/mine` | 200 with a page of personal entries |
| GET | `/api/enrolments/{enrolmentId}` | 200 with private entry details |
| PUT | `/api/enrolments/{enrolmentId}/cancel` | 200 with the Cancelled entry |
| PUT | `/api/enrolments/{enrolmentId}/reactivate` | 200 with the reactivated Confirmed entry |
| DELETE | `/api/enrolments/{enrolmentId}` | 204 with no body |
| GET | `/api/events/{eventId}/enrolments` | 200 with the owned event's roster |
| PUT | `/api/enrolments/{enrolmentId}/bib-number` | 200 with the assigned bib number |

Participant operations use the signed-in account ID. The roster and bib assignment require the event's Organiser. A single entry can be read only by its Participant or event Organiser. Missing sessions return 401; wrong roles and unrelated users return 403 (Microsoft, 2026a).

The response includes the planned IDs, status, entry time and bib number, plus Participant/event/category names, UTC event and update times, category distance and an optional result summary. Account contact details, birth dates, passwords and storage object names are excluded.

Both lists use page 1 and 10 items by default, with a maximum page size of 100. Newest entry time then highest ID comes first. Counts apply after access and filters, before paging. Personal filters are status and event ID; roster filters are status and category ID. Status ignores case and surrounding spaces. An Organiser's category filter must belong to the event. Invalid filters return 400; empty matches and later pages return 200 (Microsoft, 2026b).

## 2. Entry rules

POST takes only `eventId` and `eventCategoryId`. The category must belong to the event. The API assigns the Participant, Confirmed status, ID and UTC entry time. Unknown JSON fields are rejected. Invalid fields return 400, missing or mismatched records return 404, and non-JSON writes return 415 (Microsoft, 2026c).

- The event must be Open and its closing time must be later than now. Equality means entries are closed.
- The category must be available. Only Confirmed entries use its capacity; null capacity means unlimited places.
- A Participant has at most one saved entry per event. POST returns 409 for an existing entry, including Cancelled history.
- Age is measured in completed years on the date part of the stored UTC event date. Minimum and maximum ages are inclusive.
- Either age limit requires a date of birth. Missing or out-of-range ages return 400; unrestricted categories do not require it.
- For a 29 February birthday, the completed-year calculation advances on 1 March in a non-leap year.

These checks also apply to reactivation. Event dates and birthdays use the .NET date types (Microsoft, 2026d).

## 3. Cancellation, reactivation and deletion

Cancellation requires an owned Confirmed entry, an Open event, a future entry-closing time and no official result. It changes status and update time, keeping the original ID, entry time, category and bib number. A repeated cancellation returns 409.

Reactivation requires a Cancelled entry without a result and takes the selected category ID. It repeats the entry checks and updates the same row. The event and Participant cannot change. Changing category clears the old bib number; keeping the same category preserves it.

DELETE removes only an owned Confirmed entry while entries are open and no result exists. Cancelled entries and closed-event history are retained. A later GET returns 404. The event and category remain. State conflicts return 409.

## 4. Bib numbers and competing requests

Bib numbers use one to 20 letters, digits or hyphens, starting with a letter or digit. They are trimmed and stored in uppercase. A number is unique within its event, including Cancelled entries. Another event can reuse it. Repeating the same assignment returns 200. Invalid numbers return 400; duplicates return 409. Historical and Cancelled entries may have their bib corrected.

Entry writes use a serializable SQL transaction and a parameterised event read with `UPDLOCK, HOLDLOCK`. The same event lock is used by event updates and category updates/deletion. Requests for one event therefore check closing times, availability and capacity in a consistent order before saving. SQL unique constraints remain a second check for duplicate entries and bib numbers. The existing schema and migration are unchanged (Microsoft, 2026e; 2026f; 2026g).

## 5. Checks

At the enrolment revision, all 645 tests passed locally and on GitHub with none skipped, including 113 enrolment checks. The build has no warnings or errors, and there are no pending model changes.

The checks cover all eight operations, both roles, ownership, private response fields, invalid JSON and IDs, list filters and paging, SQL persistence, age boundaries and leap-day birthdays, closing-time boundaries, capacity, duplicate entries, cancellation, reactivation, DELETE and bib numbers. Six competing-request checks cover the last place, duplicate entry creation, reactivation, capacity reduction, event closure and duplicate bib assignment. Tests use separate temporary SQL Server databases (Microsoft, 2026h).

Swagger and HTTP checks on 18 October 2026 use a separate temporary Open event and a category with one place. Swagger entry creation returns 201 with its Location. HTTP detail and personal-list reads return 200, Organiser bib assignment saves A1042, and cancellation keeps the row and bib number. Swagger reactivation returns the same ID in Confirmed status, and the owning Organiser's Swagger roster shows it. HTTP DELETE returns 204, followed by 404 from GET. The temporary category and event are removed afterwards. The original four users, three events, six categories, six routes, four enrolments and two results remain.

| Evidence | File |
|---|---|
| Created entry and Location | [Swagger enrolment creation](evidence/swagger-enrolment-create.jpg) |
| Reactivated entry | [Swagger enrolment reactivation](evidence/swagger-enrolment-reactivate.jpg) |
| Owned event roster | [Swagger event enrolments](evidence/swagger-event-enrolments.jpg) |

The [enrolment build for f1c2c60](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39142751640) passed all 645 tests, with no warnings, errors or pending migrations. Its [planning workflow](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39142751638) also passed. The subsequent [result notes](Part-2-Results.md) cover result capture and corrections. The [image notes](Part-2-Images.md) cover the four image operations.

## References

Microsoft (2026a) 'Role-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026b) 'Pagination', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/querying/pagination (Accessed: 18 October 2026).

Microsoft (2026c) 'Model validation in ASP.NET Core MVC', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026d) 'How to use DateOnly and TimeOnly', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/standard/datetime/how-to-use-dateonly-timeonly (Accessed: 18 October 2026).

Microsoft (2026e) 'Using Transactions', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/saving/transactions (Accessed: 18 October 2026).

Microsoft (2026f) 'SQL Queries', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/querying/sql-queries (Accessed: 18 October 2026).

Microsoft (2026g) 'Table Hints (Transact-SQL)', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/sql/t-sql/queries/hints-transact-sql-table (Accessed: 18 October 2026).

Microsoft (2026h) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 October 2026).
