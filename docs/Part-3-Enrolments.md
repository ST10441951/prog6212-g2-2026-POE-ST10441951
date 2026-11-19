# RaceDay Part 3 - Event enrolments and bib numbers

## 1. Pages

Open an owned event under My events, then choose View enrolments. Only that event's Organiser can open its roster, entry details and bib form. The API repeats ownership checks on every request (Microsoft, 2026a).

| MVC page | API request |
|---|---|
| `/organiser/events/{eventId}/enrolments` | GET `/api/events/{eventId}/enrolments` |
| `/organiser/events/{eventId}/enrolments/{enrolmentId}` | GET `/api/enrolments/{enrolmentId}` |
| Bib form and save at `/{enrolmentId}/bib` | Read entry, then PUT `/api/enrolments/{enrolmentId}/bib-number` |

The roster shows ten entries per page, with Participant name, category, status, bib and original entry date. Status and category filters work together and remain selected when changing pages. Unavailable categories stay listed so their historical entries can be found. Counts come from the API.

Details show the saved entry, event date, category distance, last update and recorded result status. Contact details are not requested or displayed. Record result and View official result link to [result management](Part-3-Result-Management.md).

## 2. Bib numbers

The form sends only `bibNumber`. Entry, event and Participant IDs come from the selected saved record. Antiforgery tokens protect the form (Microsoft, 2026b; 2026c).

The API trims the value, converts it to uppercase and checks its format and uniqueness within the event. Cancelled entries still reserve their bib. Another event can reuse the same number. Reassigning an entry's current bib is allowed. MVC does not duplicate these business rules.

Rejected forms keep the entered value and display the API's reason. Successful changes return to entry details. Bib corrections preserve the Participant, category, status, original entry date and official result. Cancelled and historical entries can also receive corrections.

Expired sessions return to sign-in. Other users are blocked, and a record from another event returns 404. Service errors show a friendly message. If a save cannot be confirmed, the page asks the user to check the saved entry before retrying.

## 3. Checks and evidence

On 19 November 2026, all 502 MVC checks passed locally, with none failed or skipped. Checks cover filters, paging, roles, ownership, allowed fields, antiforgery, encoded messages, ended sessions, invalid responses and uncertain saves. A real HTTP test checks that two Organisers receive separate private rosters (Microsoft, 2026d).

Browser checks used an isolated API/database with 15 entries. Roster counts matched SQL. Filters retained their values across pages. Duplicate bibs, including those on Cancelled entries, were rejected. Saved uppercase values appeared in the Participant's entry. Wrong roles and event links were blocked. SQL checks confirmed that bib corrections preserved entry history and official results.

Pages passed at 320, 390, 768 and 1280 pixels without horizontal overflow or JavaScript errors. Screenshots are genuine browser captures without cursor overlays. The temporary database was removed afterwards; original sample data was unchanged.

| Page | Evidence |
|---|---|
| Event enrolments | [Desktop](evidence/part3-event-enrolments.png) / [mobile](evidence/part3-event-enrolments-mobile.png) |
| Paging and filters | [Second page](evidence/part3-roster-pagination.png) / [combined filters](evidence/part3-roster-filtered-page.png) |
| Cancelled and empty lists | [Cancelled](evidence/part3-roster-cancelled.png) / [empty](evidence/part3-roster-empty.png) |
| Entry details | [Desktop](evidence/part3-roster-entry-details.png) / [mobile](evidence/part3-roster-entry-details-mobile.png) |
| Bib form | [Desktop](evidence/part3-bib-form.png) / [mobile](evidence/part3-bib-form-mobile.png) |
| Bib checks | [Saved](evidence/part3-bib-saved.png) / [duplicate rejected](evidence/part3-bib-duplicate.png) |
| Historical correction | [Result preserved](evidence/part3-historical-bib-correction.png) |

## References

Microsoft (2026a) 'Role-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026b) 'Model Binding in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026c) 'Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026d) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 November 2026).
