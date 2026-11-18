# RaceDay Part 3 - Personal results

## 1. Pages

| Method | MVC route | API request |
|---|---|---|
| GET | `/results` | GET `/api/results/mine` |
| GET | `/results/{enrolmentId}` | GET `/api/enrolments/{enrolmentId}/result` |

My results is available in the Participant menu and account home. Guests are redirected to sign-in; Organisers receive 403 on these Participant pages. Each request uses only the signed-in user's server-side API cookie. The API remains responsible for ownership and result data (Microsoft, 2026a; 2026b).

## 2. Race history

The list shows event name, SAST date, selected category, outcome, recorded finish time and position. It loads ten results per page, newest event first. Status filters and page numbers go to the API; page links retain the filter. Empty accounts, unmatched filters and later pages have clear messages.

Completed results show hours:minutes:seconds and the supplied overall position against the event-wide finisher count. Hours do not wrap after 24. The API counts all recorded Completed results for that event, independently of the current Participant, filter or page. MVC displays that count without calculating a new rank (Microsoft, 2026c).

Did not finish, Did not start and Disqualified have no official time or rank. A missing position on a Completed result reads Not assigned. Statuses have both text and colour: green for Completed, amber for Did not finish/Did not start and red for Disqualified.

## 3. Result details

Opening a result shows its category, distance, bib, overall/category positions, recorded date, correction date and Organiser notes. Links open the event and the Participant's entry. Only official saved results appear; an enrolment alone does not create one.

Results are read-only here. Organiser capture and correction pages remain outstanding. Updated API results appear on the next request. Pages use no-store responses, encode Organiser text and reject inconsistent or other-user data. Missing results return 404, invalid filters return 400 and service problems show a friendly 503. An expired API session ends the local session (Microsoft, 2026a; 2026b).

## 4. Checks and evidence

On 18 November 2026, all 397 MVC checks passed locally, with none failed or skipped. Result checks cover roles, privacy, paging, filters, times over 24 hours, non-finishing outcomes, missing positions, encoding, expired sessions and API failures. The real HTTP-handler test also confirms that two browsers read different private histories (Microsoft, 2026d).

Browser checks used an isolated database with fictional race fixtures. Displayed times and positions matched the API. Paging retained filters, other Participants and Organisers were blocked, missing results returned 404 and a newly registered account had no results. Read-only requests left SQL records unchanged. A real Organiser API correction refreshed the time, position, notes and event-wide finisher count in MVC.

Pages passed at 320, 390, 768 and 1280 pixels without horizontal overflow or JavaScript errors. Screenshots are genuine browser captures without cursor overlays. Review fixtures were removed afterwards; the original sample database was unchanged.

| Page | Evidence |
|---|---|
| My results | [Desktop](evidence/part3-my-results.png) / [mobile](evidence/part3-my-results-mobile.png) |
| Recorded result | [Desktop](evidence/part3-result-details.png) / [mobile](evidence/part3-result-details-mobile.png) |
| Non-finishing outcome | [Did not finish](evidence/part3-result-did-not-finish.png) |
| Later page | [Pagination](evidence/part3-results-pagination.png) |
| API correction | [Updated result](evidence/part3-result-corrected.png) |
| Empty account | [No official results](evidence/part3-results-empty.png) |

## References

Microsoft (2026a) 'Role-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026b) 'Make HTTP requests using IHttpClientFactory in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026c) 'Custom numeric format strings', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-numeric-format-strings (Accessed: 18 November 2026).

Microsoft (2026d) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 November 2026).
