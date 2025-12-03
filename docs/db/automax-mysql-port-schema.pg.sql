-- Automax MySQL schema ported to PostgreSQL
-- This script mirrors DATABASE/AutomaxCreate.sql but aligns table/column names to the current Postgres provider expectations.
-- For local/dev convenience it drops and recreates the tables (destructive).

DROP TABLE IF EXISTS documents CASCADE;
DROP TABLE IF EXISTS expenses CASCADE;
DROP TABLE IF EXISTS reminder_records CASCADE;
DROP TABLE IF EXISTS reminders CASCADE;
DROP TABLE IF EXISTS gas_records CASCADE;
DROP TABLE IF EXISTS fuel_records CASCADE;
DROP TABLE IF EXISTS service_records CASCADE;
DROP TABLE IF EXISTS maintenance_records CASCADE;
DROP TABLE IF EXISTS supplies CASCADE;
DROP TABLE IF EXISTS vehicles CASCADE;
DROP TABLE IF EXISTS users CASCADE;
DROP TABLE IF EXISTS user_access CASCADE;
DROP TABLE IF EXISTS user_configs CASCADE;
DROP TABLE IF EXISTS odometer_records CASCADE;
DROP TABLE IF EXISTS plan_records CASCADE;
DROP TABLE IF EXISTS notes CASCADE;
DROP TABLE IF EXISTS record_extra_fields CASCADE;
DROP TABLE IF EXISTS server_config CASCADE;

CREATE TABLE users (
    id              SERIAL PRIMARY KEY,
    first_name      VARCHAR(50),
    last_name       VARCHAR(50),
    username        VARCHAR(50) NOT NULL UNIQUE,
    password_hash   VARCHAR(255) NOT NULL,
    role            VARCHAR(20) DEFAULT 'user',
    email_address   VARCHAR(100) NOT NULL UNIQUE,
    created_at      TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    is_admin        BOOLEAN NOT NULL DEFAULT FALSE,
    is_root_user    BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE vehicles (
    id              SERIAL PRIMARY KEY,
    owner_id        INTEGER REFERENCES users(id) ON DELETE SET NULL,
    make            VARCHAR(50),
    model           VARCHAR(50) NOT NULL,
    year            INTEGER CHECK (year BETWEEN 1980 AND 2100),
    vin             VARCHAR(20) UNIQUE,
    license_plate   VARCHAR(15) UNIQUE,
    odometer        INTEGER CHECK (odometer >= 0),
    purchase_date   DATE,
    color           VARCHAR(50),
    purchase_price  NUMERIC(18,2),
    notes           TEXT
);
CREATE INDEX idx_vehicles_owner ON vehicles(owner_id);

CREATE TABLE supplies (
    id              SERIAL PRIMARY KEY,
    user_id         INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    item_name       VARCHAR(100) NOT NULL,
    category        VARCHAR(50) NOT NULL,
    quantity        INTEGER CHECK (quantity > 0),
    purchase_date   DATE NOT NULL,
    cost            NUMERIC(10,2) CHECK (cost >= 0),
    supplier        VARCHAR(100)
);
CREATE INDEX idx_supplies_user ON supplies(user_id);

CREATE TABLE service_records (
    id                  SERIAL PRIMARY KEY,
    vehicle_id          INTEGER NOT NULL REFERENCES vehicles(id) ON DELETE CASCADE,
    user_id             INTEGER REFERENCES users(id) ON DELETE SET NULL,
    service_type        VARCHAR(100),
    description         TEXT,
    date                DATE NOT NULL,
    odometer            INTEGER CHECK (odometer >= 0),
    cost                NUMERIC(10,2) CHECK (cost >= 0),
    service_provider    VARCHAR(100),
    notes               TEXT,
    -- MySQL used DATE_ADD(..., INTERVAL 6 MONTH); Postgres generated column equivalent below.
    next_service_date   DATE GENERATED ALWAYS AS (date + INTERVAL '6 months') STORED
);
CREATE INDEX idx_service_vehicle ON service_records(vehicle_id);
CREATE INDEX idx_service_user ON service_records(user_id);

CREATE TABLE documents (
    id              SERIAL PRIMARY KEY,
    vehicle_id      INTEGER NOT NULL REFERENCES vehicles(id) ON DELETE CASCADE,
    record_id       INTEGER REFERENCES service_records(id) ON DELETE CASCADE,
    file_name       VARCHAR(255) NOT NULL,
    file_path       TEXT NOT NULL,
    uploaded_at     TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    file_type       VARCHAR(20),
    file_size       INTEGER
);
CREATE INDEX idx_documents_record ON documents(record_id);
CREATE INDEX idx_documents_vehicle ON documents(vehicle_id);

CREATE TABLE expenses (
    id              SERIAL PRIMARY KEY,
    vehicle_id      INTEGER NOT NULL REFERENCES vehicles(id) ON DELETE CASCADE,
    expense_type    VARCHAR(50) NOT NULL,
    description     VARCHAR(255),
    amount          NUMERIC(10,2) CHECK (amount >= 0),
    category        VARCHAR(50) NOT NULL,
    expense_date    DATE NOT NULL
);
CREATE INDEX idx_expenses_vehicle ON expenses(vehicle_id);

CREATE TABLE reminder_records (
    id              SERIAL PRIMARY KEY,
    vehicle_id      INTEGER NOT NULL REFERENCES vehicles(id) ON DELETE CASCADE,
    metric          VARCHAR(50) DEFAULT 'Date',
    description     VARCHAR(255),
    due_date        DATE,
    due_odometer    INTEGER CHECK (due_odometer >= 0),
    urgency         VARCHAR(50) DEFAULT 'NotUrgent',
    is_completed    BOOLEAN NOT NULL DEFAULT FALSE,
    tags            TEXT,
    -- Legacy MySQL fields kept for completeness.
    status          VARCHAR(20) DEFAULT 'Pending',
    reminder_type   VARCHAR(50),
    is_recurring    BOOLEAN DEFAULT FALSE
);
CREATE INDEX idx_reminders_vehicle ON reminder_records(vehicle_id);
CREATE INDEX idx_reminders_due_date ON reminder_records(due_date);

CREATE TABLE gas_records (
    id                  SERIAL PRIMARY KEY,
    vehicle_id          INTEGER NOT NULL REFERENCES vehicles(id) ON DELETE CASCADE,
    date                DATE NOT NULL,
    volume              NUMERIC(10,3) CHECK (volume > 0),
    total_cost          NUMERIC(10,2),
    odometer            INTEGER CHECK (odometer >= 0),
    notes               TEXT,
    cost_per_unit       NUMERIC(10,3),
    is_full_fill_up     BOOLEAN DEFAULT TRUE
);
CREATE INDEX idx_gas_vehicle ON gas_records(vehicle_id);
CREATE INDEX idx_gas_date ON gas_records(date);

-- Additional tables used by the Postgres provider (from docs/db/postgres-schema.sql).
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
    raw_json        TEXT
);

