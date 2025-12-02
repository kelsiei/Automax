# Postgres Provider Implementation Plan

This document maps each data-access interface to the Postgres schema defined in `docs/db/postgres-schema.sql` and outlines the expected CRUD and filtering behavior. Many providers are now runtime-implemented for Postgres (vehicles, gas, odometer, service, plans, reminders, notes, users, user access, user configs); remaining items are noted for future work.

## General Guidance
- Preserve current LiteDB semantics: `Id == 0` → INSERT (return new id); non-zero Id → UPDATE.
- Use parameterized SQL. Return `null` when a record is not found.
- Apply user/vehicle scoping consistent with LiteDB: non-root users see only allowed vehicles; root/admin sees all.
- Enums are stored as text (string names) in the schema.
- Dates use `DATE`; timestamps use `TIMESTAMP WITH TIME ZONE`; costs use `NUMERIC`.
- After bulk migration, ensure sequences are advanced to the max existing id.

## Interface Mapping

### IVehicleDataAccess
- Tables: `vehicles`, `user_access`.
- `GetVehicleAsync(id)`: `SELECT * FROM vehicles WHERE id=@id` (runtime implemented).
- `GetVehiclesAsync(userId, isRootUser, allowedVehicleIds)`: root → all vehicles; non-root → `WHERE id = ANY(@allowed)` using provided allowed ids (runtime implemented).
- `SaveVehicleAsync(vehicle)`: if id=0 insert into `vehicles` (RETURNING id); else update row by id (runtime implemented).
- `DeleteVehicleAsync(id)`: delete vehicle (cascades handled by FK) (runtime implemented).

### IGasRecordDataAccess
- Table: `gas_records`.
- `GetGasRecordAsync(id)`: select by id. (runtime implemented)
- `GetGasRecordsForVehicleAsync(vehicleId, filter)`: `WHERE vehicle_id=@vid` plus optional date range from `MethodParameter.StartDate/EndDate`; order by `date`. (runtime implemented)
- `SaveGasRecordAsync(record)`: insert/update with `date`, `volume`, `total_cost`, `odometer`, `notes` (Id==0 insert RETURNING id). (runtime implemented)
- `DeleteGasRecordAsync(id)`: delete by id. (runtime implemented)

### IOdometerRecordDataAccess
- Table: `odometer_records`.
- `GetOdometerRecordAsync(id)`: select by id. (runtime implemented)
- `GetOdometerRecordsForVehicleAsync(vehicleId, filter)`: `WHERE vehicle_id=@vid` plus optional date range; order by `date_recorded`. (runtime implemented)
- `SaveOdometerRecordAsync(record)`: insert/update with `date_recorded`, `odometer`, `notes` (Id==0 insert RETURNING id). (runtime implemented)
- `DeleteOdometerRecordAsync(id)`: delete by id. (runtime implemented)

### IServiceRecordDataAccess
- Table: `service_records`.
- `GetServiceRecordAsync(id)`: select by id. (runtime implemented)
- `GetServiceRecordsForVehicleAsync(vehicleId, filter)`: `WHERE vehicle_id=@vid` plus optional date range; order by `date`. (runtime implemented)
- `SaveServiceRecordAsync(record)`: insert/update with `date`, `cost`, `odometer`, `description`, `notes`. (runtime implemented)
- `DeleteServiceRecordAsync(id)`: delete by id. (runtime implemented)

### IReminderRecordDataAccess
- Table: `reminder_records`.
- `GetReminderRecordAsync(id)`: select by id. (runtime implemented)
- `GetReminderRecordsForVehicleAsync(vehicleId, filter)`: `WHERE vehicle_id=@vid` plus optional date range; order by `due_date`. (runtime implemented)
- `SaveReminderRecordAsync(record)`: insert/update with `metric` (ReminderMetric as text), `description`, `due_date`, `due_odometer`, `urgency` (ReminderUrgency as text), `is_completed`, `tags`. (runtime implemented)
- `DeleteReminderRecordAsync(id)`: delete by id. (runtime implemented)

### IPlanRecordDataAccess
- Table: `plan_records`.
- `GetPlanRecordAsync(id)`: select by id. (runtime implemented)
- `GetPlanRecordsForVehicleAsync(vehicleId, filter)`: `WHERE vehicle_id=@vid` plus optional date range; order by `target_date NULLS LAST`. (runtime implemented)
- `SavePlanRecordAsync(record)`: insert/update with `progress` (PlanProgress as text), `priority` (PlanPriority as text), `title`, `description`, `is_archived`, `target_date`. (runtime implemented)
- `DeletePlanRecordAsync(id)`: delete by id. (runtime implemented)

