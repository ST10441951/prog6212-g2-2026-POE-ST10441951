# RaceDay Part 3 - Docker

## 1. Image

The [Dockerfile](../src/RaceDay.Web/Dockerfile) builds and publishes MVC with the .NET 10 SDK, then copies the published files into the ASP.NET Core runtime image. The final stage runs as the non-root `app` user on port 8080. It contains no SDK, API assembly, SQL files or storage credentials (Microsoft, 2026a; 2026b).

The root [.dockerignore](../.dockerignore) sends only MVC source to the build. Build output, user settings, private configuration and certificate/key files are excluded. The API, tests, Git history and evidence are outside the build context (Docker, 2026a).

## 2. Build and run

These steps were tested on Windows with Docker Desktop using Linux containers. First start SQL Server, Azurite and the Part 2 API using the [README setup](../README.md#setup-and-run). The API must be running on port 5120. Its Azure settings stay on the API host.

1. From the repository root, build the image.

```text
docker build --pull --file src/RaceDay.Web/Dockerfile --tag raceday-web:part3 .
```

2. Start MVC with one command.

```text
docker run --detach --rm --name raceday-web --publish 127.0.0.1:5185:8080 --env ASPNETCORE_ENVIRONMENT=Development --env Api__BaseUrl=http://host.docker.internal:5120/ raceday-web:part3
```

3. Open [RaceDay in Docker](http://localhost:5185/) and sign in using the sample accounts. [Host health](http://localhost:5185/health) checks MVC; opening the home page also checks its API connection.

Docker Desktop provides `host.docker.internal` to reach the API on this PC. `localhost` inside the container refers to the container itself. For another API host, change `Api__BaseUrl`; keep its final slash (Docker, 2026b).

The command uses Development and publishes only on this PC for the local HTTP demonstration. The image itself does not force Development. A public Production deployment needs HTTPS and its own settings. MVC sessions are in memory, as in the local app; restarting the container requires signing in again.

View or stop the container:

```text
docker ps --filter name=raceday-web
docker logs raceday-web
docker stop raceday-web
```

`--rm` removes this container when stopped. The image remains available. If the name is already in use, stop that RaceDay container before running it again.

## 3. Checks and evidence

All 518 MVC tests passed locally on 19 November 2026, with none failed or skipped. Six added checks verify fee display across cultures. Fees use one display format because Windows and Linux returned different separators for `en-ZA`; forms still send numeric values to the API (Microsoft, 2026c).

The Linux amd64 image built successfully and ran as UID 1654. Runtime checks confirmed no SDK, API assembly, source files or secret settings in the final image. The host API was reachable through `host.docker.internal`.

Browser checks registered and signed in both roles, saved profiles, events, categories, entries, bibs and result corrections through the API, and verified the SQL changes. Public event details and personal results matched the local website. Picture/banner uploads, display and removal also worked. Container image checks used Azurite; the separate [image checks](Part-3-Images.md#4-checks-and-evidence) verified actual Azure storage.

Wrong roles were blocked. Signing out in Docker left an independent local login active. A restart recovered successfully and required a fresh sign-in. An unreachable API returned a friendly 503 while MVC's host-health endpoint stayed available. Pages passed at 320, 390, 768 and 1280 pixels without overflow or JavaScript errors.

Screenshots use fictional review records and have no cursor overlays. The isolated database, storage container and temporary Docker containers were removed after checking; original sample data was unchanged.

| Check | Evidence |
|---|---|
| Container home | [Desktop](evidence/part3-docker-home.png) / [mobile](evidence/part3-docker-home-mobile.png) |
| Profile changes | [Saved profile](evidence/part3-docker-profile.png) / [picture](evidence/part3-docker-profile-picture.png) |
| Organiser event | [Saved event](evidence/part3-docker-owned-event.png) |
| Public event | [Desktop](evidence/part3-docker-event-details.png) / [mobile](evidence/part3-docker-event-details-mobile.png) |
| Participant entry | [Saved entry](evidence/part3-docker-entry-saved.png) |
| Official result | [Result details](evidence/part3-docker-result-details.png) |
| API outage | [Friendly message](evidence/part3-docker-api-unavailable.png) |
| Verification | [Container check results](evidence/part3-docker-checks.json) |

## References

Microsoft (2026a) 'Containerize a .NET app', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/core/docker/build-container (Accessed: 19 November 2026).

Microsoft (2026b) 'New non-root app user in Linux images', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/core/compatibility/containers/8.0/app-user (Accessed: 19 November 2026).

Microsoft (2026c) 'NumberFormatInfo Class', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/api/system.globalization.numberformatinfo?view=net-10.0 (Accessed: 19 November 2026).

Docker (2026a) 'Build context', *Docker Docs*. Available at: https://docs.docker.com/build/concepts/context/#dockerignore-files (Accessed: 19 November 2026).

Docker (2026b) 'Explore networking how-tos on Docker Desktop', *Docker Docs*. Available at: https://docs.docker.com/desktop/features/networking/networking-how-tos/ (Accessed: 19 November 2026).
