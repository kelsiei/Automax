# Automax

Automax (formerly CarCareTracker) is an ASP.NET Core 8 MVC app for multi-vehicle tracking: garage overview, odometer, gas, service history, reminders (date/odometer), plans, notes, documents, reports, backups, settings, and per-user access.

## Key Features
- Multi-vehicle garage with per-user access (root/admin/users).
- Odometer, gas, service tracking with costs and summaries.
- Reminders (date/odometer) with email digest scheduler (requires SMTP config).
- Plans and notes per vehicle.
- Documents per vehicle with allowed extensions and size limits.
- Backup/restore via UI/API (LiteDB data ZIP).
- Theme toggle (light/dark) persisted via local storage.

## Quickstart (LiteDB)
Prerequisites: .NET 8 SDK.

1. Clone this repository.
2. From the repo root, run:
   ```bash
   dotnet run
   ```
3. App runs on the default Kestrel port (e.g., https://localhost:5001 or http://localhost:5000).
4. Root/admin: the first login seeds a root user if none exists (see login controller defaults); change credentials immediately in the UI.
5. Run tests:
   ```bash
   dotnet test Automax.sln
   ```

## Storage Providers
- **LiteDB (default, production-ready for current scope).** No external DB required; data stored under `data/`.
- **Postgres (fully implemented).** Select via `ServerConfig.StorageProvider = "Postgres"` and set `ServerConfig.PostgresConnectionString` (or env var `AUTOMAX_POSTGRES_CONNECTION_STRING`). Schema + helper scripts:
  - App schema: `docs/db/postgres-schema.sql`
  - MySQL-port schema/seeds/queries (compatible with the Postgres provider and integration tests):
    - `docs/db/automax-mysql-port-schema.pg.sql`
    - `docs/db/automax-mysql-port-seed.pg.sql`
    - `docs/db/automax-mysql-port-queries.pg.sql`
  - Migration/notes: `docs/db/POSTGRES_MIGRATION_NOTES.md`, `docs/db/POSTGRES_PROVIDER_IMPLEMENTATION_PLAN.md`

### Postgres setup (local)
1) Ensure Postgres is running and create the database:
   ```sql
   CREATE DATABASE automax;
   ```
2) Apply the schema:
   ```powershell
   psql "$env:AUTOMAX_POSTGRES_CONNECTION_STRING" -f docs/db/automax-mysql-port-schema.pg.sql
   ```
3) Seed sample data (deterministic IDs and relationships):
   ```powershell
   psql "$env:AUTOMAX_POSTGRES_CONNECTION_STRING" -f docs/db/automax-mysql-port-seed.pg.sql
   ```
4) Run the analytical queries:
   ```powershell
   psql "$env:AUTOMAX_POSTGRES_CONNECTION_STRING" -f docs/db/automax-mysql-port-queries.pg.sql
   ```
5) Run tests (includes Postgres integration tests when enabled):
   ```powershell
   $env:AUTOMAX_ENABLE_POSTGRES_INTEGRATION_TESTS='true'
   $env:AUTOMAX_POSTGRES_CONNECTION_STRING='Host=localhost;Port=5432;Database=automax;Username=automax;Password=secret'
   dotnet test Automax.sln
   ```

## Reminder Emails
- Reminder digest scheduler runs as a hosted background service.
- Enable/disable and configure via `ServerConfig` (requires SMTP settings in MailConfig).

## Backups
- UI/API backup/restore creates/restores a ZIP of the LiteDB data folder.

## Documentation
- Implementation plan and backlog: `docs/IMPLEMENTATION_PLAN_AUTOMAX.md`
- Postgres design/migration notes and provider plan: `docs/db/`
- Contributor setup and conventions: `CONTRIBUTING.md`
