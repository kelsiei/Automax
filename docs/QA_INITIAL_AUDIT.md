# QA Initial Audit (feature-tested-and-debugged)

Environment:
- DB: PostgreSQL `automax` on localhost:5432 (user `automax`)
- Schema/seed: `docs/db/automax-mysql-port-schema.pg.sql`, `docs/db/automax-mysql-port-seed.pg.sql`
- Tests run with `AUTOMAX_ENABLE_POSTGRES_INTEGRATION_TESTS=true` and `AUTOMAX_POSTGRES_CONNECTION_STRING=Host=localhost;Port=5432;Database=automax;Username=automax;Password=automax`

Baseline commands (all green):
- `dotnet clean Automax.sln`
- `dotnet build Automax.sln`
- `dotnet test Automax.sln`

Feature coverage summary

| Feature / Requirement | Test exists | Status (Pass/Fail/No Test) | Notes |
| --- | --- | --- | --- |
| Admin password update (hash, persist, new pw works) | Yes | Pass | Covered by `Automax.Tests.Controllers.AdminControllerTests.Edit_AllowsAdminToUpdatePassword_WhenNewPasswordProvided` and `PostgresIntegrationTests.AdminPasswordUpdate_PersistsNewHash`. |
| Add Vehicle → Garage list shows Year/Make/Model/Plate | Yes | Pass | Covered by `PostgresIntegrationTests.VehicleDashboard_ShowsNewVehicleDetails` and `Controllers.VehicleControllerIntegrationTests.Create_Then_Index_ShowsVehicleDetails`. |
| Reminder email digest toggle persistence & logic | Yes | Pass | Covered by `ReminderEmailConfigTests.ReminderEmailToggle_PersistsAndEnablesDigests`; background service already respects config. |
| Dashboard data (upcoming reminders, recent services, fuel summary) | Yes | Pass | Covered by `HomeDashboardIntegrationTests.Dashboard_ShowsRemindersServicesFuel`. |
| Settings persistence (user configs, units/theme/digest flags) | Partial | Pass | User config round-trip in `PostgresIntegrationTests.UserConfig_RoundTrip`; server settings via `ConfigHelper` implicitly exercised. No dedicated UI test for theme/units labels. |
| Search/filter (garage/dashboard) | Yes | Pass | Covered by new unit test `Logic.VehicleLogicSearchTests.GetVehicleDashboardAsync_FiltersBySearchTerm`. |
| Postgres provider CRUD stability (users/vehicles/gas/service/reminder/notes/plans/documents/extra) | Yes | Pass | Covered by multiple `PostgresIntegrationTests.*Crud_RoundTrip` cases. |
| Reports/queries (upcoming reminders, service cost, fuel efficiency) | Implicit | Pass | Exercised via seed + dashboard/report logic; no dedicated query-script test. |
| Settings: reminder email toggle UI state | Yes | Pass | Toggle persisted via ReminderEmailConfigTests; SettingsController integration not explicitly tested. |

Open gaps (no failures observed):
- No dedicated UI-level test for theme/units presentation.
- Query script outputs not explicitly asserted; covered indirectly through integration tests and seed data.
