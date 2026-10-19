# RaceDay API Endpoint Plan

## 1. Purpose

This plan was prepared before API development and corrected on 17 October 2026. All 40 business endpoints are implemented in Part 2; Part 3 adds MVC.

## 2. API conventions

- Business routes start with `/api/`.
- Use JSON, except multipart image uploads and empty 204 responses.
- Protected operations use a server-side session containing user ID and role, never passwords.
- 401 means no valid session; 403 means insufficient role or ownership.
- Dates use ISO 8601 and UTC. Responses exclude passwords and hashes.
- Validation uses JSON problem responses. Creation returns 201 with a resource or safe summary and Location.
- Use HTTPS when deployed (Microsoft, 2026a; 2026b; 2026c; Fielding, Nottingham and Reschke, 2022).

`GET /health` is a separate public host check: 200 with `{"status":"Healthy"}`. It does not query SQL or storage.

## 3. Role and access terms

| Term | Meaning |
|---|---|
| None (public) | The endpoint can be called without signing in. |
| Any authenticated user | A signed-in Organiser or Participant can call the endpoint. |
| Organiser | Only a signed-in user with the Organiser role can call the endpoint. |
| Participant | Only a signed-in user with the Participant role can call the endpoint. |

The role shown in the plan is the minimum access requirement. Ownership checks are additional rules. For example, an Organiser endpoint may still check that the Organiser manages the affected event.

## 4. Common error response

Validation errors use a JSON problem response:

```json
{
  "status": 400,
  "title": "Validation failed",
  "errors": {
    "email": ["A valid email address is required."]
  }
}
```

Error wording may vary; documented status codes and rules still apply.

## 5. Authentication endpoints

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Creates a new Organiser or Participant account using the selected role. The email address is normalised and checked for uniqueness. Only the two seeded role names are accepted, and an unrestricted role identifier is never accepted from the request. | None (public) | `firstName` string, required<br>`lastName` string, required<br>`email` string, required<br>`password` string, required<br>`role` string, required: Organiser or Participant<br>`phoneNumber` string, optional<br>`dateOfBirth` date, optional | `201 Created`: Safe account summary and a `Location` header for the profile resource.<br>`400 Bad Request`: A required field, role or other value is missing or invalid.<br>`409 Conflict`: An account already uses the supplied email address. |
| POST | `/api/auth/login` | Checks the supplied credentials and creates a server-side session containing the user's identifier and role. Both Organisers and Participants use this endpoint. | None (public) | `email` string, required<br>`password` string, required | `200 OK`: Safe user summary and session expiry time. A secure session cookie is established.<br>`400 Bad Request`: Required fields are missing or incorrectly formatted.<br>`401 Unauthorized`: The email address or password is incorrect.<br>`403 Forbidden`: The account exists but is inactive. |
| POST | `/api/auth/logout` | Ends the signed-in user's session so that the existing session cookie can no longer access protected endpoints. | Any authenticated user | None | `204 No Content`: The session was ended.<br>`401 Unauthorized`: No valid authenticated session exists.<br>`403 Forbidden`: The account has become inactive. |

Sessions last 20 minutes; a new login replaces that browser's previous session. See [authentication](Part-2-Authentication.md).

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
    "role": "Participant",
    "isActive": true
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

- Identity comes from the validated session; both roles manage only their own profile.
- Profile PUT rejects email, role, account status, password and image fields. It clears omitted optional phone/birth-date values.
- Password change verifies the current password, saves a hash and ends the current browser session. Other browsers retain their sessions until logout/expiry.
- Invalid fields return 400; non-JSON writes return 415. See [profiles](Part-2-Profiles.md).

### 6.3 Part 2 image implementation notes

Images use one multipart `imageFile`: JPEG/PNG, maximum 5 MiB before and after processing, 4096 pixels per side and 64 MiB decoded pixels. The request allows 64 KiB extra for form headers. Oversized files/bodies return 413, invalid images 400 and unsupported formats 415.

