# RaceDay

RaceDay is a race-event platform: an ASP.NET Core Web API (Part 2) plus an ASP.NET Core MVC web front end (Part 3).

**Demo video (unlisted YouTube):** _add your link here before submitting_

## RaceDay API (Part 2)

ASP.NET Core 10 Web API (Entity Framework Core Code-First, SQL Server) for managing race events, categories, enrolments and results.

## Project structure
| Path | Purpose |
| --- | --- |
| `src/RaceDay.Api` | The API: `Models`, `Data` (DbContext), `DTOs`, `Controllers`, `Services` (includes Azure Blob Storage) |
| `src/RaceDay.Web` | **Part 3** MVC front end: `Controllers`, `Views`, `Services` (typed API client), `Dockerfile` |
| `tests/RaceDay.Tests` | xUnit tests (run the real API against an in-memory database) |
| `docs/` | Part 1 planning documents: `erd.png`, `api-endpoint-plan.md`, `schema.sql` |
| `.github/workflows/` | `validate-docs.yml` (docs check) and `build-and-test.yml` (build + tests) |

## Running locally
Requires the .NET 10 SDK and SQL Server (LocalDB comes with Visual Studio).

1. (Optional) set your own JWT signing key: `dotnet user-secrets set "Jwt:Key" "<32+ character secret>" --project src/RaceDay.Api`. A development-only placeholder key is in `appsettings.json` so the tests can run in CI.
2. Check the connection string in `src/RaceDay.Api/appsettings.json`.
3. Create the database from the model (Code-First):
   ```
   dotnet ef migrations add InitialCreate --project src/RaceDay.Api
   dotnet ef database update --project src/RaceDay.Api
   ```
   (If `dotnet ef` is missing: `dotnet tool install --global dotnet-ef`.)
4. Run: `dotnet run --project src/RaceDay.Api`, then open `https://localhost:7100/swagger`.
5. Test: `dotnet test`

## Using Swagger
1. Call `POST /api/auth/register`, then `POST /api/auth/login`.
2. Copy the `token` from the login response, click **Authorize** (top right) and paste it in.
3. Every endpoint can now be tried directly from the page. Each one shows its purpose, expected input and responses.

## Design decisions
- **Session management:** stateless JWT bearer tokens (2-hour expiry) carrying the user id, email and role. Roles are enforced with `[Authorize(Roles = ...)]`, and ownership (an organiser may only change their own events) is checked in each controller.
- **Passwords:** hashed with BCrypt; the raw password is never stored or returned.
- **Registering as Organiser:** anyone can register as a Participant, but registering as an Organiser requires a secret `organiserCode` (`Registration:OrganiserCode` in config), so users cannot simply give themselves organiser powers.
- **Cancelling an enrolment** deletes the enrolment row (and so any result linked to it), which frees the place in the category.
- **Event link on enrolments:** an enrolment stores participant and category; its event is reached through Category -> Event.
- **Payments:** the `Payments` table from the Part 1 ERD is kept in the schema and model, but no payment endpoints were planned or requested, so none exist.
- **Status/role columns** store readable text (e.g. `Organiser`, `Run`, `Finished`) and `schema.sql` has CHECK constraints for the allowed values.

## Deviations from the Part 1 plan
| Change | Reason |
| --- | --- |
| `Events` gained `EventType` (Run/Walk/Cycle) and `Distance`; event create/update bodies include them. ERD and `schema.sql` updated. | The Part 2 brief requires every event to capture distance and event type. |
| Added `GET /api/users/me/results`. | The brief requires participants to view their own results; the plan only had the public leaderboard. |
| Added `GET /api/events/{id}/enrolments`. | The brief requires organisers to view all enrolments for their events; the plan only had them per category. |
| `POST /api/auth/register` takes an extra `organiserCode` for Organisers. | Controls who can become an Organiser (see design decisions). |
| `PUT /api/categories/{id}` can return 409. | Prevents lowering capacity below the number already enrolled. |
| Added 400 / 403 / 404 responses on several endpoints. | Documents validation and permission failures the API actually returns. |

## Tests
`dotnet test` runs the suite in `tests/RaceDay.Tests`, covering success and failure cases for registration and login, event management and ownership, role-based rejection (401 and 403), enrolments (including duplicate and full-category cases), and results. The same tests run in GitHub Actions on every push and pull request to `main`.


---

# Part 3: RaceDay MVC web application

