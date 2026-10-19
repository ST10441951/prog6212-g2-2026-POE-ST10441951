# RaceDay Part 2 Event Management

## 1. Endpoints

All five require an Organiser session. Writes check event ownership. Missing sessions return 401; wrong roles, inactive accounts and other owners return 403 (Microsoft, 2026a).

| Method | Route | Success |
|---|---|---|
| GET | `/api/events/mine` | 200 with owned events |
| POST | `/api/events` | 201 with Draft details and Location |
| PUT | `/api/events/{eventId}` | 200 with saved details |
| DELETE | `/api/events/{eventId}` | 204 |
| GET | `/api/organiser/dashboard` | 200 with event and entry totals |

## 2. Validation

Requests contain name, type, description, distance, dates and location. Coordinates are optional but supplied together. SQL text limits apply. Type is Running, Walking or Cycling; province uses a full South African name.

Distances use two decimal places, coordinates six and timestamps whole seconds. Offsets become UTC. Creation requires future closing/start times, with closing before start. Unknown/invalid fields return 400; non-JSON writes return 415 (Microsoft, 2026b).

The API assigns owner, ID, Draft status and timestamps. The owner can read the new Draft through its Location (Microsoft, 2026c).

## 3. Status and history

| Current | Allowed statuses |
|---|---|
| Draft | Draft, Open, Cancelled |
| Open | Open, Closed, Cancelled, Completed |
| Closed | Closed, Open, Cancelled, Completed |
| Cancelled | Cancelled |
| Completed | Completed |

- Opening needs an available category and future start/closing times.
- Completion requires the start time to have passed.
- Changed schedules must be in the future.
- Saved entries or a terminal status protect dates, type and distance. Descriptive corrections remain allowed.
- Recorded results prevent cancellation.
- Deletion requires no categories or enrolments, and the banner must be removed first. Cancellation preserves history.

Conflicts return 409. Checks and writes share a serializable transaction (Microsoft, 2026d).

## 4. Owned list and dashboard

The owned list accepts status and paging: page 1, size 10 by default, maximum 100. It includes all statuses, ordered by date then ID (Microsoft, 2026e).

The dashboard counts owned events and saved Confirmed/Cancelled entries separately. Upcoming means future Open/Closed events. No owned events gives zero totals (Microsoft, 2026f).

## 5. Checks and evidence

The 121 checks cover CRUD, all status pairs, history, ownership, validation, paging and dashboard totals (Microsoft, 2026g). See the [rubric review](Part-2-Rubric-Review.md).

Swagger and HTTP checks on 18 October created, read, cancelled and deleted a temporary event. Original sample records were kept.

| Evidence | File |
|---|---|
| Draft and Location | [Event creation](evidence/swagger-event-create.jpg) |
| Owned totals | [Organiser dashboard](evidence/swagger-organiser-dashboard.jpg) |

## References

Microsoft (2026a) 'Role-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026b) 'Model validation in ASP.NET Core MVC', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026c) 'Basic SaveChanges', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/saving/basic (Accessed: 18 October 2026).

Microsoft (2026d) 'Using Transactions', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/saving/transactions (Accessed: 18 October 2026).

Microsoft (2026e) 'Pagination', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/querying/pagination (Accessed: 18 October 2026).

Microsoft (2026f) 'Complex Query Operators', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/querying/complex-query-operators (Accessed: 18 October 2026).

Microsoft (2026g) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 October 2026).
