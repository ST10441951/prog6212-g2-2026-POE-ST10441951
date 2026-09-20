# RaceDay API Endpoint Plan

## 1. Purpose

This document plans the REST API endpoints for RaceDay before application development begins. It is completed during Part 1 and will guide the API built in Part 2 and consumed by the MVC application in Part 3.

The Part 2 API will be developed in C# using ASP.NET Core. Part 1 contains the endpoint design only and does not include application code.

## 2. API conventions

- Every route starts with `/api/`.
- The API sends and receives JSON.
- HTTPS must be used when the API is deployed.
- Protected endpoints require a valid authenticated server-side session.
- The session stores the signed-in user's identifier and role for later requests. Passwords and password hashes are never stored in the session.
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
| POST | `/api/auth/register` | Creates a new Organiser or Participant account using the selected role. The email address is normalised and checked for uniqueness. Only the two seeded role names are accepted, and an unrestricted role identifier is never accepted from the request. | None (public) | `firstName` string, required<br>`lastName` string, required<br>`email` string, required<br>`password` string, required<br>`role` string, required: Organiser or Participant<br>`phoneNumber` string, optional<br>`dateOfBirth` date, optional | `201 Created`: Safe account summary and a `Location` header for the profile resource.<br>`400 Bad Request`: A required field, role or other value is missing or invalid.<br>`409 Conflict`: An account already uses the supplied email address. |
| POST | `/api/auth/login` | Checks the supplied credentials and creates a server-side session containing the user's identifier and role. Both Organisers and Participants use this endpoint. | None (public) | `email` string, required<br>`password` string, required | `200 OK`: Safe user summary and session expiry time. A secure session cookie is established.<br>`400 Bad Request`: Required fields are missing or incorrectly formatted.<br>`401 Unauthorized`: The email address or password is incorrect.<br>`403 Forbidden`: The account exists but is inactive. |
| POST | `/api/auth/logout` | Ends the signed-in user's session so that the existing session cookie can no longer access protected endpoints. | Any authenticated user | None | `204 No Content`: The session was ended.<br>`401 Unauthorized`: No valid authenticated session exists. |

### 5.1 Registration response example

