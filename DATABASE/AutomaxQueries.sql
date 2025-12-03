-- =============================
-- AUTOMAX DATABASE ANALYTICAL QUERIES
-- =============================
USE automax;

-- REPORT 1: Upcoming Maintenance
SELECT 
    v.model AS Vehicle,
    v.license_plate AS License,
    r.reminder_type AS ServiceType,
    r.due_date AS DueDate,
    r.due_mileage AS DueMileage,
    r.status AS Status
FROM Reminders r
JOIN Vehicles v ON r.vehicle_id = v.vehicle_id
WHERE r.status = 'Pending'
ORDER BY r.due_date ASC;

-- REPORT 2: Service Cost Summary per Vehicle
SELECT 
    v.model AS Vehicle,
    COUNT(m.record_id) AS TotalServices,
    SUM(m.cost) AS TotalCost,
    ROUND(AVG(m.cost), 2) AS AverageCost
FROM Maintenance_Records m
JOIN Vehicles v ON m.vehicle_id = v.vehicle_id
GROUP BY v.vehicle_id
ORDER BY TotalCost DESC;

-- REPORT 3: Fuel Efficiency Overview
SELECT
    v.model AS Vehicle,
    ROUND(AVG(f.odometer_reading / f.gallons), 2) AS AvgEfficiency,
    SUM(f.total_cost) AS TotalFuelCost
FROM Fuel_Records f
JOIN Vehicles v ON f.vehicle_id = v.vehicle_id
GROUP BY v.vehicle_id;