`src/RaceDay.Web` is an ASP.NET Core MVC app that consumes the API for **all** data operations. It has no database access, no EF Core and no Azure SDK: images are uploaded to the API, which stores them in Azure Blob Storage.

## MVC structure
| Piece | Where | What it does |
| --- | --- | --- |
| Typed API client | `Services/RaceDayApi.cs` | The only place HTTP calls to the API are made. Adds the JWT, turns API errors into friendly messages, handles an unreachable API. |
| Session | `Services/SessionExtensions.cs` | Stores the JWT, user id, role, name and picture URL in server-side session (2-hour idle timeout, matching the JWT). |
| Role filter | `Filters/RequireRoleAttribute.cs` | Unauthenticated users go to Login (with a return URL); the wrong role goes to Access Denied. |
| Controllers | `Controllers/` | Account, Home, Events, Participant, Profile, and the Organiser set (`Organiser`, `ManageEvents`, `ManageCategories`, `ManageEnrolments`, `ManageResults`). |
| Views | `Views/` | Bootstrap 5 + custom RaceDay theme in `wwwroot/css/site.css`. |

Two navigation menus: **Organiser** (Dashboard, Events, Categories, Enrolments, Results) and **Participant** (Upcoming events, My enrolments, My results, Profile). Anonymous visitors can browse events and view event details, but everything else redirects to login.

Enrolment status colours: **Confirmed** (API value `Enrolled`) is green, **Pending** is amber, **Cancelled** is red. In this API an enrolment is recorded as confirmed immediately, so Participants normally see green.

## Azure Blob Storage (in the API)
- `POST /api/events/{id}/banner` and `POST /api/users/me/picture` accept an image (JPG, PNG, WEBP, max 5 MB), store it in a Blob container and save the URL (`Events.BannerUrl`, `Users.ProfilePictureUrl`).
- Configure in `src/RaceDay.Api/appsettings.json` or, better, user secrets:
  ```
  dotnet user-secrets set "AzureBlob:ConnectionString" "<your storage account connection string>" --project src/RaceDay.Api
  ```
  The container (`raceday-images` by default) is created automatically with public read access to blobs so browsers can display the images.
- Run the schema update (or an EF migration) for the two new columns, see the end of `docs/schema.sql`:
  ```
  dotnet ef migrations add AddImageUrls --project src/RaceDay.Api
  dotnet ef database update --project src/RaceDay.Api
  ```

## Running locally (without Docker)
1. Start the API: `dotnet run --project src/RaceDay.Api --urls "http://localhost:5100"`
2. Start the MVC app: `dotnet run --project src/RaceDay.Web` and open http://localhost:5200
3. The API address is `Api:BaseUrl` in `src/RaceDay.Web/appsettings.json`.
4. To register an Organiser, use the organiser code from the API's `Registration:OrganiserCode` setting.

## Running with Docker
The container needs the API running on your machine, listening on all interfaces:
```
dotnet run --project src/RaceDay.Api --urls "http://0.0.0.0:5100"
```
Then **start the MVC app with a single command** from the repository root:
```
docker compose up --build
```
or with plain Docker:
```
docker build -f src/RaceDay.Web/Dockerfile -t raceday-web . && docker run --rm -p 8080:8080 --add-host=host.docker.internal:host-gateway -e Api__BaseUrl=http://host.docker.internal:5100/ raceday-web
```
Open http://localhost:8080. The containerised app behaves the same as the local one; only the API address differs (`Api__BaseUrl`). The Dockerfile is multi-stage (SDK image to build, slim ASP.NET runtime image to run), restores packages in a separate cached layer, and runs as a non-root user.

## CI
`.github/workflows/build-and-test.yml` builds the whole solution (API, MVC app and tests), runs the tests, and builds the MVC Docker image.

## Part 3 design notes
- **No duplicated logic:** ownership, role rules, capacity and duplicate-enrolment checks stay in the API; the MVC app only shows the API's messages. Light input checks (required fields, finish-time format, image type and size) give friendlier errors before calling the API.
- **Session management:** the JWT lives only in server-side session, never in the browser. When the API answers 401 the session is cleared and the user is sent to Login.
- **Results:** saving a result publishes it immediately; participants see it on My Results. Capture is only offered once the event date has arrived. "47 out of 312" uses `totalFinishers`, which the API calculates per category.
- **Cancelling:** a participant can cancel an upcoming enrolment from My Enrolments (the API deletes the enrolment).
- **Errors:** unhandled errors and 404/403 pages show friendly RaceDay error pages, never raw exceptions.
