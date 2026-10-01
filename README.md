# Vehicle Management System

A web-based vehicle management application developed for the CreditWorks technical assignment.

The application allows users to manage vehicles and vehicle categories while automatically determining each vehicle's category based on its weight.

## Features

### Vehicle Management

- Add vehicles with owner, manufacturer, year of manufacture and weight.
- View all vehicles.
- Automatically determine a vehicle's category from its weight.
- Sort vehicles by owner, manufacturer, year and weight.
- Support ascending and descending sorting.

### Category Management

- View the current category configuration.
- Add, edit and delete categories.
- Assign an icon to each category.
- Maintain continuous category boundaries without gaps or overlaps.
- Preview proposed category changes before saving.
- Require confirmation when a change removes an existing category.
- Automatically reflect category boundary changes for existing vehicles.

## Technology Stack

- C#
- .NET 9
- ASP.NET Core Razor Pages
- Entity Framework Core
- SQL Server
- xUnit
- Bootstrap
- HTML
- CSS
- JavaScript

## Solution Structure

```text
VehicleManagement
├── VehicleManagement.Web
│   ├── Pages
│   ├── Data
│   ├── Migrations
│   ├── wwwroot
│   └── Program.cs
│
├── VehicleManagement.Core
│   ├── Entities
│   ├── Interfaces
│   ├── Services
│   └── Validation
│
├── VehicleManagement.Tests
│   ├── BusinessRules
│   ├── Validation
│   ├── Vehicles
│   ├── PageModels
│   └── Integration
│
└── VehicleManagement.sln
```

### Project Responsibilities

**VehicleManagement.Web**  
Contains the Razor Pages user interface, page handlers, Entity Framework Core database context, migrations and application configuration.

**VehicleManagement.Core**  
Contains the domain entities, category business rules, interfaces and validation logic.

**VehicleManagement.Tests**  
Contains automated xUnit tests covering business rules, validation, sorting, Razor Page models, persistence and input-safety scenarios.

## Database Design

The application uses SQL Server with Entity Framework Core.

### Vehicle

A vehicle stores:

- Owner name
- Manufacturer
- Year of manufacture
- Weight in kilograms

A vehicle does not store a `CategoryId`. Its category is calculated dynamically from its weight and the current category configuration.

This means that when category boundaries change, existing vehicles immediately reflect the new category without requiring vehicle records to be updated.

### Manufacturer

Manufacturers are stored separately and referenced by vehicles through `ManufacturerId`.

The initial manufacturers are:

- Mazda
- Mercedes
- Honda
- Ferrari
- Toyota

Storing manufacturers as database records avoids embedding manufacturer names throughout the application and makes the list easier to extend.

### VehicleCategory

A category stores:

- Name
- Minimum weight
- Maximum weight
- Icon

`MaxWeight` is nullable. A null maximum represents the final open-ended category.

The initial configuration is:

| Category | Minimum | Maximum |
| --- | ---: | ---: |
| Light | 0 kg | 500 kg |
| Medium | 500 kg | 2500 kg |
| Heavy | 2500 kg | No maximum |

## Category Business Rules

### Boundary Convention

Category ranges use a lower-inclusive and upper-exclusive convention:

```text
MinWeight <= VehicleWeight < MaxWeight
```

The final category is open-ended:

```text
VehicleWeight >= MinWeight
```

For example:

```text
Light   : 0 <= weight < 500
Medium  : 500 <= weight < 2500
Heavy   : weight >= 2500
```

Therefore:

- A vehicle weighing `499.99 kg` belongs to **Light**, while exactly `500 kg` belongs to **Medium**.
- A vehicle weighing `2499.99 kg` belongs to **Medium**, while exactly `2500 kg` belongs to **Heavy**.

This ensures that every boundary is unambiguous and each vehicle belongs to exactly one category.

### Continuous Category Coverage

A valid category configuration must:

- Start at `0 kg`.
- Have no gaps.
- Have no overlaps.
- Give each category one continuous weight range.
- End with an open-ended category.

The upper boundary of each non-final category must equal the lower boundary of the next category.

### Adding Categories

When a category is inserted, the user specifies its weight boundaries.

The application evaluates the proposed configuration and adjusts affected neighbouring boundaries where necessary to maintain continuous coverage.

If a new category completely covers one or more existing categories, those categories are identified as affected categories and require confirmation before removal.

### Editing and Deleting Categories

When a category is edited or deleted, the complete proposed configuration is validated before changes are saved.

The configuration must continue to:

- Start at `0 kg`.
- Have no gaps.
- Have no overlaps.
- Keep the final category open-ended.

If these rules are not satisfied, the changes are not saved and a validation message is displayed.

If a change removes an existing category, the user must confirm the removal before saving.

## Validation and Error Handling

Validation is performed on the server so that application rules do not rely only on browser-side validation.

### Vehicle Validation

The application checks that:

- Owner name is provided.
- A valid manufacturer is selected.
- Year of manufacture is within the accepted range.
- Weight is greater than zero.
- Weight contains no more than two decimal places.
- The vehicle can be matched to a valid category.

Manufacturer IDs submitted by the client are checked against the database.

### Category Validation

The application checks that:

