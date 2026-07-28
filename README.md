# RentalApp — peer-to-peer Library of Things

RentalApp is a deliberately compact coursework implementation by Justin Wylie (matriculation number 40535392). It targets the 70% band by completing the Tier 1 and Tier 2 requirements and adding the rental State Pattern and automatic overdue detection. It uses .NET 10, .NET MAUI, ASP.NET Core, PostgreSQL 16 with PostGIS, Entity Framework Core, xUnit, Docker Compose, and GitHub Actions.

## Implemented features

- JWT registration and login, hashed passwords, rotating hashed refresh tokens, and Android Secure Storage.
- One account type: every authenticated user can both list items and request other users' items.
- Emulator-network readiness checks and bounded retries for transient sign-in socket failures.
- Create, browse, inspect, and listing-creator-update item API operations.
- Normal collection-address entry with forward/reverse geocoding, plus a device GPS alternative.
- Configurable 1–25 km PostGIS nearby search.
- `GEOGRAPHY(POINT, 4326)` storage, GiST index, and PostGIS radius query.
- Rental requests with inclusive price calculation and date-overlap prevention.
- Consistent UK `dd/MM/yyyy` date entry and display in the Android interface.
- Relationship-based rental actions: listing creators respond/confirm; requesters confirm return.
- State Pattern for all requested workflow states plus automatic overdue detection.
- Reviews restricted to the requesting user after a completed rental, with item and community averages.
- Listing-creator-only editing and availability control in the Android UI.
- Separate relationship- and status-aware listing/request actions, plus a completed-rental picker for verified reviews.
- Direct verified-review navigation after final completion and an always-visible navigation-drawer sign-out action.
- MVVM, Repository, Unit of Work, Service Layer, dependency injection, and DTO boundaries.
- xUnit unit tests, mocked GPS, service tests, critical-path ViewModel tests, and a real PostGIS integration fixture.
- Two-job GitHub Actions pipeline with an HTML coverage report and a signed Release APK artifact.

The diagrams and rationale are in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md), the brief mapping is in [docs/COMPLETION_MATRIX.md](docs/COMPLETION_MATRIX.md), and AI usage is documented in [docs/AI_USAGE.md](docs/AI_USAGE.md).

The remaining public-repository, commit-history, CI evidence, and report
hand-off steps are in
[docs/GITHUB_SUBMISSION.md](docs/GITHUB_SUBMISSION.md).

For presentation preparation, use
[docs/DEMONSTRATION_CODE_GUIDE.md](docs/DEMONSTRATION_CODE_GUIDE.md) and search
the source for `Presentation point:` comments. Presentation-critical classes use
ordinary constructors, named private fields, explicit method bodies, and named
intermediate values so the execution path can be followed without relying on
dense C# shorthand.

The final report is available at
`output/report/RentalApp_40535392_Report_Final.docx`, with the submission PDF at
`output/pdf/RentalApp_40535392_Report_Final.pdf`. It includes the public
repository URL, architecture diagrams, feature-to-source evidence, the retained
Near me emulator capture, final CI coverage export, successful workflow
evidence, design-pattern examples, AI disclosure, and submission verification.
Three compact evidence pages include eight genuine emulator screenshots covering
authentication, browsing, item detail, listing creation, nearby search, rental
workflow, navigation, profile, reputation, and sign-out.

## Prerequisites

- Windows 10/11 with Docker Desktop running Linux containers.
- .NET 10 SDK and the MAUI Android workload.
- Android SDK, Java 17, and an Android emulator.
- Visual Studio Code with C# Dev Kit, C#, Docker, and Dev Containers extensions.

Install the workload once:

```powershell
dotnet workload install maui-android
```

The Dev Container intentionally contains only the .NET backend development environment. Docker Compose, Java, the Android SDK, and the emulator run on the Windows host; this avoids nesting or forwarding Docker through the development container. The Dev Container does not forward ports 5432 or 8080 because Docker Compose publishes those ports directly on Windows.

The workspace also disables the automatic WSLg Wayland socket mount because the project has no Linux GUI application and the injected cross-WSL socket can prevent container creation on Windows.

