# Postgres Migration Notes (Design Draft)

This document outlines a future migration path from LiteDB to PostgreSQL for Automax. No runtime changes are in place yet; the current Postgres provider is a stub.

## Target schema
See `docs/db/postgres-schema.sql` for the proposed tables covering:
- `users`, `user_access`, `user_configs`
- `vehicles`, `odometer_records`, `gas_records`, `service_records`, `reminder_records`, `plan_records`, `notes`, `documents`
- A `server_config` table to store serialized config if needed.

## Migration approach (conceptual)
1. **Create schema** in Postgres using `postgres-schema.sql`.
2. **Export LiteDB data** via existing backup/ZIP (or a purpose-built export to JSON).
3. **Transform and import**:
   - Users → `users`
   - Vehicles → `vehicles`
   - UserAccess → `user_access`
   - UserConfig → `user_configs` (or JSONB column)
   - Odometer/Gas/Service/Reminder/Plan/Note/Document rows → respective tables.
4. **Preserve IDs**: Prefer keeping existing IDs; map LiteDB integer IDs directly into Postgres `SERIAL` columns by explicitly setting IDs during import (set sequences accordingly after import).
5. **Ordering**: migrate in dependency order: users → vehicles → user_access → odometer/gas/service/reminder/plan/note/document → configs.

## Data considerations
- **Time zones**: store timestamps as `TIMESTAMP WITH TIME ZONE`; dates as `DATE`. Ensure UTC for stored times.
- **Enums**: store as `VARCHAR` aligned with existing enum string names (PlanProgress, PlanPriority, ReminderMetric/Urgency) to simplify mapping.
- **Money/decimal**: use `NUMERIC(18,2)` for costs, `NUMERIC(18,3)` for volumes.
- **Documents**: table tracks metadata only; files remain on disk (migration should copy `data/documents/{vehicleId}`).

## Future implementation steps
- Add Npgsql and connection configuration to `ServerConfig` (connection string, provider flag).
- Implement Postgres data access classes for each interface using the schema above.
- Add migrations/seed script runner.
- Integration tests: spin up ephemeral Postgres (e.g., docker) and validate CRUD for a subset (vehicles, odometer, reminders).
- One-time migration tool: read LiteDB backup/export and insert into Postgres, preserving IDs and relationships.

## Migration CLI skeleton
- A console scaffold exists in `Automax.Migration` that:
  - Reads a LiteDB database file and prints collection counts.
  - Validates Postgres connectivity (simple `SELECT 1`).
  - Accepts `--lite` and `--pg` arguments or environment variables `AUTOMAX_MIGRATION_LITEDB_PATH` and `AUTOMAX_POSTGRES_CONNECTION_STRING`.
