# RaceDay - Part 3

## Project overview

RaceDay manages South African running, walking and cycling events. Part 3 adds an ASP.NET Core MVC website to the Part 2 API, with separate pages for Organisers and Participants, Azure image uploads and Docker (The Independent Institute of Education, 2026; Microsoft, 2026a).

The API is complete. The MVC website and Docker setup have not been implemented yet. Progress is recorded in the [Part 3 checklist](docs/Part-3-Requirements-Checklist.md).

Previous work: [Part 1 README](README-Part1.md) and [Part 2 README](README-Part2.md).

## User roles

| Role | Responsibilities |
|---|---|
| Organiser | View the dashboard; manage owned events, banners and categories; view enrolments and record results. |
| Participant | Browse and enter events; view own entries and results; update personal details and upload a profile picture. |

Both roles register and log in through the API. The MVC website must keep user ID and role in a server-side session, provide the correct navigation and clear the session on logout (The Independent Institute of Education, 2026; Microsoft, 2026b).

## Application

The MVC website will use the API for every data operation. Database access and business rules remain in the API. Files will pass from MVC to the API; only the API communicates with Azure Blob Storage (The Independent Institute of Education, 2026).

The existing API has 40 business endpoints and seven SQL Server tables. See the [endpoint plan](docs/API-Endpoint-Plan.md) and [database notes](docs/Part-2-Database.md).

| Area | Current status |
|---|---|
| Part 1 planning and database | Complete |
| Part 2 API, sessions and tests | Complete |
| API picture and banner endpoints | Implemented; Part 3 Azure demonstration pending |
| MVC pages and role-based navigation | Not started |
| Docker | Not started |

## Setup and run

The instructions below open the existing solution and API. MVC setup will be added when the web project is available.

1. Clone the repository.

```text
git clone https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951.git
cd prog6212-g2-2026-POE-ST10441951
```

2. Open `RaceDay.sln` in Visual Studio with .NET 10 support and the ASP.NET/web workload.
3. Follow the [Part 2 setup steps](README-Part2.md#setup-and-run) to configure SQL Server, apply migrations and start the API and local storage.
4. Open [Swagger](http://localhost:5120/swagger/index.html) to use the existing API. The website is not available yet.

## Azure Blob Storage

The API already supports Participant pictures and Organiser banners. It validates JPEG/PNG files and stores generated blob names in SQL. Reads return temporary image URLs. See the [image notes](docs/Part-2-Images.md) (Microsoft, 2026c).

Part 3 will add upload controls and image display in MVC. The final checks must demonstrate images stored in Azure through the API. Azurite remains available for local development and tests.

## Docker

The MVC Dockerfile, container checks and tested build/run commands are still to be added (Microsoft, 2026d).

## CI/CD

The existing [API workflow](.github/workflows/part2-build.yml) builds and tests Part 2. The [planning workflow](.github/workflows/part1-validation.yml) checks the planning files.

Part 3 must extend CI to build MVC and retain the API tests and repository checks. A green Part 3 build screenshot will be added after the updated workflow passes (GitHub, 2026).

## Unit tests

Part 2 includes 810 passing tests recorded in the [Part 2 README](README-Part2.md#unit-tests). Part 3 MVC and integration checks have not been added or run yet.

## Project files

| File/folder | Contents |
|---|---|
| [Part 1 README](README-Part1.md) | Original planning and database submission |
| [Part 2 README](README-Part2.md) | API setup, features, tests and evidence |
| [Part 3 checklist](docs/Part-3-Requirements-Checklist.md) | Requirements and outstanding work |
| `src/RaceDay.Api/` | Existing API, database mappings, sessions and storage |
| `tests/RaceDay.Api.Tests/` | Existing API and database tests |
| `tools/RaceDay.PlanningChecks/` | Planning-document checks |
| `.github/workflows/` | Existing build and validation workflows |
| `docs/evidence/` | Part 1 and Part 2 screenshots |

## Video

The Part 3 walkthrough has not been recorded. Its unlisted YouTube link will be added here after recording.

## References

GitHub (2026) 'Building and testing .NET', *GitHub Docs*. Available at: https://docs.github.com/en/actions/tutorials/build-and-test-code/net (Accessed: 18 November 2026).

Microsoft (2026a) 'Overview of ASP.NET Core MVC', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/overview?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026b) 'Session and state management in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026c) 'Upload a blob with .NET', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-upload (Accessed: 18 November 2026).

Microsoft (2026d) 'Run an ASP.NET Core app in Docker containers', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/building-net-docker-images?view=aspnetcore-10.0 (Accessed: 18 November 2026).

The Independent Institute of Education (2026) *PROG6212 Portfolio of Evidence*. Part 3 requirements and marking rubric. Assessment brief.
