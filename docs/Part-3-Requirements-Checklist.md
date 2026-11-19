# RaceDay Part 3 Checklist

Based on the Part 3 requirements and marking rubric (The Independent Institute of Education, 2026). Tick only completed and verified work.

## Correct submission: 5 marks

- [x] Preserve the original Part 1 README.
- [x] Preserve the Part 2 README separately.
- [x] Create the Part 3 README with an overview, roles and references.
- [x] Add the Part 3 requirements checklist.
- [x] Keep MVC, API, tests, documents and evidence clearly organised.
- [x] Add complete local setup steps, settings and application URLs.
- [x] Include a tested Docker run command and green CI screenshot.
- [ ] Add the Part 3 unlisted video link.
- [ ] Submit the correct GitHub repository link on ARC.

## MVC application

- [x] Add a clearly named ASP.NET Core MVC project for Part 3.
- [x] Connect the home page to the API and handle unavailable or unreadable responses.
- [x] Add a shared layout with locally served Bootstrap styles.
- [x] Apply the selected Event finder design and connect province/type filters to the API.
- [x] Connect date filters, pagination and public event details to the API.
- [x] Disable shared HTTP cookies and automatic redirects in the API client.
- [x] Use the Part 2 API for every data operation.
- [x] Keep database access and business rules in the API.
- [x] Keep each signed-in user's API session separate from other users.
- [x] Display API validation and service errors without raw exception details.

## GitHub and CI/CD: 10 marks

- [x] Make at least 20 meaningful Part 3 commits using my GitHub account.
- [x] Build MVC and the API in GitHub Actions.
- [x] Retain the API tests and repository-structure checks.
- [x] Run the committed API and MVC tests in CI; 810 API and 512 MVC tests passed for `8316f29`.
- [ ] Confirm a green build for the final submitted revision.
- [x] Add current green CI screenshots and working build links to the README.

## Authentication and role-based navigation: 10 marks

- [x] Register as Organiser or Participant through MVC and the API.
- [x] Log in with both roles and keep user ID and role in a server-side session.
- [x] Maintain authentication across pages and handle expired API sessions.
- [x] Provide distinct menus and landing pages for both roles.
- [x] Redirect unauthenticated users away from protected pages.
- [x] Block the wrong role and retain API ownership checks.
- [x] Log out through the API, clear the MVC session and redirect appropriately.
- [x] Protect forms against cross-site request forgery.

## Organiser interface: 15 marks

- [x] Show owned events, enrolment totals per event and upcoming dates on the dashboard.
- [x] List owned events in every status with filtering and pagination.
- [x] Create, edit and delete owned events through the API.
- [x] Include name, description, date, location, distance and event type.
- [x] Show API status and history restrictions when a change is rejected.
- [x] Upload and display event banners when creating or editing events.
- [x] Add, edit and delete age or distance categories for owned events.
- [x] Display and manage the existing category route information.
- [x] View event enrolments with Participants, selected categories and statuses.
- [x] Connect the existing bib-number assignment functionality.
- [x] Capture finish times and positions after an event and correct saved results.
- [x] Show saved results in the Participant's personal history.

## Participant interface: 15 marks

- [x] Show upcoming events with filtering and pagination on the home page.
- [x] Display full event details, available categories and route information.
- [x] Enter an event by selecting a category while signed in.
- [x] Save the enrolment immediately through the API and show confirmation.
- [x] Show personal event entries and enrolment statuses.
- [x] Connect permitted cancellation, reactivation and deletion actions.
- [x] Show My Results with event name, date, category and finish time.
- [x] Show finishing position and total finishers, including non-finishing outcomes.
- [x] View and update personal profile information.
- [x] Connect password changes and the required sign-in afterwards.
- [x] Upload and display a Participant profile picture.

## Azure Blob Storage: 10 marks

- [x] Send picture and banner files from MVC to the API as multipart uploads.
- [x] Keep storage credentials and Azure access in the API.
- [x] Verify actual Azure Blob Storage uploads through the MVC pages.
- [x] Save image references through the API and display images in MVC.
- [x] Display event banners on event details and pictures on profiles.
- [x] Verify replacement, removal, invalid files and storage errors.
- [x] Keep real credentials out of Git and document the required settings.

## Docker: 10 marks

- [x] Add a multi-stage MVC Dockerfile using appropriate SDK and runtime images.
- [x] Add a .dockerignore and keep credentials out of the image.
- [x] Build the image cleanly and run the MVC website in a container.
- [x] Configure a reachable API address for the container.
- [x] Check login, protected pages, API operations and image display in Docker.
- [x] Compare the local and container application behaviour.
- [x] Add tested build instructions and a single Docker run command to the README.

## UI design: 10 marks

