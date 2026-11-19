# RaceDay Part 3 - Organiser dashboard

## 1. Pages

| Method | MVC route | API request |
|---|---|---|
| GET | `/organiser` | GET `/api/organiser/dashboard` |
| GET | `/organiser/events` | GET `/api/events/mine` |

Both pages require an Organiser session. Guests are redirected to sign-in and Participants receive 403. Each request forwards only that Organiser's server-side API cookie. The API selects owned records; no submitted user ID can change the owner (Microsoft, 2026a; 2026b).

## 2. Dashboard and owned events

Organiser home shows total events, upcoming events and saved entries, split into Confirmed and Cancelled. Every owned event is listed with its SAST date, status and entry counts. Upcoming totals count future Open and Closed events. All totals come from the API, including historical entries; MVC does not calculate replacement business totals.

My events includes Draft, Open, Closed, Cancelled and Completed records. The API applies the status filter and returns ten events per page, ordered by date then ID. Page links retain the filter. Empty accounts, unmatched filters and later pages have separate messages. Public links appear only for statuses supported by the public event page.

Pages use no-store responses and encode event names. Invalid filters return 400, unavailable or inconsistent responses show a friendly 503, and rejected API sessions clear local access. An unavailable service is not presented as zero events (Microsoft, 2026a; 2026b).

Create event and the private event links open the [event-management pages](Part-3-Event-Management.md). Each owned event also opens [category and route management](Part-3-Categories.md), [enrolments and bib forms](Part-3-Enrolments.md), and [result capture/corrections](Part-3-Result-Management.md). Image uploads remain outstanding.

## 3. Checks and evidence

On 19 November 2026, all 468 MVC checks passed locally, with none failed or skipped. New checks cover totals, dates, status filters, paging, empty lists, invalid responses, encoding, role restrictions and ended sessions. A real HTTP test confirms two Organisers do not share dashboard or event cookies (Microsoft, 2026c).

Browser checks used an isolated database with fictional event fixtures. Totals and counts per event matched the API and SQL. Both Organisers saw only their own records. Draft and historical events remained visible to their owner, filters and paging worked, and a new Organiser had an empty dashboard. Read-only requests left saved records unchanged. A real API cancellation refreshed the Confirmed/Cancelled totals.

The pages passed at 320, 390, 768 and 1280 pixels without horizontal overflow or JavaScript errors. Screenshots are genuine browser captures without cursor overlays. Review fixtures were removed afterwards; the original sample database was unchanged.

| Page | Evidence |
|---|---|
| Dashboard | [Desktop](evidence/part3-organiser-home.png) / [mobile](evidence/part3-organiser-home-mobile.png) |
| My events | [Desktop](evidence/part3-owned-events.png) / [mobile](evidence/part3-owned-events-mobile.png) |
| Private Draft records | [Draft filter](evidence/part3-owned-drafts.png) |
| Filtered page 2 | [Pagination](evidence/part3-owned-event-pagination.png) |
| New Organiser | [Empty dashboard](evidence/part3-organiser-empty.png) |

## References

Microsoft (2026a) 'Role-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026b) 'Make HTTP requests using IHttpClientFactory in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026c) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 November 2026).
