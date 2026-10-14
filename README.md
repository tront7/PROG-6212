# RaceDay API — Setup Guide (Part 2)

## Prerequisites
- .NET 10 SDK
- SQL Server (local instance or SQL Server Express/LocalDB)
- Visual Studio 2022 (or VS Code with the C# Dev Kit extension)

## 1. Restore and build
```
dotnet restore
dotnet build
```

## 2. Configure the connection string
Edit `RaceDay.Api/appsettings.json` and point `ConnectionStrings:DefaultConnection`
at your SQL Server instance. The default assumes a local instance with
Windows/Trusted authentication:
```
Server=.;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True;
```

## 3. Create the database (EF Core Code-First migrations)
From the `RaceDay.Api` folder:
```
dotnet ef migrations add InitialCreate
dotnet ef database update
```
This creates `RaceDayDB` with the same six tables, keys, and relationships as
the ERD and SQL script from Part 1. The two fixed roles (Organiser,
Participant) are seeded automatically.

> If `dotnet ef` isn't recognised, install it once with:
> `dotnet tool install --global dotnet-ef`

## 4. Run the API
```
dotnet run --project RaceDay.Api.csproj
```
Swagger UI opens automatically in development at:
```
https://localhost:<port>/swagger
```
Every endpoint from the Part 1 plan is listed there and can be tested
directly from the browser.

## 5. Authentication flow for testing in Swagger
1. `POST /api/auth/register` — create an Organiser and a Participant account.
2. `POST /api/auth/login` — logs in and sets a session cookie in the browser.
3. Because Swagger UI runs in the same browser tab, the session cookie is
   sent automatically on subsequent "Try it out" calls — no token to copy in.
4. `POST /api/auth/logout` ends the session.

## 6. Run the unit tests
```
dotnet test
```
Tests run against an EF Core InMemory database, so no SQL Server connection
is needed to run them — this is also how the GitHub Actions CI workflow
(`.github/workflows/dotnet-ci.yml`) runs them automatically on every push.

## Project structure
```
RaceDay.sln
RaceDay.Api/            → the ASP.NET Core Web API
  Models/                 → EF Core entities (match the Part 1 ERD)
  Data/                    → RaceDayDbContext
  DTOs/                    → request/response shapes
  Controllers/             → Auth, Users, Events, Categories, Enrolments, Results
  Services/                → PasswordHasher
RaceDay.Tests/           → xUnit test project
.github/workflows/       → CI: build + run tests on every push
```
