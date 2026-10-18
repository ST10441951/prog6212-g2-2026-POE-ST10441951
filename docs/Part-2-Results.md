# RaceDay Part 2 - Results

## 1. Endpoints and access

| Method | Route | Success |
|---|---|---|
| GET | `/api/results/mine` | 200 with the Participant's result history |
| GET | `/api/enrolments/{enrolmentId}/result` | 200 for the entry's Participant or event Organiser |
| GET | `/api/events/{eventId}/results` | 200 with the owning Organiser's event results |
| POST | `/api/enrolments/{enrolmentId}/result` | 201 with the first result and its Location |
| PUT | `/api/enrolments/{enrolmentId}/result` | 200 with the corrected result |

The API uses the session identity and checks event ownership. Missing or expired sessions return 401. Wrong roles and unrelated users receive 403. Missing entries or results return 404. Responses exclude passwords, account contact details, dates of birth and storage object names (Microsoft, 2026a).

## 2. Capture and corrections

Capture requires a Confirmed entry and an event that has started and is not Cancelled. POST returns 409 if a result already exists. PUT returns 404 if no result exists. Cancelled entries, Cancelled events and events that have not started return 409. These event checks are explained in the [endpoint plan](API-Endpoint-Plan.md#113-part-2-implementation-notes).

Completed requires a positive finish time in seconds. Overall and category positions are optional positive integers; ties are allowed. DidNotFinish, Disqualified and DidNotStart require null time and positions. Status ignores case and surrounding spaces. Notes are trimmed, blank becomes null and the limit is 500 characters. Invalid values and unexpected JSON fields return 400; non-JSON writes return 415 (Microsoft, 2026b).

The API assigns the entry link, recorder and UTC recording time. PUT replaces the editable values, clears omitted optional positions and notes, and sets the UTC update time. It keeps the original result ID, entry link, recorder and recording time. Official results have no DELETE endpoint.

Writes use a serializable SQL transaction and the shared event update lock. Competing capture requests cannot save two results for one entry, and result capture and event cancellation cannot both succeed. The existing SQL unique constraint remains in place. No migration or table change is needed (Microsoft, 2026c; 2026d).

## 3. Lists and finisher totals

Both lists default to page 1 and 10 items, with a maximum page size of 100. Personal filters are result status and event ID. Event filters are result status and category ID; the category must belong to the event. Invalid filters return 400. Empty matches and later pages return 200.

Personal history shows newest event date then highest result ID. Event lists show Completed results first, then overall position, finish time and result ID. Missing positions and times follow supplied values. Paging counts apply after access and filters (Microsoft, 2026e).

Each result includes its event, category, Participant, bib number and timestamps. `totalFinishers` counts all Completed official results in the whole event. It does not shrink when a list is filtered or paged and is not the highest saved position. Changing a result between Completed and another status changes this total for the other results too.

## 4. Checks

At the result revision, all 734 tests passed locally and on GitHub with none skipped, including 89 result checks. The solution built with no warnings or errors, the planning checks passed and there were no pending model changes.

The 89 result checks cover all five endpoints, both roles, ownership, missing and expired sessions, SQL persistence, invalid fields, all four statuses, correction history, filters, paging, ordering, finisher totals and duplicate capture. Competing requests check duplicate capture and result capture against event cancellation. Tests use their own temporary SQL Server databases (Microsoft, 2026f).

Test collections run sequentially to reduce LocalDB memory and connection pressure. The explicit concurrent HTTP requests inside the race-condition tests still run together (xUnit.net, 2026).

Swagger checks on 18 October 2026 use a temporary past event, category and Confirmed entry. The unchanged POST example returns 201 and its Location. PUT corrects the finish time to 3500 seconds, keeps the original result and recording time, and sets the update time. The Participant's filtered personal history returns the corrected result.

Separate HTTP checks read the single result and owned-event list, change Completed to DidNotFinish and confirm the finisher total becomes zero, then restore Completed and confirm it becomes one. The enrolment summary reflects the correction. Temporary records are removed afterwards; the original sample records remain.

| Evidence | File |
|---|---|
| First result and Location | [Swagger result creation](evidence/swagger-result-create.jpg) |
| Corrected time and original record | [Swagger result correction](evidence/swagger-result-update.jpg) |
| Participant history | [Swagger personal results](evidence/swagger-personal-results.jpg) |

The [result build for 480d552](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39145842878) passed all 734 tests, with none skipped, no warnings or errors and no pending migrations. Its [planning workflow](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39145842900) also passed. The subsequent [image notes](Part-2-Images.md) cover the four image operations.

## References

Microsoft (2026a) 'Role-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026b) 'Model validation in ASP.NET Core MVC', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026c) 'Using Transactions', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/saving/transactions (Accessed: 18 October 2026).

Microsoft (2026d) 'Table Hints (Transact-SQL)', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/sql/t-sql/queries/hints-transact-sql-table (Accessed: 18 October 2026).

Microsoft (2026e) 'Pagination', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/querying/pagination (Accessed: 18 October 2026).

Microsoft (2026f) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 October 2026).

xUnit.net (2026) 'Running tests in parallel'. Available at: https://xunit.net/docs/running-tests-in-parallel (Accessed: 18 October 2026).