-- Seed data for Automax MySQL schema port (Postgres compatible)
-- Assumes schema from docs/db/automax-mysql-port-schema.pg.sql is applied.
-- Truncates tables for repeatable local runs with deterministic IDs.

TRUNCATE TABLE
    documents,
    expenses,
    reminder_records,
    gas_records,
    service_records,
    supplies,
    plan_records,
    notes,
    odometer_records,
    user_access,
    user_configs,
    record_extra_fields,
    vehicles,
    users
RESTART IDENTITY CASCADE;

-- Users
-- 1: William Xu (admin), 2: Khushi Malik (user), 3: Garry Aujla (user), 4: Alex Root (root/admin)
INSERT INTO users (first_name, last_name, username, password_hash, email_address, role, is_admin, is_root_user)
VALUES
('William', 'Xu', 'williamx', 'hashedpass123', 'william@example.com', 'admin', TRUE, FALSE),
('Khushi', 'Malik', 'khushim', 'hashedpass234', 'khushi@example.com', 'user', FALSE, FALSE),
('Garry', 'Aujla', 'garrya', 'hashedpass345', 'garry@example.com', 'user', FALSE, FALSE),
('Alex', 'Root', 'alexr', 'hashedpass456', 'alex.root@example.com', 'admin', TRUE, TRUE);

-- Vehicles
-- 1: William's daily driver, 2: Khushi's commuter, 3: Garry's EV, 4: William's truck, 5: Alex's wagon
INSERT INTO vehicles (owner_id, make, model, year, vin, license_plate, odometer, purchase_date)
VALUES
(1, 'Toyota', 'Camry', 2021, 'JT1234567890ABCD1', 'BC1234', 32000, '2021-03-15'),
(2, 'Honda', 'Civic', 2020, 'HG1234567890XYZ9', 'AB5678', 45000, '2020-08-22'),
(3, 'Tesla', 'Model 3', 2023, 'TSL9876543210QWER', 'EV9000', 15000, '2023-05-10'),
(1, 'Ford', 'F-150', 2018, 'FD1234567890TRUCK', 'TRK8100', 82000, '2019-02-11'),
(4, 'Subaru', 'Outback', 2019, 'SUB9999999999WAGN', 'SW5555', 61000, '2019-06-30');

-- Supplies (procurement examples)
INSERT INTO supplies (user_id, item_name, category, quantity, purchase_date, cost, supplier)
VALUES
(1, 'Engine Oil', 'Oil', 2, '2024-10-01', 45.50, 'Canadian Tire'),
(2, 'Brake Pads', 'Part', 1, '2024-09-18', 120.00, 'AutoZone'),
(4, 'Air Filter', 'Part', 2, '2024-07-15', 34.99, 'NAPA');

-- Service history across vehicles (varied dates/costs)
INSERT INTO service_records (vehicle_id, user_id, service_type, description, date, odometer, cost, service_provider, notes)
VALUES
(1, 1, 'Oil Change', 'Changed oil and filter', '2024-09-20', 30000, 80.00, 'Quick Lube', '5W-30 synthetic'),
(1, 1, 'Tire Rotation', 'Rotated all four tires', '2024-03-20', 25000, 60.00, 'Mr. Tire', NULL),
(1, 1, 'Front Brakes', 'Pads + rotors', '2023-11-05', 21000, 320.00, 'Brake Masters', NULL),
(2, 2, 'Brake Replacement', 'Replaced brake pads', '2024-08-01', 43000, 220.00, 'Auto Experts', NULL),
(2, 2, 'Transmission Service', 'Fluid + filter', '2023-12-12', 38000, 410.00, 'Trans World', 'Preventive'),
(3, 3, 'HV Battery Check', 'Routine inspection', '2024-04-18', 12000, 0.00, 'Tesla Service', NULL),
(4, 1, 'Spark Plugs', 'Replaced spark plugs', '2024-02-10', 78000, 180.00, 'Neighborhood Garage', NULL),
(5, 4, 'Timing Belt', 'Timing belt + water pump', '2024-05-22', 59000, 950.00, 'Subaru Dealer', 'Major service');

-- Documents tied to service records (record_id) and vehicles
INSERT INTO documents (record_id, vehicle_id, file_name, file_path, file_type, file_size)
VALUES
(1, 1, 'oil_receipt.pdf', '/uploads/receipts/oil_receipt.pdf', 'PDF', 150),
(2, 1, 'tire_invoice.jpg', '/uploads/receipts/tire_invoice.jpg', 'JPG', 210),
(4, 1, 'brake_receipt.pdf', '/uploads/receipts/brake_receipt.pdf', 'PDF', 185),
(8, 5, 'timing_belt.pdf', '/uploads/receipts/timing_belt.pdf', 'PDF', 420);

