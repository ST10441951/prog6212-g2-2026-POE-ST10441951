# RaceDay Part 1 Video Presentation Plan

## 1. Purpose

This plan provides a clear order for the unlisted YouTube walkthrough required for Part 1. It is a set of speaking prompts and demonstration steps rather than a word-for-word script. The presentation should explain the design decisions naturally and show that the presenter understands how the system fits together.

Suggested length: 9 to 11 minutes.

## 2. Preparation before recording

- Pull the latest `main` branch from GitHub.
- Confirm that the GitHub Actions workflow has a green result.
- Open the GitHub repository in a browser.
- Open `RaceDay-ERD.png`, `API-Endpoint-Plan.md` and `RaceDay-Data-Dictionary.md` in readable tabs.
- Open `RaceDay.sql` in SQL Server Management Studio.
- Connect SSMS to a clean SQL Server or LocalDB instance where no RaceDay tables need to be preserved.
- Set the screen resolution and zoom so that table names and code can be read in the recording.
- Close private tabs, messages, notifications and unrelated applications.
- Test the microphone and record a short sample before starting the final video.

## 3. Suggested presentation order

| Time | Section | What to show | Main explanation |
|---|---|---|---|
| 0:00 to 0:30 | Introduction | Repository home page | Introduce RaceDay, Part 1 and the purpose of the system. |
| 0:30 to 1:20 | Problem and roles | README overview and roles | Explain the South African event-management problem and the Organiser and Participant responsibilities. |
| 1:20 to 3:40 | ERD | `RaceDay-ERD.png` | Explain the seven entities, keys, relationships and important cardinalities. |
| 3:40 to 5:30 | API endpoint plan | `API-Endpoint-Plan.md` | Explain the six table columns, resource groups, role restrictions, ownership checks and response codes. |
| 5:30 to 7:10 | SQL design | `RaceDay.sql` and data dictionary | Explain datatypes, named constraints, foreign keys, status checks, indexes and seed-data design. |
| 7:10 to 9:20 | Live SSMS demonstration | SSMS query window and Object Explorer | Run the full script on a clean instance, show the success message, record counts, tables and relationships. |
| 9:20 to 10:10 | GitHub and CI | Commit history and Actions page | Show the meaningful commit history, workflow file and green validation run. |
| 10:10 to 10:40 | Conclusion | README or repository home page | Summarise what Part 1 delivers and state that Part 2 will implement the API in C# and ASP.NET Core. |

## 4. ERD speaking prompts

### 4.1 Entity choices

- `Roles` controls the two permitted account types instead of repeating role text in every user record.
- `Users` stores both Organisers and Participants because their shared account and profile fields belong in one entity.
- `Events` belongs to an Organiser through `OrganiserUserId`.
- `EventCategories` separates the different distance, fee, age and capacity options offered by an event.
- `EventRoutes` is separate because each category may have its own route information.
- `Enrolments` resolves Participant entry into an Event and records the selected category.
- `Results` is separate because an enrolment may have no result or one official result.

### 4.2 Relationship decisions

- One Role may be assigned to many Users, while each User has exactly one Role.
- One Organiser may manage many Events, while each Event has one Organiser.
- One Event may have many Categories.
- One Category may have zero or one Route.
- A Participant may have many Enrolments, but only one enrolment record per Event.
- One Enrolment may have zero or one official Result.
- The composite Event Category relationship ensures that an enrolment cannot select a category from another event.

### 4.3 Data-preservation decisions

- Historical enrolments and results are protected with `NO ACTION` foreign-key behaviour.
- Events with dependent records are cancelled instead of deleted.
- Categories with enrolments are made unavailable instead of being deleted.
- Cancelled enrolments remain in the database for history and reporting.

## 5. API endpoint-plan speaking prompts