```json
{
  "userId": 7,
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
  "sessionExpiresAt": "2026-09-19T15:30:00Z",
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
| GET | `/api/profile` | Returns the profile of the signed-in user. The user identity is taken from the session, so a user ID is not accepted in the route. | Any authenticated user | None | `200 OK`: The signed-in user's profile.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive.<br>`404 Not Found`: The account identified by the session no longer exists. |
| PUT | `/api/profile` | Updates the permitted profile fields of the signed-in user. The endpoint cannot be used to change a role, account status, email address or password. | Any authenticated user | `firstName` string, required<br>`lastName` string, required<br>`phoneNumber` string, optional<br>`dateOfBirth` date, optional | `200 OK`: The updated profile.<br>`400 Bad Request`: Missing, incorrectly formatted or invalid field values.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive.<br>`404 Not Found`: The signed-in account no longer exists. |
| PUT | `/api/profile/password` | Changes the signed-in user's password after checking the current password. The new password is stored as a secure hash and is never returned. | Any authenticated user | `currentPassword` string, required<br>`newPassword` string, required<br>`confirmPassword` string, required | `204 No Content`: The password was changed successfully.<br>`400 Bad Request`: The current password is incorrect, the new passwords do not match or the new password is invalid.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive.<br>`404 Not Found`: The signed-in account no longer exists. |
| PUT | `/api/profile/picture` | Uploads or replaces the signed-in Participant's profile picture. The API validates the image, uploads it to Azure Blob Storage and stores only the generated blob name in the User record. | Participant | `imageFile` binary image, required as `multipart/form-data` | `200 OK`: Updated profile picture URL.<br>`400 Bad Request`: The file is missing or invalid.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive or does not have the Participant role.<br>`413 Content Too Large`: The image exceeds the configured size limit.<br>`415 Unsupported Media Type`: The file type is not permitted. |
| DELETE | `/api/profile/picture` | Removes the signed-in Participant's profile picture from Azure Blob Storage and clears the stored blob name. | Participant | None | `204 No Content`: The picture was removed or no picture was stored.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive or does not have the Participant role. |

### 6.1 Profile response example

```json
{
  "userId": 5,
  "firstName": "Naledi",
  "lastName": "Mokoena",
  "email": "naledi.mokoena@example.com",
  "phoneNumber": "0821234567",
  "dateOfBirth": "1994-06-12",
  "profilePictureUrl": "https://storage.example/profile-pictures/user-5.jpg",
  "role": "Participant",
  "isActive": true,
  "createdAt": "2026-09-19T12:00:00Z"
}
```

### 6.2 Profile access decisions

- Profile routes do not contain a user ID. The future API will obtain the current user's identity from the validated session.
- An authenticated user can retrieve and update only their own profile through these routes.
- A user cannot change their own role or account status.
- An email address cannot be changed through the general profile update endpoint. A separate verified process would be needed if email changes are added later.
- A password change requires the current password and matching new-password fields.
- Profile picture files are sent only to the API. The API stores the image in Azure Blob Storage and saves the generated blob name in `Users.ProfilePictureBlobName`.
- The API returns a usable profile picture URL in the response. The MVC application does not receive or store Azure Storage credentials.
- The API will validate request bodies before accepting changes. ASP.NET Core supports automatic validation responses when API controller conventions are used (Microsoft, 2026a).

## 7. Event endpoints

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events` | Returns a paged list of upcoming public events. Optional query values may filter the list by event type, province or date. Draft and cancelled events are not included in the public list. | None (public) | None. Optional query values: `page`, `pageSize`, `eventType`, `province`, `fromDate`, `toDate`. | `200 OK`: Paged event summaries and paging information.<br>`400 Bad Request`: A filter, date range or paging value is invalid. |
| GET | `/api/events/{eventId}` | Returns the public details of one event, including its available categories and route summaries. | None (public) | None | `200 OK`: Event details, available categories and route summaries.<br>`404 Not Found`: The event does not exist or is not publicly available. |
| GET | `/api/organiser/dashboard` | Returns the signed-in Organiser's event summary, including each event's total enrolments and upcoming event dates. | Organiser | None | `200 OK`: Dashboard totals and event summaries.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive or does not have the Organiser role. |
| GET | `/api/events/mine` | Returns all events managed by the signed-in Organiser, including Draft, Closed, Cancelled and Completed events. | Organiser | None. Optional query values: `page`, `pageSize`, `status`. | `200 OK`: Paged summaries of the Organiser's events.<br>`400 Bad Request`: A status or paging value is invalid.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive or does not have the Organiser role. |
| POST | `/api/events` | Creates a Draft event for the signed-in Organiser. The Organiser is identified from the session and cannot be selected in the request body. | Organiser | `eventName` string, required<br>`eventType` string, required: Running, Walking or Cycling<br>`description` string, required<br>`distanceKm` decimal, required<br>`eventDateTime` date-time, required<br>`entryClosingDateTime` date-time, required<br>`venueName` string, required<br>`addressLine1` string, required<br>`city` string, required<br>`province` string, required<br>`latitude` decimal, optional<br>`longitude` decimal, optional | `201 Created`: Created Draft event and a `Location` header for `/api/events/{eventId}`.<br>`400 Bad Request`: A required field or business-rule value is invalid.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive or does not have the Organiser role. |
| PUT | `/api/events/{eventId}` | Replaces the editable details of an event managed by the signed-in Organiser. It may also move the event to a valid status when its required data and categories are ready. | Organiser and event owner | `eventName` string, required<br>`eventType` string, required: Running, Walking or Cycling<br>`description` string, required<br>`distanceKm` decimal, required<br>`eventDateTime` date-time, required<br>`entryClosingDateTime` date-time, required<br>`venueName` string, required<br>`addressLine1` string, required<br>`city` string, required<br>`province` string, required<br>`latitude` decimal, optional<br>`longitude` decimal, optional<br>`status` string, required: Draft, Open, Closed, Cancelled or Completed | `200 OK`: Updated event details.<br>`400 Bad Request`: A required field, distance, date, location, coordinate or status value is invalid.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive, is not an Organiser or does not manage the event.<br>`404 Not Found`: The event does not exist.<br>`409 Conflict`: The requested status change conflicts with the event's current data or state. |
| PUT | `/api/events/{eventId}/banner` | Uploads or replaces the banner image for an event managed by the signed-in Organiser. The API validates the image, uploads it to Azure Blob Storage and stores only the generated blob name in the Event record. | Organiser and event owner | `imageFile` binary image, required as `multipart/form-data` | `200 OK`: Updated event banner URL.<br>`400 Bad Request`: The file is missing or invalid.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive, is not an Organiser or does not manage the event.<br>`404 Not Found`: The event does not exist.<br>`413 Content Too Large`: The image exceeds the configured size limit.<br>`415 Unsupported Media Type`: The file type is not permitted. |
| DELETE | `/api/events/{eventId}/banner` | Removes the banner image for an event managed by the signed-in Organiser and clears the stored blob name. | Organiser and event owner | None | `204 No Content`: The banner was removed or no banner was stored.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive, is not an Organiser or does not manage the event.<br>`404 Not Found`: The event does not exist. |
| DELETE | `/api/events/{eventId}` | Permanently deletes an event managed by the signed-in Organiser only when no categories, enrolments or results depend on it. An event with dependent records must be cancelled through the update endpoint instead. | Organiser and event owner | None | `204 No Content`: The event was deleted.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive, is not an Organiser or does not manage the event.<br>`404 Not Found`: The event does not exist.<br>`409 Conflict`: Related records prevent deletion and the event must be cancelled instead. |

