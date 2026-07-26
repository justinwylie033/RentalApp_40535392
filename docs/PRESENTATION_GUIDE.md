# Presentation guide

## Eight-minute structure

1. **Application workflow (3 minutes):** sign in as Sarah and publish an item using a street address, showing address lookup without exposing raw coordinates. Sign in as Mike, browse the item, run nearby search, and request it; then sign in as Sarah and approve it. Show that Rentals enables only valid next actions while the API independently enforces the state machine. Demonstrate owner editing and Mike selecting a completed rental to review.
2. **Architecture (2 minutes):** use `ARCHITECTURE.md`. Explain why MAUI calls an API instead of PostgreSQL, and trace View → ViewModel → service interface → API → business service → repository → PostGIS.
3. **Complex code (1 minute):** show `RentalWorkflowService.RequestAsync`. Explain inclusive price, self-rental prevention, and the overlap expression.
4. **Quality (1 minute):** show the GitHub Actions run, test result, coverage file, and PostGIS integration test.
5. **AI use (1 minute):** summarise three representative decisions from `AI_USAGE.md`, including why MediatR and SonarCloud were not added.

## Key questions and answers

**Why Repository pattern?** It keeps EF Core and PostGIS details out of business services, permits mocks in unit tests, and makes query code easy to locate.

**Why Service Layer?** Rental pricing, overlap validation, role checks, and review eligibility are business rules. Putting them in ViewModels would duplicate rules and allow another client to bypass them.

**Why a separate application project?** It makes ViewModels ordinary .NET classes. They can be tested quickly without an emulator while the MAUI project remains a thin UI.

**How does nearby search work?** A listing's typed address is forward-geocoded, or the device position is reverse-geocoded, while the readable address is stored for display. The API creates an SRID 4326 point. PostGIS uses the geography column and GiST index to restrict results by metres. Returned distances are converted to kilometres.

**How is double-booking prevented?** A booking overlaps when the requested start is before or equal to an existing end and the requested end is after or equal to an existing start. Rejected rentals do not block dates.

**Why JWT and refresh tokens?** The short-lived JWT authenticates API calls without a server session. A random refresh token is hashed in PostgreSQL, rotated when used, and never stored as plain text on the server. Android stores both tokens in Secure Storage.

**Why validate actions in both the UI and API?** The ViewModel disables actions that are not valid for the selected rental and current user, which improves usability. The API repeats the role and state checks because another client could bypass the Android UI. The server remains the security boundary.

**What would come next?** Add transaction-level protection against two simultaneous overlapping requests, owner/borrower notifications, item photos, and map rendering.
