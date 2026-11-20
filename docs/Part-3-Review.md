# RaceDay Part 3 - Rubric review

Checked on 19 November 2026 against the supplied Part 3 questions and rubric (The Independent Institute of Education, 2026). Implementation, tests and documentation are complete. The video and ARC submission were confirmed complete on 20 November 2026.

## 1. Rubric

| Criterion | Marks | Result and evidence |
|---|---:|---|
| Correct submission | 5 | Separate API/MVC projects and preserved Part 1/2 READMEs. Setup, settings, Docker command, screenshots and the Part 3 video link are documented. ARC submission was confirmed on 20 November 2026. |
| GitHub and CI/CD | 10 | The minimum of 20 meaningful Part 3 commits is met. [Build/tests](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/44499288535) and [planning](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/44499288543) passed for `81c3cf1`, including the account-cookie correction. |
| Authentication and navigation | 10 | Both roles register, sign in, retain server-side identity and sign out. Guests, wrong roles and other users' records are blocked. The account-switching correction passed locally and in Docker. See [accounts](Part-3-Accounts.md). |
| Organiser interface | 15 | Dashboard totals, owned events, banners, categories/routes, enrolments, bibs and result capture/corrections work through the API. Saved changes were checked in SQL. |
| Participant interface | 15 | Event filters/details, category selection, immediate entry, My entries, personal results, profile changes and pictures work through the API. |
| Azure Blob Storage | 10 | [Azure evidence](evidence/part3-azure-image-checks.json) verifies MVC → API → private Azure storage, signed display URLs, replacement and removal. MVC contains no Azure credentials or storage SDK. |
| Docker | 10 | The multi-stage image builds and runs as a non-root user. The [README command](../README.md#docker) starts MVC with a reachable host API. Local/container comparisons and corrected login passed. |
| UI design | 10 | The Event finder design is consistent, responsive and role-aware. Status text accompanies colours. The [interface review](Part-3-UI.md) covers 61 pages at five widths locally and in Docker. |
| Video presentation | 15 | [Part 3 walkthrough](https://youtu.be/dGE-umpjM40) uploaded. Recording and submission were confirmed complete on 20 November 2026. |

## 2. Final checks

The recorded build passed 810 API tests and 518 MVC tests, with none failed or skipped, zero build warnings/errors and no missing migration. The final category-creation correction retains the existing database mappings and API contract. The Part 1/2 READMEs are unchanged.

The final browser walkthrough passed 63 assertions across registration, event/category/route creation, banners, entry, bib assignment, cancellation/reactivation/deletion, profiles, password changes, images, completed-event results, corrections, private access and logout. Changes were checked against the real API and disposable SQL database. See the [recorded results](evidence/part3-review-checks.json), [saved entry](evidence/part3-review-saved-entry.png) and [personal result](evidence/part3-review-personal-result.png).

To test results without waiting for a future race, only the disposable fixture's dates were moved into the past. Its status was then changed to Completed through MVC before recording the result. Original sample data was preserved, and temporary review resources were removed.

## 3. Corrections

- Fixed a 503 when signing into another account while already signed in. MVC handles the API's expired-cookie header and new session value while rejecting multiple live values. Old MVC cookies cannot restore the replaced session or role.
- Serialised concurrent category creation with the existing event write lock. Repeated duplicate requests must create one category and return one 409 conflict, rather than an intermittent 500.
- Made the Part 3 setup self-contained, including database creation, emulator startup and sample accounts.
- Refreshed CI screenshots and links, and updated verified checklist items.

## 4. Submission

- [Part 3 video](https://youtu.be/dGE-umpjM40) added to the README.
- Repository submitted on ARC; completion confirmed on 20 November 2026.

## References

The Independent Institute of Education (2026) *PROG6212 Portfolio of Evidence*. Part 3 requirements and marking rubric. Assessment brief.

Technical references are recorded alongside the [account correction](Part-3-Accounts.md), [MVC implementation](Part-3-MVC.md), [image handling](Part-3-Images.md), [Docker setup](Part-3-Docker.md) and [interface](Part-3-UI.md).