### 7.1 Event summary response example

```json
{
  "eventId": 12,
  "eventName": "Durban Sunrise 10K",
  "eventType": "Running",
  "bannerImageUrl": "https://storage.example/event-banners/event-12.jpg",
  "distanceKm": 10.00,
  "eventDateTime": "2026-11-14T04:30:00Z",
  "entryClosingDateTime": "2026-11-07T21:59:59Z",
  "venueName": "Moses Mabhida Stadium",
  "city": "Durban",
  "province": "KwaZulu-Natal",
  "status": "Open"
}
```

### 7.2 Event design decisions

- Public browsing returns only events that visitors are allowed to see.
- The Organiser's own list includes every status so that Draft and historical events can still be managed.
- A new event starts in Draft status. It must have at least one category before it can be opened for enrolment.
- The entry closing date and time must be earlier than the event date and time.
- The advertised event distance must be greater than zero.
- Banner image files are sent only to the API. The API stores them in Azure Blob Storage and saves the generated blob name in `Events.BannerImageBlobName`.
- Creating or editing an event in the MVC interface may save the event details first and then call the banner endpoint when an image was selected.
- Latitude and longitude are optional, but both should be supplied together when coordinates are used.
- Ownership is checked using the signed-in Organiser's user ID and the event's `organiserUserId` value.
- Existing event history is protected. An event with dependent records is cancelled instead of being permanently deleted.

