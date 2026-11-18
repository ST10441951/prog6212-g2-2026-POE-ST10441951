# RaceDay Part 3 - Events

## 1. Find events

The home page calls `GET /api/events` and displays three events per page. Province, event type and optional from/to dates are sent to the API. Date filters are inclusive UTC dates; event times are displayed in SAST. The API controls filtering, ordering and visibility (Microsoft, 2026a; 2026b).

Page links keep the current filters. Submitting the search starts at page 1, and Clear filters removes the query. Empty results and out-of-range pages have separate messages. Invalid inputs return a friendly 400; unavailable or unreadable API responses return 503 (Microsoft, 2026a; 2026c).

## 2. Event details

Event names link to `/events/{eventId}`. MVC reads `GET /api/events/{eventId}` without an API cookie, including when an Organiser is signed in. Draft and Cancelled events remain hidden on these public pages. Missing or hidden events return 404.

Details show the description, date, venue, address, distance, entry deadline, status and coordinates when supplied. Categories include distance, age limits, rand fees and maximum entries. Capacity is not presented as remaining places. Routes show their name, instructions, start, finish, elevation and map link. Missing categories or routes have clear empty states.

Razor encodes event text. Banner and map URLs must be absolute HTTP/HTTPS addresses without credentials. Map links use `noopener noreferrer`. Existing API banner URLs can be displayed; MVC uploads and the Azure demonstration remain outstanding (Microsoft, 2026d).

Participants can now enter an event and read their private entry history. See the [entry notes](Part-3-Entries.md). Permitted entry changes are also connected through the API.

## 3. Checks and evidence

On 18 November 2026, all 222 MVC tests passed, with none failed or skipped. The Release build had zero warnings/errors. Added checks cover paging, dates, retained filters, details, hidden/missing events, empty states, encoding and unsafe links (Microsoft, 2026e).

Browser checks used a separate database with five extra cycling fixtures to exercise multiple pages. MVC names and page results matched the real API. Combined filters, inclusive dates, clear filters, invalid ranges, out-of-range pages, Completed events and Draft/Cancelled visibility all passed. The temporary database was removed afterwards; `RaceDayPart2` was not changed.

The home and detail pages were checked at 320, 390, 768 and 1280 pixels without horizontal overflow or JavaScript errors. Screenshots are genuine browser captures without cursor overlays. Additional names shown in search screenshots belong to the temporary review fixtures.

| Page | Evidence |
|---|---|
| Find events | [Desktop](evidence/part3-event-finder-desktop.png) / [mobile](evidence/part3-event-finder-mobile.png) |
| Filtered page 2 | [Pagination](evidence/part3-event-pagination.png) |
| Event categories and routes | [Desktop](evidence/part3-event-details-desktop.png) / [mobile](evidence/part3-event-details-mobile.png) |

## References

Microsoft (2026a) 'Model Binding in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026b) 'QueryHelpers.AddQueryString Method', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.webutilities.queryhelpers.addquerystring?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026c) 'Anchor Tag Helper in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/views/tag-helpers/built-in/anchor-tag-helper?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026d) 'Prevent Cross-Site Scripting (XSS) in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/cross-site-scripting?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026e) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Bootstrap (2026) 'Pagination', *Bootstrap v5.3*. Available at: https://getbootstrap.com/docs/5.3/components/pagination/ (Accessed: 18 November 2026).
