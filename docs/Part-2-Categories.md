# RaceDay Part 2 - Categories

## 1. Endpoints

Writes and the Organiser list require the event owner. Missing sessions return 401; wrong roles/owners return 403; mismatched event/category IDs return 404 (Microsoft, 2026a).

| Method | Route | Success |
|---|---|---|
| GET | `/api/events/{eventId}/categories` | 200 with public categories |
| GET | `/api/events/{eventId}/categories/{categoryId}` | 200 with visible category |
| GET | `/api/organiser/events/{eventId}/categories` | 200 with all owned categories |
| POST | `/api/events/{eventId}/categories` | 201 with category and Location |
| PUT | `/api/events/{eventId}/categories/{categoryId}` | 200 with saved category |
| DELETE | `/api/events/{eventId}/categories/{categoryId}` | 204 |

Public lists show available categories in Open, Closed and Completed events. The Organiser list includes hidden categories. Owners can read a new category's Location while its event is Draft.

## 2. Validation

| Field | Rule |
|---|---|
| Name | Required, trimmed, maximum 80 characters; unique within event |
| Distance | 0.01-9999.99 km, two decimal places |
| Age limits | Optional, inclusive 1-120; minimum cannot exceed maximum |
| Fee | Required, R0-R99999999.99, two decimal places |
| Capacity | Positive integer or null for unlimited |
| Availability | Required true or false |

Duplicate names return 409, including concurrent requests (Microsoft, 2026b). The API assigns IDs and timestamps. PUT clears omitted optional fields. Invalid/unknown fields return 400; non-JSON writes return 415 (Microsoft, 2026c).

## 3. History and deletion

Saved entries protect distance, age limits and fees. Capacity cannot fall below the Confirmed entry count. An Open event must retain one available category; close it before disabling the last one.

A category with entries cannot be deleted. An unused category can be deleted with its route; the event remains. PUT and DELETE check these rules within a serializable transaction. Conflicts return 409 (Microsoft, 2026d; 2026e).

## 4. Checks and evidence

The 93 checks cover CRUD, public/owner reads, duplicate names, capacity, history and event readiness (Microsoft, 2026f). See the [rubric review](Part-2-Rubric-Review.md).

Swagger checks on 18 October created a category, opened its Draft event and read the public list. Temporary data was removed.

| Evidence | File |
|---|---|
| Category and Location | [Category creation](evidence/swagger-category-create.jpg) |
| Public list | [Category list](evidence/swagger-category-public-list.jpg) |

## References

Microsoft (2026a) 'Role-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026b) 'Indexes', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/modeling/indexes (Accessed: 18 October 2026).

Microsoft (2026c) 'Model validation in ASP.NET Core MVC', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026d) 'Using Transactions', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/saving/transactions (Accessed: 18 October 2026).

Microsoft (2026e) 'Basic SaveChanges', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/saving/basic (Accessed: 18 October 2026).

Microsoft (2026f) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 October 2026).