## 8. Event Category endpoints

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events/{eventId}/categories` | Returns the available categories for a public event. Each category includes its route summary when a route has been added. | None (public) | None | `200 OK`: List of available categories and route summaries.<br>`404 Not Found`: The event does not exist or is not publicly available. |
| GET | `/api/events/{eventId}/categories/{categoryId}` | Returns one available category and its route details for a public event. | None (public) | None | `200 OK`: Category and route details.<br>`404 Not Found`: The event or category does not exist, the category does not belong to the event or the record is not publicly available. |
| GET | `/api/organiser/events/{eventId}/categories` | Returns every category for an event managed by the signed-in Organiser, including unavailable categories. | Organiser and event owner | None | `200 OK`: List of all categories and route summaries for the event.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive, is not an Organiser or does not manage the event.<br>`404 Not Found`: The event does not exist. |
| POST | `/api/events/{eventId}/categories` | Creates an age category, distance category or combined category for an event managed by the signed-in Organiser. | Organiser and event owner | `categoryName` string, required<br>`distanceKm` decimal, required<br>`minimumAge` integer, optional<br>`maximumAge` integer, optional<br>`entryFee` decimal, required<br>`capacity` integer, optional<br>`isAvailable` boolean, required | `201 Created`: Created category and a `Location` header for `/api/events/{eventId}/categories/{categoryId}`.<br>`400 Bad Request`: A field value or age range is missing or invalid.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive, is not an Organiser or does not manage the event.<br>`404 Not Found`: The event does not exist.<br>`409 Conflict`: The event already has a category with the supplied name. |
| PUT | `/api/events/{eventId}/categories/{categoryId}` | Replaces the editable details of an age category, distance category or combined category belonging to an event managed by the signed-in Organiser. Setting `isAvailable` to false prevents new enrolments while preserving the category. | Organiser and event owner | `categoryName` string, required<br>`distanceKm` decimal, required<br>`minimumAge` integer, optional<br>`maximumAge` integer, optional<br>`entryFee` decimal, required<br>`capacity` integer, optional<br>`isAvailable` boolean, required | `200 OK`: Updated category details.<br>`400 Bad Request`: A field value or age range is missing or invalid.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive, is not an Organiser or does not manage the event.<br>`404 Not Found`: The event or category does not exist, or the category does not belong to the event.<br>`409 Conflict`: The updated name duplicates another category for the event or the change conflicts with existing enrolments. |
| DELETE | `/api/events/{eventId}/categories/{categoryId}` | Permanently deletes a category only when it belongs to an event managed by the signed-in Organiser and has no enrolments. A category with enrolments must be made unavailable instead. | Organiser and event owner | None | `204 No Content`: The category was deleted.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive, is not an Organiser or does not manage the event.<br>`404 Not Found`: The event or category does not exist, or the category does not belong to the event.<br>`409 Conflict`: Enrolments depend on the category, so it must be made unavailable instead. |

### 8.1 Category response example

```json
{
  "eventCategoryId": 31,
  "eventId": 12,
  "categoryName": "10 km Run",
  "distanceKm": 10.00,
  "minimumAge": 15,
  "maximumAge": null,
  "entryFee": 180.00,
  "capacity": 2500,
  "isAvailable": true,
  "route": {
    "routeId": 18,
    "routeName": "Stadium Coastal Loop",
    "startLocation": "Moses Mabhida Stadium",
    "finishLocation": "Moses Mabhida Stadium",
    "routeMapUrl": "https://example.org/routes/durban-sunrise-10k",
    "elevationGainMetres": 85
  }
}
```

## 9. Event Route endpoints

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events/{eventId}/categories/{categoryId}/route` | Returns the route information for an available category in a public event. | None (public) | None | `200 OK`: Route details.<br>`404 Not Found`: The event, category or route does not exist, the category does not belong to the event or the record is not publicly available. |
| POST | `/api/events/{eventId}/categories/{categoryId}/route` | Creates the route for a category belonging to an event managed by the signed-in Organiser. A category may have no more than one route. | Organiser and event owner | `routeName` string, required<br>`startLocation` string, required<br>`finishLocation` string, required<br>`routeDescription` string, required<br>`routeMapUrl` string, optional<br>`elevationGainMetres` integer, optional | `201 Created`: Created route and a `Location` header for the route resource.<br>`400 Bad Request`: A field value or URL is invalid.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive, is not an Organiser or does not manage the event.<br>`404 Not Found`: The event or category does not exist, or the category does not belong to the event.<br>`409 Conflict`: The category already has a route. |
| PUT | `/api/events/{eventId}/categories/{categoryId}/route` | Replaces the route details for a category belonging to an event managed by the signed-in Organiser. | Organiser and event owner | `routeName` string, required<br>`startLocation` string, required<br>`finishLocation` string, required<br>`routeDescription` string, required<br>`routeMapUrl` string, optional<br>`elevationGainMetres` integer, optional | `200 OK`: Updated route details.<br>`400 Bad Request`: A field value or URL is invalid.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive, is not an Organiser or does not manage the event.<br>`404 Not Found`: The event, category or route does not exist, or the category does not belong to the event. |
| DELETE | `/api/events/{eventId}/categories/{categoryId}/route` | Deletes the route for a category belonging to an event managed by the signed-in Organiser. The category remains in the system. | Organiser and event owner | None | `204 No Content`: The route was deleted.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive, is not an Organiser or does not manage the event.<br>`404 Not Found`: The event, category or route does not exist, or the category does not belong to the event. |

