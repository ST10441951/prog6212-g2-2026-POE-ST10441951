# RaceDay Part 2 Checklist

Based on the Part 2 brief, rubric and lecturer's announcement. The Part 1 corrections also follow the general feedback (The Independent Institute of Education, 2026a; 2026b; 2026c).

Tick each item after it has been completed and checked.

## Part 1 corrections

- [x] Match the endpoint plan to the lecturer's 25 required operations.
- [x] Add enrolment DELETE and explain when deletion is allowed.
- [x] Explain how cancellation and reactivation handle status changes.
- [x] Cover minimum age, maximum age and missing dates of birth.
- [x] Keep all existing planned endpoints, including image uploads.
- [x] Let the owning Organiser read new Draft events, categories and routes through their creation locations.
- [x] Match names across the ERD, data dictionary and SQL.
- [x] Add datatypes, nullability and clear composite keys to the ERD.
- [x] Update the draw.io, PNG and SVG files.
- [x] Fix the Completed result check so a finish time is required.
- [x] Fix the SQL guard and prevent success messages after errors.
- [x] Test the script and invalid values on a separate clean database.
- [x] Update SSMS evidence and document statuses for the corrected script.
- [x] Add required method-and-route checks and matching schema-document checks to the planning workflow.
- [x] Use C# for the planning and repository checks.
- [x] Confirm the C# planning workflow is green on GitHub after pushing it.

The [database correction notes](Part-1-Database-Corrections.md) record the local checks. These are SQL checks, not Part 2 API unit tests.

## Correct submission: 5 marks

- [x] Keep the original Part 1 README separately.
- [x] Add the main Part 2 README and checklist.
- [x] Add the .NET 10 API and xUnit test projects to the solution.
- [x] Keep the API, tests, documents and evidence clearly organised.
- [ ] Explain the API and both roles in the completed README.
- [x] Give the .NET SDK requirement and numbered clone, restore, build and startup steps.
- [x] Give a tested connection-string example and database creation commands.
- [x] Give the API startup command, URL and Swagger address.
- [x] Explain the test command and Test Explorer Run All.
- [ ] Add the green CI screenshot and run link.
- [ ] Add the passing unit-test screenshot and unlisted video link.
- [ ] Submit the repository link on ARC.

## GitHub and CI/CD: 10 marks

- [x] Use a separate Part 2 branch.
- [ ] Make at least 20 meaningful Part 2 commits using my own account.
- [x] Keep the workflow in `.github/workflows/`.
- [x] Configure the Part 2 workflow to build the solution and run all tests on every push.
- [x] Confirm the Part 2 API build and test workflow is green on GitHub.
- [x] Confirm the SQL Server tests and migration check pass on GitHub.
- [x] Confirm the authentication changes and tests pass on GitHub.
- [x] Keep the Part 1 file and endpoint checks working locally.
- [ ] Get a green build for the final submitted revision.

## Authentication and role-based access: 20 marks

- [x] Register and log in as Organiser and Participant.
- [x] Validate account fields and reject duplicate emails.
- [x] Hash passwords and keep them out of responses and session data.
- [x] Store the signed-in user's ID and role in a server-side session.
- [x] End access after logout or session expiry and reject inactive accounts.
- [x] Return 401 for missing authentication and 403 for forbidden access.
- [x] Require authentication by default and test both role policies.
- [ ] Reject the wrong role on every protected operation.
- [ ] Check event ownership and protect each Participant's private records.
- [ ] Prevent requests from changing roles or assigning another user's identity.
- [x] Reject user IDs, role IDs, password hashes and account status in registration requests.
- [x] Check login and protected requests through Swagger.

## API endpoints and database: 25 marks

- [x] Set C# and SQL as the project code languages.
- [x] Build the initial C# ASP.NET Core API host and public health check.
- [x] Connect the API to SQL Server.
- [x] Use EF Core Code First, configurations and migrations.
- [x] Map all seven tables, relationships, datatypes, constraints and indexes; explain the category-name index difference.
- [x] Create and seed a separate Part 2 database.
- [ ] Implement every endpoint in the corrected plan and explain any changes.
- [x] Add own-profile reads, updates and password changes.
- [x] Use the session identity for both roles' profiles and reject protected account fields.
- [x] Check the current password, hash its replacement and end the current session after a change.
- [x] Add public event browsing with paging, event type, province and UTC date filters.
- [x] Add event details with visible categories and routes; restrict Draft and Cancelled events to their owner.
- [x] Add the Organiser dashboard with owned-event and saved-enrolment totals.
- [x] Add a paged owned-event list with a status filter.
- [x] Add owned-event creation, updates and safe deletion or cancellation.
- [x] Store each event's name, description, date, location, distance and type.
- [x] Validate event status transitions, opening readiness, field sizes and SQL precision.
- [x] Protect saved event history and reject deletion with dependent records or a banner.
- [x] Add category CRUD with age, distance, fee, availability and capacity checks.
- [x] Protect category ownership, saved entries and Open-event readiness.
- [x] Separate public category reads from the owner's full category list.
- [x] Add route CRUD and check the category belongs to the event.
- [x] Check route ownership, public visibility and readable creation Locations.
- [x] Validate route text, optional map URLs and elevation; keep related records when deleting a route.
- [x] Save enrolments with the correct Participant, Event and Category links.
- [x] Check entry dates, event status, age, capacity and duplicate enrolments.
- [x] Add enrolment reads, cancellation, reactivation, DELETE and bib numbers.
- [x] Let Organisers view enrolments for their own events.
- [x] Protect private entry reads, Participant actions and Organiser rosters with ownership checks.
- [x] Keep Cancelled history, preserve reactivated IDs and clear bib numbers when the category changes.
- [x] Check simultaneous entry, reactivation, bib, capacity and event-closing requests.
- [x] Add result capture, updates and personal and event result lists.
- [x] Check result times, positions, ownership and one result per enrolment.
- [x] Protect cancelled enrolments and historical records.
- [ ] Add profile-picture and event-banner upload, replacement and removal.
- [ ] Validate images, store blob names in SQL and keep storage settings out of Git.
- [ ] Return the planned status codes, bodies, Location headers and errors.
- [x] Read saved data through a new context or request to check persistence.