- [x] Use a consistent sporting RaceDay theme, colours and typography.
- [x] Use Bootstrap or a similar framework for responsive layouts.
- [x] Check the home page on desktop and mobile and save screenshots.
- [x] Make every page accessible through the correct role's navigation.
- [x] Use distinct status colours and text that match the API statuses.
- [x] Keep forms, buttons, tables, spacing and images consistent.
- [x] Handle invalid input, empty lists and unavailable services clearly.
- [x] Check desktop and mobile layouts.

## Testing and evidence

- [x] Pass all 518 MVC checks, including Organiser reads, results, profiles, accounts and protected pages.
- [x] Register, sign in and sign out as both roles against an isolated real API/database.
- [x] Capture login and registration screenshots without cursor overlays.
- [x] Retain the passing Part 2 tests and add relevant MVC checks.
- [x] Pass all 810 API regression tests locally with none failed or skipped.
- [x] Test both complete user journeys and saved changes.
- [x] Test role restrictions, session expiry and isolation between users.
- [x] Verify disabled, deleted and role-changed accounts lose MVC access.
- [x] Check protected role pages on desktop/mobile and capture access-denied evidence.
- [x] Verify filters, paging, event/category/route details and hidden events against an isolated API/database.
- [x] Capture genuine desktop/mobile event pages and pagination evidence.
- [x] Verify entry saves in SQL and reject duplicates, age, capacity and deadline failures through the API.
- [x] Verify private entries cannot be read by another Participant or an Organiser.
- [x] Verify read-only confirmations, protected changes, retained history/bibs and permitted deletion in SQL.
- [x] Verify closed/full entry changes and result-bearing history restrictions through MVC.
- [x] Verify both roles' profile changes in SQL and reject protected-field changes.
- [x] Verify password changes, cleared inputs, new sign-in and rejection of old credentials/cookies.
- [x] Check profile pages on desktop/mobile and capture evidence.
- [x] Verify private results, filters, paging, times, positions and non-finishing outcomes against the real API.
- [x] Verify API corrections refresh personal results and event-wide finisher totals.
- [x] Check result pages on desktop/mobile and capture evidence.
- [x] Verify Organiser totals against SQL, private ownership, filters, paging and refreshed counts.
- [x] Check Organiser dashboard/list pages on desktop/mobile and capture evidence.
- [x] Verify Draft creation, SAST/UTC times, edits, protected fields and permitted deletion in SQL.
- [x] Verify status readiness, saved-history conflicts, confirmation steps and preserved related records.
- [x] Check event-management forms on desktop/mobile and capture evidence.
- [x] Verify category/route saves, optional fields, safe links and permitted removal in SQL.
- [x] Verify age, duplicate-name, capacity, availability and saved-history restrictions through MVC.
- [x] Check category/route pages on desktop/mobile and preserve entries/results during route changes.
- [x] Verify roster counts, combined filters and retained pagination against the real API/database.
- [x] Verify private roster access and bib saves, duplicates and protected fields through MVC.
- [x] Check roster/bib pages on desktop/mobile and preserve Cancelled/history records and results.
- [x] Verify result capture/corrections in SQL and refresh Participant history and finisher totals.
- [x] Verify duplicate results, future events, Cancelled entries, private ownership and protected recording details.
- [x] Check result-management forms and lists on desktop/mobile and capture evidence.
- [x] Verify picture/banner uploads, replacements and removals against Azure and SQL.
- [x] Verify private read-only image URLs, old-blob deletion and public event display.
- [x] Check image pages on desktop/mobile, rejected files, storage failures and account isolation.
- [x] Verify Docker registration, both roles, saved API operations, images and independent sessions.
- [x] Compare local/container pages, correct fee formatting and check restart/outage behaviour.
- [x] Verify a non-root runtime image without the SDK, backend files or credentials.
- [x] Check 61 pages at five widths locally and in Docker, including labels and active navigation.
- [x] Verify keyboard skip links, long names, banner cards and consistent result errors.
- [x] Verify account switching, deletion-cookie handling and replaced-cookie rejection locally and in Docker.
- [x] Test invalid input, API failures and rejected business operations.
- [x] Capture genuine MVC, Azure, Docker and CI screenshots.
- [x] Keep screenshots readable and free of cursor overlays.
- [x] Review every rubric item against working features and evidence.

## Video: 15 marks

- [ ] Demonstrate the running MVC application and both roles.
- [ ] Cover all required Organiser and Participant pages.
- [ ] Explain API communication, sessions and role-based access.
- [ ] Show uploads reaching Azure through the API and images displayed in MVC.
- [ ] Show the Docker setup, run command and working container.
- [ ] Explain the main code and design decisions in my own voice.
- [ ] Show the green GitHub build and supporting evidence.
- [ ] Upload as an unlisted YouTube video and verify the link in the README.

## References

The Independent Institute of Education (2026) *PROG6212 Portfolio of Evidence*. Part 3 requirements and marking rubric. Assessment brief.

Technical sources are recorded in the [README references](../README.md#references) and cited alongside the implementation.