### 9.1 Category and route design decisions

- Category routes are nested under an event so that the API can confirm that the category belongs to the stated event.
- Category names must be unique within the same event.
- Distance must be greater than zero. Entry fees cannot be negative.
- Minimum age must be from 1 to 120 when supplied. Capacity must be greater than zero when supplied.
- Maximum age must be from 1 to 120 when supplied, and minimum age cannot exceed maximum age.
- An unavailable category remains visible to its Organiser but is excluded from public category lists and new enrolments.
- A category with enrolments cannot be permanently deleted because its history must be preserved.
- Each category may have zero or one route. `POST` creates that route and `PUT` updates the existing route.
- Route elevation gain cannot be negative when it is supplied.

## 10. Enrolment endpoints

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/enrolments` | Enters the signed-in Participant into an Open event using a selected available category. The Participant identity is obtained from the session. | Participant | `eventId` integer, required<br>`eventCategoryId` integer, required | `201 Created`: Confirmed enrolment and a `Location` header for `/api/enrolments/{enrolmentId}`.<br>`400 Bad Request`: The request is invalid or the Participant does not meet the category's minimum age.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive or does not have the Participant role.<br>`404 Not Found`: The event or category does not exist, or the category does not belong to the event.<br>`409 Conflict`: The Participant already has an active enrolment, entries are closed, the event is not Open, or the category is unavailable or full. |
| GET | `/api/enrolments/mine` | Returns the signed-in Participant's enrolment history. Optional query values may filter the list by status or event. | Participant | None. Optional query values: `page`, `pageSize`, `status`, `eventId`. | `200 OK`: Paged enrolment summaries and paging information.<br>`400 Bad Request`: A filter or paging value is invalid.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive or does not have the Participant role. |
| GET | `/api/enrolments/{enrolmentId}` | Returns one enrolment. Access is limited to the Participant who owns it or the Organiser who manages its event. | Participant owner or Organiser event owner | None | `200 OK`: Enrolment, event, category and optional result summary.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive or the user does not own the enrolment or manage its event.<br>`404 Not Found`: The enrolment does not exist. |
| PUT | `/api/enrolments/{enrolmentId}/cancel` | Changes a Confirmed enrolment owned by the signed-in Participant to Cancelled without deleting its history. | Participant owner | None | `200 OK`: Updated enrolment with Cancelled status.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive, is not a Participant or does not own the enrolment.<br>`404 Not Found`: The enrolment does not exist.<br>`409 Conflict`: The enrolment is already cancelled or its event state prevents cancellation. |
| PUT | `/api/enrolments/{enrolmentId}/reactivate` | Reactivates a Cancelled enrolment owned by the signed-in Participant and confirms the category to be used. The normal event and category checks are repeated. | Participant owner | `eventCategoryId` integer, required | `200 OK`: Reactivated Confirmed enrolment.<br>`400 Bad Request`: The request is invalid or the Participant does not meet the category's minimum age.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive, is not a Participant or does not own the enrolment.<br>`404 Not Found`: The enrolment, event or category does not exist, or the category does not belong to the event.<br>`409 Conflict`: The enrolment is already Confirmed, entries are closed, the event is not Open, or the category is unavailable or full. |
| GET | `/api/events/{eventId}/enrolments` | Returns enrolments for an event managed by the signed-in Organiser. Optional query values may filter the list by status or category. | Organiser and event owner | None. Optional query values: `page`, `pageSize`, `status`, `eventCategoryId`. | `200 OK`: Paged enrolment details and paging information.<br>`400 Bad Request`: A filter or paging value is invalid.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive, is not an Organiser or does not manage the event.<br>`404 Not Found`: The event does not exist. |
| PUT | `/api/enrolments/{enrolmentId}/bib-number` | Assigns or corrects the bib number for an enrolment in an event managed by the signed-in Organiser. | Organiser and event owner | `bibNumber` string, required | `200 OK`: Updated enrolment with its bib number.<br>`400 Bad Request`: The bib number is blank or invalid.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive, is not an Organiser or does not manage the related event.<br>`404 Not Found`: The enrolment does not exist.<br>`409 Conflict`: The bib number is already assigned within the event. |

### 10.1 Enrolment response example

```json
{
  "enrolmentId": 44,
  "participantUserId": 5,
  "eventId": 12,
  "eventCategoryId": 31,
  "enrolmentDate": "2026-09-19T13:10:00Z",
  "status": "Confirmed",
  "bibNumber": "A1042"
}
```

### 10.2 Enrolment design decisions

- The Participant user ID is read from the session and is not accepted in the enrolment request body.
- The selected category must belong to the selected event.
- New and reactivated enrolments require an Open event, a future closing date and an available category with remaining capacity.
- A Participant has one enrolment record per event. A cancelled record is reactivated instead of creating a duplicate row.
- Cancelling an enrolment changes its status and preserves the original record.
- Bib numbers are optional until assigned, but an assigned value must be unique within the event.
- Cancellation and reactivation are retained as useful additional endpoints after the Part 2 requirements cross-check.

## 11. Result endpoints

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/results/mine` | Returns the signed-in Participant's personal result history. Optional query values may filter the list by result status or event. | Participant | None. Optional query values: `page`, `pageSize`, `resultStatus`, `eventId`. | `200 OK`: Paged personal results and paging information.<br>`400 Bad Request`: A filter or paging value is invalid.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive or does not have the Participant role. |
| GET | `/api/enrolments/{enrolmentId}/result` | Returns the official result for one enrolment. Access is limited to the Participant who owns the enrolment or the Organiser who manages its event. | Participant owner or Organiser event owner | None | `200 OK`: Official result with its enrolment, event and category summary.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive or the user does not own the enrolment or manage its event.<br>`404 Not Found`: The enrolment or result does not exist. |
| GET | `/api/events/{eventId}/results` | Returns official results for an event managed by the signed-in Organiser. Optional query values may filter the list by result status or category. | Organiser and event owner | None. Optional query values: `page`, `pageSize`, `resultStatus`, `eventCategoryId`. | `200 OK`: Paged event results and paging information.<br>`400 Bad Request`: A filter or paging value is invalid.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive, is not an Organiser or does not manage the event.<br>`404 Not Found`: The event does not exist. |
| POST | `/api/enrolments/{enrolmentId}/result` | Records the first official result for a Confirmed enrolment in an event managed by the signed-in Organiser. The recording Organiser is obtained from the session. | Organiser and event owner | `resultStatus` string, required: Completed, DidNotFinish, Disqualified or DidNotStart<br>`finishTimeSeconds` integer, required only for Completed<br>`overallPosition` integer, optional<br>`categoryPosition` integer, optional<br>`notes` string, optional | `201 Created`: Recorded result and a `Location` header for the enrolment's result resource.<br>`400 Bad Request`: A status, time, position or notes value is invalid.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive, is not an Organiser or does not manage the related event.<br>`404 Not Found`: The enrolment does not exist.<br>`409 Conflict`: The enrolment is cancelled or already has an official result. |
| PUT | `/api/enrolments/{enrolmentId}/result` | Corrects the existing official result for an enrolment in an event managed by the signed-in Organiser. The existing record is updated rather than replaced with a second result. | Organiser and event owner | `resultStatus` string, required: Completed, DidNotFinish, Disqualified or DidNotStart<br>`finishTimeSeconds` integer, required only for Completed<br>`overallPosition` integer, optional<br>`categoryPosition` integer, optional<br>`notes` string, optional | `200 OK`: Corrected result details.<br>`400 Bad Request`: A status, time, position or notes value is invalid.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive, is not an Organiser or does not manage the related event.<br>`404 Not Found`: The enrolment or result does not exist.<br>`409 Conflict`: The enrolment is cancelled. |

