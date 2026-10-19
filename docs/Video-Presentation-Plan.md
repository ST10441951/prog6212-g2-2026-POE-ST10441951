# RaceDay Part 1 Video Presentation Plan

Used for the completed [Part 1 walkthrough](../README-Part1.md#video-presentation). Later SQL/ERD changes and fresh SSMS evidence are in the [correction notes](Part-1-Database-Corrections.md). Part 2 has a separate API video.

## 1. Open before recording

Open the repository, green Actions run, README, ERD, endpoint plan, data dictionary and `RaceDay.sql` in SSMS. Use a clean SQL instance and make the text readable. Check the microphone and close private windows.

## 2. Recording order

| Step | Show and explain |
|---|---|
| Introduction | RaceDay's purpose and the Organiser/Participant roles |
| ERD | Seven entities, keys, cardinalities and why Enrolments resolves the many-to-many relationship |
| Integrity | Composite category/event key, unique entries/bibs and NO ACTION history protection |
| Endpoint plan | Six columns, public/protected routes, role/ownership checks and response codes |
| SQL design | Datatypes, constraints, indexes, transaction, seeds and password hashes |
| SSMS | Execute the full script on the clean instance; show tables, counts and success output |
| GitHub | Required files, meaningful history, workflow and green run |

## 3. SSMS demonstration

1. Connect to the clean instance and open `docs/RaceDay.sql`.
2. Show database creation, CREATE TABLE and seed sections.
3. Execute the full script and show the success message.
4. Show counts: Roles 2, Users 4, Events 3, Categories 6, Routes 6, Enrolments 4, Results 2.
5. Refresh Object Explorer and show all tables and selected keys.

Keep existing databases intact (Microsoft, 2026).

## 4. Upload

Explain decisions in my own words and keep names, routes and output readable. Review the recording, upload as Unlisted and check its link. The completed link stays in the Part 1 README (The Independent Institute of Education, 2026).

## References

Microsoft (2026) 'Connect and query SQL Server using SQL Server Management Studio', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ssms/quickstarts/ssms-connect-query-sql-server (Accessed: 19 September 2026).

The Independent Institute of Education (2026) *PROG6212 Portfolio of Evidence*. Unpublished assessment brief.
