# RaceDay Part 2 Database

## 1. Database setup

The API uses Entity Framework Core 10.0.13 with the SQL Server provider. The seven C# entity models and their configurations create the database through Code First migrations (Microsoft, 2026a).

The development database is `RaceDayPart2` on `(localdb)\MSSQLLocalDB`. It is separate from the Part 1 `RaceDay` database. The numbered setup commands are in the [README](../README.md#setup-and-run).

`Program.cs` registers `RaceDayDbContext` for each request and reads `ConnectionStrings:RaceDay` from configuration. The Development connection uses Windows authentication. Private connection strings can be supplied through environment settings or user secrets (Microsoft, 2026b).

The initial migration is `20261018053845_InitialCreate`. EF also creates `__EFMigrationsHistory` to record applied migrations. This is an EF system table, separate from the seven planned business tables.

## 2. Mapping to Part 1

| Item | Part 2 mapping |
|---|---|
| Tables | Roles, Users, Events, EventCategories, EventRoutes, Enrolments and Results |
| Columns | All 71 names, SQL types, lengths, decimal precision, identity settings and NULL rules |
| Primary keys | All seven original names and columns |
| Foreign keys | All nine relationships use NO ACTION |
| Category/Event link | Composite alternate key and foreign key prevent a category from another event being selected |
| Validation | All 36 named CHECK constraints |
| Defaults | All 12 named defaults retain the Part 1 values |
| Indexes | Original index names, columns, uniqueness and bib-number filter |

One implementation detail differs from the Part 1 SQL script. `UQ_EventCategories_Event_CategoryName` is a unique index on `EventId, CategoryName` rather than a UNIQUE constraint. It enforces the same rule and allows category names to be changed through EF Core. Alternate-key properties cannot be changed, so the category name is not mapped as an alternate key. The separate `EventCategoryId, EventId` alternate key remains in place (Microsoft, 2026c; 2026d).

EF formats numeric defaults using explicit casts, such as `CAST(1 AS bit)`. Their values and SQL types are unchanged. Boolean properties are written explicitly so that a saved `false` value is not replaced by the database's `true` default (Microsoft, 2026g).

Roles, ownership, entry dates, capacity and age eligibility still require API validation. A foreign key cannot prove that an account has the correct role.

## 3. Sample data

The migration contains the fixed sample records from the corrected Part 1 SQL script. Fixed dates, identifiers and existing password hashes keep the migration repeatable (Microsoft, 2026e).

| Table | Records |
|---|---:|
| Roles | 2 |
| Users | 4 |
| Events | 3 |
| EventCategories | 6 |
| EventRoutes | 6 |
| Enrolments | 4 |
| Results | 2 |

The four fictional accounts retain the local demonstration password `RaceDayDemo!2026`. Only their hashes are stored in the database. These are development sample accounts. Registration and login are now implemented; the [authentication notes](Part-2-Authentication.md) describe them.

Applying the same migration again does not duplicate the records or reset changes made to them.

## 4. Checks

These checks were completed on 18 October 2026.

| Check | Result |
|---|---|
| Restore and build the solution | Passed with no warnings or errors |
| Create RaceDayPart2 using the EF migration | Passed |
| Apply the migration again | Already up to date; data kept |
| Compare the model with its migration snapshot | No pending model changes |
| Compare every seeded column value with the corrected Part 1 database | All seven tables match |
| Compare SQL check and default definitions with Part 1 | Match after equivalent numeric-cast formatting |
| Run all current tests | 70 passed, 0 failed, 0 skipped |

Seven tests cover the API host and Swagger. The other 63 use real SQL Server LocalDB. They check columns, keys, foreign keys, indexes, defaults, all CHECK constraints, sample relationships, password hashes, persistence, duplicate values, cross-event categories and protected history (Microsoft, 2026f).

Each test run creates a new `RaceDayTests_...` database. It is removed after the tests. The fixture does not use the Part 1 or Part 2 application database. A supplied `RACEDAY_TEST_CONNECTION` changes the server and authentication settings; the database name is always replaced.

The updated GitHub workflow uses the Windows 2025 runner with LocalDB and checks for missing migrations. The [build for e31e512](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39102096043) passed all 70 tests with none skipped, no build warnings or errors and no missing migrations. The authentication work adds 51 checks; all 121 now pass locally and in the [authentication build](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39104118046). Event ownership and the other business endpoint tests will be added with those features.

## References

Microsoft (2026a) 'Migrations overview', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/ (Accessed: 18 October 2026).

Microsoft (2026b) 'DbContext lifetime, configuration, and initialization', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/ (Accessed: 18 October 2026).

Microsoft (2026c) 'Keys', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/modeling/keys (Accessed: 18 October 2026).

Microsoft (2026d) 'Indexes', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/modeling/indexes (Accessed: 18 October 2026).

Microsoft (2026e) 'Data seeding', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding (Accessed: 18 October 2026).

Microsoft (2026f) 'Testing against your production database system', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/testing/testing-with-the-database (Accessed: 18 October 2026).

Microsoft (2026g) 'Generated values', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/modeling/relational/default-values (Accessed: 18 October 2026).