### 11.1 Result response example

```json
{
  "resultId": 21,
  "enrolmentId": 44,
  "recordedByUserId": 2,
  "eventName": "Durban Sunrise 10K",
  "eventDateTime": "2026-11-14T04:30:00Z",
  "categoryName": "10 km Run",
  "resultStatus": "Completed",
  "finishTimeSeconds": 2874,
  "overallPosition": 42,
  "totalFinishers": 312,
  "categoryPosition": 10,
  "notes": null,
  "recordedAt": "2026-11-14T06:15:00Z",
  "updatedAt": null
}
```

### 11.2 Result design decisions

- The recording Organiser is identified from the session and is not accepted in the request body.
- The Organiser must manage the event connected to the enrolment.
- A cancelled enrolment cannot receive a result.
- Each enrolment may have no more than one official result.
- A Completed result requires a positive `finishTimeSeconds` value.
- DidNotFinish, Disqualified and DidNotStart results do not have an official finish time.
- Overall and category positions must be positive when supplied.
- Personal result responses include the Event name, Event date, selected Category, finish time, finishing position and total number of finishers required by the Part 3 My Results page.
- Corrections update the existing result and its `updatedAt` value. Official results are not deleted through the API.
- A Participant can view only results connected to their own enrolments.

## 12. Part 2 implementation and verification requirements

