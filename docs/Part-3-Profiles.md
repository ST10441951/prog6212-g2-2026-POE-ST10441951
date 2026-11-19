# RaceDay Part 3 - Profiles

## 1. Pages

| Method | MVC route | API request |
|---|---|---|
| GET | `/profile` | GET `/api/profile` |
| GET / POST | `/profile/edit` | Read own profile / PUT `/api/profile` |
| GET / POST | `/profile/password` | Display form / PUT `/api/profile/password` |

Both roles have My profile in their navigation. Guests are redirected to sign-in. Each request sends only the current user's server-side API cookie. Identity comes from the session, with no user ID accepted by the forms (Microsoft, 2026a; 2026b).

## 2. Personal details

The profile shows name, email, phone number, birth date, role and joined date. Edit profile sends only first name, surname, phone number and birth date. Email, role and account status cannot be changed. Blank optional fields are cleared. The API checks and saves the details; MVC displays field errors and refreshes the name after a successful save (Microsoft, 2026b; 2026c).

Private pages use no-store responses. A profile must match the signed-in user's ID and role before display. Inactive, removed or changed accounts end the local session. Unavailable responses show a friendly 503; an uncertain save advises checking the profile before retrying.

Safe API picture URLs display on the profile, with an initial shown when none is available. Participants can upload, replace or remove their picture using [image management](Part-3-Images.md). Organisers do not receive Participant picture controls.

## 3. Password changes

The form requires the current password, a new password and matching confirmation. The API verifies the old password, enforces its password policy and saves a replacement hash. Rejected submissions clear all three password inputs. Both edit forms require antiforgery tokens (Microsoft, 2026c; 2026d).

Success ends the current API and MVC session and redirects to sign-in. The old password no longer works. Other browsers keep their existing sessions until logout or normal expiry, matching the API contract. If a password change cannot be confirmed, MVC signs out locally and explains how to try signing in. Passwords are never stored in MVC sessions or displayed in returned form values.

## 4. Checks and evidence

On 19 November 2026, all 496 MVC checks passed locally, with none failed or skipped. Profile checks cover both roles, allowed fields, validation, antiforgery, private responses, rejected credentials, uncertain writes and ended sessions. A real HTTP test checks that separate browsers receive only their own profiles (Microsoft, 2026e).

Browser checks against a temporary database verified saved names, phone numbers and birth dates in SQL. Blank optional fields cleared correctly. Future birth dates and attempted identity changes were rejected or ignored as appropriate. Wrong passwords, weak passwords and mismatched confirmation left empty password inputs. Both roles changed passwords and signed in again. The hash changed, the old password failed and the previous MVC cookie could not restore access. Another existing browser and an unrelated account retained access as specified by the API.

The pages were checked at 320, 390, 768 and 1280 pixels without horizontal overflow or JavaScript errors. Screenshots use fictional accounts and contain no cursor overlay or filled password fields. The temporary database was removed afterwards; the original sample accounts were not changed.

| Page | Evidence |
|---|---|
| Participant profile | [Desktop](evidence/part3-profile.png) / [mobile](evidence/part3-profile-mobile.png) |
| Organiser profile | [Personal details](evidence/part3-organiser-profile.png) |
| Edit profile | [Desktop](evidence/part3-profile-edit.png) / [mobile](evidence/part3-profile-edit-mobile.png) |
| Change password | [Desktop](evidence/part3-password-change.png) / [mobile](evidence/part3-password-change-mobile.png) |
| Wrong current password | [Cleared inputs](evidence/part3-password-rejected.png) |
| Successful change | [Sign-in notice](evidence/part3-password-changed-login.png) |

## References

Microsoft (2026a) 'Overview of ASP.NET Core authentication', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026b) 'Model Binding in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026c) 'Model validation in ASP.NET Core MVC and Razor Pages', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026d) 'Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026e) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 November 2026).
