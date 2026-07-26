# RentalApp architecture

## Component diagram

```mermaid
flowchart TD
    V[".NET MAUI Views"] --> VM["MVVM application layer"]
    VM --> C["Authenticated API client"]
    C --> API["ASP.NET Core API"]
    API --> S["Business services + state machine"]
    S --> R["EF Core repositories"]
    R --> DB["PostgreSQL 16 + PostGIS"]
```

The Views contain only binding and navigation lifecycle code. ViewModels coordinate user actions through interfaces. The API owns validation and security. Repositories own persistence and spatial queries. This prevents UI code from bypassing business rules or connecting directly to PostgreSQL.

## Database schema

```mermaid
erDiagram
    USER ||--o{ ITEM : owns
    USER ||--o{ RENTAL : borrows
    USER ||--o{ REVIEW : writes
    USER ||--o{ REFRESH_TOKEN : receives
    ITEM ||--o{ RENTAL : booked_as
    ITEM ||--o{ REVIEW : receives
    RENTAL ||--o| REVIEW : produces

    USER {
        uuid Id PK
        string Email UK
        string DisplayName
        string PasswordHash
    }
    ITEM {
        uuid Id PK
        uuid OwnerId FK
        string Title
        decimal DailyRate
        string Address
        geography Location
    }
    RENTAL {
        uuid Id PK
        uuid ItemId FK
        uuid BorrowerId FK
        datetime StartDateUtc
        datetime EndDateUtc
        string Status
    }
    REVIEW {
        uuid Id PK
        uuid RentalId FK_UK
        uuid ItemId FK
        int Rating
        string Comment
    }
```

`Item.Address` stores the readable collection address. The MAUI client resolves
typed addresses through platform geocoding, or reverse-geocodes the device GPS
position, before sending the address and coordinates to the API. The API still
validates coordinate and address bounds. `Item.Location` is
`GEOGRAPHY(POINT, 4326)` and has a GiST index. The nearby repository query is
translated by the Npgsql NetTopologySuite provider to PostGIS distance
operations, including `ST_DWithin` semantics.

## Rental request sequence

```mermaid
sequenceDiagram
    actor Borrower
    participant App as MAUI App
    participant API as Rental API
    participant Service as Rental Service
    participant Repo as Repositories
    participant DB as PostGIS DB
    Borrower->>App: Select dates
    App->>API: POST /rentals + JWT
    API->>Service: Request rental
    Service->>Repo: Check item and overlap
    Repo->>DB: Query bookings
    DB-->>Repo: No overlap
    Service->>Repo: Save Requested rental
    Repo->>DB: INSERT rental
    API-->>App: Rental and total price
    App-->>Borrower: Confirmation
```

## Rental state diagram

```mermaid
stateDiagram-v2
    [*] --> Requested
    Requested --> Approved
    Requested --> Rejected
    Approved --> Rejected
    Approved --> OutForRent
    OutForRent --> Overdue
    OutForRent --> Returned
    Overdue --> Returned
    Returned --> Completed
    Rejected --> [*]
    Completed --> [*]
```

Each state class declares only its permitted next states. The service separately checks whether the current user is the item owner or borrower. This keeps workflow validity and authorisation as distinct responsibilities.
