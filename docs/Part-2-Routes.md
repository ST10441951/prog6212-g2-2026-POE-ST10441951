# RaceDay Part 2 - Routes

## 1. Endpoints and access

Each category has at most one route. All four methods use `/api/events/{eventId}/categories/{categoryId}/route`.

| Method | Success |
|---|---|
| GET | 200 with route details |
| POST | 201 with route and Location |
| PUT | 200 with saved route |
| DELETE | 204 |

Writes require the owning Organiser. Missing sessions return 401, wrong roles/owners 403 and missing/mismatched IDs 404 (Microsoft, 2026a).

Available categories in Open, Closed and Completed events have public routes. Owners can also read hidden routes and follow a new route's Location in Draft. Hidden and missing routes return 404.

## 2. Validation

| Field | Rule |
|---|---|
| Name | Required, maximum 120 characters |
| Start and finish | Required, maximum 200 characters each |
| Description | Required, maximum 1000 characters |
| Map URL | Optional HTTP/HTTPS link, maximum 500 characters, no credentials |
| Elevation | Optional integer, 0-2147483647 metres |

Text is trimmed. Blank map links become null; the API does not visit them (Microsoft, 2026b; 2026c). PUT clears omitted optional fields. IDs and timestamps come from the API. Invalid/unknown fields return 400; non-JSON writes return 415.

## 3. Saved records

A second route for the category returns 409, including simultaneous creation. PUT does not create a missing route (Microsoft, 2026d).

Updates and deletion preserve the category, event, enrolments and results. Deleting removes only the route; another can be created later. Missing/deleted routes return 404, including deletion during a write (Microsoft, 2026e).

## 4. Checks and evidence

The 85 checks cover CRUD, visibility, roles, ownership, validation, duplicate writes, persistence and related records (Microsoft, 2026f). Swagger describes all four operations and provides tested examples (Swashbuckle, 2026). See the [rubric review](Part-2-Rubric-Review.md).

Swagger and HTTP checks on 18 October created, read, updated and deleted a temporary route. Original sample data was kept.

| Evidence | File |
|---|---|
| Route and Location | [Route creation](evidence/swagger-route-create.jpg) |
| Saved changes | [Route read](evidence/swagger-route-read.jpg) |

## References

Microsoft (2026a) 'Role-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026b) 'Model validation in ASP.NET Core MVC', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026c) 'Uri.IsWellFormedUriString Method', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/api/system.uri.iswellformeduristring?view=net-10.0 (Accessed: 18 October 2026).

Microsoft (2026d) 'Basic SaveChanges', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/saving/basic (Accessed: 18 October 2026).

Microsoft (2026e) 'Handling Concurrency Conflicts', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/saving/concurrency (Accessed: 18 October 2026).

Microsoft (2026f) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Swashbuckle (2026) *Swashbuckle.AspNetCore*. Available at: https://github.com/domaindrivendev/Swashbuckle.AspNetCore (Accessed: 18 October 2026).
