# RaceDay - Part 2

## Project overview

RaceDay manages South African running, walking and cycling events. Part 2 builds the REST API in C# and ASP.NET Core, using SQL Server and Entity Framework Core (The Independent Institute of Education, 2026a).

The API will follow the [Part 1 endpoint plan](docs/API-Endpoint-Plan.md). Login will store the user's ID and role in a server-side session. Passwords will be hashed (Microsoft, 2026a).

## Part 2 status

The endpoint plan now has 40 endpoints and a [mapping of the lecturer's 25 required operations](docs/API-Endpoint-Plan.md#141-required-operations). Enrolment deletion, status changes and age rules have been clarified.

The Part 1 planning and database corrections are complete. All 71 column definitions match, 21 database checks passed and the corrected script ran successfully in SSMS. The [database correction notes](docs/Part-1-Database-Corrections.md) include the fresh screenshots and results.

The C# planning checker builds and passes locally and on GitHub. API development has not started.

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

The EF Core model and migrations must match the corrected Part 1 database design (Microsoft, 2026b).

## API endpoints

The API will cover authentication, profiles, events, categories, routes, enrolments and results. It will also include the password, dashboard, bib-number and image operations already in the endpoint plan.

Profile pictures and event banners will pass through the API. SQL Server will store their blob names. The MVC website and Docker setup belong to Part 3.

## Setup and run

The API has not been built yet. The exact settings, commands and URLs will be added as they are tested.

1. Clone the repository using the command below (GitHub, 2026a).
2. Open `RaceDay.sln` in Visual Studio with the ASP.NET workload and required .NET SDK.
3. Set the API's SQL Server connection string for a separate Part 2 database. Keep private credentials out of Git.
4. Create and seed the database using EF Core migrations. Keep the Part 1 database unchanged.
5. Build and run the API.
6. Open Swagger, log in and test the endpoints (Microsoft, 2026c).
7. Open Test Explorer and select **Run All**.

```text
git clone https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951.git
```

## CI/CD

The planning checks are written in C# and use the .NET 10 SDK. They check the required files, image formats, all 40 endpoint rows, the lecturer's 25 required operations and matching SQL, ERD and data-dictionary columns. The workflow builds and runs them on every push and on pull requests to main (GitHub, 2026b; 2026c).

The [C# planning build for 4560c17](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39063690485) passed on GitHub. The local checks also rejected all 13 invalid-document cases.

The Part 2 workflow will also build the API and run its tests on every push.

Part 2 green-build screenshot and run link: to be added.

## Unit tests

Tests will cover registration, login, sessions, roles, ownership, event management and enrolments, with success and failure cases. The other API operations will also be tested.

The tests must pass locally and in GitHub Actions. A screenshot of the passing **Run All** result will be added here.

## Project files

| File or folder | Contents |
|---|---|
| `RaceDay.sln` | Visual Studio solution, including the C# planning checker |
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

Microsoft (2026a) 'Session and state management in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-10.0 (Accessed: 17 October 2026).

Microsoft (2026b) 'Migrations overview', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/ (Accessed: 17 October 2026).

Microsoft (2026c) 'ASP.NET Core web API documentation with Swagger / OpenAPI', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/tutorials/web-api-help-pages-using-swagger?view=aspnetcore-8.0 (Accessed: 17 October 2026).

The Independent Institute of Education (2026a) *PROG6212 Portfolio of Evidence*. Part 1 and Part 2 requirements and Part 2 marking rubric. Assessment brief.

The Independent Institute of Education (2026b) *Announcement for the Part II*. Module announcement covering the video structure and Part 2 README requirements.
