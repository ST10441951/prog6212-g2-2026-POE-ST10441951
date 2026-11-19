# RaceDay Part 3 - Accounts

## 1. Pages

| Method | MVC route | API request |
|---|---|---|
| GET / POST | `/account/register` | POST `/api/auth/register` |
| GET / POST | `/account/login` | POST `/api/auth/login` |
| POST | `/account/logout` | POST `/api/auth/logout` |
| GET | `/organiser` | GET `/api/profile` to verify the session; Organiser only |
| GET | `/participant` | GET `/api/profile` to verify the session; Participant only |

Both roles use the same forms. Registration accepts names, email, password, account type and optional phone/birth date. Confirmation is checked in MVC and is not sent to the API. Successful registration redirects to sign-in; it does not log in automatically (Microsoft, 2026a; 2026b).

The API checks password strength, roles, duplicate emails and birth dates. MVC displays returned field errors, maps duplicate emails to the email field and explains incorrect credentials or inactive accounts. Password fields are cleared on every rejected submission. Account pages use no-store responses (Microsoft, 2026b).

## 2. Login state

The API verifies the credentials. MVC stores the returned user ID, first name, role, expiry and API cookie in its server cache. The browser gets only a protected random identifier in `RaceDay.Web.Session`. The API cookie is not sent to the browser. All HTTP clients have automatic cookies and redirects disabled (Microsoft, 2026c; 2026d; 2026e).

The MVC expiry follows the API's fixed 20-minute expiry. Another login rotates the MVC identifier and sends only that browser's previous API cookie to the login endpoint, allowing the API to end its old session. Unrelated visitors do not share it.

On replacement login, the API can return an expired empty cookie followed by its new session cookie. MVC ignores only explicit deletion cookies, requires one live value and still rejects ambiguous duplicates. This corrects a 503 found when changing accounts while already signed in. Cookie lifetime handling follows `Expires` and `Max-Age`, with `Max-Age` taking precedence (Microsoft, 2026k).

The cookie is HttpOnly and SameSite=Strict. It uses Secure on HTTPS and outside Development. Sessions are held in memory and are lost when MVC restarts. Multiple MVC instances would need a shared cache (Microsoft, 2026c; 2026d).

Successful login opens the correct role's home page, or a validated local return address. External return addresses are ignored, including values changed in a submitted form (Microsoft, 2026j).

## 3. Protected pages

MVC controllers require sign-in by default. The public event list, account forms and error pages allow guests. `WebSessionHandler` reads the server-side session and calls `GET /api/profile` using only that user's API cookie. It builds the request's user ID, name and role claims after verification (Microsoft, 2026h; 2026i).

The saved ID and role must match an active API account. API 401, 403 or 404, expiry, changed identity or changed role clears the local session. Guests are redirected to sign-in. An authenticated user opening the other role's page receives a friendly 403 without losing their valid session. Account names are refreshed from the API and encoded by Razor.

An unavailable or unreadable API response blocks protected access with 503. The cached session stays available for a retry until its original expiry; requests do not extend it. Public pages hide the unverified identity and show an account-service warning.

Organisers have Organiser home and My events; Participants have My account, My entries and My results. Both roles have My profile. Organisers manage permitted owned events, categories, routes, enrolments and results. Image controls are described in the [image notes](Part-3-Images.md). API ownership rules are unchanged. See the [profile](Part-3-Profiles.md), [result](Part-3-Results.md), [Organiser](Part-3-Organiser.md), [event-management](Part-3-Event-Management.md), [category](Part-3-Categories.md), [enrolment](Part-3-Enrolments.md) and [result-management notes](Part-3-Result-Management.md).

## 4. Sign-out and form protection

Sign-out is a POST with an antiforgery token. It sends the current user's API cookie to logout and clears the MVC session. If the API cannot be reached, the local session still ends and the failure is logged. Old MVC cookies cannot restore a session after sign-out or another login (Microsoft, 2026c; 2026f).

Registration and login also require antiforgery tokens. Passwords are sent only in the API request body, never stored in sessions or copied back into form values (Microsoft, 2026a; 2026f).

## 5. Checks and evidence

On 19 November 2026, all 518 MVC checks passed locally. Account checks cover forms, protected URLs, role rejection, expiry, changed accounts, safe return addresses and outage recovery. Tests also verify default controller protection and antiforgery. Real HTTP tests verify that two browsers do not share an upstream login, private entries or profiles (Microsoft, 2026g). See the [event](Part-3-Events.md), [entry](Part-3-Entries.md) and [profile notes](Part-3-Profiles.md).

A separate temporary database was used for browser checks against the real API. Both roles registered, signed in with the correct identity and signed out. Duplicate emails and wrong passwords showed useful errors. Passwords were cleared, and browser cookies contained no API session token. The temporary database was removed afterwards.

Both protected role pages were also checked against a separate real API/database. Guests were redirected, the other role received 403, and signed-out cookies could not be reused. Disabling, changing the role of or deleting the review Participant removed its MVC access without affecting the Organiser. These changes were confined to temporary review data.

The home, account and role pages were checked at 320, 390, 768 and 1280 pixels without horizontal overflow. Screenshots are genuine browser captures without cursor overlays.

| Page | Evidence |
|---|---|
| Sign in | [Login page](evidence/part3-login.png) |
| Create account | [Registration page](evidence/part3-registration.png) |
| Organiser home | [Desktop](evidence/part3-organiser-home.png) / [mobile](evidence/part3-organiser-home-mobile.png) |
| Participant home | [Desktop](evidence/part3-participant-home.png) / [mobile](evidence/part3-participant-home-mobile.png) |
| Wrong role | [Access denied](evidence/part3-access-denied.png) |

The 810 API checks passed in GitHub on the same date; API source and database mappings are unchanged. Both checks for `81c3cf1` passed using temporary Windows runners. The current interface changes are included in that revision. See the [CI notes](Part-3-CI.md).

## References

Microsoft (2026a) 'Tag Helpers in forms in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/views/working-with-forms?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026b) 'Model validation in ASP.NET Core MVC and Razor Pages', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026c) 'Distributed caching in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026d) 'Consumer APIs overview for ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/overview?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026e) 'Make HTTP requests using IHttpClientFactory in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026f) 'Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026g) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026h) 'Overview of ASP.NET Core authentication', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026i) 'Role-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026j) 'Prevent open redirect attacks in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/preventing-open-redirects?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026k) 'SetCookieHeaderValue.MaxAge Property', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/api/microsoft.net.http.headers.setcookieheadervalue.maxage?view=aspnetcore-10.0 (Accessed: 19 November 2026).
