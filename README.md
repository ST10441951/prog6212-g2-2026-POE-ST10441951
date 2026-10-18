# RaceDay - Part 2

## Project overview

RaceDay manages South African running, walking and cycling events. Part 2 builds the REST API in C# and ASP.NET Core, using SQL Server and Entity Framework Core (The Independent Institute of Education, 2026a).

The API will follow the [Part 1 endpoint plan](docs/API-Endpoint-Plan.md). Login will store the user's ID and role in a server-side session. Passwords will be hashed (Microsoft, 2026a).

## Part 2 status

The endpoint plan now has 40 endpoints and a [mapping of the lecturer's 25 required operations](docs/API-Endpoint-Plan.md#141-required-operations). Enrolment deletion, status changes and age rules have been clarified.

The Part 1 planning and database corrections are complete. All 71 column definitions match, 21 database checks passed and the corrected script ran successfully in SSMS. The [database correction notes](docs/Part-1-Database-Corrections.md) include the fresh screenshots and results.

The C# API and xUnit test projects are set up. Swagger shows the working `GET /health` host check, and all seven startup tests pass locally. SQL Server integration and the 40 planned business endpoints have not been implemented yet.

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

The EF Core model and migrations must match the corrected Part 1 database design (Microsoft, 2026b). The current API host does not connect to SQL Server. Connection-string and database creation steps will be added with the EF Core model.

## API endpoints

The API will cover authentication, profiles, events, categories, routes, enrolments and results. It will also include the password, dashboard, bib-number and image operations already in the endpoint plan.

Profile pictures and event banners will pass through the API. SQL Server will store their blob names. The MVC website and Docker setup belong to Part 3.

`GET /health` is a separate public host check. It returns `200 OK` with `{"status":"Healthy"}`. It uses ASP.NET Core controller routing and JSON responses (Microsoft, 2026c). It does not check the database, storage or login, and is not one of the 40 planned business endpoints.

The current [Swagger host-check screenshot](docs/evidence/swagger-host-check.jpg) shows the response.

## Setup and run

Install the .NET 10 SDK. For Visual Studio, use a version that supports .NET 10 with the **ASP.NET and web development** workload. SQL Server is not needed for the current host check.

1. Clone the repository and open its folder (GitHub, 2026a).

```text
git clone https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951.git
cd prog6212-g2-2026-POE-ST10441951
```

2. Restore the packages and build the solution.

```text
dotnet restore RaceDay.sln
dotnet build RaceDay.sln --configuration Release --no-restore
```

3. Start the API using the local HTTP profile.

```text
dotnet run --project src/RaceDay.Api --configuration Release --no-build --launch-profile http
```

4. Open [Swagger](http://localhost:5120/swagger/index.html). Expand `GET /health`, select **Try it out**, then **Execute**. The response should be `200` with `{"status":"Healthy"}`. The Swagger JSON is at `http://localhost:5120/swagger/v1/swagger.json` (Swashbuckle, 2026).

5. Leave the API running and open another terminal in the repository folder to run the tests (Microsoft, 2026e).

```text
dotnet test RaceDay.sln --configuration Release --no-build
```

In Visual Studio, open `RaceDay.sln`, set `RaceDay.Api` as the startup project and choose the `http` profile. Open **Test > Test Explorer** and select **Run All** to run the tests there.

The HTTP profile is for local development. Outside Development, Swagger is disabled and the API enables HTTPS redirection and HSTS. Database credentials and migrations will be configured when the SQL Server connection is added.

## CI/CD

The planning checks are written in C# and use the .NET 10 SDK. They check the required files, image formats, all 40 endpoint rows, the lecturer's 25 required operations and matching SQL, ERD and data-dictionary columns. The workflow builds and runs them on every push and on pull requests to main (GitHub, 2026b; 2026c).

The [C# planning build for 4560c17](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39063690485) passed on GitHub. The local checks also rejected all 13 invalid-document cases.

The new `.github/workflows/part2-build.yml` restores, builds and tests the whole solution on every push and on pull requests to main. These commands pass locally. Its first GitHub run is pending (GitHub, 2026d).

Part 2 green-build screenshot and run link: to be added.

## Unit tests

The seven current startup checks use xUnit and an in-memory ASP.NET Core test host. They cover the health response in Development and Production, the Swagger document and UI assets, Swagger being unavailable in Production, and JSON Problem Details for an unknown route (Microsoft, 2026d; xUnit.net, 2026).

All seven pass locally. They are infrastructure checks; registration, login, sessions, roles, ownership, event management and enrolment tests will be added with those features.

The tests must pass locally and in GitHub Actions. A screenshot of the passing **Run All** result will be added here.

## Project files

| File or folder | Contents |
|---|---|
| `RaceDay.sln` | API, test and planning-checker projects |
| `src/RaceDay.Api/` | ASP.NET Core API, controllers and response contracts |
| `tests/RaceDay.Api.Tests/` | xUnit API tests |
| `README-Part1.md` | Original Part 1 README |
| [API-Endpoint-Plan.md](docs/API-Endpoint-Plan.md) | Planned endpoints |
| [RaceDay-Data-Dictionary.md](docs/RaceDay-Data-Dictionary.md) | Tables, fields and constraints |
| [RaceDay.sql](docs/RaceDay.sql) | Part 1 database script |
| [Part-1-Database-Corrections.md](docs/Part-1-Database-Corrections.md) | Corrected database checks and fresh SSMS evidence |
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

Microsoft (2026a) 'Session and state management in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-10.0 (Accessed: 17 October 2026).

Microsoft (2026b) 'Migrations overview', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/ (Accessed: 17 October 2026).

Microsoft (2026c) 'Create web APIs with ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-10.0 (Accessed: 17 October 2026).

Microsoft (2026d) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 17 October 2026).

Microsoft (2026e) 'dotnet test', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test (Accessed: 17 October 2026).

Swashbuckle (2026) *Swashbuckle.AspNetCore*. Available at: https://github.com/domaindrivendev/Swashbuckle.AspNetCore (Accessed: 17 October 2026).

xUnit.net (2026) 'Getting started with xUnit.net v2'. Available at: https://xunit.net/docs/getting-started/v2/getting-started (Accessed: 17 October 2026).

The Independent Institute of Education (2026a) *PROG6212 Portfolio of Evidence*. Part 1 and Part 2 requirements and Part 2 marking rubric. Assessment brief.

The Independent Institute of Education (2026b) *Announcement for the Part II*. Module announcement covering the video structure and Part 2 README requirements.
