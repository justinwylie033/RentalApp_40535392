# RentalApp 70%+ completion matrix

This matrix maps the implemented project to the coursework brief. It is an engineering readiness checklist, not a guaranteed grade: the final mark also depends on the submitted report, Git history, live demonstration, CI evidence, and the student's ability to explain the code.

## Required technology evidence

| Requirement | Implementation and evidence | Status |
| --- | --- | --- |
| .NET 10 | Every project targets `net10.0`; the Android app targets `net10.0-android` | Complete |
| .NET MAUI | XAML Views, Shell navigation, Secure Storage, GPS, forward/reverse geocoding | Complete |
| PostgreSQL 16 + PostGIS | `postgis/postgis:16-3.5-alpine`, geography point, GiST index, radius query | Complete |
| Entity Framework Core | `AppDbContext`, repositories, migration project, startup migration/bootstrap | Complete |
| xUnit | Unit, ViewModel, service, state-machine, API-client, and PostGIS integration tests | Complete |
| GitHub Actions | Pull-request/push workflow, PostgreSQL service, coverage report, signed APK artifact | Complete; run #3 is green |
| Docker Compose | Local development and secret-driven production-style Compose definitions | Complete |
| VS Code Dev Container | .NET 10 container, C# Dev Kit recommendations, Windows-host Docker guidance | Complete |

## Feature tiers

| Brief feature | Implementation | Status |
| --- | --- | --- |
| API registration/login | Hashed passwords, JWT access tokens, rotating hashed refresh tokens | Complete |
| Authenticated client | Bearer token injection, refresh, Android Secure Storage, sign-out | Complete |
| Item create/list/detail | Address-based listing form, geocoding, MAUI pages, API and service layers | Complete |
| Owner item update | Owner-only API rule plus edit/availability UI on the detail page | Complete |
| Basic rental request | Date selection, inclusive price, incoming/outgoing lists | Complete |
| MVVM | Observable ViewModels and generated commands; minimal page lifecycle code | Complete |
| Repository pattern | Generic repository plus item, rental, and review repositories | Complete |
| Location discovery | Address geocoding, device GPS alternative, radius/category, PostGIS distance | Complete |
| Rental workflow | Approve, reject, cancel, start, return, complete, relationship-aware UI actions | Complete |
| Double-booking prevention | Inclusive date-overlap validation in the API service | Complete |
| Reviews | Borrower-only verified review after completion; completed-rental picker | Complete |
| Service layer | Separate API and client services for items, location, rentals, reviews, auth | Complete |
| State Pattern | One class per rental state with parameterised transition tests | Advanced feature complete |
| Overdue detection | Background service transitions expired out-for-rent records | Advanced feature complete |
| MediatR/CQRS Lite | Optional in the brief; deliberately excluded to keep the project explainable | Not included |
| SonarCloud | Optional in the brief; local/GitHub coverage evidence used instead | Not included |

## Quality and deployment gates

Run these on the final Windows copy before submission:

```powershell
curl.exe --max-time 10 http://127.0.0.1:8080/health
dotnet test RentalApp.Test/RentalApp.Test.csproj --settings RentalApp.Test/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory TestResults
dotnet publish RentalApp/RentalApp.csproj -c Debug -f net10.0-android -p:AndroidPackageFormats=apk -p:EmbedAssembliesIntoApk=true
```

Then complete all external evidence gates:

- [x] Install the final APK and complete the workflow in `README.md` without a crash.
- [x] Push the source to the public `justinwylie033/RentalApp_40535392` repository.
- [x] Confirm both GitHub Actions jobs are green and download their artifacts.
- [x] Record the latest verified result: 89/89 passing with 87.4% line coverage (26 July 2026).
- [x] Capture login, browse, details/edit, create, nearby, rentals, reviews, and profile screenshots.
- [x] Include all report sections from `REPORT_EVIDENCE_CHECKLIST.md` in a 19-page PDF.
- [x] Retain at least 18 reachable commits, including 13 focused upgrade/fix commits reviewed through pull request #1.
- [ ] Practise the eight-minute demonstration and be able to explain the four patterns and AI decisions.

## Known scope boundaries

The project is deployment-shaped but not a public commercial service. Local Android builds use HTTP through the emulator host alias. A real public deployment requires HTTPS, a stable public API URL, a permanent Android signing key stored as a protected secret, database backups, monitoring, and a concurrency-safe database constraint or transaction for simultaneous booking requests.
