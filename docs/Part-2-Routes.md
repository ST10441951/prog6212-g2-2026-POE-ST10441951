# RaceDay Part 2 - Routes

## 1. Endpoints and access

Each category can have one route. All four operations use `/api/events/{eventId}/categories/{categoryId}/route`.

| Method | Success |
|---|---|
| GET | 200 with the route details |
| POST | 201 with the new route and its Location |
| PUT | 200 with the updated route |
| DELETE | 204 with no response body |

Writes require an Organiser session and ownership of the event. The category must belong to the event in the path. Missing or expired sessions return 401. Participants, inactive accounts and other event owners are rejected with 403. Missing or mismatched event/category records return 404 (Microsoft, 2026a).

Anyone can read an available category's route in an Open, Closed or Completed event. The owning Organiser can also read routes in Draft or Cancelled events and unavailable categories. This makes the creation Location readable immediately. Invalid, expired, inactive and changed-role sessions cannot reveal hidden routes. Hidden and missing routes return the same 404.

## 2. Fields and validation

| Field | Rule |
|---|---|
| Route name | Required, up to 120 characters |
| Start and finish | Required, up to 200 characters each |
| Route description | Required, up to 1000 characters |
| Map URL | Optional absolute HTTP/HTTPS link, up to 500 characters, without credentials |
| Elevation gain | Optional integer from zero to 2147483647 metres |

Text is trimmed and required fields cannot be blank. A blank map link becomes null. The API stores the map URL without visiting or downloading it. URL checks use the .NET URI methods (Microsoft, 2026b; 2026c).

The API assigns the route ID, category link and creation timestamp. Unexpected fields return 400. Invalid fields, oversized text, unsupported URL schemes and invalid elevation values also return 400; non-JSON writes return 415. PUT replaces all editable fields, clearing optional values when omitted or null.

## 3. Saved records

The existing SQL unique constraint allows only one route per category. A second creation returns 409, including when two requests arrive together. PUT updates an existing route; it does not create a missing one (Microsoft, 2026d).

Route corrections and deletion keep the category, event, enrolments, bib numbers and results. Deletion removes only the route. The owner can create another route afterwards. GET, PUT and DELETE return 404 when no route exists. A deletion between loading and saving is also handled as 404 (Microsoft, 2026e).

Direct route reads and routes embedded in event/category responses use the same selected response fields. No account details or Participant records are returned. Responses use no-store. The SQL schema and migration are unchanged.

## 4. Checks

At the route revision, all 532 tests passed locally and on GitHub with none skipped, including 85 route checks. The build had no warnings or errors, and there were no pending model changes.

The route checks cover CRUD, readable Draft Locations, SQL persistence, required fields, length boundaries, valid and invalid URLs, elevation limits, optional values, duplicate and simultaneous creation, both roles, ownership, event/category mismatches, missing routes, account changes, expiry and all five event statuses. They also check that updates appear in event and category responses and that deletion keeps related records. Tests use separate temporary SQL Server databases (Microsoft, 2026f).

Swagger checks on 18 October 2026 use the demonstration Organiser login and a separate temporary Draft event and unavailable category. The unchanged route example returns 201. The owner's Location returns 200, while an anonymous Draft read returns 404. After enabling the category and opening the event, the route becomes public. PUT changes the name and elevation, and GET returns the saved changes. A separate HTTP DELETE returns 204, followed by 404 from GET. The temporary category and event are then removed. The original four users, three events, six categories, six routes, four enrolments and two results remain.

| Evidence | File |
|---|---|
| Created route and Location | [Swagger route creation](evidence/swagger-route-create.jpg) |
| Updated public route | [Swagger route read](evidence/swagger-route-read.jpg) |

All four operations have Swagger descriptions, inputs, success and error responses. The creation and update example is also checked by an HTTP test (Swashbuckle, 2026).

The [route build](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39127167081) passed all 532 tests. The [planning workflow](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39127167123) also passed. The subsequent [enrolment notes](Part-2-Enrolments.md) record the new entry checks.

## References

Microsoft (2026a) 'Role-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026b) 'Model validation in ASP.NET Core MVC', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026c) 'Uri.IsWellFormedUriString Method', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/api/system.uri.iswellformeduristring?view=net-10.0 (Accessed: 18 October 2026).

Microsoft (2026d) 'Basic SaveChanges', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/saving/basic (Accessed: 18 October 2026).

Microsoft (2026e) 'Handling Concurrency Conflicts', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/saving/concurrency (Accessed: 18 October 2026).

Microsoft (2026f) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Swashbuckle (2026) *Swashbuckle.AspNetCore*. Available at: https://github.com/domaindrivendev/Swashbuckle.AspNetCore (Accessed: 18 October 2026).