- Category name is provided.
- Category name does not exceed 20 characters.
- Category names are unique using case-insensitive comparison.
- A valid icon is selected.
- Weight is not negative.
- Weight boundaries contain no more than two decimal places.
- Maximum weight is greater than minimum weight when specified.
- The first category starts at `0 kg`.
- Adjacent boundaries match exactly.
- Non-final categories have an upper boundary.
- The final category is open-ended.
- Submitted category IDs are valid and not duplicated.

The complete proposed configuration is validated before changes are persisted.

### Error Handling

Invalid input is returned to the page with validation messages so that the user can correct it.

Database update failures and unexpected application errors are handled without displaying internal exception details to the user.

Successful changes use the Post/Redirect/Get pattern where appropriate to reduce accidental duplicate submissions.

## Security Considerations

### SQL Injection Protection

Database access is performed through Entity Framework Core using LINQ and parameterised queries.

The application does not construct SQL commands by concatenating user-controlled input.

Input-safety tests include apostrophes and SQL-like strings to verify that these values are handled as data.

### Server-Side Validation

Business rules and input validation are enforced on the server. Client-side behaviour improves the user experience but is not treated as the source of truth.

Submitted manufacturer and category IDs are validated before use.

### Output Safety

Razor's standard output encoding is used when displaying user-provided values.

Category icons are selected from a predefined set rather than accepting arbitrary markup.

### Configuration and Secrets

Application configuration is separated from application logic. Sensitive production credentials should not be committed to source control.

Authentication and authorisation are outside the scope of this assignment.

## Getting Started

### Prerequisites

- .NET 9 SDK
- SQL Server 2019 or later
- Entity Framework Core tools
- Visual Studio, Visual Studio Code, or another compatible development environment

### Database Configuration

The default connection string is configured in:

```text
VehicleManagement.Web/appsettings.json
```

The supplied development configuration uses:

```text
Server=localhost;Database=VehicleManagementDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Update `DefaultConnection` if your SQL Server instance uses a different server name or authentication configuration.

### Restore Dependencies

From the solution root:

```bash
dotnet restore
```

### Create the Database

Apply the included Entity Framework Core migrations:

```bash
dotnet ef database update --project VehicleManagement.Web --startup-project VehicleManagement.Web
```

The migrations create the database schema and seed the initial manufacturers and vehicle categories.

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run --project VehicleManagement.Web
```

Open the local URL displayed in the terminal after the application starts.

### Run Tests

```bash
dotnet test
```

## Testing

The solution includes automated tests using xUnit covering business rules, validation, Razor Page behaviour, sorting, persistence and input safety.

Current test result:

```text
Total:     109
Succeeded: 109
Failed:    0
Skipped:   0
```

### Category Business Rules

Tests cover:

- Category determination from vehicle weight.
- Exact boundaries such as `500 kg` and `2500 kg`.
- Lower-inclusive and upper-exclusive boundary behaviour.
- Gap and overlap detection.
- Coverage beginning at `0 kg`.
- Final open-ended category validation.
- Adding, editing and deleting categories.
- Neighbouring boundary changes.
- Categories affected by proposed insertions.
- Duplicate names.
- Invalid names, icons and ranges.

### Vehicle Validation and Sorting

Tests cover:

- Required owner name.
- Manufacturer validation.
- Year validation.
- Positive vehicle weight.
- Maximum two-decimal-place precision.
- Sorting by owner, manufacturer, year and weight.
- Ascending and descending sorting.
- Invalid sorting parameters.

### Page Models and Persistence

Tests cover:

- Loading manufacturer and category data.
- Creating valid vehicles.
- Preventing invalid vehicles from being saved.
- Saving valid category changes.
- Rejecting invalid configurations.
- Confirming category removal.
- Persisting and retrieving vehicle data.
- Manufacturer relationships.
- Category add, update and delete operations.
- Dynamic vehicle recategorisation after category boundary changes.
- Rejecting invalid or duplicated submitted category IDs.
- Rejecting category boundaries with more than two decimal places.

### Input Safety

Tests include apostrophes, SQL-like input, manipulated identifiers and invalid sorting values to verify that untrusted input does not bypass application validation.

## Key Design Decisions

### Dynamic Categorisation

Vehicle category assignment is calculated from `WeightKg` and the current category configuration rather than storing a category reference on each vehicle.

This prevents stored category assignments from becoming outdated when category boundaries change.

### One Continuous Range per Category

Each category represents one continuous weight range.

A category cannot represent multiple disconnected ranges. When a category is inserted, affected neighbouring boundaries may therefore need to change to preserve continuous coverage.

### Whole-Configuration Validation

Category configuration is validated as a complete set because gaps and overlaps depend on relationships between neighbouring categories.

Changes are only persisted when the resulting configuration is valid.

### Staged Category Changes

Category changes are staged before being persisted so that the user can review the proposed configuration before selecting **Save Changes**.

If a proposed change removes an existing category, explicit confirmation is required.

### Razor Pages

ASP.NET Core Razor Pages was selected because the application is primarily a server-rendered CRUD application with a relatively small number of screens.

Core business rules and validation are kept separate from the user interface so that they can be tested independently.

## Future Improvements

For a production system, possible improvements include:

- Authentication and role-based authorisation for administrative operations.
- Optimistic concurrency handling for simultaneous category changes.
- Audit history for category configuration changes.
- Manufacturer management through the user interface.
- Pagination, searching and filtering for larger vehicle data sets.
- End-to-end browser testing and integration testing against an actual SQL Server instance.