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
- [ ] Update SSMS evidence, document statuses and endpoint checks in CI.

The [database correction notes](Part-1-Database-Corrections.md) record the local checks. These are SQL checks, not Part 2 API unit tests.

## Correct submission: 5 marks

- [x] Keep the original Part 1 README separately.
- [x] Add the main Part 2 README and checklist.
- [ ] Keep the API, tests, documents and evidence clearly organised.
- [ ] Explain the API and both roles in the completed README.
- [ ] Give the required tools and numbered clone and setup steps.
- [ ] Give a tested connection-string example and database creation commands.
- [ ] Give the API startup command, URL and Swagger address.
- [ ] Explain the test command and Test Explorer Run All.
- [ ] Add the green CI screenshot and run link.
- [ ] Add the passing unit-test screenshot and unlisted video link.
- [ ] Submit the repository link on ARC.

## GitHub and CI/CD: 10 marks

- [x] Use a separate Part 2 branch.
- [ ] Make at least 20 meaningful Part 2 commits using my own account.
- [ ] Keep the workflow in `.github/workflows/`.
- [ ] Build the solution and run all tests on every push.
- [ ] Keep the Part 1 file and endpoint checks working.
- [ ] Get a green build for the final submitted revision.

## Authentication and role-based access: 20 marks

- [ ] Register and log in as Organiser and Participant.
- [ ] Validate account fields and reject duplicate emails.
- [ ] Hash passwords and keep them out of responses and session data.
- [ ] Store the signed-in user's ID and role in a server-side session.
- [ ] End access after logout or session expiry and reject inactive accounts.
- [ ] Return 401 for missing authentication and 403 for forbidden access.
- [ ] Reject the wrong role on every protected operation.
- [ ] Check event ownership and protect each Participant's private records.
- [ ] Prevent requests from changing roles or assigning another user's identity.
- [ ] Check login and protected requests through Swagger.

## API endpoints and database: 25 marks

- [ ] Build the C# ASP.NET Core Web API and connect it to SQL Server.
- [ ] Use EF Core Code First, configurations and migrations.
- [ ] Match all seven tables, relationships, datatypes, constraints and indexes to Part 1.
- [ ] Create and seed a separate Part 2 database.
- [ ] Implement every endpoint in the corrected plan and explain any changes.
- [ ] Add own-profile reads, updates and password changes.
- [ ] Add event browsing, details and the Organiser dashboard.
- [ ] Add owned-event creation, updates and safe deletion or cancellation.
- [ ] Store each event's name, description, date, location, distance and type.
- [ ] Add category CRUD with age, distance, fee, availability and capacity checks.
- [ ] Add route CRUD and check the category belongs to the event.
- [ ] Save enrolments with the correct Participant, Event and Category links.
- [ ] Check entry dates, event status, age, capacity and duplicate enrolments.
- [ ] Add enrolment reads, cancellation, reactivation, DELETE and bib numbers.
- [ ] Let Organisers view enrolments for their own events.
- [ ] Add result capture, updates and personal and event result lists.
- [ ] Check result times, positions, ownership and one result per enrolment.
- [ ] Protect cancelled enrolments and historical records.
- [ ] Add profile-picture and event-banner upload, replacement and removal.
- [ ] Validate images, store blob names in SQL and keep storage settings out of Git.
- [ ] Return the planned status codes, bodies, Location headers and errors.
- [ ] Read saved data through a new context or request to check persistence.

## Swagger integration: 10 marks

- [ ] Show every implemented endpoint in Swagger.
- [ ] Describe each endpoint's purpose, inputs and responses.
- [ ] Add useful examples and relevant error responses.
- [ ] Keep the login session working across Swagger requests.
- [ ] Test file uploads through Swagger.
- [ ] Check successful requests, invalid input and forbidden access.
- [ ] Match the Swagger operation list to the endpoint plan.

## Unit testing: 15 marks

- [ ] Organise tests by feature and use separate test data.
- [ ] Cover successful and failed registration and login for both roles.
- [ ] Cover logout, sessions and unauthenticated requests.
- [ ] Cover wrong roles and attempts to access another user's records.
- [ ] Cover event CRUD and ownership checks.
- [ ] Cover enrolment rules and the saved database relationships.
- [ ] Cover profiles, categories, routes, results and images.
- [ ] Check SQL Server constraints against SQL Server.
- [ ] Keep the tests repeatable and separate from the Part 1 database.
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
