# Demonstration code guide

The source contains concise `Presentation point:` comments at the architectural
decisions most useful in the assessment. In VS Code, search the entire workspace
for that exact phrase, or run:

```powershell
rg -n "Presentation point:" RentalApp.Api RentalApp.Application RentalApp.Database RentalApp RentalApp.Test
```

Do not read comments word-for-word during the presentation. Use them as prompts,
then explain the decision in your own words and show the running behaviour.

The main demonstration classes deliberately favour explicit C#: constructor
assignments show dependency injection, method bodies use named intermediate
values, and important branches use ordinary `if` statements. This is slightly
longer than compressed expression syntax, but easier to trace and defend.

## Suggested eight-minute code tour

| Time | Open this file | Explain |
| --- | --- | --- |
| 0:00 | `RentalApp/MauiProgram.cs` | Mobile dependency injection: Views → ViewModels → interfaces. |
| 0:40 | `RentalApp.Application/ViewModels/CreateItemViewModel.cs` | MVVM commands, address lookup, hidden spatial coordinate, testable navigation. |
| 1:30 | `RentalApp/Services/AddressGeocodingService.cs` | MAUI forward/reverse geocoding isolated behind an application interface. |
| 2:10 | `RentalApp.Api/Endpoints/ItemEndpoints.cs` | Thin authenticated Minimal API endpoints; user identity comes from JWT claims. |
| 2:45 | `RentalApp.Api/Services/ItemApplicationService.cs` | Service Layer validation, ownership, DTO mapping, address plus PostGIS point. |
| 3:35 | `RentalApp.Database/Data/Repositories/ItemRepository.cs` | Repository Pattern, SRID 4326, `IsWithinDistance`, GiST/PostGIS server query. |
| 4:30 | `RentalApp.Api/Services/RentalWorkflowService.cs` | Inclusive pricing, double-booking check, role authorisation, Unit of Work. |
| 5:25 | `RentalApp.Database/States/RentalStates.cs` | Advanced State Pattern and Open/Closed Principle. |
| 6:05 | `RentalApp.Application/Services/ApiClient.cs` | Secure token injection, refresh-token rotation, concurrency lock, API errors. |
| 6:50 | `RentalApp.Test/Api/RentalApiTests.cs` | Full HTTP/JWT/PostGIS integration testing. |
| 7:25 | `RentalApp.Test/Repositories/ItemRepositoryTests.cs` | Real spatial integration test and evidence for the 86.4% coverage result. |

## Four pattern answers

### MVVM

Views bind to observable properties and relay commands. ViewModels depend on
interfaces rather than Android types, allowing ordinary xUnit tests. Code-behind
only handles page lifecycle/navigation parameters.

### Repository and Unit of Work

Repositories contain EF Core queries and hide persistence details. The generic
repository supplies shared CRUD behaviour; specialised repositories implement
spatial and workflow queries. Services call `SaveChangesAsync` through one Unit
of Work after the complete business operation is ready to commit.

### Service Layer

API endpoints only translate HTTP input/output. Services own validation,
authorisation and workflow rules, so a different client cannot bypass rules that
the MAUI app happens to hide.

### State Pattern

Every rental state declares its allowed successors. `RentalStateMachine` resolves
the object for the current status and asks it to validate the next status. Actor
permission remains in `RentalWorkflowService`, keeping role security separate
from domain transition validity.

## Likely questions

**Why store both an address and a point?**  The address is useful to people; the
SRID 4326 geography point is useful to PostGIS. Storing both avoids repeated
geocoding and permits indexed distance queries.

**Why does longitude come first in `Point`?**  NetTopologySuite follows X/Y
ordering: longitude is X and latitude is Y.

**How is double booking prevented?**  The API checks inclusive overlap using
`newStart <= existingEnd && newEnd >= existingStart` before committing.

**Why not trust disabled UI buttons?**  UI controls improve usability but are not
a security boundary. The authenticated API repeats ownership, role, state and
validation checks.

**What proves PostGIS is genuinely used?**  `ItemRepositoryTests` runs against
PostgreSQL 16 with the PostGIS extension and checks items inside and outside a
real radius query.

**What remains before public deployment?**  HTTPS, a stable public API URL,
protected production secrets/signing keys, backups, monitoring, and a stronger
concurrency strategy for simultaneous booking requests.
