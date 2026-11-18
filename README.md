# RaceDay - Part 3

## Project overview

RaceDay manages South African running, walking and cycling events. Part 3 adds an ASP.NET Core MVC website to the Part 2 API, with separate pages for Organisers and Participants, Azure image uploads and Docker (The Independent Institute of Education, 2026; Microsoft, 2026a).

The MVC event search, pagination, event details, accounts and protected role home pages are working through the API. Event management, entries, results, profile editing and Docker remain to be added. Progress is recorded in the [Part 3 checklist](docs/Part-3-Requirements-Checklist.md).

Previous work: [Part 1 README](README-Part1.md) and [Part 2 README](README-Part2.md).

## User roles

| Role | Responsibilities |
|---|---|
| Organiser | View the dashboard; manage owned events, banners and categories; view enrolments and record results. |
| Participant | Browse and enter events; view own entries and results; update personal details and upload a profile picture. |

Both roles register and log in through the API. MVC keeps user ID, role and the API cookie on the server; the browser receives a separate protected session identifier. Each request rechecks the account through the API. The header shows the correct role's home link, guests are redirected to sign-in and the wrong role receives 403. Sign-out clears the session. See the [account notes](docs/Part-3-Accounts.md) (Microsoft, 2026b; 2026g; 2026h).

## Application

The MVC website uses a typed HTTP client to call the API. Database access and business rules remain in the API. Files will pass from MVC to the API; only the API communicates with Azure Blob Storage (The Independent Institute of Education, 2026; Microsoft, 2026e).

The existing API has 40 business endpoints and seven SQL Server tables. See the [endpoint plan](docs/API-Endpoint-Plan.md) and [database notes](docs/Part-2-Database.md).

The shared MVC layout uses the Event finder design: blue accents, a light background, compact navigation and white event cards. Bootstrap styles are served locally (Bootstrap, 2026).

| Area | Current status |
|---|---|
| Part 1 planning and database | Complete |
| Part 2 API, sessions and tests | Complete |
| API picture and banner endpoints | Implemented; Part 3 Azure demonstration pending |
| MVC project, API connection and shared layout | Implemented; [MVC notes](docs/Part-3-MVC.md) |
| Public event search, paging, categories and routes | Implemented; [event notes](docs/Part-3-Events.md) |
| Registration, login and sign-out | Working for both roles; [account notes](docs/Part-3-Accounts.md) |
| Role-specific navigation and protected home pages | Implemented; API session rechecked before access |
| Organiser management, entries, results and profiles | Pending; role home pages are the starting pages |
| Docker | Not started |

## MVC screenshots

The home page displays live API events and supports province, event type and date filters. Pagination keeps the selected filters. The search screenshots use temporary review fixtures to show multiple pages.

![RaceDay Event finder home page](docs/evidence/part3-event-finder-desktop.png)

The [mobile layout](docs/evidence/part3-event-finder-mobile.png) stacks the filters and event details on smaller screens.

Event names open full details with categories and routes. See [event details](docs/evidence/part3-event-details-desktop.png), the [mobile detail page](docs/evidence/part3-event-details-mobile.png) and [filtered pagination](docs/evidence/part3-event-pagination.png).

Both roles use the same sign-in form. The [registration page](docs/evidence/part3-registration.png) includes account type and optional personal details.

![RaceDay sign-in page](docs/evidence/part3-login.png)

Protected pages: [Organiser home](docs/evidence/part3-organiser-home.png), [Participant home](docs/evidence/part3-participant-home.png) and [access denied](docs/evidence/part3-access-denied.png). These pages retain the same Event finder style.

## Setup and run

Install the .NET 10 SDK, SQL Server LocalDB and Node.js with npm. Visual Studio needs .NET 10 support and the ASP.NET/web workload.

1. Clone the repository.

```text
git clone https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951.git
cd prog6212-g2-2026-POE-ST10441951
```

2. Restore and build the solution.

```text
dotnet restore RaceDay.sln
dotnet tool restore
dotnet build RaceDay.sln --configuration Release --no-restore
```

