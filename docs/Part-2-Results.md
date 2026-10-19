# RaceDay Part 2 - Results

## 1. Endpoints

Writes and event lists require the owning Organiser. Participants read their own history. Single-result reads allow its Participant or event owner. Missing sessions return 401, unrelated users 403 and missing records 404 (Microsoft, 2026a).

| Method | Route | Success |
|---|---|---|
| GET | `/api/results/mine` | 200 with personal history |
| GET | `/api/enrolments/{enrolmentId}/result` | 200 with result |
| GET | `/api/events/{eventId}/results` | 200 with owned-event results |
| POST | `/api/enrolments/{enrolmentId}/result` | 201 with result and Location |
| PUT | `/api/enrolments/{enrolmentId}/result` | 200 with corrected result |

## 2. Capture and corrections

- Entry must be Confirmed; event must have started and cannot be Cancelled.
- One result per entry. Duplicate POST returns 409; PUT without an existing result returns 404.
- Completed requires positive seconds. Optional positions must be positive; ties are allowed.
- DidNotFinish, Disqualified and DidNotStart require null time and positions.
- Status ignores case/spaces. Notes are trimmed, maximum 500 characters; blank becomes null.
- Invalid/unknown fields return 400; non-JSON writes return 415 (Microsoft, 2026b).

PUT replaces editable fields but keeps the original ID, recorder and recording time. It sets the update time. There is no result DELETE.

Serializable transactions and the shared event lock prevent duplicate capture and conflicting cancellation (Microsoft, 2026c; 2026d). See the [endpoint rules](API-Endpoint-Plan.md#113-part-2-implementation-notes).

## 3. Lists and totals

Lists default to page 1 and size 10, maximum 100. Personal filters: status/event ID. Event filters: status/category ID; category must belong to the event. Personal results use newest event first. Event results use Completed first, then position, time and ID. Missing values sort last (Microsoft, 2026e).

`totalFinishers` counts all Completed results in the whole event, regardless of filtering or paging. Corrections update this count. Responses exclude private account fields.

## 4. Checks and evidence

The 89 checks cover all operations, access, field rules, persistence, corrections, paging, totals and competing writes (Microsoft, 2026f). Collections run sequentially; explicit concurrent-request tests still run together (xUnit.net, 2026). Current results are in the [rubric review](Part-2-Rubric-Review.md).

Swagger/HTTP checks on 18 October created and corrected a temporary result, read personal history and verified changing Completed to DidNotFinish updated the finisher count. Temporary records were removed.

| Evidence | File |
|---|---|
| First result | [Result creation](evidence/swagger-result-create.jpg) |
| Corrected time | [Result correction](evidence/swagger-result-update.jpg) |
| Participant history | [Personal results](evidence/swagger-personal-results.jpg) |

## References

Microsoft (2026a) 'Role-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026b) 'Model validation in ASP.NET Core MVC', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026c) 'Using Transactions', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/saving/transactions (Accessed: 18 October 2026).

Microsoft (2026d) 'Table Hints (Transact-SQL)', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/sql/t-sql/queries/hints-transact-sql-table (Accessed: 18 October 2026).

Microsoft (2026e) 'Pagination', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/querying/pagination (Accessed: 18 October 2026).

Microsoft (2026f) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 October 2026).

xUnit.net (2026) 'Running tests in parallel'. Available at: https://xunit.net/docs/running-tests-in-parallel (Accessed: 18 October 2026).