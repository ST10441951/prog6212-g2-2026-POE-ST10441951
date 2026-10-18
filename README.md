# RaceDay - Part 2

[![Build and Test RaceDay API](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/workflows/part2-build.yml/badge.svg?branch=main)](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/workflows/part2-build.yml)

## Project overview

RaceDay manages South African running, walking and cycling events. Part 2 builds the REST API in C# and ASP.NET Core, using SQL Server and Entity Framework Core (The Independent Institute of Education, 2026a).

The API follows the [Part 1 endpoint plan](docs/API-Endpoint-Plan.md). Login stores the user's ID and role in a server-side session. Passwords are hashed before they are saved.

## Part 2 status

The endpoint plan now has 40 endpoints and a [mapping of the lecturer's 25 required operations](docs/API-Endpoint-Plan.md#141-required-operations). Enrolment deletion, status changes and age rules have been clarified.

The Part 1 planning and database corrections are complete. All 71 column definitions match, 21 database checks passed and the corrected script ran successfully in SSMS. The [database correction notes](docs/Part-1-Database-Corrections.md) include the fresh screenshots and results.

The API has an EF Core SQL Server context, seven entity models, table configurations, an initial migration and the Part 1 sample data. Authentication, own profiles, event browsing and management, the Organiser dashboard and category management are implemented. Nineteen of the 40 planned business endpoints are complete; 21 remain. All 447 current tests pass locally. GitHub Actions runs the full suite on every push; the badge above shows the latest main-branch result.

Progress is recorded in the [Part 2 checklist](docs/Part-2-Requirements-Checklist.md).

The original README is kept in [Part 1 README](README-Part1.md).

## User roles

| Role | What they can do |
|---|---|
| Organiser | Manage their own events, categories and routes; view event enrolments; assign bib numbers; record and update results. |
| Participant | Manage their profile, browse events, enter a category and view their own enrolments and results. |

Both roles can view events and available categories. Users cannot change another user's private records.

## Database

The seven tables are `Roles`, `Users`, `Events`, `EventCategories`, `EventRoutes`, `Enrolments` and `Results`.

EF Core Code First creates the separate `RaceDayPart2` database (Microsoft, 2026b). The mappings preserve all 71 columns, nine foreign keys, 36 checks and 12 named defaults. The original Part 1 database and SQL script are kept.

One SQL detail differs: the unique rule on `EventId, CategoryName` uses a unique index with the original name `UQ_EventCategories_Event_CategoryName`. It still rejects duplicate names within an event. It allows category names to be updated through EF Core, whereas an alternate-key property cannot be changed (Microsoft, 2026f; 2026g). The composite Category/Event foreign key remains an alternate key.

The [Part 2 database notes](docs/Part-2-Database.md) explain the migration, sample data and checks.

## API endpoints

The API will cover authentication, profiles, events, categories, routes, enrolments and results. It will also include the password, dashboard, bib-number and image operations already in the endpoint plan.

Profile pictures and event banners will pass through the API. SQL Server will store their blob names. The MVC website and Docker setup belong to Part 3.

`GET /health` is a separate public host check. It returns `200 OK` with `{"status":"Healthy"}`. It uses ASP.NET Core controller routing and JSON responses (Microsoft, 2026c). It does not check the database, storage or login, and is not one of the 40 planned business endpoints.

The current [Swagger host-check screenshot](docs/evidence/swagger-host-check.jpg) shows the response.

## Authentication

| Method | Route | Current behaviour |
|---|---|---|
| POST | `/api/auth/register` | Creates an Organiser or Participant account and returns 201 |
| POST | `/api/auth/login` | Checks the password, starts a session and returns 200 |
| POST | `/api/auth/logout` | Ends the session and returns 204; returns 401 when no session exists |

Registration accepts only the planned account fields. Email addresses are trimmed, converted to lower case and checked for duplicates. Passwords need 8 to 128 characters, including an upper-case letter, lower-case letter, digit and symbol. ASP.NET Core's password hasher creates the stored hash (Microsoft, 2026k).

The session lasts 20 minutes. The user ID, role and expiry stay in the server cache; the browser receives a protected random ID in an HttpOnly, SameSite=Strict cookie. HTTPS uses a Secure cookie. Logout removes the server record, and another login replaces the old session (Microsoft, 2026l; 2026m).

Controllers require authentication by default. Role policies return 403 for the wrong role. Protected requests check that the account still exists, is active and still has the same role (Microsoft, 2026n; 2026o).

The [authentication notes](docs/Part-2-Authentication.md) include the request examples and checks. After login, the registration Location at `/api/profile` returns the new user's own profile.

## User profiles

| Method | Route | Current behaviour |
|---|---|---|
| GET | `/api/profile` | Returns the signed-in user's profile |
| PUT | `/api/profile` | Updates names, phone number and date of birth |
| PUT | `/api/profile/password` | Checks the current password, saves a new hash and ends the current session |

Both roles use these endpoints. The user ID comes from the session. Profile updates cannot change email, role, account status, passwords or image fields. Optional phone and birth-date values are cleared when omitted or null. A supplied birth date must be earlier than today's UTC date.

Password changes require the correct current password and matching new-password fields. Sign in again with the new password after the 204 response. Other browsers keep their existing sessions until logout or normal expiry. Profile image URLs remain null until the image endpoints are added.

The [profile notes](docs/Part-2-Profiles.md) record the validation, persistence and privacy checks (Microsoft, 2026p).

## Event browsing

| Method | Route | Current behaviour |
|---|---|---|
| GET | `/api/events` | Returns a filtered page of upcoming Open and Closed events |
| GET | `/api/events/{eventId}` | Returns one visible event with its categories and routes |

Browsing does not require login. Filters cover event type, province and inclusive UTC dates. Page numbers start at 1; page size defaults to 10 and is limited to 100. Results are ordered by event date and then event ID (Microsoft, 2026q).

Open, Closed and Completed event details are public. Draft and Cancelled details are visible only to the owning Organiser after login. Other callers receive 404. Public details show available categories; the owner also sees unavailable ones. Responses exclude account contact details, enrolments and storage object names.

The [event-browsing notes](docs/Part-2-Event-Browsing.md) explain the filters, visibility rules and checks.

## Organiser event management

| Method | Route | Current behaviour |
|---|---|---|
| GET | `/api/events/mine` | Paged owned events in every status, with an optional status filter |
| POST | `/api/events` | Creates a Draft event for the signed-in Organiser |
| PUT | `/api/events/{eventId}` | Updates an owned event or changes its status |
| DELETE | `/api/events/{eventId}` | Deletes an owned event with no dependent records or banner |
| GET | `/api/organiser/dashboard` | Owned event dates and saved-enrolment totals |

Every operation requires an Organiser session. The API assigns ownership and checks it before updates or deletion. Requests cannot assign IDs, image fields or timestamps. Event writes validate the planned fields, future creation dates, SQL precision and paired coordinates (Microsoft, 2026p; 2026r).

Opening or reopening needs an available category and future event and closing times. Cancelled and Completed statuses cannot be reversed. Saved entries protect the event's dates, type and distance. Events with results cannot be cancelled. Categories and entries prevent deletion, so cancellation keeps their history. Descriptive corrections remain possible.

The dashboard counts all saved enrolments, with Confirmed and Cancelled rows shown separately. Its totals and event list belong only to the signed-in Organiser. The [event-management notes](docs/Part-2-Event-Management.md) record the status rules, validation and checks.

## Categories

| Method | Route | Current behaviour |
|---|---|---|
| GET | `/api/events/{eventId}/categories` | Available categories for Open, Closed and Completed events |
| GET | `/api/events/{eventId}/categories/{categoryId}` | Public category details; the owner can also read hidden records |
| GET | `/api/organiser/events/{eventId}/categories` | All categories for an owned event |
| POST | `/api/events/{eventId}/categories` | Creates an owned category and returns 201 with its Location |
| PUT | `/api/events/{eventId}/categories/{categoryId}` | Updates permitted category details |
| DELETE | `/api/events/{eventId}/categories/{categoryId}` | Deletes an unused category and its optional route |

Writes require an Organiser session and event ownership. Names are unique within each event. Requests validate distance, age limits, entry fee, availability and capacity. Saved entries protect the category's distance, age limits and fee. Capacity cannot be below Confirmed entries, and an Open event must keep an available category (Microsoft, 2026p; 2026r).

Category responses include the event ID and optional route details. Route CRUD is still to be added. The [category notes](docs/Part-2-Categories.md) record the rules, tests and Swagger checks.

## Setup and run

Install the .NET 10 SDK and SQL Server LocalDB on Windows. For Visual Studio, use a version that supports .NET 10 with the **ASP.NET and web development** workload. Another SQL Server instance can be used by changing the connection string.

1. Clone the repository and open its folder (GitHub, 2026a).

```text
git clone https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951.git
cd prog6212-g2-2026-POE-ST10441951
```

2. Restore the packages, restore the local EF migration tool and build the solution.

```text
dotnet restore RaceDay.sln
dotnet tool restore
dotnet build RaceDay.sln --configuration Release --no-restore
```

3. Set the connection string in PowerShell and select Development. This example uses Windows authentication and a separate Part 2 database. The same local connection is provided in `appsettings.Development.json` (Microsoft, 2026h).

```text
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__RaceDay = 'Server=(localdb)\MSSQLLocalDB;Database=RaceDayPart2;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
```

Use your SQL Server instance name if LocalDB is not installed. Keep private connection strings in environment settings or user secrets. The trusted-certificate option above is for local development.

4. Create the database and apply its migration (Microsoft, 2026i).

```text
dotnet ef database update --project src/RaceDay.Api --configuration Release --no-build
```

The command creates the tables and sample records. Running it again keeps the saved data and applies only missing migrations. Do not run the Part 1 SQL script against this database. The API does not run migrations automatically when it starts.

5. Start the API using the local HTTP profile.

```text
dotnet run --project src/RaceDay.Api --configuration Release --no-build --launch-profile http
```

6. Open [Swagger](http://localhost:5120/swagger/index.html). Expand `GET /health`, select **Try it out**, then **Execute**. The response should be `200` with `{"status":"Healthy"}`. The Swagger JSON is at `http://localhost:5120/swagger/v1/swagger.json` (Swashbuckle, 2026).

To check authentication, execute the login example using `naledi.mokoena@example.com` and `RaceDayDemo!2026`. Then execute logout without a body. Swagger sends the cookie automatically; entering it in **Authorize** is not needed. Logout returns 204, and a second logout returns 401.

While signed in, execute `GET /api/profile` to view your own account. Execute the `PUT /api/profile` example to update its personal details, then GET again to check the saved values. To test a password change, register a separate sample account first. Supply its current password and matching replacement values at `PUT /api/profile/password`, then log in again with the replacement. The Swagger example passwords are local demonstration values, not private credentials.

The registration example uses `new.participant@example.com`. Use another email to create another account. Reusing an existing email returns 409.

To check event browsing without login, execute `GET /api/events`. The original sample data includes the upcoming Cape Peninsula cycling event and Soweto walk. Filter `eventType` to `Cycling`, then execute `GET /api/events/{eventId}` with event ID `2` to see its details, categories and routes. The past Completed event at ID `1` can be viewed directly but is excluded from the upcoming list. The list changes as event dates pass.

For event management, log in as `thabo.ndlovu@example.com` using the same demonstration password. Execute the POST event example and follow its Location to view the new Draft. Use its ID with POST `/api/events/{eventId}/categories` and execute the category example. GET `/api/organiser/events/{eventId}/categories` shows its saved categories. For PUT, reuse the editable fields from the creation request and add `"status": "Open"`. The event must have an available category and future start and closing times. The public category list can then show it. GET `/api/events/mine` and `/api/organiser/dashboard` show only that Organiser's records.

7. Open another terminal in the repository folder to run the tests (Microsoft, 2026e).

```text
dotnet test RaceDay.sln --configuration Release --no-build
```

In Visual Studio, open `RaceDay.sln`, set `RaceDay.Api` as the startup project and choose the `http` profile. Open **Test > Test Explorer** and select **Run All** to run the tests there.

The tests create and remove their own temporary `RaceDayTests_...` database. They use LocalDB by default. For another SQL Server, set `RACEDAY_TEST_CONNECTION` before starting the tests; the test fixture always chooses a new database name. It does not use `RaceDay` or `RaceDayPart2`.

The HTTP profile is for local development. Outside Development, Swagger is disabled and the API enables HTTPS redirection and HSTS. Set `ConnectionStrings:RaceDay` separately for that environment.

## CI/CD

The planning checks are written in C# and use the .NET 10 SDK. They check the required files, image formats, all 40 endpoint rows, the lecturer's 25 required operations and matching SQL, ERD and data-dictionary columns. The workflow builds and runs them on every push and on pull requests to main (GitHub, 2026b; 2026c).

The [C# planning build for 4560c17](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39063690485) passed on GitHub. The local checks also rejected all 13 invalid-document cases.

The `.github/workflows/part2-build.yml` restores, builds and tests the whole solution on every push and on pull requests to main (GitHub, 2026d). The [API build for ecce46b](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39100427831) passed with all seven startup tests.

The updated workflow uses Windows 2025, which includes SQL Server LocalDB, and runs the SQL Server tests and a pending-migration check (GitHub, 2026e). The [database build for e31e512](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39102096043) passed all 70 tests with none skipped, no build warnings or errors and no missing migrations.

The [authentication build for b1daebb](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39104118046) passed all 121 tests with none skipped, no build warnings or errors and no missing migrations.

The [profile build for 0d65a7b](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39108330909) passed all 178 tests with none skipped, no build warnings or errors and no missing migrations. The planning workflow also passed.

The [event-management build for 6d3a956](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39124637480) passed all 354 tests with none skipped, no build warnings or errors and no missing migrations. The [planning workflow](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39124637429) also passed. Category management adds 93 checks and brings the local total to 447. The API workflow runs the complete suite and migration check on every push.

Part 2 green-build screenshot: to be added.

## Unit tests

The tests use xUnit. There are seven startup checks, 63 database checks, 51 authentication checks, 57 profile and password checks, 55 event-browsing checks, 121 Organiser event-management checks and 93 category checks. API requests run through the real request pipeline against temporary SQL Server databases (Microsoft, 2026d; 2026j; xUnit.net, 2026).

All 447 pass locally with none skipped and no build warnings or errors. The new checks cover category CRUD, both roles, ownership, invalid fields, capacity, saved history, route removal, public visibility, duplicate requests and Swagger examples. Route, enrolment, result and image-operation tests will be added with those features.

The tests must pass locally and in GitHub Actions. A screenshot of the passing **Run All** result will be added here.

## Project files

| File or folder | Contents |
|---|---|
| `RaceDay.sln` | API, test and planning-checker projects |
| `src/RaceDay.Api/` | ASP.NET Core API, controllers and response contracts |
| `src/RaceDay.Api/Models/` | Seven database entities |
| `src/RaceDay.Api/Data/` | SQL Server context, configurations, seed data and migrations |
| `src/RaceDay.Api/Authentication/` | Server-side sessions, authentication handler and Swagger examples |
| `tests/RaceDay.Api.Tests/` | xUnit API tests |
| `tests/RaceDay.Api.Tests/Database/` | SQL Server migration and constraint tests |
| `tests/RaceDay.Api.Tests/Authentication/` | Registration, login, logout and role-policy checks |
| `tests/RaceDay.Api.Tests/Profiles/` | Own-profile, password-change and privacy checks |
| `tests/RaceDay.Api.Tests/Events/` | Event browsing, management, ownership, history and dashboard checks |
| `tests/RaceDay.Api.Tests/Categories/` | Category validation, ownership, visibility, capacity and history checks |
| `README-Part1.md` | Original Part 1 README |
| [API-Endpoint-Plan.md](docs/API-Endpoint-Plan.md) | Planned endpoints |
| [RaceDay-Data-Dictionary.md](docs/RaceDay-Data-Dictionary.md) | Tables, fields and constraints |
| [RaceDay.sql](docs/RaceDay.sql) | Part 1 database script |
| [Part-1-Database-Corrections.md](docs/Part-1-Database-Corrections.md) | Corrected database checks and fresh SSMS evidence |
| [Part-2-Database.md](docs/Part-2-Database.md) | EF Core mapping, migration and database checks |
| [Part-2-Authentication.md](docs/Part-2-Authentication.md) | Account validation, sessions and Swagger checks |
| [Part-2-Profiles.md](docs/Part-2-Profiles.md) | Personal details, password changes and checks |
| [Part-2-Event-Browsing.md](docs/Part-2-Event-Browsing.md) | Event filters, visibility and read checks |
| [Part-2-Event-Management.md](docs/Part-2-Event-Management.md) | Owned events, status changes and dashboard checks |
| [Part-2-Categories.md](docs/Part-2-Categories.md) | Category rules, validation and Swagger checks |
| [Part-2-Requirements-Checklist.md](docs/Part-2-Requirements-Checklist.md) | Part 2 tasks |
| `tools/RaceDay.PlanningChecks/` | C# endpoint-plan, schema-document and repository checks |
| `docs/evidence/` | Screenshots |
| `.github/workflows/` | GitHub Actions workflows |

## Video

Part 2 YouTube link: to be added.

The video must be no longer than 20 minutes. It must open with the rubric, show every endpoint working in Swagger, explain the core logic and show passing tests using **Run All** (The Independent Institute of Education, 2026b).

## References

GitHub (2026a) 'Getting changes from a remote repository', *GitHub Docs*. Available at: https://docs.github.com/en/get-started/using-git/getting-changes-from-a-remote-repository (Accessed: 19 September 2026).

GitHub (2026b) 'Workflow syntax for GitHub Actions', *GitHub Docs*. Available at: https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax (Accessed: 17 October 2026).

GitHub (2026c) 'actions/setup-dotnet', *GitHub*. Available at: https://github.com/actions/setup-dotnet (Accessed: 17 October 2026).

GitHub (2026d) 'Building and testing .NET', *GitHub Docs*. Available at: https://docs.github.com/en/actions/tutorials/build-and-test-code/net (Accessed: 17 October 2026).

GitHub (2026e) 'Windows Server 2025', *actions/runner-images*. Available at: https://github.com/actions/runner-images/blob/main/images/windows/Windows2025-Readme.md (Accessed: 18 October 2026).

Microsoft (2026a) 'Session and state management in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-10.0 (Accessed: 17 October 2026).

Microsoft (2026b) 'Migrations overview', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/ (Accessed: 17 October 2026).

Microsoft (2026c) 'Create web APIs with ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-10.0 (Accessed: 17 October 2026).

Microsoft (2026d) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 17 October 2026).

Microsoft (2026e) 'dotnet test', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test (Accessed: 17 October 2026).

Microsoft (2026f) 'Keys', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/modeling/keys (Accessed: 18 October 2026).

Microsoft (2026g) 'Indexes', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/modeling/indexes (Accessed: 18 October 2026).

Microsoft (2026h) 'DbContext lifetime, configuration, and initialization', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/ (Accessed: 18 October 2026).

Microsoft (2026i) 'Applying migrations', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying (Accessed: 18 October 2026).

Microsoft (2026j) 'Testing against your production database system', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/testing/testing-with-the-database (Accessed: 18 October 2026).

Microsoft (2026k) 'PasswordHasher<TUser> class', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026l) 'Distributed caching in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026m) 'Consumer APIs overview', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/overview?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026n) 'AuthenticationHandler<TOptions> class', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.authenticationhandler-1?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026o) 'Role-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026p) 'Model validation in ASP.NET Core MVC', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026q) 'Pagination', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/querying/pagination (Accessed: 18 October 2026).

Microsoft (2026r) 'Basic SaveChanges', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/saving/basic (Accessed: 18 October 2026).

Swashbuckle (2026) *Swashbuckle.AspNetCore*. Available at: https://github.com/domaindrivendev/Swashbuckle.AspNetCore (Accessed: 17 October 2026).

xUnit.net (2026) 'Getting started with xUnit.net v2'. Available at: https://xunit.net/docs/getting-started/v2/getting-started (Accessed: 17 October 2026).

The Independent Institute of Education (2026a) *PROG6212 Portfolio of Evidence*. Part 1 and Part 2 requirements and Part 2 marking rubric. Assessment brief.

The Independent Institute of Education (2026b) *Announcement for the Part II*. Module announcement covering the video structure and Part 2 README requirements.