3. Follow [Part 2 setup steps 3-5](README-Part2.md#setup-and-run) to set the SQL connection, apply migrations and start Azurite. Keep storage running for image tests.

4. Start the API in one PowerShell terminal.

```text
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/RaceDay.Api --configuration Release --no-build --launch-profile http
```

5. Start MVC in another terminal. `Api__BaseUrl` selects the API address (Microsoft, 2026e).

```text
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Api__BaseUrl = 'http://localhost:5120/'
dotnet run --project src/RaceDay.Web --configuration Release --no-build --launch-profile http
```

6. Open [RaceDay](http://localhost:5180/) or [API Swagger](http://localhost:5120/swagger/index.html). In Visual Studio, open `RaceDay.sln` and configure both API and Web as startup projects using their `http` profiles.

The home page displays three upcoming events per page. Filter by province, event type or inclusive UTC dates, then open an event for its details, categories and routes. Times are displayed in SAST. Search starts on page 1, page links retain filters and Clear filters resets the search.

Use Create account or Sign in in the header. Registration redirects to sign-in; login opens `/organiser` or `/participant`. A valid local return address takes the user back to the requested page. External destinations are rejected (Microsoft, 2026i). The fictional [Part 2 sample accounts](README-Part2.md#setup-and-run) also work through MVC. Management pages, entry actions and personal records remain to be added.

A stopped API produces a friendly message instead of an empty list or raw error. See [MVC settings](docs/Part-3-MVC.md#2-settings).

7. Run tests with the API test database server and Azurite available. The MVC tests do not need the API running (Microsoft, 2026f).

```text
dotnet test RaceDay.sln --configuration Release --no-build
```

## Azure Blob Storage

The API already supports Participant pictures and Organiser banners. It validates JPEG/PNG files and stores generated blob names in SQL. Reads return temporary image URLs. See the [image notes](docs/Part-2-Images.md) (Microsoft, 2026c).

Part 3 will add upload controls and image display in MVC. The final checks must demonstrate images stored in Azure through the API. Azurite remains available for local development and tests.

## Docker

The MVC Dockerfile, container checks and tested build/run commands are still to be added (Microsoft, 2026d).

## CI/CD

The [build workflow](.github/workflows/part2-build.yml) now builds the API, MVC and both test projects. The [planning workflow](.github/workflows/part1-validation.yml) checks the planning files and required Part 3 files (GitHub, 2026).

The [build job for f60e944](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/44302444572) and [planning job](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/44302444548) did not start. GitHub reported that the account is locked due to a billing issue. No job steps ran, so a green Part 3 build and screenshot remain outstanding.

## Unit tests

Part 2 includes 810 tests recorded in the [Part 2 README](README-Part2.md#unit-tests). MVC now has 138 checks covering event search, paging, details, settings, accounts, validation, cookies, role restrictions, redirects and sign-out (Microsoft, 2026f).

All 138 MVC checks passed locally on 18 November 2026, with none failed or skipped. The 810 unchanged API tests passed earlier on the same date. The Release build has zero warnings or errors, and planning checks pass. Browser checks verified role restrictions, changed accounts, combined filters, paging and event details against isolated databases. See the [account checks](docs/Part-3-Accounts.md#5-checks-and-evidence) and [event checks](docs/Part-3-Events.md#3-checks-and-evidence).

## Project files

| File/folder | Contents |
|---|---|
| [Part 1 README](README-Part1.md) | Original planning and database submission |
| [Part 2 README](README-Part2.md) | API setup, features, tests and evidence |
| [Part 3 checklist](docs/Part-3-Requirements-Checklist.md) | Requirements and outstanding work |
| [MVC notes](docs/Part-3-MVC.md) | API connection, settings, layout and checks |
| [Account notes](docs/Part-3-Accounts.md) | Registration, login, protected pages, sessions and sign-out |
| [Event notes](docs/Part-3-Events.md) | Search, paging, public details, categories, routes and checks |
| `src/RaceDay.Web/` | MVC controllers, views, API client and local Bootstrap styles |
| `src/RaceDay.Api/` | Existing API, database mappings, sessions and storage |
| `tests/RaceDay.Web.Tests/` | MVC and HTTP connection checks |
| `tests/RaceDay.Api.Tests/` | Existing API and database tests |
| `tools/RaceDay.PlanningChecks/` | Planning-document checks |
| `.github/workflows/` | API/MVC build and planning-validation workflows |
| `docs/evidence/` | Part 1/2 evidence and Part 3 desktop/mobile screenshots |

## Video

The Part 3 walkthrough has not been recorded. Its unlisted YouTube link will be added here after recording.

## References

GitHub (2026) 'Building and testing .NET', *GitHub Docs*. Available at: https://docs.github.com/en/actions/tutorials/build-and-test-code/net (Accessed: 18 November 2026).

Microsoft (2026a) 'Overview of ASP.NET Core MVC', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/overview?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026b) 'Session and state management in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026c) 'Upload a blob with .NET', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-upload (Accessed: 18 November 2026).

Microsoft (2026d) 'Run an ASP.NET Core app in Docker containers', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/building-net-docker-images?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026e) 'Make HTTP requests using IHttpClientFactory in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026f) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026g) 'Overview of ASP.NET Core authentication', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026h) 'Role-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026i) 'Prevent open redirect attacks in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/preventing-open-redirects?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Bootstrap (2026) 'Download', *Bootstrap v5.3*. Available at: https://getbootstrap.com/docs/5.3/getting-started/download/ (Accessed: 18 November 2026).

The Independent Institute of Education (2026) *PROG6212 Portfolio of Evidence*. Part 3 requirements and marking rubric. Assessment brief.
