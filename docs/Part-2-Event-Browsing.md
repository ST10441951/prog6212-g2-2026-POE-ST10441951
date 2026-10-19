# RaceDay Part 2 Event Browsing

## 1. Endpoints

| Method | Route | Response |
|---|---|---|
| GET | `/api/events` | 200 with upcoming events and paging totals |
| GET | `/api/events/{eventId}` | 200 with event, categories and routes |

[Event management](Part-2-Event-Management.md) covers Organiser writes and the dashboard.

## 2. Filters and paging

The upcoming list includes future Open and Closed events. It excludes Draft, Cancelled, Completed and past events, including for their owner.

| Query value | Rule |
|---|---|
| `page` | Starts at 1; default 1 |
| `pageSize` | 1-100; default 10 |
| `eventType` | Running, Walking or Cycling |
| `province` | Full South African province name |
| `fromDate`, `toDate` | Inclusive UTC dates; YYYY-MM-DD |

Text filters ignore case and surrounding spaces. Blank or invalid filters, reversed dates and unsupported paging offsets return 400. Filtering precedes counts and paging. Order is event date, then ID. Empty pages return 200 (Microsoft, 2026a; 2026b).

## 3. Visibility

Open, Closed and Completed details are public. Draft and Cancelled details require the owning Organiser; hidden and missing events return 404. Public details show available categories. Owners also see unavailable ones. A category without a route returns null (Microsoft, 2026c; 2026d).

An invalid session cannot grant owner access. Responses exclude contact details, enrolments and stored blob names, and use no-store. Saved banners have fresh one-hour URLs; see [image storage](Part-2-Images.md).

## 4. Checks and evidence

The 55 checks cover filters, date boundaries, paging, statuses, owner access and private fields (Microsoft, 2026e). Current results are in the [rubric review](Part-2-Rubric-Review.md).

Swagger checks on 18 October filtered Cycling, read event 2 and confirmed Completed event 1 was absent from the upcoming list but readable directly. The original sample records were unchanged. Upcoming results depend on today's date.

| Evidence | File |
|---|---|
| Filtered list | [Event browsing](evidence/swagger-event-browsing.jpg) |
| Details | [Event details](evidence/swagger-event-details.jpg) |

## References

Microsoft (2026a) 'Pagination', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/querying/pagination (Accessed: 18 October 2026).

Microsoft (2026b) 'Model validation in ASP.NET Core MVC', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026c) 'Eager Loading of Related Data', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/querying/related-data/eager (Accessed: 18 October 2026).

Microsoft (2026d) 'Simple authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/simple?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026e) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 October 2026).
