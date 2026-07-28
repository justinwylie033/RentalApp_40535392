# AI tool usage

This document records transparent and controlled AI assistance for learning outcome 4.

## Tools used

- ChatGPT Codex: architecture planning, implementation support, refactoring, test design, and documentation review.
- Visual Studio Code C# Dev Kit: compiler diagnostics, code navigation, and debugging.

## Significant interaction 1: architecture

**Task:** Turn the coursework brief into a manageable implementation targeting approximately 70% while retaining every required technology.

**Suggestion received:** Split the system into MAUI presentation, a testable application/MVVM library, an ASP.NET Core API, an EF Core database library, and a migrations project.

**Evaluation and control:** The separation was accepted because a mobile client should not connect directly to PostgreSQL and because ViewModels remain testable without an Android emulator. MediatR and SonarCloud were rejected for this scope: both are optional, and the state pattern already demonstrates an advanced pattern with less code to defend.

**Validation:** Dependencies point inward through interfaces, the API owns authorisation, and tests target ViewModels, business services, state transitions, and a real PostGIS query.

## Significant interaction 2: spatial search

**Task:** Implement “find within a radius” using PostGIS without placing spatial logic in a ViewModel.

**Suggestion received:** Store longitude and latitude as an SRID 4326 NetTopologySuite `Point`, map it to PostgreSQL `geography (point)`, add a GiST index, and query through `IsWithinDistance`.

**Evaluation and control:** Longitude is deliberately passed as X and latitude as Y. The API validates both coordinate ranges and restricts radius to 0.1–100 km. The location service converts metres to kilometres before returning client DTOs.

**Validation:** `ItemRepositoryTests` runs against PostgreSQL 16 with PostGIS, inserts one nearby and one distant item, and confirms only the nearby result is returned.

## Significant interaction 3: rental workflow

**Task:** Prevent invalid status changes and overlapping bookings while keeping the code explainable.

**Suggestion received:** Use one class per rental state and keep role checks in `RentalWorkflowService`.

**Evaluation and control:** The state pattern was retained because it demonstrates Open/Closed design clearly. Price uses inclusive rental days. Overlap uses `newStart <= existingEnd && newEnd >= existingStart`. Owner and borrower permissions are checked separately from transition validity.

**Validation:** Parameterised tests cover permitted and forbidden transitions. Service tests cover price, self-rental prevention, double-booking prevention, role authorisation, and overdue detection.

## Reflection

AI accelerated repetitive code and highlighted architectural options, but its output still required decisions and verification. The most important controls are automated tests, compiler warnings treated as errors, database constraints, authentication at the API boundary, and keeping the feature set small enough to explain. Before presenting, every class listed in `docs/PRESENTATION_GUIDE.md` should be traced in the debugger so the implementation can be defended without relying on generated explanations.

## Significant interaction 4: Android deployment and runtime debugging

**Task:** Diagnose an APK that displayed its splash screen and closed, then diagnose an item-details navigation crash.

**Suggestion received:** Capture Android Logcat rather than repeatedly changing code. The first log showed a Fast Deployment APK without embedded .NET assemblies. The second showed that application resources were requested before `App.xaml` had been initialised. The detail route was then hardened by removing a runtime-only static date expression and by catching navigation failures in the ViewModel.

**Evaluation and control:** The project now embeds assemblies in Debug APKs, initialises application resources before resolving `AppShell`, uses a bounded API timeout, and converts navigation failures into visible ViewModel errors. These changes were accepted because each directly corresponds to observed evidence rather than speculation.

**Validation:** Rebuild the APK, uninstall the previous package to remove Fast Deployment residue, reinstall, and capture a fresh Logcat only if Android still terminates. The item-detail, owner-edit, review-selection, and rental-action ViewModels now have focused automated tests.

## Significant interaction 5: measurable coverage improvement

**Task:** Raise the initial 52.8% line coverage above the distinction testing threshold without inflating the result by excluding handwritten production code.

**Suggestion received:** Use the Cobertura class list to identify real behavioural gaps, then add authenticated HTTP integration tests and focused client-service, authentication, profile, and navigation tests. Exclude only generated OpenAPI/`obj` sources, EF migration code, and DTO-only contracts from the metric.

**Evaluation and control:** The new tests exercise API endpoints through `WebApplicationFactory`, use the real PostgreSQL/PostGIS service for integration paths, and retain handwritten API, application, and database code in scope. When Windows Application Control blocked a newly built test assembly, the same tests were run in the documented .NET 10 Linux Dev Container image instead of weakening the machine's security policy.

