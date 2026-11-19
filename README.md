# RaceDay - Part 3

[![Build and Test RaceDay](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/workflows/part2-build.yml/badge.svg?branch=main)](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/workflows/part2-build.yml)
[![Validate submission](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/workflows/part1-validation.yml/badge.svg?branch=main)](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/workflows/part1-validation.yml)

## Project overview

RaceDay manages South African running, walking and cycling events. Part 3 adds an ASP.NET Core MVC website to the Part 2 API, with separate pages for Organisers and Participants, Azure image uploads and Docker (The Independent Institute of Education, 2026; Microsoft, 2026a).

The MVC accounts, event pages, Participant entries/results, profiles, image uploads and Organiser management pages work through the API. The website also runs in Docker. Implementation and checks are complete; the video and ARC submission remain. See the [Part 3 checklist](docs/Part-3-Requirements-Checklist.md).

Previous work: [Part 1 README](README-Part1.md) and [Part 2 README](README-Part2.md).

## User roles

| Role | Responsibilities |
|---|---|
| Organiser | View the dashboard; manage owned events, banners and categories; view enrolments and record results. |
| Participant | Browse and enter events; view own entries and results; update personal details and upload a profile picture. |

Both roles register and log in through the API. MVC keeps user ID, role and the API cookie on the server; the browser receives a separate protected session identifier. Each request rechecks the account through the API. The header shows the correct role's home link, guests are redirected to sign-in and the wrong role receives 403. Sign-out clears the session. See the [account notes](docs/Part-3-Accounts.md) (Microsoft, 2026b; 2026g; 2026h).

## Application

The MVC website uses typed HTTP clients to call the API. Database access and business rules remain in the API. Files pass from MVC to the API; only the API communicates with Azure Blob Storage (The Independent Institute of Education, 2026; Microsoft, 2026e).

The existing API has 40 business endpoints and seven SQL Server tables. See the [endpoint plan](docs/API-Endpoint-Plan.md) and [database notes](docs/Part-2-Database.md).

The shared MVC layout uses the Event finder design: blue accents, a light background, compact navigation and white event cards. Bootstrap styles are served locally. Uploaded banners appear on event cards; forms and status badges use consistent styles. See the [interface notes and checks](docs/Part-3-UI.md) (Bootstrap, 2026).

| Area | Current status |
|---|---|
| Part 1 planning and database | Complete |
| Part 2 API, sessions and tests | Complete |
| API picture and banner endpoints | Verified through MVC with private Azure Blob Storage |
| MVC project, API connection and shared layout | Implemented; [MVC notes](docs/Part-3-MVC.md) |
| Public event search, paging, categories and routes | Implemented; [event notes](docs/Part-3-Events.md) |
| Participant entries, changes and My entries | Implemented; [entry notes](docs/Part-3-Entries.md) |
| Registration, login and sign-out | Working for both roles; [account notes](docs/Part-3-Accounts.md) |
| Role-specific navigation and protected home pages | Implemented; API session rechecked before access |
| Personal profiles and password changes | Working for both roles; [profile notes](docs/Part-3-Profiles.md) |
| Participant race history and result details | Implemented; [result notes](docs/Part-3-Results.md) |
| Organiser dashboard and owned-event list | Implemented; [Organiser notes](docs/Part-3-Organiser.md) |
| Organiser event creation, editing and deletion | Implemented; [event management](docs/Part-3-Event-Management.md) |
| Organiser categories and routes | Implemented; [category notes](docs/Part-3-Categories.md) |
| Organiser event enrolments and bib assignment | Implemented; [enrolment notes](docs/Part-3-Enrolments.md) |
| Organiser result capture and corrections | Implemented; [result management](docs/Part-3-Result-Management.md) |
| MVC profile pictures and event banners | Implemented; [image notes](docs/Part-3-Images.md) |
| Docker | MVC image built and tested; [Docker notes](docs/Part-3-Docker.md) |
| Interface and mobile layouts | Checked locally and in Docker; [interface notes](docs/Part-3-UI.md) |

## MVC screenshots

The home page displays live API events and supports province, event type and date filters. Pagination keeps the selected filters. The search screenshots use temporary review fixtures to show multiple pages.

![RaceDay Event finder home page](docs/evidence/part3-event-finder-desktop.png)

The [mobile layout](docs/evidence/part3-event-finder-mobile.png) stacks the filters and event details on smaller screens.

Event names open full details with categories and routes. See [event details](docs/evidence/part3-event-details-desktop.png), the [mobile detail page](docs/evidence/part3-event-details-mobile.png) and [filtered pagination](docs/evidence/part3-event-pagination.png).

Both roles use the same sign-in form. The [registration page](docs/evidence/part3-registration.png) includes account type and optional personal details.

![RaceDay sign-in page](docs/evidence/part3-login.png)

