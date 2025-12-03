# IMPLEMENTATION PLAN - AUTOMAX

## 1. High-Level Overview
- **Automax** is an ASP.NET Core 8 MVC app for tracking multiple vehicles: odometer, gas, service history, reminders (date/odometer), plans, notes, documents, reports, backups, and user management.
- **Tech stack:** ASP.NET Core 8 MVC; cookie auth with root/admin/user roles; per-vehicle access via `UserAccess`; data access abstraction with LiteDB implementations (Postgres stubs not yet wired); xUnit + Moq tests in `Automax.Tests`.
- **Branding:** Namespaces, project metadata, and physical folders use Automax naming.

## Current Status & Onboarding
- LiteDB backend is wired and tested (default runtime provider).
- Reminder email scheduler runs as a hosted service (requires SMTP in `ServerConfig.MailConfig`).
- Backup/restore flows are implemented (LiteDB ZIP).
- Postgres provider is scaffolded with schema/migration/implementation notes in `docs/db/`; runtime CRUD is implemented for vehicles, gas, odometer, service, plans, reminders, notes, users, user access, user configs, document metadata, and extra fields; LiteDB stays the default provider; integration tests are scaffolded (skipped by default) in `Automax.Tests/PostgresIntegrationTests.cs` with CRUD smoke tests across these providers; a migration CLI (Automax.Migration) supports `inspect`, `migrate-vehicles`, `migrate-gas-odometer`, `migrate-service-reminders`, `migrate-plans-notes`, `migrate-users-access-configs`, `migrate-documents`, `migrate-extra-fields`, `migrate-all` (vehicles/users first), `verify-postgres`, `verify-postgres-relations`, and `migrate-document-files` (file copy via metadata, dry-run by default), with dry-run by default and `--apply` to write; CI workflow (`.github/workflows/postgres-integration.yml`) spins up Postgres, applies the schema, and runs integration tests when env vars are set.
- Theme toggle persistence and targeted nullability cleanup are complete.
- Quickstart and feature overview: see `README.md`.

## 2. Repository Structure (Current)
- `Automax.csproj` (main web app).
- `Automax.sln` (references Automax + Automax.Tests).
- `Controllers/` - MVC controllers (Home, Vehicle, Report, Settings, Admin, Account, Backup, ReminderEmail, API, Odometer, Gas, Service, Reminder, Note, Plan, Document, Login, Error).
- `Logic/` - Business logic (VehicleLogic, ReportLogic, ReminderLogic, ReminderEmailLogic, HomeDashboardLogic, UserLogic, OdometerLogic).
- `External/Interfaces/` and `External/Implementations/Litedb/` - Data access abstractions and LiteDB implementations (vehicles, gas, service, reminders, plans, odometer, notes, users, user configs/access, extra fields).
- `Models/` - Domain entities (Vehicle, GasRecord, ServiceRecord, OdometerRecord, Reminder*, PlanRecord, Note, UserData), view models (VehicleViewModel, VehicleIndexViewModel, ReportIndexViewModel, HomeDashboardViewModel, ServerSettingsViewModel, ReminderCalendarItem, ReminderEmailDigest), API DTOs, settings models.
- `Helper/` - ConfigHelper, FileHelper, StaticHelper (data paths), LiteDBHelper, MailHelper, ReminderHelper, PasswordHelper, BackupHelper, LocaleHelper.
- `Middleware/` - Authen, SecurityHeadersMiddleware.
- `Enum/` - ReminderMetric/Urgency, PlanPriority/Progress, ExtraFieldType, DashboardMetric, TagFilter, KioskMode.
- `Views/` - Razor views for all feature areas (Garage/Vehicle, Reports, Odometer/Gas/Service/Reminder/Note/Plan/Document, Settings, Admin, Account, Login, Backup, ReminderEmail, Error, Home).
- `Automax.Tests/` - test project `Automax.Tests` with controller/logic/helper/middleware tests.
- `wwwroot/` - CSS/JS (site.css/site.js), static assets.
- `data/` - LiteDB and config directory; `appsettings*.json`, docker files at root.

