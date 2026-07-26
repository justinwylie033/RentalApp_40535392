# Report evidence checklist

Use this after the project has been run locally and in GitHub Actions. Do not fabricate evidence; capture the actual screens and results.

A professionally formatted 16-page working draft is generated at
`output/report/RentalApp_40535392_Report_Draft.docx`, with a matching PDF at
`output/pdf/RentalApp_40535392_Report_Draft.pdf`. It already contains the four
required diagrams, feature matrix, test-class breakdown, three test excerpts,
workflow excerpt, pattern evidence, AI record, and references. The deliberately
marked evidence slots below must be completed with the student's real results
before the PDF is submitted.

- [ ] Cover page: Justin Wylie, matriculation number 40535392, project title, submission date, and final public GitHub URL. The report contains all fields; only the public URL remains unknown.
- [x] Component diagram from `ARCHITECTURE.md`.
- [x] Database schema diagram from `ARCHITECTURE.md`.
- [x] Rental sequence diagram from `ARCHITECTURE.md`.
- [x] State diagram from `ARCHITECTURE.md`.
- [x] Feature checklist covering authentication, item create/list/detail/owner update, nearby search, rental workflow, verified reviews, MVVM, repositories, services, tests, and CI/CD.
- [ ] Screenshots from the running emulator: login, browse, detail/request, address-based item creation, nearby results, rentals, reviews, and profile. One real Near me capture is included; seven captures remain.
- [x] Record verified coverage: 86.4% line coverage from the final Docker/Linux Cobertura report.
- [ ] Insert an HTML/Cobertura coverage screenshot from the successful GitHub Actions artifact.
- [x] Test class and test-count list: 65 total, 65 passed, 0 failed, 0 skipped.
- [x] Three representative test excerpts.
- [ ] `.github/workflows/build.yml` excerpt and screenshot of both green jobs. The excerpt is included; the live-run screenshot remains.
- [x] MVVM, Repository, Service Layer, and State Pattern explanations with one code excerpt each.
- [x] At least three significant AI interactions and a critical reflection from `AI_USAGE.md`.
- [x] References to the official .NET MAUI, EF Core, Npgsql/NetTopologySuite, PostGIS, xUnit, Docker, and GitHub Actions documentation.
