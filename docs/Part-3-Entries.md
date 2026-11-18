# RaceDay Part 3 - Event entries

## 1. Pages

| Method | MVC route | API request |
|---|---|---|
| GET | `/events/{eventId}/enter` | Public event details and available categories |
| POST | `/events/{eventId}/enter` | POST `/api/enrolments` |
| GET | `/entries` | GET `/api/enrolments/mine` |
| GET | `/entries/{enrolmentId}` | GET `/api/enrolments/{enrolmentId}` |
| GET / POST | `/entries/{enrolmentId}/cancel` | Read confirmation / PUT cancellation |
| GET / POST | `/entries/{enrolmentId}/reactivate` | Read categories / PUT reactivation |
| GET / POST | `/entries/{enrolmentId}/delete` | Read confirmation / DELETE entry |

Only Participants can use these pages. My entries is available in their header and account home. Guests are redirected to sign-in; Organisers receive 403 (Microsoft, 2026a).

## 2. Enter an event

The Participant selects an available category. MVC sends only the route's event ID and selected category ID. The API assigns the Participant, status and entry time, and checks age, birth date, capacity, deadlines and duplicate entries. Posted user IDs or statuses are not accepted by the form (Microsoft, 2026b).

A successful save redirects to the private entry page, which reads the saved API record. Refreshing this page does not submit the form again. Rejected entries show the API's reason and keep the selection. If a write times out or cannot be confirmed, the user is told to check My entries before retrying. The form requires an antiforgery token (Microsoft, 2026b; 2026c).

## 3. Private entries

The list supports Confirmed/Cancelled filters and ten entries per page. Details show the selected category, event date, saved status and bib number when assigned. Confirmed is green and Cancelled is red; both include status text.

Each API request receives only the current user's server-side API cookie. Responses must belong to that Participant before being displayed. Expired API sessions clear the local session; unrelated entries return 403. Private pages use no-store responses (Microsoft, 2026a; 2026d).

Confirmed entries have Cancel entry and Delete entry links. Both open a read-only confirmation page. Cancel keeps history; Delete permanently removes a permitted entry. Keep my entry returns without changing anything. Only the protected POST submits a change (Microsoft, 2026c).

Cancelled entries have a Reactivate entry link. The form loads categories for the saved event and sends only the selected category ID. The API checks the deadline, age, capacity, status and recorded results. Reactivation keeps the original entry ID and date. Keeping the category preserves the bib; changing category clears it. Cancelled entries cannot be deleted.

Rejected changes display the API's reason. Successful changes redirect to the saved entry or list. Uncertain writes return 503 and advise checking My entries before retrying. API ownership checks remain in place for every operation (Microsoft, 2026a; 2026d).

## 4. Checks and evidence

On 18 November 2026, all 222 MVC tests passed locally, with none failed or skipped. Checks cover field binding, antiforgery, saved records, entry changes, validation, ownership, role restrictions, stale forms, API failures and isolated cookies. A real HTTP test verifies that two browsers read only their own entries (Microsoft, 2026e).

Browser checks used a separate database and fictional accounts. One submitted entry was verified in SQL. Refresh did not submit another entry. Duplicate, missing birth date, age, capacity, closed-deadline and foreign-category requests were rejected. Another Participant could not read the entry and had an empty personal list. Organisers could not access Participant entry pages.

Further browser checks verified cancellation, same/new-category reactivation and confirmed deletion in SQL. Entry ID/date and bib behaviour matched the API contract. Closed/full entries, Cancelled deletion, missing antiforgery and other users' changes were rejected. Existing result-bearing history remained unchanged. After permitted deletion, the record was absent from SQL and its private URL returned 404.

The form, saved entry, list and change pages were checked at 320, 390, 768 and 1280 pixels without horizontal overflow or JavaScript errors. Screenshots are genuine captures without cursor overlays. The temporary databases were removed afterwards.

| Page | Evidence |
|---|---|
| Select category | [Desktop](evidence/part3-enter-event.png) / [mobile](evidence/part3-enter-event-mobile.png) |
| Saved entry | [Confirmation](evidence/part3-entry-confirmation.png) |
| My entries | [Desktop](evidence/part3-my-entries.png) / [mobile](evidence/part3-my-entries-mobile.png) |
| Rejected duplicate | [Validation](evidence/part3-entry-duplicate.png) |
| Cancelled status | [Filtered list](evidence/part3-entry-cancelled.png) |
| Cancel confirmation | [Desktop](evidence/part3-entry-cancel-confirmation.png) / [mobile](evidence/part3-entry-cancel-confirmation-mobile.png) |
| Reactivation | [Desktop](evidence/part3-entry-reactivate.png) / [mobile](evidence/part3-entry-reactivate-mobile.png) |
| Saved category change | [Reactivated entry](evidence/part3-entry-reactivated.png) |
| Delete confirmation | [Desktop](evidence/part3-entry-delete-confirmation.png) / [mobile](evidence/part3-entry-delete-confirmation-mobile.png) |
| Deleted entry | [Empty personal history](evidence/part3-entry-deleted.png) |
| Rejected change | [Closed-entry restriction](evidence/part3-entry-change-rejected.png) |

## References

Microsoft (2026a) 'Role-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026b) 'Model Binding in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026c) 'Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026d) 'Make HTTP requests using IHttpClientFactory in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026e) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 November 2026).