Automatic VS Code port forwarding is disabled for ports 8080 and 5432. Docker Desktop publishes these ports directly; forwarding them from the Dev Container can make a healthy API time out on the Windows host.

## Run the database and API

### One-command Windows startup

After the APK has been installed once, start Docker, wait for API health, start
the configured emulator, and open RentalApp with:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\Start-RentalApp.ps1"
```

After changing mobile source code, rebuild and reinstall the APK as part of the
same startup process:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\Start-RentalApp.ps1" -Rebuild
```

The script is deliberately commented for demonstration. It selects the complete
Android SDK, starts Docker Compose, waits for `/health`, cold-starts the emulator
when needed, waits for Android's package service, optionally publishes/installs
the APK, and launches the app. Before rebuilding, it checks for 5 GB of free
space and, when necessary, automatically clears only the current user's Temp and
NuGet download caches before checking again. Individual commands remain documented below for
troubleshooting and assessment explanation.

From the repository root in a Windows PowerShell terminal outside the Dev Container:

```powershell
docker compose up --build -d
docker compose ps
```

Wait until both services show as healthy. The API is available at `http://localhost:8080`, its health endpoint is `http://localhost:8080/health`, and development OpenAPI JSON is at `http://localhost:8080/openapi/v1.json`. The API applies discovered EF Core migrations, falls back to creating a new local schema from the EF model when no migrations are discovered, and inserts demonstration data on first start.

For compatibility with early coursework builds that created tables before EF
migration history was available, startup recognises an existing complete schema,
records the initial migration as its baseline, and then applies newer migrations.
This allows the address migration to upgrade an existing Docker volume without
deleting user data.

Demonstration accounts:

| Account | Email | Password |
| --- | --- | --- |
| Demo account 1 | `sarah@example.com` | `Rental123!` |
| Demo account 2 | `mike@example.com` | `Rental123!` |

These are development-only accounts. Production secrets must be supplied through environment variables or a secret manager.

If `docker compose ps` reports healthy services but the health request times out, close older VS Code windows that may still own a forwarded port and retry:

```powershell
curl.exe --max-time 10 http://127.0.0.1:8080/health
```

## Build and run the Android app

Start an Android emulator. The simplest repeatable path is to run `build-install-android.cmd` from the repository root. It publishes an APK with its .NET assemblies embedded, installs it, and launches the package.

The equivalent PowerShell commands are:

```powershell
dotnet publish RentalApp/RentalApp.csproj -c Debug -f net10.0-android -p:AndroidPackageFormats=apk -p:EmbedAssembliesIntoApk=true
adb uninstall com.justinwylie.rentalapp
adb install RentalApp/bin/Debug/net10.0-android/publish/com.justinwylie.rentalapp-Signed.apk
```

`EmbedAssembliesIntoApk` is also enabled for Debug in the project file. This matters when installing with ADB: Fast Deployment APKs omit managed assemblies and are intended to be accompanied by a Visual Studio deployment step.

The Android client calls `http://10.0.2.2:8080`, the emulator alias for the Windows host. Cleartext HTTP is enabled only for this local coursework environment.

## Demonstration workflow

Use this short sequence to exercise the core and advanced requirements. Both
accounts have identical capabilities; two are used only to demonstrate both
sides of one transaction:

1. Sign in as Mike, browse the drill, open its details, and request future dates.
2. Sign out and sign in as Sarah. Open Rentals, select the incoming request, approve it, then start the rental.
3. Sign in as Mike, select the outgoing rental, and mark it returned.
4. Sign in as Sarah and give final completion confirmation.
5. Sign in as Mike, select the completed outgoing rental, choose **Leave verified review**, and submit a rating.
6. As Sarah, open one of her item details and demonstrate listing-creator-only editing and availability control.
7. Use Near me to demonstrate the adjustable-radius PostGIS query and displayed distance.

The navigation drawer includes **Sign out** on every authenticated page, while
Profile retains the same action for discoverability and demonstration.

To publish a listing, open **List an item**, enter a normal street address
(including town and postcode), and select **Find address**. The MAUI geocoder
converts the address to coordinates used by PostGIS; the coordinates are not
shown as form fields. **Use current location** reverse-geocodes the device GPS
position as an alternative. The readable collection address is stored and
displayed on Browse, Nearby, and Item Details.

