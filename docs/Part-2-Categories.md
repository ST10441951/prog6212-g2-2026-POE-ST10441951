# RaceDay Part 2 - Categories

## 1. Endpoints and access

The six category operations follow the corrected endpoint plan. Writes and the full Organiser list require an Organiser session and event ownership (Microsoft, 2026a).

| Method | Route | Success |
|---|---|---|
| GET | `/api/events/{eventId}/categories` | 200 with available categories for a public event |
| GET | `/api/events/{eventId}/categories/{categoryId}` | 200 with one visible category |
| GET | `/api/organiser/events/{eventId}/categories` | 200 with all categories for the owned event |
| POST | `/api/events/{eventId}/categories` | 201 with the new category and its Location |
| PUT | `/api/events/{eventId}/categories/{categoryId}` | 200 with the saved category |
| DELETE | `/api/events/{eventId}/categories/{categoryId}` | 204 with no response body |

Missing or expired sessions return 401 on protected operations. Wrong roles and other owners return 403. The category ID must belong to the event ID in the route; mismatches return 404.

Public lists include only available categories in Open, Closed and Completed events. Draft and Cancelled events return 404, including when their owner uses the public list. The separate Organiser list includes every category for an owned event.

Single-category reads also accept a valid owning Organiser session for hidden records. The owner can follow a new category's Location while the event is still Draft. Expired, inactive and changed-role sessions cannot reveal hidden categories. Missing and hidden records use the same 404 response.

## 2. Fields and validation

Names are trimmed, required and limited to 80 characters. The same name cannot be used twice within an event. Another event may use it. The SQL unique index also rejects a duplicate if two creation requests arrive together (Microsoft, 2026b).

| Field | Rule |
|---|---|
| Distance | 0.01 to 9999.99 km, at most two decimal places |
| Minimum and maximum age | Optional, inclusive ages from 1 to 120; minimum cannot exceed maximum |
| Entry fee | Required, zero to 99999999.99 rand, at most two decimal places |
| Capacity | Optional positive integer; null means unlimited |
| Availability | Required true or false; explicit false is saved |

The API assigns IDs, event links and timestamps. Unexpected JSON fields are rejected. Invalid fields return 400, and non-JSON writes return 415. PUT replaces the editable fields, clearing omitted optional ages and capacity (Microsoft, 2026c).

Responses contain the event ID and optional route details. Event details use the same category response. This adds `eventId` to embedded categories and matches the planned category example. No Participant records, enrolments, passwords or account contact details are returned. Responses use no-store.

## 3. Saved entries and deletion

Any saved enrolment, including a Cancelled entry, prevents changes to category distance, age limits and entry fee. This keeps the details agreed when entering. Names can still be corrected, and availability can be changed when the event's readiness rule allows it.

Capacity cannot be below the number of Confirmed entries. Cancelled entries do not use a place. An Open event must keep at least one available category. Close the event before disabling or deleting its last available category.

Any saved enrolment prevents category deletion. Make the category unavailable instead. An unused category can be deleted with its optional route in one transaction; the parent event remains. The database retains its existing NO ACTION foreign keys. PUT and DELETE use serializable transactions to keep the checks and writes together. Conflicts return 409 (Microsoft, 2026d; 2026e).

These history and readiness rules are recorded in the endpoint-plan implementation notes. They do not change the schema. The [route notes](Part-2-Routes.md) cover the subsequent route CRUD work.

## 4. Checks

At the category revision, all 447 tests passed locally and on GitHub with none skipped. This includes 93 category checks. The solution built with no warnings or errors, and there were no pending model changes.

The checks cover creation, readable Locations, validation, optional fields, duplicate names, simultaneous duplicate creation, updates, route preservation and removal, saved entry history, capacity, event readiness, both roles, ownership, expiry, changed accounts, public visibility, event/category mismatches and Swagger descriptions and examples. Tests run through the HTTP pipeline using temporary SQL Server databases (Microsoft, 2026f).

The event workflow test creates a Draft, confirms it cannot open without a category, adds an available category, opens the event and reads its public categories. It also confirms that the last available category cannot be hidden until the event closes, and that reopening fails while no category is available.

Swagger checks on 18 October 2026 use the demonstration Organiser account and a separate temporary event. The unchanged category example returns 201 with its Location. The event can then be opened and its public category list returns 200. The temporary records are removed after checking; the original sample records remain.

| Evidence | File |
|---|---|
| Created category and Location | [Swagger category creation](evidence/swagger-category-create.jpg) |
| Public category list | [Swagger category list](evidence/swagger-category-public-list.jpg) |

The [category build](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39126036588) passed all 447 tests. The [planning workflow](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39126036609) also passed. The subsequent route work adds 85 checks and brings the local total to 532.

## References

Microsoft (2026a) 'Role-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026b) 'Indexes', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/modeling/indexes (Accessed: 18 October 2026).

Microsoft (2026c) 'Model validation in ASP.NET Core MVC', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026d) 'Using Transactions', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/saving/transactions (Accessed: 18 October 2026).

Microsoft (2026e) 'Basic SaveChanges', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/saving/basic (Accessed: 18 October 2026).

Microsoft (2026f) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 October 2026).
