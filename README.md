# RaceDay

## System Description

RaceDay is a full-stack, web-based event management system built for the South African road running, walking, and cycling community. Many local events, from park runs to large road races, are still managed through paper registration, spreadsheets, and disconnected communication channels. RaceDay replaces that with a single platform where event organisers can create and manage events, categories, and results, and participants can browse events, enter them, and track their own performance history.

The project is built progressively across three parts of the Portfolio of Evidence:

| Part | What it delivers |
|---|---|
| **Part 1** | System planning and database design: ERD, API endpoint plan, SQL script |
| **Part 2** | RESTful API (ASP.NET Core, Entity Framework Core Code-First, SQL Server) with cookie-based role authentication, Swagger and unit tests |
| **Part 3** | ASP.NET Core MVC web application that consumes the API, Azure Blob Storage image uploads, and Docker |

## Roles

RaceDay supports two distinct user roles:

- **Organiser:** can create, edit, and delete events (with a banner image), manage event categories, view all enrolments for their own events, change enrolment status, and capture and publish finish times and positions.
- **Participant:** can create an account, browse and filter upcoming events, enter an event by selecting a category, view their own enrolments and their status, see their personal results (finish time and position, e.g. "47th of 312 finishers"), and manage their profile and profile picture.

Role-based access is enforced at the API level (Part 2) and reflected consistently in the MVC interface (Part 3): each role has its own navigation menu and pages, unauthenticated users are redirected to the login page, and users of the wrong role are shown an "access denied" page.

## How the pieces fit together

```
Browser ──► RaceDay.Web (MVC) ──HTTP──► RaceDay.Api (Web API) ──► SQL Server
                                              │
                                              └──► Azure Blob Storage (event banners, profile pictures)
```

- The **MVC app has no database access and no duplicated business logic**. Every read and write is an HTTP call to the API.
- **Session management:** on login the MVC app stores the user's id, role and name in its server-side session, and keeps the API's own session cookie in that same per-user session. That cookie is replayed on each API call, so every user acts as themselves and the API enforces roles. Logging out clears both. If the API session ends, the user is sent back to the login page.
- **Azure Blob Storage is integrated into the API.** The MVC app sends the chosen image to the API, the API stores it in Blob Storage and saves the public URL in the database, and the MVC app displays the image from that URL. The MVC app never talks to Azure directly.

## Folder Structure

```
PROG-6212/
├── .github/workflows/
│   ├── ci.yml                          → Part 1: validates repo structure and /docs contents
│   └── dotnet-ci.yml                   → Builds the API and MVC app, runs all unit tests, builds both Docker images
├── docs/
│   ├── RaceDay_ERD.png                 → Entity Relationship Diagram (updated in Part 3)
│   ├── RaceDay_API_Endpoint_Plan.md    → Full API endpoint specification (updated in Part 3)
│   └── RaceDay_Database.sql            → SQL script to create and populate the schema (updated in Part 3)
├── RaceDay.Api/                        → ASP.NET Core Web API
│   ├── Controllers/                    → Auth, Users, Events, Categories, Enrolments, Results
│   ├── Data/                           → RaceDayDbContext (EF Core, Code-First)
│   ├── DTOs/                           → Request and response shapes
│   ├── Models/                         → EF Core entities (match the ERD)
│   ├── Services/                       → PasswordHasher, BlobStorageService (Azure), ImageValidator
│   ├── Dockerfile
│   └── Program.cs
├── RaceDay.Web/                        → ASP.NET Core MVC front end
│   ├── Controllers/                    → Account, Home, Organiser, Participant
│   ├── Filters/                        → SessionAuthorize (login + role checks)
│   ├── Helpers/                        → Session keys, display formatters
│   ├── Models/                         → View models and API response shapes
│   ├── Services/                       → ApiClient (the only way the app reaches data)
│   ├── Views/                          → Razor views for both roles
│   ├── wwwroot/                        → Stylesheet and scripts
│   ├── Dockerfile
│   └── Program.cs
├── RaceDay.Tests/                      → xUnit tests for the API
├── docker-compose.yml                  → Runs SQL Server + API + MVC with one command
├── .env.example                        → Template for secrets (copy to .env)
├── RaceDay.sln
└── README.md
```

## Running RaceDay with Docker (one command)

**Prerequisites:** Docker Desktop (give it at least 4 GB of memory because SQL Server runs in a container) and an Azure Storage account for image uploads (see below).

```
copy .env.example .env      (macOS/Linux: cp .env.example .env)
# open .env and paste your AZURE_STORAGE_CONNECTION_STRING

docker compose up --build
```

| What | URL |
|---|---|
| **MVC web app** | http://localhost:5002 |
| API + Swagger | http://localhost:5000/swagger |
| SQL Server (for SSMS) | `localhost,14330`, user `sa`, password from `.env` |

The database is created automatically on first start (the API applies its migrations and retries until SQL Server is ready). Stop with `Ctrl+C`, or `docker compose down` (add `-v` to also delete the database).

To run only the MVC container against an API already running on your machine:

