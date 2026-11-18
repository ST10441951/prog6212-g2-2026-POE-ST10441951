# RaceDay Part 3 - CI checks

## 1. Hosted-runner failure

Both checks for `dbabfde` stopped before any steps ran. GitHub reported: "The job was not started because your account is locked due to a billing issue." This was not a test or YAML error.

## 2. Local runner

Both workflows support a Windows x64 runner labelled `raceday`. Repository variable `RACEDAY_USE_LOCAL_RUNNER=true` selects it. Setting the variable to `false` returns the jobs to GitHub-hosted runners (GitHub, 2026a; 2026b).

Runs are limited to the repository owner's pushes to `main` and manual runs from `main`. Pull requests do not start local jobs. The runner is registered for one job and is removed afterwards; it is not installed as a Windows service. Another job needs a new temporary registration. The PC must be awake and connected while a job runs (GitHub, 2026a; 2026c).

The machine needs Git, Node/npm, PowerShell 7, the .NET 10 SDK and SQL Server LocalDB. Runner registration files stay outside the source repository. Follow GitHub's linked setup instructions and select the `raceday` label.

## 3. Checks

The build workflow restores packages, builds the API/MVC solution, runs all tests and checks for missing migrations. It reuses an existing Azurite instance or starts one, waits for it to be ready and stops only the process started by that job. The planning workflow runs the C# document/schema checks.

Local-runner verification is pending. A green result will be recorded only after GitHub reports that the complete jobs passed.

## References

GitHub (2026a) 'Adding self-hosted runners', *GitHub Docs*. Available at: https://docs.github.com/en/actions/how-tos/manage-runners/self-hosted-runners/add-runners (Accessed: 18 November 2026).

GitHub (2026b) 'Billing and usage', *GitHub Docs*. Available at: https://docs.github.com/en/actions/concepts/billing-and-usage (Accessed: 18 November 2026).

GitHub (2026c) 'Compromised runners', *GitHub Docs*. Available at: https://docs.github.com/en/actions/concepts/security/compromised-runners (Accessed: 18 November 2026).