- The API will be built in C# as an ASP.NET Core Web API project.
- Entity Framework Core will use the Code-First approach. The entity classes and Fluent API configuration must reproduce the Part 1 ERD and SQL constraints. EF Core migrations provide a tracked way to keep the database schema aligned with the application model (Microsoft, 2026e).
- Login will create a server-side session containing the signed-in user's identifier and role. ASP.NET Core session state is intended for retaining user data while a user interacts with an application (Microsoft, 2026d).
- Protected endpoints will return `401 Unauthorized` when no valid session exists. Role and ownership failures will return `403 Forbidden`.
- Swagger UI must display and allow browser testing of every implemented endpoint. Each operation must describe its purpose, parameters or request body, success response and relevant failure responses. Swagger tooling can generate interactive API documentation from ASP.NET Core endpoint information (Microsoft, 2026f).
- Unit tests must cover successful and failed registration and login, authenticated and unauthenticated requests, Organiser event management, rejection of the wrong role, Participant enrolment and correct persistence of the enrolment relationship.
- The Part 2 GitHub Actions workflow must build the solution and run all tests on each push. The `dotnet test` command runs the configured test projects and can be used in an automated workflow (Microsoft, 2026g).

## 13. Part 3 MVC, storage and container requirements

- The ASP.NET Core MVC application will consume the Part 2 API for every data operation. It will not use the database directly or duplicate API business rules.
- Registration, login and logout in MVC will call the API. The MVC application will maintain only the safe identity, role and API session information needed for the current user. Passwords will never be stored in MVC session state.
- Organiser and Participant pages will have distinct role-appropriate navigation. Protected MVC actions will reject unauthenticated users and redirect them to login.
- The Organiser dashboard will use `/api/organiser/dashboard` for event totals, enrolment totals and upcoming dates.
- Participant pages will use the public Event endpoints, Participant Enrolment endpoints, `/api/results/mine` and the Profile endpoints.
- Image files will always pass from MVC to the API. Only the API will communicate with Azure Blob Storage. The Azure Storage client library for .NET supports uploading blob content from streams, files and other data sources (Microsoft, 2026h).
- Profile and Event responses will contain image URLs generated by the API from their stored blob names.
- Image validation will restrict accepted content types and file sizes before upload. Storage credentials will be kept in API configuration and will not be committed to GitHub.
- The MVC project will include a well-structured multi-stage Dockerfile and one documented Docker run command. Microsoft documents separate SDK and ASP.NET runtime images for building and running containerised ASP.NET Core applications (Microsoft, 2026i).
- The Part 3 GitHub Actions workflow will build the API, tests and MVC project on each push.
- Part 3 will retain the Part 2 API unit tests and add suitable MVC or integration checks where practical.

