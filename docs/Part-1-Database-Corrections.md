# RaceDay Part 1 Database Corrections

## 1. Changes

Corrected on 17 October 2026 using the lecturer's feedback (The Independent Institute of Education, 2026).

- Match all 71 column names/types/NULL rules and nine relationships across SQL, dictionary and diagram exports.
- Label composite keys, UML multiplicities and the filtered bib index.
- Require positive, non-null finish time for Completed; other results use NULL (Microsoft, 2026a).
- Reject the wrong database or existing tables before schema creation, in the same batch (Microsoft, 2026b; 2026c).
- Report success only after commit; roll back and rethrow errors (Microsoft, 2026d).

## 2. Checks

All 21 SQL checks passed on an isolated clean LocalDB database. SSMS also ran the full script successfully using `(localdb)\RaceDayEvidence_20261017`. Original application data and evidence were kept (Microsoft, 2026e).

| Check | Result |
|---|---|
| Create all seven tables and seed them | Passed |
| Compare all 71 live column names, datatypes, identity settings and NULL rules with the ERD and dictionary | Passed |
| Check all nine foreign keys use NO ACTION on delete and update | Passed |
| Completed finish time: NULL, zero and negative | All rejected |
| Completed finish time: positive | Accepted |
| DidNotFinish, Disqualified and DidNotStart: NULL finish time | All accepted |
| DidNotFinish, Disqualified and DidNotStart: positive finish time | All rejected |
| Rerun with existing tables, without the sqlcmd stop-on-error option | Error 50001; no success message |
| Run the schema batch in the wrong database | Error 50002; no RaceDay tables created there |
| Insert an invalid seed in a separate empty test database | Error returned; all new tables rolled back; no success message |
| Check sample record counts after the rejection checks | Unchanged |

## 3. Sample records

| Table | Records |
|---|---:|
| Roles | 2 |
| Users | 4 |
| Events | 3 |
| EventCategories | 6 |
| EventRoutes | 6 |
| Enrolments | 4 |
| Results | 2 |

![SSMS record counts](evidence/ssms-corrected-database-counts.png)

![SSMS success message](evidence/ssms-corrected-database-success.png)

## 4. Planning checks

Run from the repository folder with .NET 10 (Microsoft, 2026f):

```text
dotnet build tools/RaceDay.PlanningChecks --configuration Release
dotnet run --project tools/RaceDay.PlanningChecks --configuration Release --no-build
```

The C# checker validates files/PNGs, 40 endpoint rows, all 25 required operations, six columns, 71 matching schema columns and nine relationships. It uses regular expressions and LINQ to XML (Microsoft, 2026g; 2026h). Thirteen invalid-document cases were rejected.

The [C# workflow for 4560c17](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39063690485) passed (GitHub, 2026a; 2026b). Planning checks do not execute SQL or test the API. Current Part 2 results are in the [rubric review](Part-2-Rubric-Review.md).

## References

GitHub (2026a) 'Workflow syntax for GitHub Actions', *GitHub Docs*. Available at: https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax (Accessed: 17 October 2026).

GitHub (2026b) 'actions/setup-dotnet', *GitHub*. Available at: https://github.com/actions/setup-dotnet (Accessed: 17 October 2026).

Microsoft (2026a) 'Unique constraints and check constraints', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/sql/relational-databases/tables/unique-constraints-and-check-constraints (Accessed: 17 October 2026).

Microsoft (2026b) 'THROW (Transact-SQL)', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/sql/t-sql/language-elements/throw-transact-sql (Accessed: 17 October 2026).

Microsoft (2026c) 'SQL Server Utilities Statements - GO', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/sql/t-sql/language-elements/sql-server-utilities-statements-go (Accessed: 17 October 2026).

Microsoft (2026d) 'TRY...CATCH (Transact-SQL)', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/sql/t-sql/language-elements/try-catch-transact-sql (Accessed: 17 October 2026).

Microsoft (2026e) 'Connect and query SQL Server using SQL Server Management Studio', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ssms/quickstarts/ssms-connect-query-sql-server (Accessed: 17 October 2026).

Microsoft (2026f) 'dotnet run', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-run (Accessed: 17 October 2026).

Microsoft (2026g) 'Regex class', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regex (Accessed: 17 October 2026).

Microsoft (2026h) 'LINQ to XML overview', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/standard/linq/linq-xml-overview (Accessed: 17 October 2026).

The Independent Institute of Education (2026) *POE Part I General Feedback (HE)*. Lecturer-provided ERD and endpoint feedback.
