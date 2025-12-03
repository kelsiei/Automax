-- Automax PostgreSQL schema (design draft)
-- This schema is derived from current LiteDB models. IDs are integers to ease migration.

CREATE TABLE users (
    id              SERIAL PRIMARY KEY,
    username        VARCHAR(255) NOT NULL UNIQUE,
    email_address   VARCHAR(255),
    password_hash   TEXT NOT NULL,
    is_admin        BOOLEAN NOT NULL DEFAULT FALSE,
    is_root_user    BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE vehicles (
    id              SERIAL PRIMARY KEY,
    year            INTEGER NOT NULL,
    make            VARCHAR(255),
    model           VARCHAR(255),
    license_plate   VARCHAR(255),
    vin             VARCHAR(255),
    color           VARCHAR(255),
    purchase_date   DATE,
    purchase_price  NUMERIC(18,2),
    notes           TEXT
);
CREATE INDEX idx_vehicles_license_plate ON vehicles(license_plate);

CREATE TABLE user_access (
    id              SERIAL PRIMARY KEY,
    user_id         INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    vehicle_id      INTEGER NOT NULL REFERENCES vehicles(id) ON DELETE CASCADE,
    can_edit        BOOLEAN NOT NULL DEFAULT TRUE
);
CREATE UNIQUE INDEX ux_user_access_user_vehicle ON user_access(user_id, vehicle_id);

CREATE TABLE user_configs (
    id              SERIAL PRIMARY KEY,
    user_id         INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    preferred_units VARCHAR(50),
    dashboard_tabs  TEXT,
    raw_json        JSONB
);

CREATE TABLE odometer_records (
    id              SERIAL PRIMARY KEY,
    vehicle_id      INTEGER NOT NULL REFERENCES vehicles(id) ON DELETE CASCADE,
    date_recorded   DATE NOT NULL,
    odometer        INTEGER NOT NULL,
    notes           TEXT
);
CREATE INDEX idx_odo_vehicle_date ON odometer_records(vehicle_id, date_recorded);

CREATE TABLE gas_records (
    id              SERIAL PRIMARY KEY,
    vehicle_id      INTEGER NOT NULL REFERENCES vehicles(id) ON DELETE CASCADE,
    date            DATE NOT NULL,
    volume          NUMERIC(18,3),
    total_cost      NUMERIC(18,2),
    odometer        INTEGER,
    notes           TEXT
);
CREATE INDEX idx_gas_vehicle_date ON gas_records(vehicle_id, date);

CREATE TABLE service_records (
    id              SERIAL PRIMARY KEY,
    vehicle_id      INTEGER NOT NULL REFERENCES vehicles(id) ON DELETE CASCADE,
    date            DATE NOT NULL,
    cost            NUMERIC(18,2),
    odometer        INTEGER,
    description     TEXT,
    notes           TEXT
);
CREATE INDEX idx_service_vehicle_date ON service_records(vehicle_id, date);

CREATE TABLE reminder_records (
    id              SERIAL PRIMARY KEY,
    vehicle_id      INTEGER NOT NULL REFERENCES vehicles(id) ON DELETE CASCADE,
    metric          VARCHAR(50) NOT NULL, -- Date/Odometer enums
    description     TEXT,
    due_date        DATE,
    due_odometer    INTEGER,
    urgency         VARCHAR(50),
    is_completed    BOOLEAN NOT NULL DEFAULT FALSE,
    tags            TEXT
);
CREATE INDEX idx_reminder_vehicle_due ON reminder_records(vehicle_id, due_date);

CREATE TABLE plan_records (
    id              SERIAL PRIMARY KEY,
    vehicle_id      INTEGER NOT NULL REFERENCES vehicles(id) ON DELETE CASCADE,
    progress        VARCHAR(50), -- PlanProgress enum
    priority        VARCHAR(50), -- PlanPriority enum
    title           VARCHAR(255),
    description     TEXT,
    is_archived     BOOLEAN NOT NULL DEFAULT FALSE,
    target_date     DATE
);
CREATE INDEX idx_plan_vehicle ON plan_records(vehicle_id);

CREATE TABLE notes (
    id              SERIAL PRIMARY KEY,
    vehicle_id      INTEGER NOT NULL REFERENCES vehicles(id) ON DELETE CASCADE,
    created_at      TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    content         TEXT
);
CREATE INDEX idx_notes_vehicle ON notes(vehicle_id);

CREATE TABLE documents (
    id              SERIAL PRIMARY KEY,
    vehicle_id      INTEGER NOT NULL REFERENCES vehicles(id) ON DELETE CASCADE,
    file_name       VARCHAR(255) NOT NULL,
    file_path       TEXT NOT NULL,
    uploaded_at     TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
CREATE INDEX idx_docs_vehicle ON documents(vehicle_id);

CREATE TABLE server_config (
    id              SERIAL PRIMARY KEY,
    raw_json        JSONB NOT NULL
);

-- Extra fields attached to records (polymorphic via record_id)
CREATE TABLE record_extra_fields (
    id          SERIAL PRIMARY KEY,
    record_id   INTEGER NOT NULL,
    name        TEXT NOT NULL,
    value       TEXT,
    type        TEXT NOT NULL
);
CREATE INDEX idx_extra_fields_record ON record_extra_fields(record_id);
