# RaceDay

## System Description

RaceDay is a full-stack, web-based event management system built for the South African road running, walking, and cycling community. Many local events, from park runs to large road races, are still managed through paper registration, spreadsheets, and disconnected communication channels. RaceDay replaces that with a single platform where event organisers can create and manage events, categories, and results, and participants can browse events, enter them, and track their own performance history.

The project is built progressively across three parts of the Portfolio of Evidence:

- **Part 1:** System planning and database design (ERD, API endpoint plan, SQL script)
- **Part 2:** RESTful API built with ASP.NET Core, Entity Framework Core, and SQL Server
- **Part 3:** MVC web application that consumes the API

This repository currently contains Parts 1 and 2.

## Roles

RaceDay supports two distinct user roles:

- **Organiser:** can create, edit, and delete events, manage event categories, capture participant results, and view all enrolments for their own events.
- **Participant:** can create an account, browse events, enter an event by selecting a category, view their own enrolments, and track their personal results.

Role-based access is enforced at the API level in Part 2 using cookie-based session authentication, and is reflected consistently in the MVC interface in Part 3.

## Folder Structure

```
PROG-6212/
├── .github/
│   └── workflows/
│       ├── ci.yml                      → Part 1: validates repo structure and /docs contents
│       └── dotnet-ci.yml               → Part 2: builds the solution and runs all unit tests
├── docs/
│   ├── RaceDay_ERD.png                 → Entity Relationship Diagram
│   ├── RaceDay_API_Endpoint_Plan.md    → Full API endpoint specification table
│   └── RaceDay_Database.sql            → SQL script to create and populate the schema
├── RaceDay.Api/                        → ASP.NET Core Web API
│   ├── Controllers/                    → Auth, Users, Events, Categories, Enrolments, Results
│   ├── Data/                           → RaceDayDbContext (EF Core, Code-First)
│   ├── DTOs/                           → Request and response shapes
│   ├── Models/                         → EF Core entities (match the Part 1 ERD)
│   ├── Properties/
│   │   └── launchSettings.json         → Runs in Development mode and opens Swagger
│   ├── Services/                       → PasswordHasher (PBKDF2)
│   ├── appsettings.json                → Connection string and logging config
│   ├── Program.cs                      → App startup, Swagger, cookie authentication
│   └── RaceDay.Api.csproj
├── RaceDay.Tests/                      → xUnit test project
│   ├── AuthControllerTests.cs
│   ├── EventsControllerTests.cs
│   ├── EnrolmentsControllerTests.cs
│   ├── CustomWebApplicationFactory.cs  → Runs the API in memory for tests
│   ├── TestHelpers.cs
│   └── RaceDay.Tests.csproj
├── RaceDay.sln
├── .gitignore
└── README.md
```

## Contents of /docs

| File | Description |
|---|---|
| `RaceDay_ERD.png` | Entity Relationship Diagram for the full data model |
| `RaceDay_API_Endpoint_Plan.md` | Full API endpoint specification table |
| `RaceDay_Database.sql` | SQL script to create and populate the database schema |

## Setup and Running the API (Part 2)

### Prerequisites

- .NET 10 SDK
- SQL Server (local instance or SQL Server Express/LocalDB)
- Visual Studio 2022 (or VS Code with the C# Dev Kit extension)

### 1. Restore and build

```
dotnet restore
dotnet build
```

### 2. Configure the connection string

Edit `RaceDay.Api/appsettings.json` and point `ConnectionStrings:DefaultConnection` at your SQL Server instance. The default assumes a local instance with Windows/Trusted authentication:

```
Server=.;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True;
```

### 3. Create the database (EF Core Code-First migrations)

From the `RaceDay.Api` folder:

```
dotnet ef migrations add InitialCreate
dotnet ef database update
```

This creates `RaceDayDB` with the same six tables, keys, and relationships as the ERD and SQL script from Part 1. The two fixed roles (Organiser, Participant) are seeded automatically.

> If `dotnet ef` isn't recognised, install it once with:
> `dotnet tool install --global dotnet-ef`

### 4. Run the API

From the `RaceDay.Api` folder:

```
dotnet run
```

Swagger UI is available at:

```
http://localhost:5000/swagger
```

Every endpoint from the Part 1 plan is listed there and can be tested directly from the browser.

> Swagger is only enabled in the Development environment. If the console shows `Hosting environment: Production`, make sure `Properties/launchSettings.json` exists, or set the environment manually before running:
> `$env:ASPNETCORE_ENVIRONMENT="Development"`

### 5. Authentication flow for testing in Swagger

1. `POST /api/auth/register`: create an Organiser and a Participant account.
2. `POST /api/auth/login`: logs in and sets a session cookie in the browser.
3. Because Swagger UI runs in the same browser tab, the session cookie is sent automatically on subsequent "Try it out" calls, so there is no token to copy in.
4. `POST /api/auth/logout`: ends the session.

### 6. Run the unit tests

```
dotnet test
```

Tests run against an EF Core InMemory database, so no SQL Server connection is needed to run them. This is also how the GitHub Actions workflow runs them automatically on every push.

## CI/CD

This repository uses two GitHub Actions workflows that run on every push and pull request:

- **`.github/workflows/ci.yml` (Part 1):** validates the repository structure, confirming that the `/docs` folder exists and contains the required planning files (`RaceDay_ERD.png`, `RaceDay_API_Endpoint_Plan.md`, and `RaceDay_Database.sql`).
- **`.github/workflows/dotnet-ci.yml` (Part 2):** restores, builds, and runs all unit tests for the solution.

The Part 1 workflow is passing: the latest run on the `main` branch completed successfully with a green build.

## Video Walkthrough

An unlisted YouTube video walks through the planning documents, the ERD design decisions, the endpoint plan choices, and runs the SQL script live in SSMS.

**Video link:** Part 1:(https://youtu.be/jazqQ2GsgoA?si=kMgBBIjxG0KRrSAK)
                Part 2:(https://www.youtube.com/watch?v=Qjjwui7VOEc)