### INoteDataAccess
- Table: `notes`.
- `GetNoteAsync(id)`: select by id. (runtime implemented)
- `GetNotesForVehicleAsync(vehicleId, filter)`: `WHERE vehicle_id=@vid` with optional date range on `created_at`; order by `created_at DESC`. (runtime implemented)
- `SaveNoteAsync(note)`: insert/update with `vehicle_id`, serialized title/body stored in `content`, `created_at`. (runtime implemented)
- `DeleteNoteAsync(id)`: delete by id. (runtime implemented)

### IDocumentDataAccess
- Table: `documents`.
- `GetDocumentsForVehicleAsync(vehicleId)`: select by vehicle; order by `uploaded_at`. (runtime implemented)
- `SaveDocumentMetadataAsync(...)`: insert metadata row with `file_name`, `file_path`, `uploaded_at`; return new id. (runtime implemented)
- `DeleteDocumentMetadataAsync(id)`: delete by id. File contents remain on disk under `data/documents/{vehicleId}`. (runtime implemented; metadata only)

### IUserRecordDataAccess
- Table: `users`.
- `GetUserByIdAsync / GetUserByUserNameAsync`: select by id or username. (runtime implemented)
- `GetAllUsersAsync`: select all ordered by id. (runtime implemented)
- `SaveUserAsync(user)`: insert/update with `username`, `email_address`, `password_hash`, `is_admin`, `is_root_user` (Id==0 insert RETURNING id). (runtime implemented)
- `AnyUsersAsync`: `SELECT EXISTS(SELECT 1 FROM users)`. (runtime implemented)
- `DeleteUserAsync(id)`: delete by id. (runtime implemented)

### IUserConfigDataAccess
- Table: `user_configs`.
- `GetUserConfigAsync(userId)`: select by user_id; deserialize `raw_json` to `UserConfig`. (runtime implemented)
- `SaveUserConfigAsync(userId, config)`: upsert row with serialized `UserConfig` JSON. (runtime implemented)

### IUserAccessDataAccess
- Table: `user_access`.
- Methods add/remove/list vehicle access for users; ensure unique (user_id, vehicle_id) pairs and persist `can_edit`. (runtime implemented)

### IExtraFieldDataAccess
- Table: `record_extra_fields`.
- `GetExtraFieldsForRecordAsync(recordId)`: select by record_id ordered by id. (runtime implemented)
- `SaveExtraFieldAsync(extraField)`: insert/update with explicit type/name/value and record_id; Id==0 insert RETURNING id. (runtime implemented)
- `DeleteExtraFieldAsync(id)`: delete by id. (runtime implemented)

## ID & Migration Notes
- Preserve existing LiteDB integer IDs during import; insert with explicit ids, then `setval` sequences to `max(id)`.
- Maintain FK integrity (vehicles before child records; users before user_access/config).
- Store enums as string names to avoid additional enum type creation in Postgres.

## References
- Schema: `docs/db/postgres-schema.sql`
- Migration notes: `docs/db/POSTGRES_MIGRATION_NOTES.md`
- Integration testing: a scaffolded suite lives in `Automax.Tests/PostgresIntegrationTests.cs` and is skipped by default until a reliable Postgres test environment is available.

## Alignment audit (Postgres provider vs MySQL-port schema)
- Checked interfaces/implementations: users, vehicles, gas_records, service_records, reminder_records, plan_records, odometer_records, notes, documents, user_access, user_configs, record_extra_fields.
- Table/column matches:
  - `users`: provider uses `username`, `email_address`, `password_hash`, `is_admin`, `is_root_user`; schema keeps these and allows nullable first/last name/role for compatibility.
  - `vehicles`: provider uses `year`, `make`, `model`, `license_plate`, `vin`; schema keeps optional extras (color, purchase info) and allows null VIN/plate to avoid insert errors.
  - `gas_records`: provider uses `date`, `volume`, `total_cost`, `odometer`, `notes`; schema keeps cost_per_unit/is_full_fill_up nullable for future use.
  - `service_records`: provider uses `date`, `cost`, `odometer`, `description`, `notes`; schema adds service_type/service_provider/next_service_date but keeps all nullable; added `notes` column to align.
  - `reminder_records`: provider uses `metric`, `description`, `due_date`, `due_odometer`, `urgency`, `is_completed`, `tags`; schema keeps legacy fields (status/reminder_type/is_recurring) nullable.
  - `plan_records`, `odometer_records`, `notes`, `documents`, `user_access`, `user_configs` (raw_json now TEXT to match code), `record_extra_fields` all align with provider SQL mappings.
- Resolutions applied:
  - Added nullable `notes` to `service_records` and made `raw_json` TEXT in schema.
  - Left provider code unchanged where schema already offered superset/nullable columns.
