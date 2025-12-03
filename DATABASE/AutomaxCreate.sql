-- =============================
-- AUTOMAX DATABASE SCHEMA (CREATE TABLES)
-- =============================
CREATE DATABASE IF NOT EXISTS automax;
USE automax;

CREATE TABLE Users (
    user_id INT AUTO_INCREMENT PRIMARY KEY,
    first_name VARCHAR(50) NOT NULL,
    last_name VARCHAR(50) NOT NULL,
    username VARCHAR(50) NOT NULL UNIQUE,
    password VARCHAR(255) NOT NULL,
    role VARCHAR(20) DEFAULT 'user',
    email VARCHAR(100) NOT NULL UNIQUE,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Vehicles (
    vehicle_id INT AUTO_INCREMENT PRIMARY KEY,
    owner_id INT NOT NULL,
    model VARCHAR(50) NOT NULL,
    year INT CHECK (year BETWEEN 1980 AND 2100),
    VIN VARCHAR(20) UNIQUE NOT NULL,
    license_plate VARCHAR(15) UNIQUE NOT NULL,
    odometer INT NOT NULL CHECK (odometer >= 0),
    purchase_date DATE,
    FOREIGN KEY (owner_id) REFERENCES Users(user_id)
);

CREATE TABLE Supplies (
    supply_id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    item_name VARCHAR(100) NOT NULL,
    category VARCHAR(50) NOT NULL,
    quantity INT CHECK (quantity > 0),
    purchase_date DATE NOT NULL,
    cost DECIMAL(10,2) CHECK (cost >= 0),
    supplier VARCHAR(100),
    FOREIGN KEY (user_id) REFERENCES Users(user_id)
);

CREATE TABLE Maintenance_Records (
    record_id INT AUTO_INCREMENT PRIMARY KEY,
    vehicle_id INT NOT NULL,
    user_id INT NOT NULL,
    service_type VARCHAR(100) NOT NULL,
    description TEXT,
    service_date DATE NOT NULL,
    odometer_reading INT NOT NULL CHECK (odometer_reading >= 0),
    cost DECIMAL(10,2) CHECK (cost >= 0),
    service_provider VARCHAR(100),
    next_service_date DATE GENERATED ALWAYS AS (DATE_ADD(service_date, INTERVAL 6 MONTH)) STORED,
    FOREIGN KEY (vehicle_id) REFERENCES Vehicles(vehicle_id),
    FOREIGN KEY (user_id) REFERENCES Users(user_id)
);

CREATE TABLE Documents (
    document_id INT AUTO_INCREMENT PRIMARY KEY,
    record_id INT NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    file_path VARCHAR(255) NOT NULL,
    file_type VARCHAR(20) NOT NULL,
    file_size INT,
    upload_date DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (record_id) REFERENCES Maintenance_Records(record_id)
);

CREATE TABLE Expenses (
    expense_id INT AUTO_INCREMENT PRIMARY KEY,
    vehicle_id INT NOT NULL,
    expense_type VARCHAR(50) NOT NULL,
    description VARCHAR(255),
    amount DECIMAL(10,2) CHECK (amount >= 0),
    category VARCHAR(50) NOT NULL,
    expense_date DATE NOT NULL,
    FOREIGN KEY (vehicle_id) REFERENCES Vehicles(vehicle_id)
);

CREATE TABLE Reminders (
    reminder_id INT AUTO_INCREMENT PRIMARY KEY,
    vehicle_id INT NOT NULL,
    due_date DATE NOT NULL,
    due_mileage INT CHECK (due_mileage >= 0),
    description VARCHAR(255),
    status VARCHAR(20) DEFAULT 'Pending',
    reminder_type VARCHAR(50) NOT NULL,
    is_recurring BOOLEAN DEFAULT FALSE,
    FOREIGN KEY (vehicle_id) REFERENCES Vehicles(vehicle_id)
);

CREATE TABLE Fuel_Records (
    fuel_id INT AUTO_INCREMENT PRIMARY KEY,
    vehicle_id INT NOT NULL,
    fill_date DATE NOT NULL,
    odometer_reading INT CHECK (odometer_reading >= 0),
    gallons DECIMAL(8,2) CHECK (gallons > 0),
    cost_per_gallon DECIMAL(6,2) CHECK (cost_per_gallon > 0),
    total_cost DECIMAL(10,2) GENERATED ALWAYS AS (gallons * cost_per_gallon) STORED,
    FOREIGN KEY (vehicle_id) REFERENCES Vehicles(vehicle_id)
);