-- Expenses (insurance/registration/other)
INSERT INTO expenses (vehicle_id, expense_type, description, amount, category, expense_date)
VALUES
(1, 'Insurance', 'Annual car insurance payment', 1200.00, 'Insurance', '2024-01-10'),
(1, 'Registration', 'License renewal', 150.00, 'Registration', '2024-05-15'),
(2, 'Detail', 'Interior + exterior detail', 180.00, 'Cosmetic', '2024-06-12'),
(4, 'Toll Pass', 'Prepaid toll refill', 60.00, 'Tolls', '2024-07-01');

-- Reminders with mix of pending, near-term, and completed
INSERT INTO reminder_records (vehicle_id, due_date, due_odometer, description, reminder_type, status, is_recurring, metric, urgency, is_completed, tags)
VALUES
(1, '2025-03-20', 36000, 'Next oil change due', 'Oil Change', 'Pending', FALSE, 'Date', 'NotUrgent', FALSE, 'maintenance'),
(2, '2025-01-15', 45500, 'Rotate tires', 'Tire Rotation', 'Pending', FALSE, 'Date', 'Urgent', FALSE, 'tires'),
(3, '2024-11-05', 16000, 'Cabin filter', 'Filter', 'Completed', FALSE, 'Date', 'NotUrgent', TRUE, 'filters'),
(4, '2024-12-01', 84000, 'Coolant flush', 'Coolant', 'Pending', FALSE, 'Date', 'Urgent', FALSE, 'cooling'),
(5, '2024-09-20', 63000, 'Brake fluid flush', 'Brake', 'Pending', FALSE, 'Date', 'VeryUrgent', FALSE, 'brakes');

-- Fuel/gas records (varied odometer/volume for efficiency differences)
INSERT INTO gas_records (vehicle_id, date, odometer, volume, cost_per_unit, total_cost)
VALUES
(1, '2024-09-05', 31000, 12.5, 5.30, 12.5 * 5.30),
(1, '2024-10-12', 33200, 13.0, 5.20, 13.0 * 5.20),
(2, '2024-09-10', 44000, 10.8, 5.10, 10.8 * 5.10),
(2, '2024-11-15', 46250, 11.2, 5.25, 11.2 * 5.25),
(3, '2024-09-15', 14800, 9.2, 4.85, 9.2 * 4.85),
(4, '2024-08-20', 80500, 16.5, 4.95, 16.5 * 4.95),
(5, '2024-10-01', 60050, 14.1, 5.05, 14.1 * 5.05);

-- Odometer snapshots (for dashboards/reports)
INSERT INTO odometer_records (vehicle_id, date_recorded, odometer, notes)
VALUES
(1, '2024-09-01', 30800, 'Before oil change'),
(2, '2024-08-15', 43500, NULL),
(5, '2024-09-25', 60500, 'Pre-road-trip check');

-- Notes (per vehicle)
INSERT INTO notes (vehicle_id, content)
VALUES
(1, 'Needs windshield chip repair before winter.'),
(2, 'Check ABS light intermittently on damp days.'),
(5, 'Great for road trips; consider roof box.');

-- Plans (future work)
INSERT INTO plan_records (vehicle_id, progress, priority, title, description, is_archived, target_date)
VALUES
(1, 'NotStarted', 'Medium', 'Detail + wax', 'Full detail before holidays', FALSE, '2024-12-05'),
(2, 'NotStarted', 'High', 'Replace tires', 'All-season set before snow', FALSE, '2024-12-01'),
(5, 'InProgress', 'Medium', 'Roof rack', 'Install crossbars and box', FALSE, '2025-01-15');

-- User access (shared vehicles)
INSERT INTO user_access (user_id, vehicle_id, can_edit)
VALUES
(2, 1, TRUE),
(3, 2, TRUE),
(4, 1, TRUE),
(4, 5, TRUE);

-- Extra fields (polymorphic; here tied to plan/vehicle ids)
INSERT INTO record_extra_fields (record_id, name, value, type)
VALUES
(1, 'Budget', '150', 'Number'),
(2, 'PriorityReason', 'Tread depth 3/32', 'Text'),
(5, 'Season', 'Winter prep', 'Text');