The API validates and re-encodes images, stores generated blob names in SQL and returns one-hour read-only URLs. Private storage failures return 503. Picture writes require the Participant; banner writes require the event owner. See [image rules and recovery](Part-2-Images.md). MVC upload pages remain Part 3.

## 7. Event endpoints

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events` | Returns a paged list of upcoming public events. Optional query values may filter the list by event type, province or date. Draft and cancelled events are not included in the public list. | None (public) | None. Optional query values: `page`, `pageSize`, `eventType`, `province`, `fromDate`, `toDate`. | `200 OK`: Paged event summaries and paging information.<br>`400 Bad Request`: A filter, date range or paging value is invalid. |
| GET | `/api/events/{eventId}` | Returns one public event. Its owning Organiser may also read a Draft or other non-public event. | Public; owning Organiser for non-public records | None | `200 OK`: Event details, categories and route summaries visible to the caller.<br>`404 Not Found`: Event missing or not visible to the caller. |
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

- New events start Draft. Owners can follow the creation Location; public callers cannot read Draft/Cancelled records.
- The upcoming list contains future Open/Closed events. Completed details remain public. Public reads show available categories; owners also see unavailable ones.
- Browse filters use type, province and inclusive UTC dates. Lists default to page 1/size 10, maximum 100, ordered by date then ID.
- Creation requires future start/closing times, closing before start, positive distance and paired optional coordinates. Use whole seconds, two decimal distance places and six coordinate places.
- Opening requires an available category and future dates. Draft may become Open/Cancelled; Open and Closed may switch or become Cancelled/Completed. Completion requires the start to have passed. Cancelled/Completed are terminal.
- Saved entries and terminal statuses protect dates, type and distance. Results prevent cancellation. Descriptive corrections remain allowed.
- Deletion requires no categories/entries and no banner. Cancel instead to keep history. Conflicts return 409.
- Writes share a serializable transaction and event update lock. The dashboard counts owned Confirmed/Cancelled entries separately.

See [browsing](Part-2-Event-Browsing.md) and [event management](Part-2-Event-Management.md).

## 8. Event Category endpoints

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events/{eventId}/categories` | Returns the available categories for a public event. Each category includes its route summary when a route has been added. | None (public) | None | `200 OK`: List of available categories and route summaries.<br>`404 Not Found`: The event does not exist or is not publicly available. |
| GET | `/api/events/{eventId}/categories/{categoryId}` | Returns one public available category. The owning Organiser may also read unavailable categories and categories in a Draft event. | Public; owning Organiser for non-public records | None | `200 OK`: Category and route details.<br>`404 Not Found`: Event or category missing, category belongs to another event or record is not visible to the caller. |
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

Category names are unique within an event. Public lists show available categories in Open/Closed/Completed events; owners use the full Organiser list or single-category reads for hidden records. PUT clears omitted optional fields. Saved entries protect distance, ages and fees. Capacity counts Confirmed entries only. An Open event must keep an available category. An unused category and its route can be deleted together. Conflicts return 409. See [categories](Part-2-Categories.md).

