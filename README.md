# RaceDay - Part 3

## Project overview

RaceDay manages South African running, walking and cycling events. Part 3 adds an ASP.NET Core MVC website to the Part 2 API, with separate pages for Organisers and Participants, Azure image uploads and Docker (The Independent Institute of Education, 2026; Microsoft, 2026a).

The MVC project, API connection and shared layout are in place. The home page displays upcoming events from the API. Login, role-specific pages and Docker are still to be added. Progress is recorded in the [Part 3 checklist](docs/Part-3-Requirements-Checklist.md).

Previous work: [Part 1 README](README-Part1.md) and [Part 2 README](README-Part2.md).

## User roles

| Role | Responsibilities |
|---|---|
| Organiser | View the dashboard; manage owned events, banners and categories; view enrolments and record results. |
| Participant | Browse and enter events; view own entries and results; update personal details and upload a profile picture. |

Both roles register and log in through the API. The MVC website must keep user ID and role in a server-side session, provide the correct navigation and clear the session on logout (The Independent Institute of Education, 2026; Microsoft, 2026b).

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
| Registration, login and role-specific pages | Not started |
| Docker | Not started |

## MVC screenshots

The home page displays live API events and supports province and event-type filters.

![RaceDay Event finder home page](docs/evidence/part3-event-finder-desktop.png)

The [mobile layout](docs/evidence/part3-event-finder-mobile.png) stacks the filters and event details on smaller screens.

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

The home page displays up to three upcoming events, with province and event-type filters. Date filters, pagination, event details and account pages are still to be added. A stopped API produces a friendly message instead of an empty list or raw error. See [MVC settings](docs/Part-3-MVC.md#2-settings).

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

A GitHub result and Part 3 build screenshot are pending until the updated workflow is committed, pushed and passes.

## Unit tests

Part 2 includes 810 tests recorded in the [Part 2 README](README-Part2.md#unit-tests). Part 3 adds 33 MVC checks for API communication, filters, settings, error handling, encoded content and cookie isolation (Microsoft, 2026f).

Local checks on 18 November 2026 passed all 810 API tests and all 33 MVC tests, with none failed or skipped. The Release solution build has zero warnings or errors, planning checks pass and EF reports no pending model changes.

## Project files

| File/folder | Contents |
|---|---|
| [Part 1 README](README-Part1.md) | Original planning and database submission |
| [Part 2 README](README-Part2.md) | API setup, features, tests and evidence |
| [Part 3 checklist](docs/Part-3-Requirements-Checklist.md) | Requirements and outstanding work |
| [MVC notes](docs/Part-3-MVC.md) | API connection, settings, layout and checks |
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
