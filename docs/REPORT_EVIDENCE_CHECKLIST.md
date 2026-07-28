# Report evidence checklist

Use this after the project has been run locally and in GitHub Actions. Do not fabricate evidence; capture the actual screens and results.

A professionally formatted final report is generated at
`output/report/RentalApp_40535392_Report_Final.docx`, with a matching PDF at
`output/pdf/RentalApp_40535392_Report_Final.pdf`. It contains the four required
diagrams, feature matrix, feature-to-source evidence, eight genuine emulator
captures across three evidence pages, test-class breakdown, three test excerpts,
verified CI/coverage exports, workflow excerpt, pattern evidence, AI record, and
references.

- [x] Minimal designed cover: project title and matriculation number are prominent; Justin Wylie, the 27 July 2026 submission date, and public GitHub URL are integrated into the main title-page design to preserve all cover-page marks.
- [x] Component diagram from `ARCHITECTURE.md`.
- [x] Database schema diagram from `ARCHITECTURE.md`.
- [x] Rental sequence diagram from `ARCHITECTURE.md`.
- [x] State diagram from `ARCHITECTURE.md`.
- [x] Feature checklist covering authentication, item create/list/detail/owner update, nearby search, rental workflow, verified reviews, MVVM, repositories, services, tests, and CI/CD.
- [x] Eight genuine running-emulator captures document login/registration, marketplace browsing, item details and creator edit, item creation/address entry, nearby search, rental workflow, navigation/sign-out, and profile/reputation.
- [x] All feature claims are also traced to XAML, ViewModels, API services, and executable tests.
- [x] Record verified coverage: 87.4% line coverage from the final GitHub Actions Cobertura export.
- [x] Insert a rendered coverage evidence panel sourced from the successful Actions artifact.
- [x] Test class and test-count list: 89 total, 89 passed, 0 failed, 0 skipped.
- [x] Three representative test excerpts.
- [x] `.github/workflows/build.yml` excerpt and API-verified evidence that both jobs passed in run #3.
- [x] MVVM, Repository, Service Layer, and State Pattern explanations with one code excerpt each.
- [x] At least three significant AI interactions and a critical reflection from `AI_USAGE.md`.
- [x] References to the official .NET MAUI, EF Core, Npgsql/NetTopologySuite, PostGIS, xUnit, Docker, and GitHub Actions documentation.