## 3. Auth & Access Control (Current Behavior)
- **Login:** Cookie-based auth via `LoginController`; root user seeds on first login if no users exist (username `root`, password `password`). Claims include `IsAdmin`, `IsRootUser`, and `NameIdentifier`.
- **Roles:** Root/admin can see all data; regular users limited to their accessible vehicles.
- **Per-user access:** `UserLogic` + `IUserAccessDataAccess` govern accessible vehicle IDs. Non-root users see only vehicles they own or are granted access to; root/admin skip filtering.
- **Garage:** `VehicleController.Index` obtains allowed vehicle IDs from `UserLogic`, passes to `VehicleLogic.GetVehicleDashboardAsync` with search/urgent filters; non-admin lists only accessible vehicles. On create, non-root vehicles are associated to the creator via `UserAccess`.
- **Reports:** `ReportController.Index` and `ExportCsv` fetch allowed IDs from `UserLogic` and call `ReportLogic` with filtering; non-admin reports/CSV only include accessible vehicles; root/admin see all.
- **Other controllers:** Access checks per vehicle are enforced via `UserLogic.UserHasAccessToVehicleAsync` for Odometer, Gas, Service, Reminder, Note, Plan, Document, etc.
- **Implemented:** Filtering for Garage/Reports and vehicle creation ownership. **Potential gaps:** Other actions rely on per-vehicle access checks but may not filter list queries at data layer.

## 4. Configuration & Data Layout
- **Pipeline (Program.cs):** Loads appsettings, configures DI (LiteDB data access, helpers, logic); sets up auth/authorization, localization, custom security headers middleware, static files, MVC routing.
- **Data directories:** `StaticHelper.EnsureDataDirectoriesExist` ensures `data/`, `data/config/`, `data/images/`, `data/documents/`, `data/translations/`, `data/temp/`, `data/widgets.html`.
- **Config files:** `data/config/serverConfig.json` (via `ConfigHelper.Load/SaveServerConfig`) for MOTD, auth toggle, locale override, upload limits, reminder email settings, mail config, etc.; `data/config/userConfig.json` (via `ConfigHelper.Load/SaveUserConfig`) for user preferences (tabs, units, column prefs, dashboard metrics).
- **Settings page:** `SettingsController` loads/saves `ServerConfig` (MOTD, auth toggle, locale overrides, max upload size, reminder email settings). Known UX gap: success messaging may be minimal; ensure TempData is shown in the view (currently uses TempData["StatusMessage"]).
- **Documents:** Uploads stored under `data/documents/{vehicleId}` with allowed extensions from `ServerConfig`; size limit configurable.

## 5. Domain Model & Core Features
- **Entities/Models:**
  - Vehicle (`Models/Vehicle/Vehicle.cs`) with view models (`VehicleViewModel`, `VehicleIndexViewModel`).
  - Odometer/Gas/Service records per vehicle (`Models/OdometerRecord`, `GasRecord`, `ServiceRecord`).
  - Reminders (`Models/Reminder/ReminderRecord`) with calendar items (`ReminderCalendarItem`, `ReminderEmailDigest`), metrics/urgency enums.
  - Plans (`Models/PlanRecord/PlanRecord`), Notes (`Models/Note/Note`), Documents (`FileHelper`).
  - Reports (`VehicleReportSummary`, `ReportIndexViewModel`).
  - Users (`UserData`, UserConfigData, UserAccess).
