# RaceDay - Part 3

## Project overview

RaceDay manages South African running, walking and cycling events. Part 3 adds an ASP.NET Core MVC website to the Part 2 API, with separate pages for Organisers and Participants, Azure image uploads and Docker (The Independent Institute of Education, 2026; Microsoft, 2026a).

The MVC home page, filters, registration, login and sign-out are working through the API. Role-specific pages and Docker are still to be added. Progress is recorded in the [Part 3 checklist](docs/Part-3-Requirements-Checklist.md).

Previous work: [Part 1 README](README-Part1.md) and [Part 2 README](README-Part2.md).

## User roles

| Role | Responsibilities |
|---|---|
| Organiser | View the dashboard; manage owned events, banners and categories; view enrolments and record results. |
| Participant | Browse and enter events; view own entries and results; update personal details and upload a profile picture. |

Both roles register and log in through the API. MVC keeps user ID, role and the API cookie on the server; the browser receives a separate protected session identifier. Sign-out clears the session. Distinct role menus and protected pages remain to be added. See the [account notes](docs/Part-3-Accounts.md) (Microsoft, 2026b).

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
| Registration, login and sign-out | Working for both roles; [account notes](docs/Part-3-Accounts.md) |
| Role-specific menus and protected pages | Not started |
| Docker | Not started |

## MVC screenshots

The home page displays live API events and supports province and event-type filters.

![RaceDay Event finder home page](docs/evidence/part3-event-finder-desktop.png)

The [mobile layout](docs/evidence/part3-event-finder-mobile.png) stacks the filters and event details on smaller screens.

Both roles use the same sign-in form. The [registration page](docs/evidence/part3-registration.png) includes account type and optional personal details.

![RaceDay sign-in page](docs/evidence/part3-login.png)

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

The home page displays up to three upcoming events, with province and event-type filters. Use Create account or Sign in in the header. Registration redirects to sign-in; login returns to the event list. The fictional [Part 2 sample accounts](README-Part2.md#setup-and-run) also work through MVC. Date filters, pagination, event details and role-specific pages remain to be added.

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

Part 2 includes 810 tests recorded in the [Part 2 README](README-Part2.md#unit-tests). MVC now has 70 checks covering the home page, filters, settings, account forms, validation, cookies, sessions and sign-out (Microsoft, 2026f).

All 70 MVC checks passed locally on 18 November 2026, with none failed or skipped. The 810 unchanged API tests passed earlier on the same date. The Release build has zero warnings or errors, and planning checks pass. Browser checks verified registration, login and sign-out for both roles using a separate temporary database.

## Project files

| File/folder | Contents |
|---|---|
| [Part 1 README](README-Part1.md) | Original planning and database submission |
| [Part 2 README](README-Part2.md) | API setup, features, tests and evidence |
| [Part 3 checklist](docs/Part-3-Requirements-Checklist.md) | Requirements and outstanding work |
| [MVC notes](docs/Part-3-MVC.md) | API connection, settings, layout and checks |
| [Account notes](docs/Part-3-Accounts.md) | Registration, login, sessions, validation and sign-out |
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

Bootstrap (2026) 'Download', *Bootstrap v5.3*. Available at: https://getbootstrap.com/docs/5.3/getting-started/download/ (Accessed: 18 November 2026).

The Independent Institute of Education (2026) *PROG6212 Portfolio of Evidence*. Part 3 requirements and marking rubric. Assessment brief.
