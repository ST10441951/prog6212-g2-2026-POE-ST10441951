# RaceDay API Endpoint Plan

## 1. Purpose

This document plans the REST API endpoints for RaceDay before application development begins. It will be completed during Part 1 and used as the guide for the API that will be built in Part 2.

The Part 2 API will be developed in C# using ASP.NET Core. Part 1 contains the endpoint design only and does not include application code.

## 2. API conventions

- Every route starts with `/api/`.
- The API sends and receives JSON.
- HTTPS must be used when the API is deployed.
- Protected endpoints require a valid bearer access token.
- Authentication identifies the signed-in user. Authorisation determines whether that user may perform an action (Microsoft, 2026b; Microsoft, 2026c).
- A `401 Unauthorized` response means that valid authentication credentials were not supplied.
- A `403 Forbidden` response means that the user is authenticated but is not permitted to perform the action (Fielding, Nottingham and Reschke, 2022).
- Dates and times use ISO 8601 format and are stored or exchanged in UTC where a time value is required.
- Passwords and password hashes are never returned in a response.
- Validation and error responses use one consistent JSON structure.
- Resource creation responses use `201 Created` and include the created resource or a safe summary of it. ASP.NET Core can also include a `Location` header for the new resource (Microsoft, 2026a).

## 3. Role and access terms

| Term | Meaning |
|---|---|
| None (public) | The endpoint can be called without signing in. |
| Any authenticated user | A signed-in Organiser or Participant can call the endpoint. |
| Organiser | Only a signed-in user with the Organiser role can call the endpoint. |
| Participant | Only a signed-in user with the Participant role can call the endpoint. |

The role shown in the plan is the minimum access requirement. Ownership checks are additional rules. For example, an Organiser endpoint may still check that the Organiser manages the affected event.

## 4. Common error response

The future API will return a consistent error body so that the client can display useful feedback.

```json
{
  "status": 400,
  "title": "Validation failed",
  "errors": {
    "email": ["A valid email address is required."]
  }
}
```

The exact wording may change during implementation, but the documented HTTP status codes and reasons must remain consistent with this plan.

## 5. Authentication endpoints

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Creates a new Participant account. Public registration cannot create an Organiser account. The email address is normalised and checked for uniqueness before the account is created. | None (public) | `firstName` string, required<br>`lastName` string, required<br>`email` string, required<br>`password` string, required<br>`phoneNumber` string, optional<br>`dateOfBirth` date, optional | `201 Created`: Participant account summary and a `Location` header for the profile resource.<br>`400 Bad Request`: Missing, incorrectly formatted or invalid field values.<br>`409 Conflict`: An account already uses the supplied email address. |
| POST | `/api/auth/login` | Checks the supplied credentials and returns an access token with a safe user summary. Both Organisers and Participants use this endpoint. | None (public) | `email` string, required<br>`password` string, required | `200 OK`: Access token, expiry time and user summary.<br>`400 Bad Request`: Required fields are missing or incorrectly formatted.<br>`401 Unauthorized`: The email address or password is incorrect.<br>`403 Forbidden`: The account exists but is inactive. |

### 5.1 Registration response example

```json
{
  "userId": 5,
  "firstName": "Naledi",
  "lastName": "Mokoena",
  "email": "naledi.mokoena@example.com",
  "role": "Participant",
  "isActive": true
}
```

### 5.2 Login response example

```json
{
  "accessToken": "token-value-returned-by-the-api",
  "expiresAt": "2026-09-19T15:30:00Z",
  "user": {
    "userId": 5,
    "firstName": "Naledi",
    "lastName": "Mokoena",
    "email": "naledi.mokoena@example.com",
    "role": "Participant"
  }
}
```

## 6. User profile endpoints

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/profile` | Returns the profile of the signed-in user. The user identity is taken from the access token, so a user ID is not accepted in the route. | Any authenticated user | None | `200 OK`: The signed-in user's profile.<br>`401 Unauthorized`: The access token is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive.<br>`404 Not Found`: The account identified by the token no longer exists. |
| PUT | `/api/profile` | Updates the permitted profile fields of the signed-in user. The endpoint cannot be used to change a role, account status, email address or password. | Any authenticated user | `firstName` string, required<br>`lastName` string, required<br>`phoneNumber` string, optional<br>`dateOfBirth` date, optional | `200 OK`: The updated profile.<br>`400 Bad Request`: Missing, incorrectly formatted or invalid field values.<br>`401 Unauthorized`: The access token is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive.<br>`404 Not Found`: The signed-in account no longer exists. |
| PUT | `/api/profile/password` | Changes the signed-in user's password after checking the current password. The new password is stored as a secure hash and is never returned. | Any authenticated user | `currentPassword` string, required<br>`newPassword` string, required<br>`confirmPassword` string, required | `204 No Content`: The password was changed successfully.<br>`400 Bad Request`: The current password is incorrect, the new passwords do not match or the new password is invalid.<br>`401 Unauthorized`: The access token is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive.<br>`404 Not Found`: The signed-in account no longer exists. |

### 6.1 Profile response example

```json
{
  "userId": 5,
  "firstName": "Naledi",
  "lastName": "Mokoena",
  "email": "naledi.mokoena@example.com",
  "phoneNumber": "0821234567",
  "dateOfBirth": "1994-06-12",
  "role": "Participant",
  "isActive": true,
  "createdAt": "2026-09-19T12:00:00Z"
}
```

### 6.2 Profile access decisions

- Profile routes do not contain a user ID. The future API will obtain the current user's identity from the validated access token.
- An authenticated user can retrieve and update only their own profile through these routes.
- A user cannot change their own role or account status.
- An email address cannot be changed through the general profile update endpoint. A separate verified process would be needed if email changes are added later.
- A password change requires the current password and matching new-password fields.
- The API will validate request bodies before accepting changes. ASP.NET Core supports automatic validation responses when API controller conventions are used (Microsoft, 2026a).

## References

Fielding, R., Nottingham, M. and Reschke, J. (2022) *HTTP Semantics*. RFC 9110. Available at: https://www.rfc-editor.org/rfc/rfc9110.html (Accessed: 19 September 2026).

Microsoft (2026a) 'Controller action return types in ASP.NET Core web API', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types?view=aspnetcore-8.0 (Accessed: 19 September 2026).

Microsoft (2026b) 'Overview of ASP.NET Core Authentication', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-10.0 (Accessed: 19 September 2026).

Microsoft (2026c) 'Introduction to authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/introduction?view=aspnetcore-8.0 (Accessed: 19 September 2026).

The Independent Institute of Education (2026) *PROG6212 Portfolio of Evidence*. Unpublished assessment brief.