- **Garage:** `VehicleController.Index` shows vehicles with last mileage/service/gas dates, costs, counts, urgent flag; filters (search/urgent), pagination.
- **Per-vehicle modules:** OdometerController, GasController, ServiceController, ReminderController, NoteController, PlanController, DocumentController enforce access via `UserLogic`.
- **Reports:** `ReportController.Index` shows vehicle summaries (cost, mileage, urgent flag, counts) with filters/pagination; CSV export includes all accessible vehicles.
- **Dashboard:** `HomeController` + `HomeDashboardLogic` show MOTD, metrics, upcoming reminders (next 30 days) for accessible vehicles.
- **Admin:** `AdminController` (user management), `SettingsController` (server settings), `BackupController` (ZIP of data/), `ReminderEmailController` (manual reminder email digests), `LoginController`, `AccountController`.
- **Reminder Emails:** `ReminderEmailLogic` builds digests using due-date reminders within configured window; `ReminderEmailController` can preview/send via `MailHelper`.

## 6. Testing: Automax.Tests Status
- **Project:** `Automax.Tests`, target framework net8.0; references Automax project; uses xUnit + Moq.
- **Logic tests:** VehicleLogicTests, ReportLogicTests, ReminderLogicTests, ReminderEmailLogicTests, HomeDashboardLogicTests, ReminderHelperTests, LocaleHelperTests, PasswordHelperTests, SecurityHeadersMiddlewareTests.
- **Controller tests:** AdminControllerTests, BackupControllerTests, LoginControllerTests, ReminderEmailControllerTests, SettingsControllerTests.
- **Access-control tests:**
  - `VehicleControllerTests`: non-admin with one vehicle sees only that vehicle; non-admin with none sees empty; admin sees multiple vehicles.
  - `ReportControllerTests`: non-admin report summaries filtered to accessible vehicles; admin sees all.
- Tests compile and are expected to pass with current naming/references.

## 7. Progress & Backlog
### Completed / Implemented
- **Rebrand:** Branding standardized to Automax (main project namespaces/usings/csproj/views/layout and solution/test project setup).
- **Access control:** Garage/Reports filter by accessible vehicle IDs; vehicle creation links non-root user via `UserAccess`.
  - Key files: `Controllers/VehicleController.cs`, `Controllers/ReportController.cs`, `Logic/ReportLogic.cs`, `Logic/VehicleLogic.cs`.
- **Razor fix:** Removed nested `@{}` in `Views/Report/Index.cshtml`; search binding corrected.
- **Type/compat fixes:** `Models/API/VehicleInfo.cs` mileage cast; `Helper/FileHelper` bool returns; `DocumentController` allowed extensions; Security headers middleware; PBKDF2 password hashing with legacy SHA256 support.
- **Tests:** Access-control controller tests added; existing logic/helper/middleware tests retained.

### Backlog / Known TODOs
- [PARTIAL] Postgres provider: schema/migration/per-interface plan documented; runtime CRUD implemented for vehicles/gas/odometer/service/plan/reminder/note/users/user_access/user_configs/documents (metadata)/extra fields; integration-test harness exists (skipped by default, toggle via env); migration CLI supports inspect + vehicles + gas/odometer + service/reminders + plans/notes + users/access/configs + documents + extra fields + migrate-all + verify-postgres + verify-postgres-relations + migrate-document-files; CI job exercises integration tests against a fresh schema. Remaining work: broader orchestration (e.g., full pipelines combining migrate-all + verify modes + document file copy), multi-entity transactional guarantees, enable/automate Postgres integration tests in broader pipelines, and any future Postgres-backed interfaces.
- [NICE TO HAVE] Postgres integration tests enabled in CI once infra is ready, and full migration tooling once runtime provider coverage is complete.

## 8. How to Continue in a New Session
1. Load `docs/IMPLEMENTATION_PLAN_AUTOMAX.md` as primary context.
2. Run `dotnet build` and `dotnet test` from repo root to verify status.
3. For auth/access control: review `VehicleController`, `ReportController`, `UserLogic`, `IUserAccessDataAccess`, and related data access implementations.
4. For settings/config: review `ConfigHelper`, settings models, `SettingsController`, and settings views; confirm TempData messaging and persistence.
5. Pick items from Backlog and implement incrementally; update this document after significant changes.

Treat this document as the single source of truth for project state and keep it up to date.
