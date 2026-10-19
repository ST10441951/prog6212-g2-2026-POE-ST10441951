# RaceDay Part 2 Checklist

Based on the brief, rubric, lecturer's announcement and Part 1 feedback (The Independent Institute of Education, 2026a; 2026b; 2026c). Tick only verified work. See the [rubric review](Part-2-Rubric-Review.md) for results.

## Part 1 corrections

- [x] Match the plan to all 25 required operations, including enrolment DELETE.
- [x] Clarify cancellation, reactivation, age limits and missing birth dates.
- [x] Keep all 40 endpoints and readable owner-only creation Locations.
- [x] Match all 71 columns and nine relationships across SQL, ERD and dictionary.
- [x] Correct finish-time validation, database guards and rollback reporting.
- [x] Check the SQL on a clean database and save fresh SSMS evidence.
- [x] Update the C# planning checks and confirm their GitHub result.

## Correct submission: 5 marks

- [x] Preserve the original Part 1 README separately.
- [x] Organise API, tests, documents and evidence.
- [x] Explain the API and both roles.
- [x] Give numbered clone, connection-string, database, API, Swagger and test steps.
- [x] Add the green CI and GitHub test screenshots.
- [x] Add the passing Visual Studio Run All screenshot.
- [ ] Add and verify the unlisted video link.
- [ ] Submit the repository link on ARC.

## GitHub and CI/CD: 10 marks

- [x] Use a clearly named Part 2 branch or folder.
- [x] Build and run tests on every push.
- [x] Validate planning files and check for pending migrations.
- [x] Confirm the image revision passes both GitHub workflows.
- [ ] Confirm at least 20 meaningful Part 2 commits using my account.
- [ ] Confirm a green build for the final submitted revision.

## Authentication and roles: 20 marks

- [x] Register and log in with both roles; validate fields and duplicate emails.
- [x] Hash passwords and exclude credentials from responses and sessions.
- [x] Store user ID, role and expiry in server-side sessions.
- [x] Check logout, expiry, inactive accounts and changed roles.
- [x] Return 401/403 correctly and enforce roles on protected operations.
- [x] Check ownership and reject identity, role and account-status changes in requests.

## API endpoints and database: 25 marks

- [x] Use C#, ASP.NET Core, SQL Server and EF Core Code First.
- [x] Match the seven-table Part 1 model and explain the category-name index difference.
- [x] Create and seed a separate Part 2 database through migrations.
- [x] Implement all 40 planned business endpoints.
- [x] Add own profiles and password changes for both roles.
- [x] Add public event browsing, filters, paging and owner-only hidden reads.
- [x] Add owned event CRUD and dashboard totals.
- [x] Validate dates, precision, status changes and saved event history.
- [x] Add category CRUD, ages, fees, availability and capacity checks.
- [x] Add route CRUD with correct category/event links.
- [x] Add Participant entry, own history, cancellation, reactivation and DELETE.
- [x] Check age, closing time, capacity, duplicates and category/event links.
- [x] Add owned-event rosters and unique bib numbers.
- [x] Add result capture, corrections, private reads and finisher totals.
- [x] Add picture/banner upload, replacement and removal with private storage.
- [x] Validate files, save only generated blob names and keep real keys out of Git.
- [x] Protect history and competing writes; verify SQL persistence.

## Swagger: 10 marks

- [x] Show all operations with purpose, inputs and responses.
- [x] Provide working JSON examples and image-upload controls.
- [x] Keep login cookies working across requests.
- [x] Check success, invalid input and forbidden access.
- [x] Match all 40 operations to the endpoint plan.

## Unit testing: 15 marks

- [x] Organise tests by feature and use separate SQL/storage test data.
- [x] Cover successful/failed registration, login, logout and expiry.
- [x] Cover both roles, missing sessions and access to other users' records.
- [x] Cover event CRUD, categories, routes, enrolments, results and images.
- [x] Check SQL constraints, saved relationships and concurrent requests.
- [x] Confirm all 810 tests pass on GitHub.
- [x] Resolve the local execution block and capture Visual Studio Run All passing.

## Video: 15 marks

- [ ] Open the rubric and show each requirement.
- [ ] Demonstrate every endpoint working in Swagger.
- [ ] Explain the core logic, roles, sessions and database relationships.
- [ ] Select Run All and show passing tests.
- [ ] Show the green GitHub build and README evidence.
- [ ] Use my own voice and keep the video within 20 minutes.
- [ ] Upload as unlisted and check the link.

## References

The Independent Institute of Education (2026a) *PROG6212 Portfolio of Evidence*. Part 1 and Part 2 requirements and Part 2 marking rubric. Assessment brief.

The Independent Institute of Education (2026b) *Announcement for the Part II*. Module announcement covering the video structure and Part 2 README requirements.

The Independent Institute of Education (2026c) *POE Part I General Feedback (HE)*. General feedback on the ERD and required endpoint operations.
