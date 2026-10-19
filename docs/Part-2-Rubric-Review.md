# RaceDay Part 2 Rubric Review

Checked on 19 October 2026 against the brief, rubric and lecturer's announcement (The Independent Institute of Education, 2026a; 2026b).

## 1. Rubric

| Section | Marks | Status |
|---|---|---|
| Submission | 5 | Setup, roles, GitHub and Run All screenshots included; video link and ARC submission pending |
| GitHub and CI/CD | 10 | Both workflows pass; meaningful-commit requirement and final revision still need checking |
| Authentication and roles | 20 | Both roles, password hashes, server-side sessions, role and ownership checks implemented |
| API and database | 25 | All 40 planned operations implemented; seven tables and 71 columns match Part 1 |
| Swagger | 10 | All operations described and testable; every endpoint still needs showing in the video |
| Unit testing | 15 | All 810 tests passed on GitHub and in Visual Studio Run All; screenshots included |
| Video | 15 | Recording and unlisted link pending |

No missing planned endpoint or functional correction was found. These are checks, not awarded marks. MVC, weather and Docker remain Part 3 work.

## 2. Results

Revision `67b1489`:

- [API workflow](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39218030480): 810 passed, none failed/skipped, zero build warnings/errors and no pending model changes.
- [Planning workflow](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39218030482): 40 endpoints, all 25 required operations, seven tables, 71 columns and nine relationships passed.
- Visual Studio Run All on 19 October: 810 passed, zero failed or skipped. The full run took 4.2 minutes.
- Release/Debug builds on 19 October: zero warnings/errors. Dependency audit: no known vulnerable packages.
- Local planning checks passed, EF reports no pending model changes and `RaceDayPart2` is up to date.
- The running API returned Healthy; Swagger loaded with all 40 business operations plus `/health`.

Smart App Control initially blocked local execution on 19 October (`0x800711C7`), confirmed by Code Integrity event 3077 (Microsoft, 2026; 2026b). After the Windows setting was changed manually, the checks and Visual Studio Run All passed. No application code changes were needed.

Evidence: [green GitHub build](evidence/part2-github-build.jpg), [GitHub test output](evidence/part2-github-tests.jpg) and [Visual Studio Run All](evidence/part2-visual-studio-tests.jpg).

## 3. Submission

Record the video and add its unlisted link. Open the rubric first, show every endpoint in Swagger, explain the code and show Run All. Use my own voice and keep it within 20 minutes.

The 20 meaningful Part 2 commits are not yet confirmed. Repository-wide counts and build-result notes alone do not establish that requirement. Check the final approved revision, links and ARC submission before marking the [checklist](Part-2-Requirements-Checklist.md) complete.

## References

The Independent Institute of Education (2026a) *PROG6212 Portfolio of Evidence*. Part 2 requirements and marking rubric. Assessment brief.

The Independent Institute of Education (2026b) *Announcement for the Part II*. Module announcement covering the video structure and Part 2 README requirements.

Microsoft (2026) 'Understanding App Control event IDs', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/windows/security/application-security/application-control/app-control-for-business/operations/event-id-explanations (Accessed: 19 October 2026).

Microsoft (2026b) 'Smart App Control Frequently Asked Questions', *Microsoft Support*. Available at: https://support.microsoft.com/en-us/windows/security/threat-malware-protection/smart-app-control-frequently-asked-questions (Accessed: 19 October 2026).

Implementation sources are recorded alongside the code and in the [main README references](../README.md#references) and feature notes.