CREATE TABLE odometer_records (
    id              SERIAL PRIMARY KEY,
    vehicle_id      INTEGER NOT NULL REFERENCES vehicles(id) ON DELETE CASCADE,
    date_recorded   DATE NOT NULL,
    odometer        INTEGER NOT NULL,
    notes           TEXT
);
CREATE INDEX idx_odo_vehicle_date ON odometer_records(vehicle_id, date_recorded);

CREATE TABLE plan_records (
    id              SERIAL PRIMARY KEY,
    vehicle_id      INTEGER NOT NULL REFERENCES vehicles(id) ON DELETE CASCADE,
    progress        VARCHAR(50),
    priority        VARCHAR(50),
    title           VARCHAR(255),
    description     TEXT,
    is_archived     BOOLEAN NOT NULL DEFAULT FALSE,
    target_date     DATE
);
CREATE INDEX idx_plan_vehicle ON plan_records(vehicle_id);

CREATE TABLE notes (
    id              SERIAL PRIMARY KEY,
    vehicle_id      INTEGER NOT NULL REFERENCES vehicles(id) ON DELETE CASCADE,
    created_at      TIMESTAMPTZ DEFAULT NOW(),
    content         TEXT
);
CREATE INDEX idx_notes_vehicle ON notes(vehicle_id);

CREATE TABLE record_extra_fields (
    id          SERIAL PRIMARY KEY,
    record_id   INTEGER NOT NULL,
    name        TEXT NOT NULL,
    value       TEXT,
    type        TEXT NOT NULL
);
CREATE INDEX idx_extra_fields_record ON record_extra_fields(record_id);

CREATE TABLE server_config (
    id              SERIAL PRIMARY KEY,
    raw_json        JSONB NOT NULL
);
