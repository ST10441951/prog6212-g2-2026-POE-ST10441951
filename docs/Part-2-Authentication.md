# RaceDay Part 2 Authentication

## 1. Endpoints

The three authentication endpoints follow the Part 1 plan.

| Method | Route | Access | Response |
|---|---|---|---|
| POST | `/api/auth/register` | Public | 201 with a safe account summary |
| POST | `/api/auth/login` | Public | 200 with account details and session expiry |
| POST | `/api/auth/logout` | Signed-in user | 204 without a response body |

Registration does not sign in the account. Its Location header points to `/api/profile`, which will be implemented with the profile endpoints.

## 2. Account validation

- First name, surname, email, password and role are required.
- Email addresses are trimmed, converted to lower case and stored uniquely. Duplicate accounts return 409.
- Only Organiser and Participant role names are accepted.
- User IDs, role IDs, password hashes and account status cannot be supplied in the request.
- Phone number and date of birth are optional. A supplied date of birth must be earlier than today's UTC date.
- Passwords need 8 to 128 characters, including an upper-case letter, lower-case letter, digit and symbol.

Request classes use model validation and reject unexpected JSON fields (Microsoft, 2026a; 2026b). The SQL unique constraint also rejects simultaneous registrations using the same email.

The API uses `PasswordHasher<User>` to create and verify salted password hashes. Login rehashes an older supported hash when needed. Passwords and hashes are excluded from account responses and session data (Microsoft, 2026c).

Invalid credentials return 401. Correct credentials for an inactive account return 403.

## 3. Sessions and roles

Each login creates a random session ID. The user ID, role and fixed expiry are kept in the server cache. The browser receives only the protected session ID, in the `RaceDay.Session` cookie (Microsoft, 2026d; 2026e).

Sessions expire 20 minutes after login. Logout removes the server record and clears the cookie. Another login in the same browser removes the previous session and creates a fresh ID.

The cookie is HttpOnly and SameSite=Strict. HTTPS uses a Secure cookie; the local HTTP development profile can use it without HTTPS. Outside Development, Secure is always enabled.

The authentication handler checks the saved account before accepting a protected request. Disabled accounts return 403 and lose their session. Deleted accounts or changed roles invalidate the session and return 401. Role policies return 403 when a signed-in user has the wrong role (Microsoft, 2026f; 2026g).

Controllers require authentication by default. Public actions must be marked `AllowAnonymous`. Registration, login and the host check are public.

The cache currently runs in the API process. Restarting the API ends its sessions. Multiple API instances would need a shared cache.

## 4. Swagger checks

These checks were completed on 18 October 2026 using the running API and `RaceDayPart2`.

1. Execute the registration example with a new email. It returns 201 with the saved account summary.
2. Execute the login example using `naledi.mokoena@example.com` and `RaceDayDemo!2026`. It returns 200 and a UTC expiry.
3. Execute logout without a body. It returns 204. The browser sends its HttpOnly cookie automatically; nothing needs to be entered in Authorize.
4. Execute logout again. It returns 401 because the session has ended.

For an Organiser login, use `thabo.ndlovu@example.com` with the same local demonstration password. Both accounts come from the Part 1 seed data.

The extra account created by the browser registration check was removed after testing. The four original sample accounts remain.

| Evidence | File |
|---|---|
| Successful login and expiry | [Swagger login](evidence/swagger-authentication-login.jpg) |
| Rejected request after logout | [Swagger logout after sign-out](evidence/swagger-logout-after-signout.jpg) |

## 5. Test results

All 121 tests pass locally and in GitHub Actions: seven startup checks, 63 database checks and 51 authentication checks. The solution builds with no warnings or errors. The database model has no pending migration changes.

Authentication checks cover both roles, invalid fields, duplicate and simultaneous registration, password hashing, rehashing, stored session fields, cookie flags, session replacement, logout replay, expiry, separate browsers and account changes.

Role policies are checked through test-only controllers in the test project. Those routes are not included in the running API or Swagger. Event ownership and role checks on the remaining business endpoints will be tested when those endpoints are added (Microsoft, 2026h).

The [authentication build for b1daebb](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39104118046) passed all 121 tests with none skipped. The planning workflow also passed.

## References

Microsoft (2026a) 'Model validation in ASP.NET Core MVC', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026b) 'Handle unmapped members during deserialization', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/missing-members (Accessed: 18 October 2026).

Microsoft (2026c) 'PasswordHasher<TUser> class', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026d) 'Distributed caching in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026e) 'Consumer APIs overview', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/overview?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026f) 'AuthenticationHandler<TOptions> class', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.authenticationhandler-1?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026g) 'Role-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026h) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 October 2026).
