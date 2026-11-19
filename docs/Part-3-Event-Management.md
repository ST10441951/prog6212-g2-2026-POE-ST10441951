# RaceDay Part 3 - Event management

## 1. Pages

| Method | MVC route | API request |
|---|---|---|
| GET / POST | `/organiser/events/create` | Display form / POST `/api/events` |
| GET | `/organiser/events/{eventId}` | Private ownership check, then GET event details |
| GET / POST | `/organiser/events/{eventId}/edit` | Read owned event / PUT `/api/events/{eventId}` |
| GET / POST | `/organiser/events/{eventId}/delete` | Read confirmation / DELETE `/api/events/{eventId}` |

Only Organisers can use these pages. Event links are available in the dashboard and My events. Before displaying private details or accepting changes, MVC checks the event ID against the current Organiser's private dashboard. The API also checks ownership on each write (Microsoft, 2026a; 2026b).

## 2. Create and edit

The form includes name, sport, description, distance, start, entry deadline, venue, address, city, province and optional coordinates. All entered dates use SAST. MVC attaches UTC+02:00 explicitly; the API stores UTC. Edit forms convert back to SAST and retain seconds, regardless of the browser or server time zone (Microsoft, 2026c).

Creation sends only the twelve editable fields. The API assigns the ID, owner, Draft status and timestamps. Editing adds the requested status; posted IDs, owners, storage names and timestamps are not accepted. Blank coordinates clear them. Forms require antiforgery tokens, and successful writes redirect to the saved event (Microsoft, 2026b; 2026d).

The API validates future creation dates, deadlines, decimal precision, paired coordinates and status readiness. Opening requires an available category and future start/deadline. Saved entries and terminal status restrict schedule, type and distance changes. Events with results cannot be cancelled. Rejected edits retain the input and show the API's field errors or conflict reason. These rules stay in the API.

## 3. Delete and history

Delete event opens a read-only confirmation. Keep event returns without changing data; Delete permanently submits the protected form. The API refuses deletion when a banner, categories or entries exist. Editing the status to Cancelled keeps related history when cancellation is allowed.

Other Organisers and Participants cannot use these forms or writes. Missing upstream records return 404; ownership rejection returns 403. Service problems return a friendly 503. If a write cannot be confirmed, the page advises checking My events before retrying. No automatic retry can create a duplicate event.

[Category and route management](Part-3-Categories.md) is available from private event details. [Enrolments and bib numbers](Part-3-Enrolments.md) are also available from these details. Banner controls and result management pages remain outstanding.

## 4. Checks and evidence

On 18 November 2026, all 427 MVC checks passed locally, with none failed or skipped. Event checks cover roles, ownership, allowed fields, date offsets, validation, antiforgery, redirects, read-only confirmation, conflicts, invalid responses and uncertain writes (Microsoft, 2026e).

Browser checks used a temporary database. A Draft was created and edited through MVC, with owner, status, fields and exact UTC times verified in SQL. Refresh did not duplicate it. Opening without a category failed; adding an available category through the API allowed opening. Saved entries prevented a distance change. Other accounts' edits were blocked, dependent deletion preserved history, cancellation kept the entry, and terminal reopening failed. A separate event without dependencies was deleted after confirmation and returned API 404.

Pages passed at 320, 390, 768 and 1280 pixels without horizontal overflow or JavaScript errors. A browser using a different time zone still saved the entered SAST times correctly. Screenshots are genuine captures without cursor overlays. The temporary database was removed afterwards; original sample data was unchanged.

| Page | Evidence |
|---|---|
| Create event | [Desktop](evidence/part3-event-create.png) / [mobile](evidence/part3-event-create-mobile.png) |
| Saved Draft | [Desktop](evidence/part3-event-created.png) / [mobile](evidence/part3-owned-event-details-mobile.png) |
| Edit event | [Desktop](evidence/part3-event-edit.png) / [mobile](evidence/part3-event-edit-mobile.png) |
| Invalid dates | [Field validation](evidence/part3-event-validation.png) |
| Opening restriction | [Category readiness](evidence/part3-event-status-rejected.png) |
| Saved-history restriction | [Rejected schedule change](evidence/part3-event-history-rejected.png) |
| Delete confirmation | [Desktop](evidence/part3-event-delete-confirmation.png) / [mobile](evidence/part3-event-delete-confirmation-mobile.png) |
| Dependent event | [Rejected deletion](evidence/part3-event-delete-rejected.png) |
| Cancelled event | [Preserved event](evidence/part3-event-cancelled.png) |
| Permitted deletion | [Saved confirmation](evidence/part3-event-deleted.png) |

## References

Microsoft (2026a) 'Role-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026b) 'Model Binding in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026c) 'DateTimeOffset constructors', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset.-ctor?view=net-10.0 (Accessed: 18 November 2026).

Microsoft (2026d) 'Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026e) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 November 2026).
