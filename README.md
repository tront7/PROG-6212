# RaceDay

## System Description

RaceDay is a full-stack, web-based event management system built for the South African road running, walking, and cycling community. Many local events — from park runs to large road races — are still managed through paper registration, spreadsheets, and disconnected communication channels. RaceDay replaces that with a single platform where event organisers can create and manage events, categories, and results, and participants can browse events, enter them, and track their own performance history.

This repository contains **Part 1** of the Portfolio of Evidence: the system planning and database design. Before any application code is written, this part produces:

- An Entity Relationship Diagram (ERD) covering the full RaceDay data model
- A full API endpoint plan for the RESTful API to be built in Part 2
- A SQL script that creates and populates the database schema in SQL Server

All planning documents and the SQL script are committed inside the `/docs` folder of this repository.

## Roles

RaceDay supports two distinct user roles:

- **Organiser** — can create, edit, and delete events, manage event categories, capture participant results, and view all enrolments for their own events.
- **Participant** — can create an account, browse events, enter an event by selecting a category, view their own enrolments, and track their personal results.

Role-based access is enforced at the API level in Part 2 and reflected consistently in the MVC interface in Part 3.

## Contents of /docs

| File | Description |
|---|---|
| `RaceDay_ERD.png` | Entity Relationship Diagram for the full data model |
| `RaceDay_API_Endpoint_Plan.md` | Full API endpoint specification table |
| `RaceDay_Database.sql` | SQL script to create and populate the database schema |

## CI/CD

This repository uses a GitHub Actions workflow (`.github/workflows/ci.yml`) that validates the repository structure on every push and pull request — confirming that the `/docs` folder exists and contains the required planning files (`RaceDay_ERD.png`, `RaceDay_API_Endpoint_Plan.md`, and `RaceDay_Database.sql`).

The workflow is currently passing: the latest run on the `main` branch completed successfully with a green build, confirming that all required Part 1 planning documents are present and correctly placed in the repository.


## Video Walkthrough

An unlisted YouTube video walks through the planning documents, the ERD design decisions, the endpoint plan choices, and runs the SQL script live in SSMS.

**Video link:** [(https://youtu.be/jazqQ2GsgoA?si=kMgBBIjxG0KRrSAK)]