Protected pages: [Organiser home](docs/evidence/part3-organiser-home.png), [Participant home](docs/evidence/part3-participant-home.png) and [access denied](docs/evidence/part3-access-denied.png). These pages retain the same Event finder style.

Participant entry: [select category](docs/evidence/part3-enter-event.png), [saved entry](docs/evidence/part3-entry-confirmation.png) and [My entries](docs/evidence/part3-my-entries.png). The screenshots use fictional review accounts.

Entry changes: [cancel confirmation](docs/evidence/part3-entry-cancel-confirmation.png), [reactivation](docs/evidence/part3-entry-reactivate.png) and [delete confirmation](docs/evidence/part3-entry-delete-confirmation.png).

Profiles: [personal details](docs/evidence/part3-profile.png), [edit form](docs/evidence/part3-profile-edit.png) and [password change](docs/evidence/part3-password-change.png). Both roles have a My profile link.

Personal results: [race history](docs/evidence/part3-my-results.png), [result details](docs/evidence/part3-result-details.png) and [non-finishing result](docs/evidence/part3-result-did-not-finish.png). These screenshots use temporary review fixtures.

Organisers: [dashboard](docs/evidence/part3-organiser-home.png), [owned events](docs/evidence/part3-owned-events.png) and [Draft filter](docs/evidence/part3-owned-drafts.png). The dashboard shows API totals and dates for every owned event.

Event management: [create form](docs/evidence/part3-event-create.png), [saved Draft](docs/evidence/part3-event-created.png), [edit form](docs/evidence/part3-event-edit.png) and [delete confirmation](docs/evidence/part3-event-delete-confirmation.png). The screenshots use fictional review events.

Categories and routes: [category list](docs/evidence/part3-categories.png), [category form](docs/evidence/part3-category-create.png) and [saved route](docs/evidence/part3-route-details.png).

Event enrolments: [roster](docs/evidence/part3-event-enrolments.png), [entry details](docs/evidence/part3-roster-entry-details.png), [saved bib](docs/evidence/part3-bib-saved.png) and [duplicate rejection](docs/evidence/part3-bib-duplicate.png). These screenshots use fictional review entries.

Official results: [event results](docs/evidence/part3-event-results.png), [capture form](docs/evidence/part3-result-capture.png), [correction](docs/evidence/part3-official-result-corrected.png) and [refreshed Participant result](docs/evidence/part3-participant-result-refreshed.png).

Azure images: [saved profile picture](docs/evidence/part3-azure-profile-picture.png), [event banner](docs/evidence/part3-azure-event-banner.png), [public event](docs/evidence/part3-azure-public-event-banner.png) and [rejected file](docs/evidence/part3-azure-image-rejected.png). Screenshots use fictional accounts and test graphics.

Docker: [home](docs/evidence/part3-docker-home.png), [saved event](docs/evidence/part3-docker-owned-event.png), [entry](docs/evidence/part3-docker-entry-saved.png) and [official result](docs/evidence/part3-docker-result-details.png).

Current interface: [desktop event finder](docs/evidence/part3-ui-guest-home.png), [mobile event finder](docs/evidence/part3-ui-guest-home-mobile.png), [Organiser form](docs/evidence/part3-ui-organiser-organiser-events-create.png) and [result validation](docs/evidence/part3-ui-result-validation-mobile.png).

## Setup and run

Install the .NET 10 SDK, SQL Server LocalDB and Node.js with npm. Visual Studio needs .NET 10 support and the ASP.NET/web workload. For the container steps, install Docker Desktop and start its Linux container engine.

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