```
docker build -f RaceDay.Web/Dockerfile -t raceday-web .
docker run -p 5002:8080 -e Api__BaseUrl=http://host.docker.internal:5000/ raceday-web
```

The containerised app behaves identically to the app running locally.

## Azure Blob Storage setup

1. In the [Azure Portal](https://portal.azure.com), create a **Storage account** (Standard, locally-redundant is fine).
2. Open the account → **Settings → Configuration** and set **Allow Blob anonymous access** to **Enabled**, then save. (Images are served publicly by URL.)
3. Open **Security + networking → Access keys** and copy a **connection string**.
4. Provide it to the API:
   - **Docker:** put it in `.env` as `AZURE_STORAGE_CONNECTION_STRING`.
   - **Running locally:** `cd RaceDay.Api` then `dotnet user-secrets set "AzureBlobStorage:ConnectionString" "<your connection string>"`.

The API creates the `raceday-images` container automatically (public read access for blobs) and stores event banners under `event-banners/` and profile pictures under `profile-pictures/`. Never commit your connection string: `.env` is git-ignored and user-secrets are stored outside the repository.

## Running locally (without Docker)

**Prerequisites:** .NET 10 SDK, SQL Server (or Express/LocalDB), and the EF tool (`dotnet tool install --global dotnet-ef`).

1. **Connection string:** check `ConnectionStrings:DefaultConnection` in `RaceDay.Api/appsettings.json`.
2. **Create or update the database** from the `RaceDay.Api` folder:
   ```
   dotnet ef migrations add Part3Additions
   dotnet ef database update
   ```
   (If you have not created any migration before, use `InitialCreate` instead. The Part 3 migration adds the event distance, event type, banner URL and profile picture columns. **Commit the `Migrations` folder** as Docker uses it too.)
3. **Configure Azure** (see above) if you want image uploads.
4. **Start the API** (terminal 1): `dotnet run --project RaceDay.Api` → http://localhost:5000/swagger
5. **Start the MVC app** (terminal 2): `dotnet run --project RaceDay.Web` → http://localhost:5002

The MVC app finds the API through `Api:BaseUrl` in `RaceDay.Web/appsettings.json` (default `http://localhost:5000/`).

**Trying it out:** register one Organiser and one Participant (use two different browsers or a private window for the second). As the Organiser, create an event with a banner, add categories, then log in as the Participant, enter the event, and finally return to the Organiser to capture a result.

## Unit tests

```
dotnet test
```

The tests cover registration and login, event management, role-based rejection (a Participant cannot create events, an Organiser cannot enrol, anonymous users get 401), enrolments, event filtering, image uploads, enrolment status changes and result capture. They run against an in-memory database with a fake blob storage service, so they need neither SQL Server nor Azure.

## CI/CD

GitHub Actions workflows run on every push and pull request:

- **`ci.yml` (Part 1):** validates the repository structure, confirming the `/docs` folder exists with the required planning files.
- **`dotnet-ci.yml` (Parts 2 and 3):** restores, builds the whole solution (API, **MVC project** and tests), runs all unit tests, then builds both Docker images.

**Successful build screenshot:**

[Insert screenshot of the green GitHub Actions run here]

## Changes to the Part 1 design (explained)

While building the Part 3 interface, the Organiser event form needed fields that the first version of the ERD did not include. The ERD, SQL script and endpoint plan in `/docs` were updated to match the final code:

| Table | Added column | Why |
|---|---|---|
| Events | `DistanceKm` | The brief requires each event to capture a distance |
| Events | `EventType` (Run / Walk / Cycle) | The brief requires each event to capture an event type |
| Events | `BannerImageUrl` | Stores the Azure Blob Storage URL of the event banner |
| Users | `ProfilePictureUrl` | Stores the Azure Blob Storage URL of the profile picture |

New endpoints were also added: event filtering, `GET /api/events/mine` (dashboard), banner and profile picture uploads, and `PUT /api/enrolments/{id}/status`. All are listed in `docs/RaceDay_API_Endpoint_Plan.md`.

## Video Walkthroughs

- **Part 1:** [Planning documents, ERD, endpoint plan and SQL script](https://youtu.be/jazqQ2GsgoA?si=kMgBBIjxG0KRrSAK)
- **Part 3:** [Insert unlisted YouTube link here]

## Troubleshooting

| Problem | Fix |
|---|---|
| API stops with *"The model for context 'RaceDayDbContext' has pending changes"* | Run `dotnet ef migrations add Part3Additions` in `RaceDay.Api` and commit the new migration, then run again. |
| Uploads say image storage is not configured | The Azure connection string is missing: see **Azure Blob Storage setup**. |
| Uploaded images don't display | In the storage account's Configuration, enable **Allow Blob anonymous access**, then upload again. |
| The MVC app says it couldn't reach the RaceDay service | The API isn't running, or `Api:BaseUrl` is wrong. |
| `docker compose` can't bind a port | Something else is using 5000, 5002 or 14330. Change the left-hand port in `docker-compose.yml`. |
| SQL Server container keeps restarting | Give Docker Desktop at least 4 GB of memory and make sure `SA_PASSWORD` meets SQL Server's complexity rules. |