- Point out the six required columns: HTTP Method, Route, Description, Role Required, Request Body and Expected Response.
- Explain that all routes begin with `/api/` and use JSON.
- Show the public registration, login, event and category-browsing endpoints.
- Show that profile, enrolment and result records require authentication.
- Explain that the role check is not enough on its own. Organiser operations also check event ownership.
- Explain why `401 Unauthorized`, `403 Forbidden`, `404 Not Found` and `409 Conflict` have different meanings.
- Point out that new resources return `201 Created` and successful deletions return `204 No Content`.
- State that the plan was cross-checked against Part 2 and Part 3 and now includes role selection, session authentication, event distance, image uploads, the Organiser dashboard, Swagger, testing, MVC, Blob Storage and Docker expectations.

## 6. SQL design speaking prompts

- The script creates the database before creating the tables in relationship order.
- The seven SQL tables match the ERD and data dictionary.
- Events store the required advertised distance, and Categories support both lower and upper age limits.
- Users and Events store optional Azure Blob object names for profile pictures and Event banners, while the image files remain outside SQL Server.
- Primary keys identify records and foreign keys prevent orphaned records.
- `NOT NULL`, `UNIQUE`, `DEFAULT` and `CHECK` constraints protect the database from invalid values.
- Status checks restrict events, enrolments and results to their permitted values.
- The filtered bib-number index allows many unassigned `NULL` values while preventing duplicate assigned bib numbers within an event.
- Schema creation and seed inserts run inside a transaction so a failure rolls back the incomplete work.
- The seed data covers both roles, three event types, every entity and both Completed and DidNotFinish result cases.
- Passwords are represented by hashes and are not stored as plain text.

## 7. Live SSMS demonstration

Microsoft explains that SSMS can connect to SQL Server, execute T-SQL and display the results in the query window (Microsoft, 2026).

1. Show the connected clean SQL Server instance in Object Explorer.
2. Open `docs/RaceDay.sql` in the SSMS query editor.
3. Briefly show the database creation section and the seven `CREATE TABLE` statements.
4. Briefly show the seed `INSERT` sections.
5. Select Execute or press `F5`.
6. Wait for the script to finish without changing tabs.
7. Show the message `RaceDay database schema and sample data created successfully.`
8. Show the final record-count result grid:
   - Roles: 2
   - Users: 4
   - Events: 3
   - EventCategories: 6
   - EventRoutes: 6
   - Enrolments: 4
   - Results: 2
9. Refresh the Databases node and expand `RaceDay`.
10. Expand Tables and show all seven tables.
11. Expand one or two table key folders to show primary and foreign-key constraints.
12. Explain that application-level role and ownership checks will be implemented in the Part 2 C# API.

Do not delete or overwrite an existing database that contains work which must be preserved. Use a clean demonstration instance.

## 8. GitHub and CI demonstration

- Show the repository `/docs` folder and required files.
- Show that the commit history contains at least 20 meaningful commits.
- Open `.github/workflows/part1-validation.yml` and explain what it checks.
- Open the Actions tab and show a green `Validate Part 1 Submission` run.
- Point out the successful `Validate Part 1 files` job.

## 9. Recording quality checklist

- Speak clearly and at a steady pace.
- Explain the reasons for decisions instead of only reading file contents.
- Keep the cursor near the item being discussed.
- Avoid scrolling quickly through large documents.
- Make table names, routes and SQL output readable.
- Remove long pauses and failed setup attempts from the final recording.
- Check the complete video before uploading it.

## 10. Upload and final README update

1. Upload the final recording to YouTube.
2. Set the visibility to Unlisted.
3. Open the unlisted link in a private browser window to confirm that it works.
4. Replace the pending video statement in `README.md` with the working YouTube link.
5. Commit the README update with a meaningful message.
6. Confirm that the GitHub Actions workflow is still green.

## References

Microsoft (2026) 'Connect and query SQL Server using SQL Server Management Studio', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ssms/quickstarts/ssms-connect-query-sql-server (Accessed: 19 September 2026).

The Independent Institute of Education (2026) *PROG6212 Portfolio of Evidence*. Unpublished assessment brief.
