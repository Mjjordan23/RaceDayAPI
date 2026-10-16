# RaceDay API

## Student Information

**Name:** Murendeni Nethenzheni
**Student Number:** ST10396326
**Module:** PROG6212 – Programming 2B
**Assessment:** Portfolio of Evidence – Part 1 (System Planning and Database) and Part 2 (RESTful API)
**Repository:** https://github.com/Mjjordan23/RaceDayAPI

---

## Project Description

RaceDay is a full-stack event management platform designed for South African running, walking, and cycling events.

**Organisers** create and manage sporting events, categories, enrolments, and race results. **Participants** create accounts, browse events, enter events by category, view their enrolments, and track their personal results.

Part 1 covered the planning and database design. Part 2 is the ASP.NET Core Web API built from that plan.

---

## User Roles

### Organiser

* Create, edit, and delete their own events.
* Manage categories for their events.
* View all enrolments for their events.
* Capture participant results.

### Participant

* Create an account and log in.
* Browse events.
* Enter events by category.
* View their own enrolments and results.

Anyone, even when logged out, can browse events, categories, and published results.

---

## Technologies Used

| Area           | Choice                                                                        |
| -------------- | ----------------------------------------------------------------------------- |
| Framework      | ASP.NET Core 8 Web API (C#)                                                   |
| Database       | SQL Server, EF Core 8 Code-First migrations                                   |
| Authentication | Server-side sessions; passwords hashed with `PasswordHasher<User>`            |
| Authorisation  | Custom `[RequireRole]` filter: 401 if not logged in, 403 if the role is wrong |
| Documentation  | Swagger (Swashbuckle) using XML comments                                      |
| Testing        | xUnit, EF InMemory provider, `WebApplicationFactory` integration tests        |
| CI             | GitHub Actions: restore, build, and test                                      |
| Tools          | Visual Studio, SQL Server Management Studio, GitHub                           |

---

## Project Structure

```text
RaceDayAPI/
├── .github/workflows/
│   ├── part1-ci.yml
│   └── part2-ci.yml
├── docs/
│   ├── RaceDay ERD
│   ├── API Endpoint Plan
│   └── RaceDayDB.sql
├── RaceDayAPI/
│   └── Web API source files
├── RaceDayAPI.Tests/
│   └── Unit and integration tests
├── RaceDayAPI.sln
└── README.md
```

---

# Part 1 – System Planning and Database

The `docs` folder holds the Part 1 documentation:

* **RaceDay ERD** – entities, attributes, primary and foreign keys, relationships, and cardinalities (7 entities).
* **API Endpoint Plan** – planned methods, routes, descriptions, required roles, request bodies, and expected responses.
* **RaceDayDB.sql** – creates the database, tables, relationships, constraints, and sample data.

### Running the SQL Script

1. Clone the repository.
2. Open SQL Server Management Studio and connect to your SQL Server instance.
3. Open `docs/RaceDayDB.sql` and execute it to create `RaceDayDB`.

### Part 1 CI Build
<img width="1882" height="955" alt="Part 1 green build" src="https://github.com/user-attachments/assets/57626631-9a23-4ed3-81de-9857a2368a01" />



### Part 1 Video

<img width="1913" height="928" alt="image" src="https://github.com/user-attachments/assets/c785953b-38b6-45b7-b6b5-85a4b0687aa7" />

---

# Part 2 – RESTful API

## Part 2 CI Build

<img width="1907" height="970" alt="image" src="https://github.com/user-attachments/assets/f2bf1b36-7e47-484c-86e1-2c749aa2fce5" />


## Part 2 Video

_Paste your unlisted Part 2 YouTube link here._

## How to Run It

1. Open `RaceDayAPI.sln` in Visual Studio 2022 or use the .NET 8 SDK.

2. In `RaceDayAPI/appsettings.json`, set the connection string to your own SQL Server instance.

   Example (a backslash must be doubled in JSON):

   ```json
   "DefaultConnection": "Server=YOUR_SERVER\\SQLEXPRESS;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True;"
   ```

3. Create the database from the migrations:

   ```bash
   dotnet ef database update --project RaceDayAPI
   ```

   Alternatively, use `Update-Database` in the Package Manager Console.

4. Run the API using `dotnet run --project RaceDayAPI` or press F5 in Visual Studio.

5. Open Swagger at `https://localhost:7254/swagger`.

6. Run the tests:

   ```bash
   dotnet test
   ```

   The tests use an in-memory database, so SQL Server is not required to run them.

### Trying It in Swagger

1. Send `POST /api/auth/register` to register an Organiser, then use `POST /api/auth/login`.
2. Create an event and a category.
3. In a second browser profile or incognito window (the session is a cookie), register and log in as a Participant, then enrol using the category ID.
4. As the Organiser, capture a result for that enrolment. The Participant can view it through `GET /api/results/me`.

## Endpoints

| Method | Route                                | Role               | Success | Errors             |
| ------ | ------------------------------------ | ------------------ | ------- | ------------------ |
| POST   | `/api/auth/register`                 | Public             | 201     | 400, 409           |
| POST   | `/api/auth/login`                    | Public             | 200     | 401                |
| POST   | `/api/auth/logout`                   | Any logged-in user | 200     | 401                |
| GET    | `/api/users/me`                      | Any logged-in user | 200     | 401                |
| PUT    | `/api/users/me`                      | Any logged-in user | 200     | 400, 401           |
| GET    | `/api/events`                        | Public             | 200     | -                  |
| GET    | `/api/events/{id}`                   | Public             | 200     | 404                |
| POST   | `/api/events`                        | Organiser          | 201     | 400, 401, 403      |
| PUT    | `/api/events/{id}`                   | Organiser (owner)  | 200     | 400, 403, 404      |
| DELETE | `/api/events/{id}`                   | Organiser (owner)  | 204     | 403, 404, 409      |
| GET    | `/api/events/{eventId}/categories`   | Public             | 200     | 404                |
| POST   | `/api/events/{eventId}/categories`   | Organiser (owner)  | 201     | 400, 403, 404      |
| PUT    | `/api/categories/{id}`               | Organiser (owner)  | 200     | 400, 403, 404      |
| DELETE | `/api/categories/{id}`               | Organiser (owner)  | 204     | 403, 404, 409      |
| POST   | `/api/events/{eventId}/enrolments`   | Participant        | 201     | 400, 403, 404, 409 |
| GET    | `/api/enrolments/me`                 | Participant        | 200     | 401, 403           |
| GET    | `/api/events/{id}/enrolments`        | Organiser (owner)  | 200     | 403, 404           |
| POST   | `/api/enrolments/{id}/results`       | Organiser (owner)  | 201     | 400, 403, 404, 409 |
| GET    | `/api/results/me`                    | Participant        | 200     | 401, 403           |
| GET    | `/api/events/{eventId}/results`      | Public             | 200     | 404                |

## Differences from My Part 1 Plan

* **`POST /api/events/{id}/banner` is not in Part 2.** Image upload needs Azure Blob Storage, which belongs to Part 3. The `BannerImageUrl` column already exists.
* **Extra 409 responses on delete.** Deleting an event or category that still has enrolments returns 409 with a message instead of a database error.
* **Extra filters on `GET /api/events`:** `type` (Run, Walk, Cycle), `search`, and `upcomingOnly`.
* Category routes use `{eventId}` instead of `{id}` for clarity.

## Testing

* **Unit tests** call controller methods directly with an in-memory database and a fake session. They cover registration and login, enrolment rules, event create/update/delete (another Organiser's event returns 403, delete with enrolments returns 409), and profile reads and updates.
* **Integration tests** start the real API in memory and make HTTP calls, so sessions and the role filter are exercised for real: 401 when not logged in, 403 for the wrong role, duplicate enrolment 409, results, logout, and registering again after a failed attempt.

## Design Decisions

* **Session authentication**, as the brief requires. The session stores `UserId` and `Role`, and no endpoint trusts a user id sent by the client.
* **Ownership checks:** an Organiser can only change their own events and categories.
* **Responses are projections**, not raw entities, which avoids circular JSON and keeps password hashes out of responses.

---

## AI Usage Disclosure

I used Claude (Anthropic) as an assistant during this project. It helped me to organise the Part 1 documents and reference list, plan and structure the Part 2 controllers, suggest unit and integration tests, tidy the Swagger comments, and draft this README and my video notes. I ran, tested, and corrected the project myself, I can explain the code, and the videos are recorded in my own voice.

## References

Microsoft (n.d.) *ASP.NET Core web API documentation*. Available at: https://learn.microsoft.com/aspnet/core/web-api (Accessed: 16 October 2026).

Microsoft (n.d.) *Entity Framework Core documentation*. Available at: https://learn.microsoft.com/ef/core (Accessed: 16 October 2026).

Microsoft (n.d.) *Integration tests in ASP.NET Core*. Available at: https://learn.microsoft.com/aspnet/core/test/integration-tests (Accessed: 16 October 2026).

Microsoft (n.d.) *Session and state management in ASP.NET Core*. Available at: https://learn.microsoft.com/aspnet/core/fundamentals/app-state (Accessed: 16 October 2026).

xUnit.net (n.d.) *Getting started with xUnit.net*. Available at: https://xunit.net/ (Accessed: 16 October 2026).

GitHub (n.d.) *GitHub Actions documentation*. Available at: https://docs.github.com/actions (Accessed: 16 October 2026).

Anthropic (2026) *Claude* [Large language model]. Available at: https://claude.ai (Accessed: 16 October 2026).