## 9. Event Route endpoints

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events/{eventId}/categories/{categoryId}/route` | Returns a public category's route. The owning Organiser may also read the route of a Draft event or unavailable category. | Public; owning Organiser for non-public records | None | `200 OK`: Route details.<br>`404 Not Found`: Event, category or route missing, category belongs to another event or record is not visible to the caller. |
| POST | `/api/events/{eventId}/categories/{categoryId}/route` | Creates the route for a category belonging to an event managed by the signed-in Organiser. A category may have no more than one route. | Organiser and event owner | `routeName` string, required<br>`startLocation` string, required<br>`finishLocation` string, required<br>`routeDescription` string, required<br>`routeMapUrl` string, optional<br>`elevationGainMetres` integer, optional | `201 Created`: Created route and a `Location` header for the route resource.<br>`400 Bad Request`: A field value or URL is invalid.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive, is not an Organiser or does not manage the event.<br>`404 Not Found`: The event or category does not exist, or the category does not belong to the event.<br>`409 Conflict`: The category already has a route. |
| PUT | `/api/events/{eventId}/categories/{categoryId}/route` | Replaces the route details for a category belonging to an event managed by the signed-in Organiser. | Organiser and event owner | `routeName` string, required<br>`startLocation` string, required<br>`finishLocation` string, required<br>`routeDescription` string, required<br>`routeMapUrl` string, optional<br>`elevationGainMetres` integer, optional | `200 OK`: Updated route details.<br>`400 Bad Request`: A field value or URL is invalid.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive, is not an Organiser or does not manage the event.<br>`404 Not Found`: The event, category or route does not exist, or the category does not belong to the event. |
| DELETE | `/api/events/{eventId}/categories/{categoryId}/route` | Deletes the route for a category belonging to an event managed by the signed-in Organiser. The category remains in the system. | Organiser and event owner | None | `204 No Content`: The route was deleted.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive, is not an Organiser or does not manage the event.<br>`404 Not Found`: The event, category or route does not exist, or the category does not belong to the event. |

### 9.1 Category and route design decisions

- One category may have one route. The event/category IDs must match.
- Owners can read Draft, Cancelled and unavailable category routes; public reads require an available category in an Open/Closed/Completed event.
- Route text follows SQL length limits. Map links are optional HTTP/HTTPS URLs without credentials; the API does not visit them. Elevation is a non-negative integer.
- PUT clears omitted optional map/elevation values. Invalid/unknown fields return 400; non-JSON bodies return 415.
- Duplicate creation returns 409, including concurrent requests. Updates/deletion preserve the event, category, entries and results; missing routes return 404.

See [route rules](Part-2-Routes.md).

## 10. Enrolment endpoints

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/enrolments` | Enters the signed-in Participant into an Open event using an available category. The Participant ID comes from the session. | Participant | `eventId` integer, required<br>`eventCategoryId` integer, required | `201 Created`: Confirmed enrolment with its resource `Location`.<br>`400 Bad Request`: Invalid fields, age outside the category limits or missing date of birth for an age-restricted category.<br>`401 Unauthorized`: No valid session.<br>`403 Forbidden`: Inactive account or wrong role.<br>`404 Not Found`: Event or category missing, or category belongs to another event.<br>`409 Conflict`: An enrolment already exists, entries are closed, the event is not Open, or the category is unavailable or full. A cancelled enrolment must be reactivated. |
| GET | `/api/enrolments/mine` | Returns the signed-in Participant's enrolment history. Optional query values may filter the list by status or event. | Participant | None. Optional query values: `page`, `pageSize`, `status`, `eventId`. | `200 OK`: Paged enrolment summaries and paging information.<br>`400 Bad Request`: A filter or paging value is invalid.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive or does not have the Participant role. |
| GET | `/api/enrolments/{enrolmentId}` | Returns one enrolment. Access is limited to the Participant who owns it or the Organiser who manages its event. | Participant owner or Organiser event owner | None | `200 OK`: Enrolment, event, category and optional result summary.<br>`401 Unauthorized`: The session is missing, invalid or expired.<br>`403 Forbidden`: The account is inactive or the user does not own the enrolment or manage its event.<br>`404 Not Found`: The enrolment does not exist. |
| PUT | `/api/enrolments/{enrolmentId}/cancel` | Changes the Participant's own Confirmed enrolment to Cancelled and keeps the row for history. | Participant owner | None. The route sets the status to Cancelled. | `200 OK`: Updated Cancelled enrolment.<br>`401 Unauthorized`: No valid session.<br>`403 Forbidden`: Inactive account, wrong role or another Participant's entry.<br>`404 Not Found`: Enrolment missing.<br>`409 Conflict`: Already Cancelled, result exists, event is not Open or entries have closed. |
| PUT | `/api/enrolments/{enrolmentId}/reactivate` | Changes the Participant's own Cancelled enrolment to Confirmed after checking the selected category again. | Participant owner | `eventCategoryId` integer, required | `200 OK`: Reactivated Confirmed enrolment.<br>`400 Bad Request`: Invalid fields, age outside the category limits or missing date of birth for an age-restricted category.<br>`401 Unauthorized`: No valid session.<br>`403 Forbidden`: Inactive account, wrong role or another Participant's entry.<br>`404 Not Found`: Enrolment, event or category missing, or category belongs to another event.<br>`409 Conflict`: Already Confirmed, result exists, entries are closed, event is not Open, or category is unavailable or full. |
| DELETE | `/api/enrolments/{enrolmentId}` | Removes the Participant's own Confirmed entry before entries close, only if the event is Open and no result exists. Cancelled entries are retained as history. | Participant owner | None | `204 No Content`: Entry removed. A later GET returns 404.<br>`401 Unauthorized`: No valid session.<br>`403 Forbidden`: Inactive account, wrong role or another Participant's entry.<br>`404 Not Found`: Enrolment missing.<br>`409 Conflict`: Entry is Cancelled, result exists, event is not Open or entries have closed. |
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

