# RaceDay Part 3 - MVC

## 1. Project

`src/RaceDay.Web` is a .NET 10 MVC project in `RaceDay.sln`. It has no API project reference, EF Core package, SQL connection or Azure SDK. Controllers use `RaceDayApiClient` for public events, `AccountApiClient` for accounts and `EntryApiClient` for private entries (Microsoft, 2026a; 2026b).

The home page reads upcoming events from `GET /api/events` with three per page. Province, type, date filters and the page number are passed to the API. Event names open public details with categories and routes. Participants can enter an event and read their private entries. See the [event notes](Part-3-Events.md) and [entry notes](Part-3-Entries.md). Accounts, protected pages and session rechecks are covered in the [account notes](Part-3-Accounts.md). Organiser management, results and profile editing remain outstanding.

## 2. Settings

| Setting | Value |
|---|---|
| `Api:BaseUrl` | Development: `http://localhost:5120/` |
| `Api:TimeoutSeconds` | Default 15; allowed range 1-120 seconds |
| MVC address | `http://localhost:5180/` |

Use `Api__BaseUrl` to select another API. It must be an HTTP/HTTPS address ending in `/`, without credentials, query parameters or a fragment. Invalid settings stop startup with a configuration message. Outside Development, the API address must be supplied explicitly (Microsoft, 2026c).

The JSON settings and launch profile follow ASP.NET Core configuration conventions. The solution entries were added using `dotnet sln add` (Microsoft, 2026c; 2026d).

## 3. API connection

All three typed HTTP clients have automatic cookies and redirects disabled. API login cookies stay in each user's server-side MVC session. Account and entry requests forward a cookie only for that user; public event reads send none (Microsoft, 2026b).

An API error, timeout, connection failure or unreadable response shows a friendly message and returns 503. An empty successful list shows that no events are available. Exceptions and raw API errors are not displayed. `/health` checks the MVC host only (Microsoft, 2026b; 2026e).

The search form uses the API's province/type choices and optional inclusive UTC dates. QueryHelpers encodes the values, and paging links retain them. Invalid typed inputs and rejected filters return a friendly 400. MVC does not repeat the API's filtering or business rules (Microsoft, 2026i).

## 4. Layout

Razor pages share a header, navigation and footer. Signed-in users see their verified name and their role's home link. Bootstrap 5.3.9 is served locally, with its MIT licence in `wwwroot/lib/bootstrap`. The selected Event finder design uses blue accents, a light background, compact navigation, a search panel and white event cards. Keyboard focus styles, a skip link and responsive layouts are included (Microsoft, 2026f; Bootstrap, 2026; MDN, 2026).

Razor encodes event text. The shared error page handles missing pages and unexpected errors without exposing exception details (Microsoft, 2026e; 2026g).

## 5. Checks

The 222 MVC checks cover search, dates, paging, event details, entries, settings, accounts, protected pages, role restrictions, redirects, errors, cookie isolation and expiry. HTTP-handler checks use a temporary local server. Other MVC checks replace the upstream API response and do not need SQL or Azure (Microsoft, 2026h).

On 18 November 2026, all 222 MVC checks passed locally, with none failed or skipped. The unchanged 810 API tests also passed in GitHub. The Release build had zero warnings/errors. Planning checks passed and the API model is unchanged.

The home and detail pages passed browser checks at 320, 390, 768 and 1280 pixels without horizontal overflow. Combined filters, inclusive dates, paging and route details worked against an isolated real API/database. The [desktop](evidence/part3-event-finder-desktop.png) and [mobile](evidence/part3-event-finder-mobile.png) search screenshots use temporary paging fixtures and have no cursor overlay. Further details and screenshots are in the [event notes](Part-3-Events.md).

Both GitHub workflows passed for `1f16d9e` using temporary Windows runners. That revision ran 810 API and 173 MVC tests. The newer entry-change controls have 222 passing MVC tests locally and are not yet pushed. See the [CI notes](Part-3-CI.md) and [README](../README.md#cicd).

Run instructions and current results are in the [README](../README.md#setup-and-run).

## References

Bootstrap (2026) 'Download', *Bootstrap v5.3*. Available at: https://getbootstrap.com/docs/5.3/getting-started/download/ (Accessed: 18 November 2026).

MDN (2026) ':focus-visible', *MDN Web Docs*. Available at: https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/Selectors/:focus-visible (Accessed: 18 November 2026).

Microsoft (2026a) 'Overview of ASP.NET Core MVC', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/overview?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026b) 'Make HTTP requests using IHttpClientFactory in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026c) 'Options pattern in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026d) 'dotnet sln', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-sln (Accessed: 18 November 2026).

Microsoft (2026e) 'Handle errors in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026f) 'Layout in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/views/layout?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026g) 'Prevent Cross-Site Scripting (XSS) in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/cross-site-scripting?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026h) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026i) 'QueryHelpers.AddQueryString Method', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.webutilities.queryhelpers.addquerystring?view=aspnetcore-10.0 (Accessed: 18 November 2026).