**Validation:** The address-enabled Docker/Linux run completed 60 tests with 60 passed, 0 failed, and 0 skipped in 23 seconds. Its Cobertura report measured 86.5% line coverage, exceeding the brief's 80% distinction threshold. Later workflow, sign-out, network-retry, unified-account, review-count, and submission-upgrade changes were covered by the final 89-test GitHub Actions run described below.

## Significant interaction 6: user-friendly collection addresses

**Task:** Replace raw latitude/longitude form fields with professional address entry while retaining PostGIS spatial search.

**Suggestion received:** Accept a readable collection address, use the MAUI platform geocoder to resolve it to a coordinate, support reverse geocoding from the device location, and persist both the address and PostGIS point.

**Evaluation and control:** Coordinates remain the authoritative spatial value for radius queries, while the API validates the address and coordinate ranges independently. The UI does not ask ordinary users to understand geographic coordinates. Address editing uses the same owner-only API rule as the rest of the listing.

**Validation:** ViewModel tests verify that a current device position becomes a readable address and that a resolved address, latitude, and longitude are sent together when publishing. The authenticated API integration workflow verifies that an entered address is returned from item details.

## Significant interaction 7: presentation-focused readability

**Task:** Make the most important code easier for a JavaScript-familiar student to trace without removing the architecture required by the brief.

**Suggestion received:** Preserve project boundaries and patterns, but replace dense expression-bodied methods and primary-constructor shorthand in presentation-critical classes with ordinary constructors, named private fields, explicit method bodies, and intermediate variables.

**Evaluation and control:** The refactor retains MVVM, Service Layer, Repository, Unit of Work, State Pattern, dependency injection, JWT, EF Core and PostGIS. It deliberately avoids collapsing the system into a few large files, because that would reduce separation of concerns and testability.

**Validation:** After the readability refactor, the Linux Docker run completed all 60 tests with 60 passed, 0 failed, and 0 skipped in 22 seconds. Cobertura measured 86.9% line coverage, and the .NET 10 Android APK publish completed successfully. This established that the refactor improved readability without sacrificing verified behaviour or deployability.

## Significant interaction 8: unified accounts and relationship-aware actions

**Task:** Remove the artificial borrower/owner account split while retaining secure control of rental actions.

**Suggestion received:** Treat every authenticated account as capable of both listing and renting, then derive the permitted buttons from the user's relationship to each rental: listing creator or requester.

**Evaluation and control:** The account model was simplified, but ownership and borrower checks were deliberately retained in the API. The UI now presents only valid actions for the current relationship and state; it does not grant permissions by hiding buttons alone.

**Validation:** Focused rental ViewModel tests and API workflow tests confirm the correct actions and server-side role checks. The final GitHub Actions run on 26 July 2026 completed **89 tests with 89 passed, 0 failed, and 0 skipped**. Cobertura measured **87.4% line coverage**, above the brief's 80% distinction testing threshold.

## Significant interaction 9: review completion and rating counts

**Task:** Make the feedback workflow discoverable and show the number of reviews beside average ratings.

**Suggestion received:** Navigate directly to a verified-review form after completion, keep a completed-rental picker for later access, and add `ReviewCount` to item DTOs and rating labels.

**Evaluation and control:** The database still enforces one review per rental, and the API still limits review creation to the requester after the rental is completed. The count is calculated from loaded review data and transported through DTOs rather than queried directly by the View.

**Validation:** Review service, item-detail, profile, rental, and review ViewModel tests cover eligibility, navigation, and display data. These tests are included in the final 89-test, 87.4%-coverage result.

## Significant interaction 10: submission-focused marketplace upgrades

**Task:** Improve the final submission with meaningful upgrades and a verifiable
GitHub history rather than padding the history with empty changes.

**Suggestion received:** Add user-visible search, sorting, paging, My Listings,
availability, cancellation, accessibility semantics, and engineering controls
for readiness, rate limiting, security headers, and correlation tracing.

**Evaluation and control:** The changes were separated into 13 focused commits
whose messages describe independently reviewable behaviour. They were reviewed
through pull request #1. A final API-route correction and matching regression
test restored green CI before the pull request was merged.

**Validation:** GitHub Actions run #3 completed both `backend-tests` and
`android-build` successfully, enforced the 80% coverage gate, and uploaded the
coverage/test artifact and signed Android APK.
