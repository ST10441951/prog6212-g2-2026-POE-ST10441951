# RaceDay Part 1 Requirements Checklist

## Scope

- [ ] Produce planning and database artefacts only.
- [ ] Do not include C# API implementation, MVC, Azure Blob Storage, or Docker work in Part 1.

## Deliverables

- [ ] ERD exported as PNG or PDF.
- [ ] API endpoint plan exported as Markdown or PDF.
- [ ] SQL Server script saved as a `.sql` file.
- [ ] README with a system description, both roles, CI evidence, and the unlisted video link.
- [ ] GitHub Actions workflow in `.github/workflows/`.
- [ ] At least 20 meaningful commits using the student's GitHub account.
- [ ] Repository link submitted on ARC.
- [ ] Brief disclosure of AI assistance.

## ERD — 25 marks

- [ ] Include at least six entities covering the full RaceDay data model.
- [ ] Show all attributes, primary keys, foreign keys, relationships, and cardinalities.
- [ ] Correctly resolve any many-to-many relationships.
- [ ] Match the SQL schema exactly or explain deliberate differences in the README.

## API endpoint plan — 25 marks

- [ ] Cover authentication: registration and login.
- [ ] Cover user profiles, events, categories, enrolments, and results.
- [ ] Add all endpoints required by the Part 2 functional requirements.
- [ ] For every endpoint, document the method, `/api/` route, description, role, request body, and expected success and failure responses.
- [ ] Complete the plan before Part 2 API implementation begins.

## Roles

### Organiser

- [ ] Create, edit, and delete events.
- [ ] Manage event categories.
- [ ] Capture participant results.
- [ ] View all event enrolments.

### Participant

- [ ] Create an account.
- [ ] Browse events.
- [ ] Enter an event by selecting a category.
- [ ] View personal enrolments.
- [ ] Track personal results.

- [ ] Plan role-based access for later enforcement at API level.

## SQL database script — 20 marks

- [ ] Use SQL Server-compatible syntax and run through SSMS.
- [ ] Create every entity shown in the ERD.
- [ ] Define primary keys, foreign keys, and suitable `NOT NULL`, `UNIQUE`, `DEFAULT`, and other integrity constraints.
- [ ] Seed at least two Organisers and two Participants.
- [ ] Seed at least three Events, categories for every Event, and realistic Enrolments.
- [ ] Seed every additional entity included in the ERD.
- [ ] Run without errors on a clean SQL Server instance.

## GitHub and CI/CD — 15 marks

- [ ] Store the planning artefacts and SQL script in `/docs`.
- [ ] Validate the repository structure with a GitHub Actions workflow.
- [ ] Verify that `/docs` and all required files exist.
- [ ] Obtain a green workflow run and include a screenshot in the README.
- [ ] Maintain at least 20 meaningful commits.

## Video presentation — 10 marks

- [ ] Upload an unlisted YouTube walkthrough.
- [ ] Explain the ERD decisions and endpoint-plan choices.
- [ ] Explain the SQL design and run the script in SSMS.
- [ ] Add the video link to the README.

## Correct submission — 5 marks

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

- [ ] Lecturer-provided GitHub repository URL, if one has already been issued.
- [ ] Part 2 functional-requirements pages referenced by the Part 1 endpoint-plan instructions.
- [ ] Any additional PoE cover-sheet requirements.

