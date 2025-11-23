## Automax

# CarCareTracker (.NET 8)

CarCareTracker is a web application for tracking vehicle maintenance and ownership costs. It lets you keep all service history, fuel logs, odometer readings, reminders, notes, plans, and documents in one place so you always know what’s going on with each vehicle.

## Features

* Manage multiple vehicles in a single account
* Track:

  * Odometer readings
  * Fuel (gas) fill-ups
  * Service records and repair history
  * Reminders for upcoming work
  * Free-form notes per vehicle
  * Maintenance “plans” and to-dos
  * Uploaded documents (invoices, PDFs, photos)
* Dashboard with high-level summaries and quick status
* Report views for cost breakdowns and mileage statistics
* Configurable settings (display options, thresholds, default message, etc.)
* Embedded local database by default, with optional external database support

---

## Tech Stack

* **Runtime:** .NET 8 (ASP.NET Core MVC)
* **UI:** Razor views + Bootstrap-style layout + custom CSS/JS
* **Data access:**

  * LiteDB (embedded file database) for local usage (default)
  * Postgres providers available under `External/Implementations/Postgres` (optional)
* **Tests:** xUnit + Moq (under `CarCareTracker.Tests`)

---

## Getting Started

### Prerequisites

* .NET 8 SDK installed
* Windows / macOS / Linux (developed and tested on Windows)

Optional (for advanced setups):

* PostgreSQL server if you want to use Postgres instead of the embedded database.

---

### 1. Clone and open the project

```bash
git clone <your-repo-url> CarCareTracker
cd CarCareTracker
```

You should see at least:

* `CarCareTracker.csproj`
* `CarCareTracker.sln`
* `Controllers/`, `Models/`, `Views/`, `wwwroot/`
* `External/`, `Logic/`, `Helper/`, `Middleware/`
* `CarCareTracker.Tests/`
* `Config/infra/` and a `data/` folder (created on first run if missing)

---

### 2. Restore and build the main web app

From the repository root:

```bash
dotnet restore
dotnet build CarCareTracker.csproj
```

> Note: `dotnet build` on the **solution** may also build the test project. If you only want to build the web app, target `CarCareTracker.csproj` explicitly as above.

---

### 3. Run the application

From the repository root:

```bash
dotnet run --project CarCareTracker.csproj
```

You should see console output similar to:

* `Now listening on: http://localhost:5000`
* Messages about ensuring `data`, `data/config`, `data/images`, etc.
* A line that seeds a default user with username `"root"` on first run

Then open your browser and go to:

```text
http://localhost:5000
```

If HTTPS redirection is enabled and fails to detect a port, stay on HTTP.

---

## Data and Configuration

On first run, the app will create and/or populate:

* `data/carrtracker.db` – LiteDB embedded database
* `data/config/serverConfig.json` – server-level configuration
* `data/config/userConfig.json` (and related config files) – user-related configuration
* `data/images/` – uploaded images
* `data/documents/` – uploaded documents
* `data/translations/` – localization resources
* `data/temp/` – temporary files

Most configuration is driven from:

* `appsettings.json`
* `appsettings.Development.json`
* JSON files under `data/config/` (created and updated at runtime)

You can inspect `serverConfig.json` to see the seeded default user details and adjust them if needed.

---

## Basic Usage

1. **Login**

   * Start the app and browse to `http://localhost:5000`.
   * Use the seeded account created on first run (see `data/config/serverConfig.json` for details).
   * After logging in, you’ll land on the dashboard.

2. **Garage**

   * Add vehicles and edit their details.
   * Each vehicle will then appear in the Garage and in various reports.

3. **Maintenance Data**

   For the selected vehicle you can manage:

   * **Odometer** – add mileage readings over time.
   * **Gas** – record fuel purchases (date, litres/gallons, price, etc.).
   * **Service** – log maintenance work and repairs.
   * **Reminders** – set reminders based on time and/or mileage.
   * **Notes** – keep free-form notes about the vehicle.
   * **Plans** – plan future work or upgrades.
   * **Documents** – upload files like invoices, PDFs, and images.

4. **Reports**

   * View cost summaries, fuel efficiency, and mileage statistics across one or more vehicles.
   * Filter and search within the report view.

5. **Settings**

   * Update preferences such as UI text (e.g., default message), thresholds, and other configuration values.
   * Settings changes are saved via the Settings page and applied throughout the app.
   * After saving, you should see a confirmation message and the updated values should be reflected in the UI and behavior.

---


## Project Structure (High Level)

```text
CarCareTracker.sln
CarCareTracker.csproj
CarCareTracker.Tests/
Controllers/
  HomeController.cs
  VehicleController.cs
  ReportController.cs
  ... (Odometer, Gas, Service, Reminders, Notes, Plans, Documents, Login, Settings, etc.)
Logic/
  ... domain and application logic
External/
  Interfaces/
  Implementations/
    Litedb/
    Postgres/
Helper/
  ConfigHelper.cs
  FileHelper.cs
  LiteDBHelper.cs
  ... other helpers
Middleware/
  Authen.cs
  SecurityHeadersMiddleware.cs
Models/
  User/
  Admin/
  API/
  GasRecord/
  ServiceRecord/
  Odometer/
  Report/
  Shared/
  ...
Views/
  Shared/
  Home/
  Vehicle/
  Report/
  Settings/
  ...
wwwroot/
  css/
  js/
  images/
Config/
  infra/ (Dockerfile, docker-compose, appsettings, etc.)
data/        (created at runtime)
```

---

## Docker (optional)

If `Config/infra` includes Docker files, a typical workflow is:

```bash
cd Config/infra
docker-compose up --build
```

This may start the web app and database together depending on how the compose files are configured. Adjust ports and environment variables as required.

---
