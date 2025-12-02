-- Analytical queries ported from DATABASE/AutomaxQueries.sql (Postgres compatible)

-- Report 1: Upcoming maintenance reminders per vehicle (pending only).
SELECT 
    v.make AS vehicle_make,
    v.model AS vehicle_model,
    v.license_plate AS license,
    r.reminder_type AS service_type,
    r.due_date AS due_date,
    r.due_odometer AS due_mileage,
    r.status AS status
FROM reminder_records r
JOIN vehicles v ON r.vehicle_id = v.id
WHERE r.status = 'Pending'
ORDER BY r.due_date ASC;

-- Report 2: Service cost summary per vehicle.
SELECT 
    v.make AS vehicle_make,
    v.model AS vehicle_model,
    COUNT(m.id) AS total_services,
    SUM(m.cost) AS total_cost,
    ROUND(AVG(m.cost), 2) AS average_cost
FROM service_records m
JOIN vehicles v ON m.vehicle_id = v.id
GROUP BY v.id, v.make, v.model
ORDER BY total_cost DESC;

-- Report 3: Fuel efficiency overview per vehicle.
SELECT
    v.make AS vehicle_make,
    v.model AS vehicle_model,
    ROUND(AVG(f.odometer / NULLIF(f.volume, 0)), 2) AS avg_efficiency,
    SUM(f.total_cost) AS total_fuel_cost
FROM gas_records f
JOIN vehicles v ON f.vehicle_id = v.id
GROUP BY v.id, v.make, v.model;
