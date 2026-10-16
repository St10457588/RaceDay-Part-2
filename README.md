# RaceDay API — Part 2

A RESTful ASP.NET Core Web API for the RaceDay system. It manages running, walking, and cycling events, age/distance categories, participant enrolments, and race results.

## Tech stack

- ASP.NET Core Web API (.NET 8)
- Entity Framework Core, Code-First
- SQL Server / LocalDB
- Cookie and session-based authentication
- Role-based authorization
- Swagger / OpenAPI
- xUnit integration tests
- GitHub Actions CI

## Roles

### Organiser

An Organiser can:

- Create, update, and delete their own events.
- Add categories to their own events.
- View all enrolments for their own events.
- Capture participant finish times and positions.

### Participant

A Participant can:

- Register and log in.
- View events and categories.
- Enrol in an event by selecting a category.
- View their own enrolments and results.

## Setup

1. Install the .NET 8 SDK and Visual Studio 2022.
2. Clone this repository.
3. Open `RaceDay.sln`.
4. Check the connection string in `RaceDay.Api/appsettings.json`.
5. Create and apply the initial EF Core migration:

```bash
dotnet ef migrations add InitialCreate --project RaceDay.Api
dotnet ef database update --project RaceDay.Api
```

6. Run the API:

```bash
dotnet run --project RaceDay.Api
```

7. Open Swagger at:

```text
https://localhost:<port>/swagger
```

## Running tests

```bash
dotnet test
```

The tests use an in-memory SQLite database, so they do not require a local SQL Server installation.

## API endpoints

| Area | Endpoint | Access |
|---|---|---|
| Register | `POST /api/auth/register` | Public |
| Login | `POST /api/auth/login` | Public |
| Logout | `POST /api/auth/logout` | Authenticated |
| Current user | `GET /api/auth/me` | Authenticated |
| Profile | `GET /api/users/profile` | Authenticated |
| Update profile | `PUT /api/users/profile` | Authenticated |
| List events | `GET /api/events` | Public |
| Event details | `GET /api/events/{id}` | Public |
| Create event | `POST /api/events` | Organiser |
| Update event | `PUT /api/events/{id}` | Event owner |
| Delete event | `DELETE /api/events/{id}` | Event owner |
| List categories | `GET /api/events/{eventId}/categories` | Public |
| Create category | `POST /api/events/{eventId}/categories` | Event owner |
| Enrol | `POST /api/enrolments` | Participant |
| My enrolments | `GET /api/enrolments/my` | Participant |
| Event enrolments | `GET /api/events/{eventId}/enrolments` | Event owner |
| Capture result | `POST /api/enrolments/{enrolmentId}/result` | Event owner |
| My results | `GET /api/results/my` | Participant |

## CI/CD

GitHub Actions builds the solution and runs all tests on every push.

![Successful GitHub Actions build](docs/ci-success.png)

## Demo video

Add your unlisted YouTube demonstration link here.