- Participant identity comes from the session; the selected category must belong to the event.
- New/reactivated entries require Open status, a future closing time, category availability and capacity. Only Confirmed entries use places.
- One saved entry per Participant/event. POST returns 409 for existing Confirmed or Cancelled rows; reactivate the latter.
- Cancellation keeps the ID, time, category and bib. DELETE removes only an owned Confirmed entry. Both require Open status, entries not closed and no result. Cancelled/history rows are retained.
- Reactivation updates the same row and repeats all entry checks. Changing category clears the bib.
- Age uses completed years on the UTC event date, inclusive minimum/maximum. Missing required birth dates or ineligible ages return 400. Leap-day birthdays advance on 1 March in non-leap years.
- Bibs are uppercase, 1-20 letters/digits/hyphens, starting with a letter/digit. They are unique within the event, including Cancelled entries.
- Lists default to page 1/size 10, maximum 100, newest entry first. Responses add names, UTC times, distance and an optional result summary.
- Shared event locks and serializable transactions protect concurrent entry, capacity, status and bib changes. Invalid/unknown JSON returns 400; non-JSON writes return 415.

See [enrolment rules](Part-2-Enrolments.md).

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

- The recorder comes from the session and must own the entry's event.
- One official result per enrolment; Participants read only their own results.
- Completed requires positive seconds. Other statuses require null time/positions. Optional finishing positions must be positive; ties are allowed.
- PUT corrects the same result, sets UpdatedAt and keeps its original recorder/time. No result DELETE is provided.

### 11.3 Part 2 implementation notes

Capture/correction requires a Confirmed entry and a started, non-Cancelled event; state conflicts return 409. Status ignores case/spaces. PUT clears omitted optional notes/positions. Invalid/unknown fields return 400; non-JSON writes return 415.

Responses add Participant/event/category IDs, name, distance and bib. Total finishers counts all Completed results in the event regardless of paging/filters. Personal history uses newest event first; event lists use Completed, position, time and ID, with missing values last.

Serializable transactions and the shared event lock prevent duplicate capture and conflicting cancellation. See [results](Part-2-Results.md).

## 12. Part 2 implementation and verification requirements

- C# ASP.NET Core API with EF Core Code First matching the Part 1 schema (Microsoft, 2026e).
- Server-side identity/role sessions, role/ownership checks and 401/403 responses (Microsoft, 2026d).
- Swagger descriptions, request inputs and responses for every endpoint (Microsoft, 2026f).
- Success/failure tests for authentication, both roles, events and saved enrolments.
- GitHub Actions builds and runs all tests on each push (Microsoft, 2026g).

## 13. Part 3 MVC, storage and container requirements

