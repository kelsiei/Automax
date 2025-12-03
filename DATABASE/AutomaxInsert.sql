-- =============================
-- AUTOMAX DATABASE SAMPLE DATA (INSERTS)
-- =============================
USE automax;

INSERT INTO Users (first_name, last_name, username, password, email, role)
VALUES
('William', 'Xu', 'williamx', 'hashedpass123', 'william@example.com', 'admin'),
('Khushi', 'Malik', 'khushim', 'hashedpass234', 'khushi@example.com', 'user'),
('Garry', 'Aujla', 'garrya', 'hashedpass345', 'garry@example.com', 'user');

INSERT INTO Vehicles (owner_id, model, year, VIN, license_plate, odometer, purchase_date)
VALUES
(1, 'Toyota Camry', 2021, 'JT1234567890ABCD1', 'BC1234', 32000, '2021-03-15'),
(2, 'Honda Civic', 2020, 'HG1234567890XYZ9', 'AB5678', 45000, '2020-08-22'),
(3, 'Tesla Model 3', 2023, 'TSL9876543210QWER', 'EV9000', 15000, '2023-05-10');

INSERT INTO Supplies (user_id, item_name, category, quantity, purchase_date, cost, supplier)
VALUES
(1, 'Engine Oil', 'Oil', 2, '2024-10-01', 45.50, 'Canadian Tire'),
(2, 'Brake Pads', 'Part', 1, '2024-09-18', 120.00, 'AutoZone');

INSERT INTO Maintenance_Records (vehicle_id, user_id, service_type, description, service_date, odometer_reading, cost, service_provider)
VALUES
(1, 1, 'Oil Change', 'Changed oil and filter', '2024-09-20', 30000, 80.00, 'Quick Lube'),
(1, 1, 'Tire Rotation', 'Rotated all four tires', '2024-03-20', 25000, 60.00, 'Mr. Tire'),
(2, 2, 'Brake Replacement', 'Replaced brake pads', '2024-08-01', 43000, 220.00, 'Auto Experts');

INSERT INTO Documents (record_id, file_name, file_path, file_type, file_size)
VALUES
(1, 'oil_receipt.pdf', '/uploads/receipts/oil_receipt.pdf', 'PDF', 150),
(2, 'tire_invoice.jpg', '/uploads/receipts/tire_invoice.jpg', 'JPG', 210);

INSERT INTO Expenses (vehicle_id, expense_type, description, amount, category, expense_date)
VALUES
(1, 'Insurance', 'Annual car insurance payment', 1200.00, 'Insurance', '2024-01-10'),
(1, 'Registration', 'License renewal', 150.00, 'Registration', '2024-05-15');

INSERT INTO Reminders (vehicle_id, due_date, due_mileage, description, reminder_type)
VALUES
(1, '2025-03-20', 36000, 'Next oil change due', 'Oil Change'),
(2, '2025-02-01', 47000, 'Brake inspection due', 'Inspection');

INSERT INTO Fuel_Records (vehicle_id, fill_date, odometer_reading, gallons, cost_per_gallon)
VALUES
(1, '2024-09-05', 31000, 12.5, 5.30),
(2, '2024-09-10', 44000, 10.8, 5.10),
(3, '2024-09-15', 14800, 9.2, 4.85);
