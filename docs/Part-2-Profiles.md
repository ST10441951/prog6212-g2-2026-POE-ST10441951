# RaceDay Part 2 Profiles

## 1. Endpoints

Both roles can view and update their own account after login.

| Method | Route | Response |
|---|---|---|
| GET | `/api/profile` | 200 with personal details |
| PUT | `/api/profile` | 200 with the saved profile |
| PUT | `/api/profile/password` | 204 without a response body |

These routes follow the Part 1 endpoint plan. The user ID comes from the authenticated session, rather than a request field or route value. A query value or invented identity header cannot select another account (Microsoft, 2026a).

Profiles include names, email, phone number, date of birth, role, account status and UTC creation time. Passwords, hashes, storage object names and related private records are excluded. Responses use `Cache-Control: no-store`.

Profile pictures will be added with the image endpoints. Until then, `profilePictureUrl` is null.

## 2. Personal details

- First name and surname are required, with a maximum of 50 characters each. Saved names are trimmed.
- Phone numbers are optional, validated and limited to 20 characters. Blank numbers are cleared.
- A supplied date of birth must be earlier than today's UTC date.
- PUT replaces the permitted details. Omitting phone number or date of birth clears that value, as does null.
- Email, identity, role, account status, password and image fields are rejected.
- SQL Server saves the changes and `UpdatedAt`. The existing database stores timestamps to whole seconds.

Request validation returns 400 with field errors. Unexpected JSON fields are also rejected. Non-JSON writes return 415 (Microsoft, 2026b; 2026c; 2026d).

Missing, invalid or expired sessions return 401. Inactive accounts return 403. A deleted account normally returns 401 during authentication. If it disappears between authentication and the action, the action returns 404.

## 3. Password changes

The request requires `currentPassword`, `newPassword` and `confirmPassword`. The current password must be correct. The replacement needs 8 to 128 characters with an upper-case letter, lower-case letter, digit and symbol, and the confirmation must match exactly.

Passwords are not trimmed. The API verifies the existing hash and saves a new salted hash using `PasswordHasher<User>`. Neither password is returned. Invalid requests leave the saved hash and session unchanged (Microsoft, 2026e).

A successful change ends the current browser's session. The old cookie cannot be replayed, and the old password cannot start another session. Sign in again using the replacement password. Other browsers keep their existing sessions until logout or their normal expiry.

## 4. Swagger checks

Profile reads and updates were checked in Swagger on 18 October 2026 using a separate temporary account in `RaceDayPart2`.

1. Log in and execute GET `/api/profile`. It returns 200 with that account's personal details.
2. Execute PUT `/api/profile` with valid names, phone number and date of birth. It returns 200 with the saved values.
3. Execute GET again. It returns the updated details.
4. Log out and execute GET again. It returns 401.

The browser sends the login cookie automatically. The profile request examples also run through the API tests. Password changes are checked against temporary SQL Server test databases, including the Swagger password example.

| Evidence | File |
|---|---|
| Saved personal details | [Swagger profile update](evidence/swagger-profile-update.jpg) |
| Rejected profile access after logout | [Swagger profile after sign-out](evidence/swagger-profile-after-signout.jpg) |

The temporary browser-check account was removed after testing. The original four sample accounts were kept.

## 5. Test results

At the profile revision, all 178 tests passed locally and in GitHub Actions: seven startup, 63 database, 51 authentication and 57 profile and password checks. None were skipped. The build had no warnings or errors, the planning checks passed and the model had no pending migration changes. Event browsing then passed 233 tests locally and on GitHub. Event management then passed 354 tests locally and on GitHub. Category management then passed 447 tests locally and on GitHub. The route revision passed 532 tests locally and on GitHub. The [enrolment notes](Part-2-Enrolments.md) record the subsequent entry checks.

The new checks cover both roles, own-account access, protected fields, optional values, invalid dates and bodies, length limits, SQL persistence, UTC timestamps, password hashing, session removal, replay rejection and working Swagger examples (Microsoft, 2026f).

Tests use separate `RaceDayTests_...` databases and remove them afterwards. They do not change the application or Part 1 database. The [profile build for 0d65a7b](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39108330909) passed all 178 tests. The planning workflow also passed.

## References

Microsoft (2026a) 'Claims-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/claims?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026b) 'Model validation in ASP.NET Core MVC', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026c) 'Handle unmapped members during deserialization', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/missing-members (Accessed: 18 October 2026).

Microsoft (2026d) 'Basic SaveChanges', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/saving/basic (Accessed: 18 October 2026).

Microsoft (2026e) 'PasswordHasher<TUser> class', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026f) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 October 2026).
