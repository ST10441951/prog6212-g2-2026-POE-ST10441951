# RaceDay Part 3 - Accounts

## 1. Pages

| Method | MVC route | API request |
|---|---|---|
| GET / POST | `/account/register` | POST `/api/auth/register` |
| GET / POST | `/account/login` | POST `/api/auth/login` |
| POST | `/account/logout` | POST `/api/auth/logout` |

Both roles use the same forms. Registration accepts names, email, password, account type and optional phone/birth date. Confirmation is checked in MVC and is not sent to the API. Successful registration redirects to sign-in; it does not log in automatically (Microsoft, 2026a; 2026b).

The API checks password strength, roles, duplicate emails and birth dates. MVC displays returned field errors, maps duplicate emails to the email field and explains incorrect credentials or inactive accounts. Password fields are cleared on every rejected submission. Account pages use no-store responses (Microsoft, 2026b).

## 2. Login state

The API verifies the credentials. MVC stores the returned user ID, first name, role, expiry and API cookie in its server cache. The browser gets only a protected random identifier in `RaceDay.Web.Session`. The API cookie is not sent to the browser. Both HTTP clients have automatic cookies and redirects disabled (Microsoft, 2026c; 2026d; 2026e).

The MVC expiry follows the API's fixed 20-minute expiry. Another login rotates the MVC identifier and sends only that browser's previous API cookie to the login endpoint, allowing the API to end its old session. Unrelated visitors do not share it.

The cookie is HttpOnly and SameSite=Strict. It uses Secure on HTTPS and outside Development. Sessions are held in memory and are lost when MVC restarts. Multiple MVC instances would need a shared cache (Microsoft, 2026c; 2026d).

Successful login currently returns to the event list. Protected pages, distinct role menus and checks for accounts disabled or changed at the API remain to be added.

## 3. Sign-out and form protection

Sign-out is a POST with an antiforgery token. It sends the current user's API cookie to logout and clears the MVC session. If the API cannot be reached, the local session still ends and the failure is logged. Old MVC cookies cannot restore a session after sign-out or another login (Microsoft, 2026c; 2026f).

Registration and login also require antiforgery tokens. Passwords are sent only in the API request body, never stored in sessions or copied back into form values (Microsoft, 2026a; 2026f).

## 4. Checks and evidence

On 18 November 2026, all 70 MVC checks passed, including the 33 earlier checks. Account tests cover both roles, allowed request fields, validation, antiforgery, safe cookies, incomplete responses, rotation, expiry and sign-out. Real HTTP tests verify that two browsers do not share an upstream login (Microsoft, 2026g).

A separate temporary database was used for browser checks against the real API. Both roles registered, signed in with the correct identity and signed out. Duplicate emails and wrong passwords showed useful errors. Passwords were cleared, and browser cookies contained no API session token. The temporary database was removed afterwards.

The home page and both account pages were checked at 320, 390, 768 and 1280 pixels without horizontal overflow. Screenshots are genuine browser captures without cursor overlays.

| Page | Evidence |
|---|---|
| Sign in | [Login page](evidence/part3-login.png) |
| Create account | [Registration page](evidence/part3-registration.png) |

The 810 API checks passed earlier on the same date; API source and database mappings are unchanged. GitHub jobs for `f60e944` did not start because GitHub reported an account billing lock. This is separate from the local results.

## References

Microsoft (2026a) 'Tag Helpers in forms in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/views/working-with-forms?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026b) 'Model validation in ASP.NET Core MVC and Razor Pages', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026c) 'Distributed caching in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026d) 'Consumer APIs overview for ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/overview?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026e) 'Make HTTP requests using IHttpClientFactory in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026f) 'Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026g) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 November 2026).
