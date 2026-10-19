# RaceDay Part 2 Profiles

## 1. Endpoints

Both roles use their session identity to access their own account (Microsoft, 2026a).

| Method | Route | Response |
|---|---|---|
| GET | `/api/profile` | 200 with personal details |
| PUT | `/api/profile` | 200 with saved details |
| PUT | `/api/profile/password` | 204 |

## 2. Personal details

- Names are required, trimmed and limited to 50 characters each.
- Phone is optional, validated and limited to 20 characters. Blank clears it.
- Date of birth must be before today in UTC.
- PUT clears omitted or null phone and birth-date values.
- Email, identity, role, account status, password and image fields cannot be changed through profile PUT.
- Invalid or unknown fields return 400; non-JSON writes return 415 (Microsoft, 2026b; 2026c; 2026d).

Responses exclude hashes and stored blob names, and use no-store. [Image endpoints](Part-2-Images.md) manage Participant pictures; GET provides a fresh one-hour URL or null.

Missing/expired sessions return 401, inactive accounts 403, and an account removed after authentication returns 404.

## 3. Password changes

Send `currentPassword`, `newPassword` and `confirmPassword`. The current password must be correct, confirmation must match, and the replacement follows the registration password rules. Passwords are compared without trimming and saved as salted hashes (Microsoft, 2026e).

Success ends the current browser's session. Log in again with the new password. Other browsers keep their sessions until logout or expiry. Rejected requests leave the hash and session unchanged.

## 4. Checks and evidence

The 57 checks cover both roles, privacy, field validation, SQL persistence, password hashes and session removal (Microsoft, 2026f). See the [rubric review](Part-2-Rubric-Review.md) for current results.

Swagger checks on 18 October saved and reread a temporary account's changes, then confirmed 401 after logout. The temporary account was removed.

| Evidence | File |
|---|---|
| Saved details | [Profile update](evidence/swagger-profile-update.jpg) |
| Access after logout | [Profile after sign-out](evidence/swagger-profile-after-signout.jpg) |

## References

Microsoft (2026a) 'Claims-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/claims?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026b) 'Model validation in ASP.NET Core MVC', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026c) 'Handle unmapped members during deserialization', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/missing-members (Accessed: 18 October 2026).

Microsoft (2026d) 'Basic SaveChanges', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/saving/basic (Accessed: 18 October 2026).

Microsoft (2026e) 'PasswordHasher<TUser> class', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026f) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 October 2026).
