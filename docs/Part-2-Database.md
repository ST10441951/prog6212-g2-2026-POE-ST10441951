# RaceDay Part 2 Database

## 1. Setup

EF Core 10.0.13 uses SQL Server Code First migrations. Development uses `RaceDayPart2` on `(localdb)\MSSQLLocalDB`, separate from Part 1 `RaceDay`. Follow the [README setup](../README.md#setup-and-run) (Microsoft, 2026a; 2026b).

The initial migration is `20261018053845_InitialCreate`. EF adds `__EFMigrationsHistory` alongside the seven business tables. Migrations are applied by command, not on API startup. Private connection strings belong in environment settings or user secrets.

## 2. Part 1 mapping

| Item | Mapping |
|---|---|
| Tables | Roles, Users, Events, EventCategories, EventRoutes, Enrolments, Results |
| Columns | All 71 names, types, sizes, precision, identity and NULL rules |
| Primary/foreign keys | Seven primary keys; nine foreign keys with NO ACTION |
| Category/event link | Composite key prevents a category from another event |
| Checks/defaults | 36 named CHECK constraints and 12 named defaults |
| Indexes | Original names, columns, uniqueness and bib filter |

`UQ_EventCategories_Event_CategoryName` is a unique index instead of a UNIQUE constraint. It enforces the same uniqueness while allowing EF to rename categories; alternate-key properties cannot change. The separate category/event alternate key remains (Microsoft, 2026c; 2026d).

Equivalent casts in generated defaults do not change their values. Boolean mappings preserve explicit false values (Microsoft, 2026g).

## 3. Sample data

| Table | Records |
|---|---:|
| Roles | 2 |
| Users | 4 |
| Events | 3 |
| EventCategories | 6 |
| EventRoutes | 6 |
| Enrolments | 4 |
| Results | 2 |

Fixed seeds match the corrected Part 1 data. The four fictional accounts use `RaceDayDemo!2026`; only hashes are stored. Reapplying the migration preserves saved changes (Microsoft, 2026e).

## 4. Checks

The 63 SQL tests check columns, keys, indexes, constraints, defaults, seeds, hashes, persistence and protected history against real SQL Server (Microsoft, 2026f). Tests create their own `RaceDayTests_...` database. `RACEDAY_TEST_CONNECTION` can change the server; the fixture always replaces the database name.

Metadata queries allow 120 seconds on a busy LocalDB instance; assertions and API timeouts are unchanged (Microsoft, 2026h).

Current test/build results and the local execution issue are in the [rubric review](Part-2-Rubric-Review.md).

## References

Microsoft (2026a) 'Migrations overview', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/ (Accessed: 18 October 2026).

Microsoft (2026b) 'DbContext lifetime, configuration, and initialization', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/ (Accessed: 18 October 2026).

Microsoft (2026c) 'Keys', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/modeling/keys (Accessed: 18 October 2026).

Microsoft (2026d) 'Indexes', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/modeling/indexes (Accessed: 18 October 2026).

Microsoft (2026e) 'Data seeding', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding (Accessed: 18 October 2026).

Microsoft (2026f) 'Testing against your production database system', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/testing/testing-with-the-database (Accessed: 18 October 2026).

Microsoft (2026g) 'Generated values', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/modeling/relational/default-values (Accessed: 18 October 2026).

Microsoft (2026h) 'DbCommand.CommandTimeout Property', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbcommand.commandtimeout?view=net-10.0 (Accessed: 18 October 2026).
