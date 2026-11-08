# RaceDay — Part 3: MVC Web Application

**Module:** PROG6212 Programming 2B  
**Stack:** ASP.NET Core MVC + Part 2 REST API + SQL Server + Azure Blob Storage + Docker + GitHub Actions  
**Framework:** .NET 8
RaceDay Part 3 adds the MVC web front end to the existing Part 2 API. The MVC application does not connect to SQL Server and contains no duplicate database/business rules: it calls the API for registration, authentication, event data, categories, enrolments, results, profile changes and image uploads.


## Project structure

- `RaceDay.Api/` — existing REST API, extended with Azure Blob image upload endpoints.
- `RaceDay.Mvc/` — new MVC web application with role-aware pages and Bootstrap styling.
- `RaceDay.Tests/` — existing API unit tests.
- `docs/RaceDayDB.sql` — Part 1 schema with the two Part 3 image URL columns.
- `docker-compose.yml` — SQL Server, API and MVC containers.
- `.github/workflows/dotnet.yml` — CI validation and .NET 8 builds/tests.


## Features

### Authentication and navigation
- Participant and Organiser registration and login through the Part 2 API.
- MVC session stores the authenticated user ID, role and API session cookie.
- Organiser and Participant navigation is role-specific.
- Logout signs out through the API and clears the MVC session.
- Protected MVC actions redirect unauthenticated users to Login.

### Organiser interface
- Dashboard with event count, total enrolments, result count and event dates.
- Create, edit and delete events.
- Event fields: name, description, date, venue, city and status.
- Upload an event banner while creating or editing.
- Add and delete distance/age-labelled categories with capacity and entry fee.
- View enrolled participants, selected category, race number and enrolment status.
- Record finish times and finishing positions.

### Participant interface
- Browse and search events; filter by city and starting date.
- View event details and available categories.
- Enrol in a selected category and receive API confirmation immediately.
- View and cancel personal enrolments.
- View personal race history, finish times and finishing positions.
- Update first and last name and upload a profile picture.

### Azure Blob Storage
The MVC application sends image files to the Part 2 API. The API validates file type and size, uploads the image to Azure Blob Storage, and saves its URL in SQL Server. The MVC application never accesses Azure directly.

Supported formats: JPG/JPEG, PNG and WEBP. Maximum upload size: 5 MB.

The API uses:
- `POST /api/events/{eventId}/banner`
- `POST /api/profile/picture`

The `Event.BannerUrl` and `ParticipantProfile.ProfilePictureUrl` columns store the resulting image URLs.

## Local setup (Visual Studio 2022)

1. Install the .NET 8 SDK and SQL Server/SQL Server Express/LocalDB.
2. Open `RaceDay.sln`.
3. Run `docs/RaceDayDB.sql` against your development SQL Server. It also loads demo data (7 events, categories, enrolments and results). Demo logins: organisers `lerato.organiser@raceday.example` / `Organiser@123`, participants `ayanda.participant@raceday.example` / `Participant@123`. **The supplied Part 1 script drops and recreates the RaceDay tables; back up data first.**
4. If you already created the Part 2 database and do not want to recreate it, apply these two schema updates once:

   ```sql
   ALTER TABLE dbo.Event ADD BannerUrl NVARCHAR(1000) NULL;
   ALTER TABLE dbo.Event ADD EventType NVARCHAR(50) NOT NULL CONSTRAINT DF_Event_EventType DEFAULT N'Road Race';
   ALTER TABLE dbo.ParticipantProfile ADD ProfilePictureUrl NVARCHAR(1000) NULL;
   ```

5. Set `RaceDay.Api/appsettings.json` → `ConnectionStrings:RaceDay` to your SQL Server instance.
6. Set the Azure connection string using User Secrets or an environment variable. Do not commit credentials:

   ```powershell
   dotnet user-secrets init --project RaceDay.Api
   dotnet user-secrets set "AzureBlobStorage:ConnectionString" "YOUR_AZURE_STORAGE_CONNECTION_STRING" --project RaceDay.Api
   ```

   Create an Azure Storage account and ensure the `raceday-images` container can serve uploaded images. The API creates the container when needed. If the account blocks anonymous blob access, configure an approved public-read container policy or adapt delivery to a secured SAS/proxy approach before the demonstration.

7. Start `RaceDay.Api` first. Its local HTTPS address defaults to `https://localhost:7157/`.
8. Start `RaceDay.Mvc`. It defaults to the API address configured in `RaceDay.Mvc/appsettings.json`.
9. Browse to the MVC URL printed by Visual Studio (normally `https://localhost:7xxx`).

## Build and test

From the repository root:

```bash
dotnet restore RaceDay.sln
dotnet build RaceDay.sln --configuration Release
dotnet test RaceDay.sln --configuration Release
dotnet run --project RaceDay.Api/RaceDay.Api.csproj
```

In another terminal:

```bash
dotnet run --project RaceDay.Mvc/RaceDay.Mvc.csproj
```

## Docker

Docker Desktop must be running. Copy `.env.example` to `.env` and set a strong local SQL Server password and your Azure Blob Storage connection string. Do not commit `.env`.

Build and start all services:

```bash
docker compose up --build
```

- MVC: `http://localhost:8080`
- API: `http://localhost:8081`
- SQL Server: `localhost,1433`

Run the database schema against the SQL Server container once before registering users. For example, connect with SQL Server Management Studio or `sqlcmd` and execute `docs/RaceDayDB.sql`. The API uses the internal Docker service address `sql` and the MVC uses `api`; these are intentionally different from local development URLs.

Stop containers:

```bash
docker compose down
```

Keep database data between runs in the `raceday-sql` Docker volume. To delete the local database volume as well (destructive):

```bash
docker compose down -v
```

## GitHub Actions

The workflow at `.github/workflows/dotnet.yml` checks the required project structure, restores the solution, builds the API and MVC projects, and runs the automated tests on pushes and pull requests. Confirm the workflow succeeds in your own GitHub repository and capture a genuine screenshot of the green run for submission.