- MVC uses the API for all data operations, including login, dashboards, entries, results and profiles. It has no direct SQL or storage access.
- Keep only safe identity/role/session information in MVC, never passwords. Use role-based navigation and protected actions.
- MVC sends files to the API; the API validates them and generates image URLs. Storage keys stay private (Microsoft, 2026h).
- Add a multi-stage Dockerfile and one documented run command (Microsoft, 2026i).
- CI builds MVC, API and tests; retain Part 2 tests and add relevant MVC checks.

## 14. Endpoint-plan coverage

The plan has 40 endpoints. The table below maps the 25 operations in the lecturer's Part 1 feedback to the planned routes. The feedback allows different route names; the method and operation must still match (The Independent Institute of Education, 2026b).

### 14.1 Required operations

| No. | Area | Required operation | Planned endpoint |
|---|---|---|---|
| 1 | Authentication | Register | POST `/api/auth/register` |
| 2 | Authentication | Login | POST `/api/auth/login` |
| 3 | Authentication | Logout | POST `/api/auth/logout` |
| 4 | Profile | View own profile | GET `/api/profile` |
| 5 | Profile | Update own profile | PUT `/api/profile` |
| 6 | Events | Browse events | GET `/api/events` |
| 7 | Events | View one event | GET `/api/events/{eventId}` |
| 8 | Events | List owned events | GET `/api/events/mine` |
| 9 | Events | Create event | POST `/api/events` |
| 10 | Events | Update event | PUT `/api/events/{eventId}` |
| 11 | Events | Delete event | DELETE `/api/events/{eventId}` |
| 12 | Categories | View event categories | GET `/api/events/{eventId}/categories` |
| 13 | Categories | Create category | POST `/api/events/{eventId}/categories` |
| 14 | Categories | Update category | PUT `/api/events/{eventId}/categories/{categoryId}` |
| 15 | Categories | Delete category | DELETE `/api/events/{eventId}/categories/{categoryId}` |
| 16 | Enrolments | Enter an event | POST `/api/enrolments` |
| 17 | Enrolments | View own enrolments | GET `/api/enrolments/mine` |
| 18 | Enrolments | View one enrolment | GET `/api/enrolments/{enrolmentId}` |
| 19 | Enrolments | View event enrolments | GET `/api/events/{eventId}/enrolments` |
| 20 | Enrolments | Update status | PUT `/api/enrolments/{enrolmentId}/cancel` |
| 21 | Enrolments | Delete entry | DELETE `/api/enrolments/{enrolmentId}` |
| 22 | Results | Record result | POST `/api/enrolments/{enrolmentId}/result` |
| 23 | Results | View own results | GET `/api/results/mine` |
| 24 | Results | View event results | GET `/api/events/{eventId}/results` |
| 25 | Results | Update result | PUT `/api/enrolments/{enrolmentId}/result` |

The cancellation route covers the required PUT status change. Reactivation provides the reverse change from Cancelled to Confirmed.

### 14.2 Other planned endpoints

The other 15 endpoints remain in Part 2: password changes, profile-picture upload and removal, the Organiser dashboard, event-banner upload and removal, single-category and Organiser-category reads, four route operations, the second enrolment status route, bib numbers and a single-enrolment result read.

The image endpoints are implemented and tested through the API in Part 2. The MVC interface, its upload pages and Docker setup remain Part 3 work.

### 14.3 Corrections dated 17 October 2026

- Added the missing enrolment DELETE operation.
- Mapped all 25 required operations to the planned methods and routes.
- Set out the differences between cancellation, reactivation and deletion.
- Added both age limits, the event-date age rule and the missing-date-of-birth response.
- Allowed the owning Organiser to read new Draft events, categories and routes through their creation locations.
- Kept all 39 original endpoints. These planning corrections were completed before the API project was created.

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

The Independent Institute of Education (2026b) *POE Part I General Feedback (HE)*. Lecturer-provided ERD and endpoint feedback.