## Swagger integration: 10 marks

- [x] Add development Swagger UI and XML descriptions for the host check.
- [x] Execute GET /health through Swagger and verify its 200 response.
- [ ] Show every implemented endpoint in Swagger.
- [ ] Describe each endpoint's purpose, inputs and responses.
- [ ] Add useful examples and relevant error responses.
- [ ] Keep the login session working across Swagger requests.
- [x] Use the login cookie automatically for logout and reject another logout after sign-out.
- [x] Describe profile inputs and responses, add usable examples and check profile reads and updates in Swagger.
- [x] Describe event-browsing filters and optional owner login; check public reads through Swagger.
- [x] Describe all five Organiser event operations and provide working event-write examples.
- [x] Describe all six category operations and test the creation example through Swagger.
- [x] Describe all four route operations and provide working write examples.
- [x] Describe all eight enrolment operations, their roles, fields, filters and responses.
- [x] Describe all five result operations, their fields, access rules, filters and responses.
- [ ] Test file uploads through Swagger.
- [ ] Check successful requests, invalid input and forbidden access.
- [ ] Match the Swagger operation list to the endpoint plan.

## Unit testing: 15 marks

- [x] Add the xUnit project and pass all seven startup integration checks locally.
- [x] Pass the 63 SQL Server mapping, migration and constraint tests locally.
- [ ] Organise tests by feature and use separate test data.
- [x] Cover successful and failed registration and login for both roles.
- [x] Cover logout, sessions and unauthenticated requests.
- [x] Pass all 51 authentication checks locally.
- [x] Pass all 57 profile and password checks locally, including privacy and SQL persistence.
- [x] Confirm the profile changes and tests pass on GitHub.
- [x] Pass all 55 event-browsing checks locally, including hidden events and owner sessions.
- [x] Include the event-browsing tests in the GitHub build and test workflow.
- [x] Pass all 121 Organiser event-management checks locally.
- [x] Confirm the event-management revision passes on GitHub after an approved push.
- [x] Pass all 93 category checks locally, including duplicate writes and event-opening rules.
- [x] Confirm the category revision passes on GitHub after an approved push.
- [x] Pass all 85 route checks locally, including saved relationships, visibility and duplicate creation.
- [x] Confirm the route revision passes on GitHub after an approved push.
- [x] Pass all 113 enrolment checks locally, including saved links, age boundaries and competing writes.
- [x] Confirm the enrolment revision passes on GitHub after an approved push.
- [x] Pass all 89 result checks locally, including corrections, private history, finisher totals and competing writes.
- [ ] Confirm the result revision passes on GitHub after an approved push.
- [ ] Cover wrong roles and attempts to access another user's records.
- [x] Cover event CRUD and ownership checks.
- [x] Cover enrolment rules and the saved database relationships.
- [ ] Cover profiles, categories, routes, results and images.
- [x] Check SQL Server constraints against SQL Server.
- [x] Keep the tests repeatable and separate from the Part 1 database.
- [ ] Pass Run All locally and pass the tests in GitHub Actions.

## Video presentation: 15 marks

- [ ] Keep the recording within 20 minutes and use my own voice.
- [ ] Open the rubric first and show each requirement being met.
- [ ] Show every endpoint working in Swagger.
- [ ] Explain the core code logic while demonstrating the API.
- [ ] Show both roles, sessions and restricted access.
- [ ] Show the relevant saved data and database relationships.
- [ ] Select Run All and show the passing tests.
- [ ] Show the green CI build and README evidence.
- [ ] Upload the unlisted video, check its link and add it to the README.

## References

The Independent Institute of Education (2026a) *PROG6212 Portfolio of Evidence*. Part 1 and Part 2 requirements and Part 2 marking rubric. Assessment brief.

The Independent Institute of Education (2026b) *Announcement for the Part II*. Module announcement covering the video structure and Part 2 README requirements.

The Independent Institute of Education (2026c) *POE Part I General Feedback (HE)*. General feedback on the ERD and required endpoint operations.
