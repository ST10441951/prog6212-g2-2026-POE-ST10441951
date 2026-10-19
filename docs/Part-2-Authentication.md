# RaceDay Part 2 Authentication

## 1. Endpoints

| Method | Route | Access | Response |
|---|---|---|---|
| POST | `/api/auth/register` | Public | 201 with account summary and profile Location |
| POST | `/api/auth/login` | Public | 200 with account details and session expiry |
| POST | `/api/auth/logout` | Signed-in user | 204 |

Register first, then log in to read `/api/profile`. Registration does not sign in automatically.

## 2. Validation

- Names, email, password and Organiser/Participant role are required.
- Emails are trimmed, stored in lower case and unique. Duplicates return 409, including simultaneous registrations.
- Phone and date of birth are optional. A supplied birth date must be before today in UTC.
- Passwords require 8-128 characters with an upper-case letter, lower-case letter, digit and symbol.
- Unknown JSON fields, including IDs and account status, return 400.
- PasswordHasher saves salted hashes. Credentials and hashes are excluded from responses and sessions.
- Incorrect credentials return 401; valid credentials for an inactive account return 403 (Microsoft, 2026a; 2026b; 2026c).

## 3. Sessions and roles

Login stores user ID, role and a fixed 20-minute expiry in the server cache. The browser receives a protected random ID in an HttpOnly, SameSite=Strict cookie. HTTPS and non-Development environments use Secure cookies (Microsoft, 2026d; 2026e).

Logout removes the session. Another login replaces that browser's previous session. Inactive accounts return 403 and lose their session; missing accounts or changed roles invalidate it. Wrong roles return 403 (Microsoft, 2026f; 2026g).

Controllers require authentication by default. Public reads stay public; optional sessions allow owners to read hidden event information. Restarting the API clears the in-memory sessions. Multiple instances would need a shared cache.

## 4. Checks and evidence

The 51 authentication checks cover registration, hashing, both roles, cookies, expiry, logout, replay and account changes. Business endpoints also test their own role and ownership rules (Microsoft, 2026h). Current results are in the [rubric review](Part-2-Rubric-Review.md).

Swagger login returned 200, logout 204 and repeated logout 401 on 18 October. Sample logins: Naledi for Participant, Thabo for Organiser; password `RaceDayDemo!2026`. Temporary registration data was removed.

| Evidence | File |
|---|---|
| Login and expiry | [Swagger login](evidence/swagger-authentication-login.jpg) |
| Access after logout | [Swagger logout](evidence/swagger-logout-after-signout.jpg) |

## References

Microsoft (2026a) 'Model validation in ASP.NET Core MVC', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026b) 'Handle unmapped members during deserialization', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/missing-members (Accessed: 18 October 2026).

Microsoft (2026c) 'PasswordHasher<TUser> class', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026d) 'Distributed caching in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026e) 'Consumer APIs overview', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/overview?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026f) 'AuthenticationHandler<TOptions> class', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.authenticationhandler-1?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026g) 'Role-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026h) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 October 2026).