- Migration modes implemented so far:
  - `--mode migrate-vehicles` (default dry run; add `--apply` to write):
    - Reads vehicles from LiteDB with preserved IDs.
    - Inserts missing vehicles into Postgres with explicit IDs; skips existing IDs.
    - Adjusts the vehicles sequence to `MAX(id)` after apply.
  - `--mode migrate-gas-odometer` (default dry run; add `--apply` to write):
    - Reads gas_records and odometer_records from LiteDB with preserved IDs.
    - Requires vehicles to already exist in Postgres; skips records whose vehicle is missing.
    - Skips records whose IDs already exist in Postgres.
    - Adjusts gas_records and odometer_records sequences to `MAX(id)` after apply.
  - `--mode migrate-service-reminders` (default dry run; add `--apply` to write):
    - Reads service_records and reminder_records from LiteDB with preserved IDs.
    - Requires vehicles to already exist in Postgres; skips records whose vehicle is missing.
    - Skips records whose IDs already exist in Postgres.
    - Adjusts service_records and reminder_records sequences to `MAX(id)` after apply.
  - `--mode migrate-plans-notes` (default dry run; add `--apply` to write):
    - Reads plan_records and notes from LiteDB with preserved IDs.
    - Requires vehicles to already exist in Postgres; skips records whose vehicle is missing.
    - Skips records whose IDs already exist in Postgres.
    - Adjusts plan_records and notes sequences to `MAX(id)` after apply.
  - `--mode migrate-users-access-configs` (default dry run; add `--apply` to write):
    - Reads users, user_access, and user_configs from LiteDB with preserved IDs.
    - Requires vehicles to already exist in Postgres for access records; skips entries whose user or vehicle is missing.
    - Skips records whose IDs already exist in Postgres; upserts user_configs using the same JSON format as runtime.
    - Adjusts users and user_access sequences to `MAX(id)` after apply.
  - `--mode migrate-documents` (default dry run; add `--apply` to write):
    - Reads document metadata from LiteDB with preserved IDs (files remain on disk; metadata only).
    - Requires vehicles to already exist; skips records whose vehicle is missing.
    - Skips records whose IDs already exist in Postgres.
    - Adjusts documents sequence to `MAX(id)` after apply.
  - `--mode migrate-document-files` (default dry run; add `--apply` to copy):
    - Uses LiteDB `documents` metadata to copy files from a source root to a target root.
    - Controlled via CLI flags `--source-root`/`--target-root` or env vars `AUTOMAX_MIGRATION_SOURCE_ROOT`/`AUTOMAX_MIGRATION_TARGET_ROOT`.
    - Dry run logs “would copy” with no filesystem changes; apply copies only when source exists and target does not, skipping missing source or existing target; safe to re-run.
    - Does not modify database rows; metadata migration is handled by `--mode migrate-documents`.
  - `--mode migrate-extra-fields` (default dry run; add `--apply` to write):
    - Reads extra field records from LiteDB with preserved IDs.
    - Skips records with Id=0 or RecordId=0.
    - Skips records whose IDs already exist in Postgres.
    - Adjusts record_extra_fields sequence to `MAX(id)` after apply.
  - `--mode verify-postgres-relations`:
    - Read-only Postgres-only relational integrity check; detects orphan rows for key relationships:
      - gas/odometer/service/plan/reminder/notes/documents → vehicles
      - user_access → users and vehicles
      - user_configs → users
    - Does not validate record_extra_fields parents (polymorphic).
    - Exit code 0 when no orphans; non-zero when orphans are detected.
  - `--mode migrate-all` (default dry run; add `--apply` to write):
    - Runs all migration phases in sequence: vehicles → gas/odometer → service/reminders → plans/notes → users/access/configs → documents (metadata) → extra fields.
    - Stops on first failure; partial progress may exist in apply mode.
    - Convenience wrapper once individual modes are validated.
  - `--mode verify-postgres`:
    - Read-only comparison of LiteDB vs Postgres row counts for vehicles, gas_records, odometer_records, service_records, plan_records, reminder_records, notes, documents, users, user_access, user_configs, and record_extra_fields.
    - Returns exit code 0 when counts match; non-zero when mismatches are found. Safe to run after `migrate-all --apply` to confirm totals.
- Remaining work: document file content copy (if desired), multi-entity transactional guarantees, and any additional Postgres-backed features beyond current scope.

## Postgres integration tests
- Location: `Automax.Tests/PostgresIntegrationTests.cs`.
- Controlled via environment variables:
  - `AUTOMAX_ENABLE_POSTGRES_INTEGRATION_TESTS`: set to `true` to enable; otherwise tests are skipped.
  - `AUTOMAX_POSTGRES_CONNECTION_STRING`: must contain a valid Postgres connection string.
- Example invocation:
  - `AUTOMAX_ENABLE_POSTGRES_INTEGRATION_TESTS=true AUTOMAX_POSTGRES_CONNECTION_STRING="Host=localhost;Port=5432;Database=automax;Username=automax;Password=secret" dotnet test Automax.sln`
- By default (env vars unset), these tests are skipped and standard `dotnet test` runs remain unaffected.
- CI harness: `.github/workflows/postgres-integration.yml` spins up Postgres, applies `docs/db/postgres-schema.sql`, and runs `dotnet test` with integration tests enabled via the env vars above. Migrations themselves are not invoked in CI yet; they can be layered in later if desired.
