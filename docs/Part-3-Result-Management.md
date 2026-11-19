# RaceDay Part 3 - Result capture and corrections

## 1. Pages

Open View results on an owned event to see its official results. Use View enrolments, open an entry and choose Record result to add its first result. Saved results have a Correct result link. Only the event's Organiser can use these pages (Microsoft, 2026a).

| MVC page | API request |
|---|---|
| `/organiser/events/{eventId}/results` | GET `/api/events/{eventId}/results` |
| `/organiser/events/{eventId}/results/{enrolmentId}` | GET `/api/enrolments/{enrolmentId}/result` |
| Record form at `/{enrolmentId}/capture` | Read entry, then POST `/api/enrolments/{enrolmentId}/result` |
| Correction form at `/{enrolmentId}/edit` | Read entry/result, then PUT `/api/enrolments/{enrolmentId}/result` |

Results show Participant name, category, bib, outcome, time and positions. Status/category filters work together, with ten results per page. Counts, order and event-wide finisher totals come from the API. Changing pages retains both filters.

## 2. Recording and correcting

The form sends only result status, elapsed seconds, overall position, category position and notes. MVC converts `HH:MM:SS` to elapsed seconds; hours can exceed 24. Entry IDs, recorder and timestamps are taken from the saved API context, not posted fields. Forms require antiforgery tokens (Microsoft, 2026b; 2026c).

The API requires a Confirmed entry and an event that has started and is not Cancelled. Completed needs a positive finish time. Positions are optional positive numbers, with ties allowed. Did not finish, Did not start and Disqualified require blank time and positions. Notes are optional and limited to 500 characters.

First capture creates one result. Corrections keep its ID, entry link, original recorder and original recording time, and set the correction time. Blank notes clear the old notes. There is no result deletion control. The Participant's history and event-wide finisher totals refresh on the next request.

Rejected forms retain the values and show the API's reason. A successful save opens official result details. Expired sessions return to sign-in; wrong roles and other owners are blocked. Missing results and entries from another event return 404. Service failures show a friendly message; uncertain saves ask the Organiser to check the saved result before submitting again.

## 3. Checks and evidence

On 19 November 2026, all 496 MVC checks passed locally, with none failed or skipped. Result-management checks cover roles, private filters, elapsed-time conversion, allowed fields, antiforgery, validation, encoded messages, response checks, ended sessions and uncertain saves. A real HTTP test checks that Organisers receive separate private event results (Microsoft, 2026d).

Browser checks used an isolated database with fictional entries. Capture matched SQL, including a time over 24 hours. Corrections covered all four outcomes and refreshed the Participant's history and finisher totals. Result ID, recorder, original recording time, entry history and unrelated results stayed unchanged. Duplicate capture, future events, Cancelled entries, wrong-event links, other users and missing antiforgery tokens were rejected.

Filtering, pagination, empty lists and invalid queries were checked against the real API. Pages passed at 320, 390, 768 and 1280 pixels without horizontal overflow or JavaScript errors. Screenshots are genuine browser captures without cursor overlays. The temporary database was removed afterwards; original sample data was unchanged.

| Page | Evidence |
|---|---|
| Event results | [Desktop](evidence/part3-event-results.png) / [mobile](evidence/part3-event-results-mobile.png) |
| Paging and filters | [Second page](evidence/part3-event-result-pagination.png) / [combined filters](evidence/part3-event-result-filtered-page.png) |
| Record result | [Desktop](evidence/part3-result-capture.png) / [mobile](evidence/part3-result-capture-mobile.png) |
| Saved result | [Desktop](evidence/part3-official-result-saved.png) / [mobile](evidence/part3-official-result-saved-mobile.png) |
| Correction form | [Desktop](evidence/part3-result-correction.png) / [mobile](evidence/part3-result-correction-mobile.png) |
| Validation | [Missing time](evidence/part3-result-time-validation.png) / [non-finishing outcome](evidence/part3-result-outcome-validation.png) |
| Saved corrections | [Changed outcome](evidence/part3-result-corrected-outcome.png) / [corrected time](evidence/part3-official-result-corrected.png) |
| Participant history | [Refreshed result](evidence/part3-participant-result-refreshed.png) |
| API restrictions | [Cancelled entry](evidence/part3-result-cancelled-entry-rejected.png) / [future event](evidence/part3-result-future-event-rejected.png) |

## References

Microsoft (2026a) 'Role-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0 (Accessed: 19 November 2026).

Microsoft (2026b) 'Model Binding in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0 (Accessed: 19 November 2026).

Microsoft (2026c) 'Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0 (Accessed: 19 November 2026).

Microsoft (2026d) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 19 November 2026).