Invalid rental transitions and overlapping dates remain protected by the API even if a second client bypasses the UI.

## Run tests and collect coverage

Keep the Compose database running, then execute:

```powershell
dotnet test RentalApp.Test/RentalApp.Test.csproj --settings RentalApp.Test/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory TestResults
```

The `DatabaseFixture` creates and later removes an isolated `rentalapp_test` database. It verifies the actual PostGIS radius query rather than replacing spatial behaviour with an in-memory substitute. Other tests use EF Core InMemory or mocks for speed.

The coverage settings measure the handwritten API, application, and database
assemblies. Generated OpenAPI/`obj` sources, EF migration code, and DTO-only
contracts are outside the metric; endpoint startup and other handwritten API
code remain included and are exercised through `WebApplicationFactory` HTTP
integration tests.

Latest verified GitHub Actions result (26 July 2026): **89 passed, 0 failed,
0 skipped**, with **87.4% line coverage**. The tests ran on Linux .NET 10
against the workflow's PostgreSQL 16/PostGIS service.

GitHub Actions runs the same PostGIS-backed tests, fails the job if measured
line coverage drops below 80%, and converts the Cobertura output into HTML and
text summaries. A second job installs Android API 36 and publishes a signed
Release APK. The workflow can be started manually as well as by pushes and pull
requests to `main` or `master`. Download the `test-results-and-coverage` and
`rentalapp-android` artifacts from a successful run for report evidence.

## Production-style container deployment

The default `docker-compose.yml` is intentionally convenient for local coursework. A separate production-style definition removes the public database port, runs the API as a non-root user, uses restart policies, and requires externally supplied secrets:

```powershell
Copy-Item .env.example .env
# Edit .env and replace POSTGRES_PASSWORD and JWT_SECRET.
docker compose -f docker-compose.production.yml up --build -d
```

Do not commit `.env`; it is ignored by Git. The CI workflow creates an ephemeral signing key only for an installable coursework artifact. A real Play Store deployment must keep a permanent Android signing key in protected GitHub secrets.

## API endpoints

| Method and route | Purpose |
| --- | --- |
| `POST /auth/register` | Register and issue access/refresh tokens |
| `POST /auth/token` | Sign in |
| `POST /auth/refresh` | Rotate a refresh token |
| `GET /auth/me` | Current profile and community rating |
| `GET /items` | Browse available items; optional category |
| `GET /items/{id}` | Item detail |
| `POST /items` | Create an owned item |
| `PUT /items/{id}` | Update an owned item |
| `GET /items/nearby` | PostGIS radius search |
| `POST /rentals` | Request an item |
| `GET /rentals/incoming` | Requests for the current user's listings |
| `GET /rentals/outgoing` | Current user's requests to rent |
| `PATCH /rentals/{id}/status` | Apply a permitted state transition |
| `GET /reviews/items/{itemId}` | Item reviews |
| `POST /reviews` | Review a completed rental |

All routes except registration, token, refresh, health, and development OpenAPI require `Authorization: Bearer <JWT>`.

## Project structure

```text
RentalApp/              .NET MAUI Android views and device services
RentalApp.Application/  ViewModels, API client, and client service interfaces
RentalApp.Contracts/    API request/response records and shared enums
RentalApp.Api/          ASP.NET Core endpoints and business services
RentalApp.Database/     EF Core entities, repositories, PostGIS, and rental states
RentalApp.Migrations/   Initial migration and design-time factory
RentalApp.Test/         xUnit unit and PostGIS integration tests
docs/                   Architecture, AI record, presentation, and report evidence
```

## Development workflow

Use feature branches and small conventional commits such as `feat: add rental overlap validation`, `test: cover state transitions`, and `docs: explain PostGIS query`. Pull requests trigger both CI jobs. Do not manufacture the brief’s minimum commit history after the fact; commit meaningful progress as it occurs.

To stop services while keeping data:

```powershell
docker compose down
```

To deliberately reset the local coursework database:

```powershell
docker compose down -v
```

The second command permanently removes the local Compose volume, so use it only when a clean database is intended.