## 14. Endpoint-plan coverage

The endpoint plan covers every resource group named in the supplied Part 1, Part 2 and Part 3 briefs: Authentication, User Profile, Events, Event Categories, Event Routes, Enrolments, Results, image uploads and the Organiser dashboard. Every endpoint records its method, route, description, required role, request body and expected success and failure responses.

The supplied Part 2 and Part 3 functional-requirement pages were cross-checked on 20 September 2026. Registration supports both required roles, authentication uses sessions, logout is planned, Events include distance and banner images, Categories support upper and lower age limits, Participants can upload a profile picture, and the Swagger, testing, MVC, Blob Storage and Docker expectations are recorded. The endpoint plan is complete for Part 1 and is the specification for later development.

## References

Fielding, R., Nottingham, M. and Reschke, J. (2022) *HTTP Semantics*. RFC 9110. Available at: https://www.rfc-editor.org/rfc/rfc9110.html (Accessed: 19 September 2026).

Microsoft (2026a) 'Controller action return types in ASP.NET Core web API', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types?view=aspnetcore-8.0 (Accessed: 19 September 2026).

Microsoft (2026b) 'Overview of ASP.NET Core Authentication', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-10.0 (Accessed: 19 September 2026).

Microsoft (2026c) 'Introduction to authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/introduction?view=aspnetcore-8.0 (Accessed: 19 September 2026).

Microsoft (2026d) 'Session and state management in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-10.0 (Accessed: 20 September 2026).

Microsoft (2026e) 'Migrations overview', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/ (Accessed: 20 September 2026).

Microsoft (2026f) 'ASP.NET Core web API documentation with Swagger/OpenAPI', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/tutorials/web-api-help-pages-using-swagger?view=aspnetcore-8.0 (Accessed: 20 September 2026).

Microsoft (2026g) 'Unit testing C# in .NET using dotnet test and xUnit', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-csharp-with-xunit (Accessed: 20 September 2026).

Microsoft (2026h) 'Get started with Azure Blob Storage and .NET', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-dotnet-get-started (Accessed: 20 September 2026).

Microsoft (2026i) 'Run an ASP.NET Core app in Docker containers', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/building-net-docker-images?view=aspnetcore-10.0 (Accessed: 20 September 2026).

The Independent Institute of Education (2026) *PROG6212 Portfolio of Evidence*. Unpublished assessment brief.