3. Set the API's local database connection and apply migrations. This creates the tables and fictional sample accounts; rerunning preserves saved data (Microsoft, 2026j).

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__RaceDay = 'Server=(localdb)\MSSQLLocalDB;Database=RaceDayPart2;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet ef database update --project src/RaceDay.Api --configuration Release --no-build
```

Use another SQL Server by changing the connection string. Keep private settings in environment variables or user secrets. Do not apply the Part 1 SQL script to this migration-managed database.

Start the local image emulator in a separate terminal and keep it running for image tests (Microsoft, 2026k).

```text
npx --yes --package azurite@3.37.0 azurite-blob --blobHost 127.0.0.1 --location .azurite --silent
```

4. Start the API in the same PowerShell terminal used for the database commands, so it keeps the selected connection string.

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

| Role | Local sample email | Password |
|---|---|---|
| Organiser | thabo.ndlovu@example.com | RaceDayDemo!2026 |
| Organiser | ayesha.khan@example.com | RaceDayDemo!2026 |
| Participant | naledi.mokoena@example.com | RaceDayDemo!2026 |
| Participant | sipho.dlamini@example.com | RaceDayDemo!2026 |

These accounts are fictional. Event 1 has past results; events 2 and 3 are upcoming while their dates remain in the future. Register a separate account for password-change checks.

Use Create account or Sign in in the header. Registration redirects to sign-in; login opens `/organiser` or `/participant`. A valid local return address takes the user back to the requested page. External destinations are rejected (Microsoft, 2026i). The fictional [Part 2 sample accounts](README-Part2.md#setup-and-run) also work through MVC.

Participants can open an Open event, choose Enter event, select a category and confirm. My entries lists their saved records and statuses. Age-restricted categories require a date of birth on the account; include it when registering or add it under My profile.

Open a saved entry to cancel, reactivate or delete it. Cancel keeps the history; Delete requires explicit confirmation and removes a permitted Confirmed entry. Reactivation can keep or change category. The API rejects closed entries, full categories, recorded results and other users' records.

Both roles can use My profile to view their account, edit names, phone number and birth date, or change their password. Blank optional fields are cleared. A successful password change signs out the current browser; sign in again with the new password. Email and role cannot be changed here. See the [profile notes](docs/Part-3-Profiles.md).

Participants can open My results to see official race history, filter by outcome and open a saved result. Times appear as hours:minutes:seconds, with positions and event-wide finisher totals from the API. Did not finish, Did not start and Disqualified results have no official time or rank. The Organiser must record a result before it appears here.

Organiser home shows event totals, upcoming event count and saved Confirmed/Cancelled entries, with dates and counts per event. My events lists all owned records, including Draft and historical events, with status filters and ten events per page.

Use Create event to save a Draft. Event names open private details with Edit event and Delete event. Enter times in SAST; the API stores UTC. Status readiness, saved history and dependent records can prevent changes. A rejected form shows the reason and keeps the entered details. Delete requires confirmation. See the [event-management notes](docs/Part-3-Event-Management.md).

Open Manage categories and routes on an owned event to add distance or age categories, fees, capacity and availability. Each category has route details with add/edit/remove controls. Age limits, capacity, map URL and elevation are optional. Category and route removal require confirmation. See the [category notes](docs/Part-3-Categories.md).

Use View enrolments on an owned event to filter entries by status and category. Open a Participant's entry to assign or correct its bib number. The API requires bibs to be unique within that event, including Cancelled entries. See the [enrolment notes](docs/Part-3-Enrolments.md).

After an event, open an enrolment and choose Record result. Enter the outcome, elapsed finish time, optional positions and notes. Saved results can be corrected from View results. The API checks event timing and entry status, and updates the Participant's history. See [result management](docs/Part-3-Result-Management.md).

Participants use Manage profile picture under My profile. Organisers can choose an optional banner when creating/editing an event, or use Manage banner on event details. Upload one JPEG/PNG up to 5 MiB. Removal requires confirmation; replacement keeps the current image if rejected. See the [image notes](docs/Part-3-Images.md).

A stopped API produces a friendly message instead of an empty list or raw error. See [MVC settings](docs/Part-3-MVC.md#2-settings).

7. Run tests with the API test database server and Azurite available. The MVC tests do not need the API running (Microsoft, 2026f).

```text
dotnet test RaceDay.sln --configuration Release --no-build
```

## Azure Blob Storage

The API already supports Participant pictures and Organiser banners. It validates JPEG/PNG files and stores generated blob names in SQL. Reads return temporary image URLs. See the [image notes](docs/Part-2-Images.md) (Microsoft, 2026c).

MVC sends image files through the API and displays the returned temporary URLs. The API alone accesses storage. The Azure setup uses private container `raceday-images` in account `racedayst10441951`. Put the connection string and container setting in the API project's private user secrets and start it in Development. See [storage settings](docs/Part-3-Images.md#3-storage-settings). Azurite remains available for local development and tests.

On 19 November 2026, MVC uploads, replacement, removal, private signed reads and image display passed against Azure. Returned URLs used the Azure hostname; anonymous reads were denied. SQL references and old-blob removal were verified. See the [image checks](docs/Part-3-Images.md#4-checks-and-evidence).

## Docker

Keep the Part 2 API running on port 5120. From the repository root, build the MVC image (Microsoft, 2026d).

```text
docker build --pull --file src/RaceDay.Web/Dockerfile --tag raceday-web:part3 .
```

Start the website with one command, then open [RaceDay in Docker](http://localhost:5185/).

```text
docker run --detach --rm --name raceday-web --publish 127.0.0.1:5185:8080 --env ASPNETCORE_ENVIRONMENT=Development --env Api__BaseUrl=http://host.docker.internal:5120/ raceday-web:part3
```

This is the local HTTP demonstration on Docker Desktop. The container runs MVC as a non-root user and calls the host API; database and Azure settings stay on the API host. See [Docker setup and checks](docs/Part-3-Docker.md). Stop it with `docker stop raceday-web`.

## CI/CD

The [build workflow](.github/workflows/part2-build.yml) now builds the API, MVC and both test projects. The [planning workflow](.github/workflows/part1-validation.yml) checks the planning files and required Part 3 files (GitHub, 2026).

Both workflows passed for `81c3cf1` using temporary Windows runners on my PC: [build and tests](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/44499288535) and [planning validation](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/44499288543). The build ran 810 API tests and 518 MVC tests, with none failed or skipped. The migration check also passed.

![Passing GitHub build and tests](docs/evidence/part3-ci-build-success.png)

The [planning screenshot](docs/evidence/part3-ci-planning-success.png) records the second green check. Earlier hosted jobs were blocked by an account billing issue. `RACEDAY_USE_LOCAL_RUNNER` selects the temporary local runner; each job needs a new registration and the PC online. No tests were skipped to obtain these results. See the [CI notes](docs/Part-3-CI.md).

The account-cookie correction is included in these passing checks. See the [rubric review](docs/Part-3-Review.md).

The later category-creation correction passes all 93 category tests locally, including six duplicate-creation races. The badges above show the latest checks on `main`.

## Unit tests

Part 2 includes 810 tests recorded in the [Part 2 README](README-Part2.md#unit-tests). MVC now has 518 checks covering browsing, entries, results, profiles, Organiser events, categories/routes, rosters/bibs, result capture/corrections, image uploads, accounts, validation, cookies, role restrictions and redirects (Microsoft, 2026f).

All 518 MVC checks passed locally on 19 November 2026, with none failed or skipped. The 810 API tests also passed in GitHub on that date. The Release build has zero warnings or errors, and planning checks pass. Browser checks verified both roles, saved changes and private records against isolated databases. See the [account](docs/Part-3-Accounts.md#5-checks-and-evidence), [event](docs/Part-3-Events.md#3-checks-and-evidence), [entry](docs/Part-3-Entries.md#4-checks-and-evidence), [profile](docs/Part-3-Profiles.md#4-checks-and-evidence), [result](docs/Part-3-Results.md#4-checks-and-evidence), [Organiser](docs/Part-3-Organiser.md#3-checks-and-evidence), [enrolment](docs/Part-3-Enrolments.md#3-checks-and-evidence), [result-management](docs/Part-3-Result-Management.md#3-checks-and-evidence) and [image checks](docs/Part-3-Images.md#4-checks-and-evidence).

## Project files

| File/folder | Contents |
|---|---|
| [Part 1 README](README-Part1.md) | Original planning and database submission |
| [Part 2 README](README-Part2.md) | API setup, features, tests and evidence |
| [Part 3 checklist](docs/Part-3-Requirements-Checklist.md) | Requirements and outstanding work |
| [MVC notes](docs/Part-3-MVC.md) | API connection, settings, layout and checks |
| [Account notes](docs/Part-3-Accounts.md) | Registration, login, protected pages, sessions and sign-out |
| [Event notes](docs/Part-3-Events.md) | Search, paging, public details, categories, routes and checks |
| [Entry notes](docs/Part-3-Entries.md) | Category selection, saved entries, private history and checks |
| [Profile notes](docs/Part-3-Profiles.md) | Personal details, password changes and checks |
| [Result notes](docs/Part-3-Results.md) | Private race history, finish times, positions and checks |
| [Organiser notes](docs/Part-3-Organiser.md) | Dashboard totals, owned events, filters and checks |
| [Event management](docs/Part-3-Event-Management.md) | Draft creation, editing, status changes, deletion and checks |
| [Category notes](docs/Part-3-Categories.md) | Age/distance categories, route management and checks |
| [Enrolment notes](docs/Part-3-Enrolments.md) | Event rosters, filters, bib numbers and checks |
| [Result management](docs/Part-3-Result-Management.md) | Official event results, capture, corrections and checks |
| [Image notes](docs/Part-3-Images.md) | Profile pictures, event banners, private storage and checks |
| [Docker notes](docs/Part-3-Docker.md) | Image build, run command, API address and container checks |
| [Interface notes](docs/Part-3-UI.md) | Shared styles, status colours, navigation and responsive checks |
| [Rubric review](docs/Part-3-Review.md) | Requirement mapping, final checks, corrections and outstanding submission tasks |
| [CI notes](docs/Part-3-CI.md) | Hosted billing failure, local runner and workflow checks |
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

Microsoft (2026j) 'Applying Migrations', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying (Accessed: 19 November 2026).

Microsoft (2026k) 'Use the Azurite emulator for local Azure Storage development', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite (Accessed: 19 November 2026).

Bootstrap (2026) 'Download', *Bootstrap v5.3*. Available at: https://getbootstrap.com/docs/5.3/getting-started/download/ (Accessed: 18 November 2026).

The Independent Institute of Education (2026) *PROG6212 Portfolio of Evidence*. Part 3 requirements and marking rubric. Assessment brief.
