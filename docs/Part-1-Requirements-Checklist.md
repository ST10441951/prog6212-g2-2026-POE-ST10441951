# RaceDay Part 1 Requirements Checklist

## Scope

- [ ] Produce planning and database artefacts only.
- [ ] Do not include C# API implementation, MVC, Azure Blob Storage, or Docker work in Part 1.

## Deliverables

- [x] ERD exported as PNG.
- [ ] API endpoint plan exported as Markdown or PDF.
- [x] SQL Server script saved as a `.sql` file.
- [ ] README with a system description, both roles, CI evidence, and the unlisted video link.
- [ ] GitHub Actions workflow in `.github/workflows/`.
- [x] At least 20 meaningful commits using the student's GitHub account.
- [ ] Repository link submitted on ARC.
- [ ] Brief disclosure of AI assistance.

## ERD: 25 marks

- [x] Include at least six entities covering the full RaceDay data model.
- [x] Show all attributes, primary keys, foreign keys, relationships, and cardinalities.
- [x] Correctly resolve any many-to-many relationships.
- [x] Match the SQL schema exactly or explain deliberate differences in the README.

## API endpoint plan: 25 marks

- [x] Cover authentication: registration and login.
- [x] Cover user profiles.
- [x] Cover events.
- [x] Cover categories and routes.
- [x] Cover enrolments.
- [x] Cover results.
- [ ] Add all endpoints required by the Part 2 functional requirements.
- [x] For every currently planned endpoint, document the method, `/api/` route, description, role, request body, and expected success and failure responses.
- [ ] Complete the plan before Part 2 API implementation begins.

## Roles

### Organiser

- [x] Create, edit, and delete events.
- [x] Manage event categories.
- [x] Capture participant results.
- [x] View all event enrolments.

### Participant

- [ ] Create an account.
- [x] Browse events.
- [x] Enter an event by selecting a category.
- [x] View personal enrolments.
- [x] Track personal results.

- [x] Plan role-based access for later enforcement at API level.

## SQL database script: 20 marks

- [ ] Use SQL Server-compatible syntax and run through SSMS.
- [x] Create every entity shown in the ERD.
- [x] Define primary keys, foreign keys, and suitable `NOT NULL`, `UNIQUE`, `DEFAULT`, and other integrity constraints.
- [x] Seed at least two Organisers and two Participants.
- [x] Seed at least three Events, categories for every Event, and realistic Enrolments.
- [x] Seed every additional entity included in the ERD.
- [x] Run without errors on a clean SQL Server instance.

## GitHub and CI/CD: 15 marks

- [x] Store the planning artefacts and SQL script in `/docs`.
- [ ] Validate the repository structure with a GitHub Actions workflow.
- [ ] Verify that `/docs` and all required files exist.
- [ ] Obtain a green workflow run and include a screenshot in the README.
- [x] Maintain at least 20 meaningful commits.

## Video presentation: 10 marks

- [ ] Upload an unlisted YouTube walkthrough.
- [ ] Explain the ERD decisions and endpoint-plan choices.
- [ ] Explain the SQL design and run the script in SSMS.
- [ ] Add the video link to the README.

## Correct submission: 5 marks

- [ ] Use a clear folder structure and detailed README.
- [ ] Include all required `/docs` files, CI evidence, and video link.
- [ ] Submit the correct repository link through ARC.

## Academic requirements

- [ ] Review and understand every design decision as individual work.
- [ ] Disclose AI assistance used for planning, proofreading, or coding.
- [ ] Do not copy source material except clearly marked direct quotations.
- [ ] Keep quotations below 10% and use one consistent citation style if external sources are used.
- [ ] Preserve a backup and follow the PoE cover-sheet instructions.

## Still required from the student

- [x] Lecturer-provided GitHub repository URL: `https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951.git`.
- [ ] Part 2 functional-requirements pages referenced by the Part 1 endpoint-plan instructions.
- [ ] Any additional PoE cover-sheet requirements.
