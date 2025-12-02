# Contributing to Automax

## Project Overview
Automax is an ASP.NET Core 8 MVC app for multi-vehicle tracking. LiteDB is the default and only production-ready storage provider; Postgres is currently a documented stub (design only).

## Prerequisites
- .NET 8 SDK
- Optional (future work only): Docker/Postgres for implementing the Postgres provider
- Recommended editors: VS Code or Visual Studio

## Getting Started (Development)
1. Clone the repository.
2. Restore/build/test:
   ```bash
   dotnet build Automax.sln
   dotnet test Automax.sln
   ```
3. Run the app:
   ```bash
   dotnet run
   ```
   Default Kestrel ports are the usual ASP.NET Core defaults (e.g., https://localhost:5001 / http://localhost:5000). LiteDB data lives under the `data/` folder.

## Coding & Testing Conventions
- ASP.NET Core 8 MVC; tests use xUnit + Moq (project `Automax.Tests`).
- Nullable reference types are enabled in many files; keep nullability clean and warnings at zero.
- Add/adjust tests when changing logic or data access.

## Storage Providers (For Contributors)
- LiteDB: default and fully implemented; no external DB required.
- Postgres: selectable via `ServerConfig.StorageProvider` but currently throws `NotSupportedException`. Schema/migration/implementation plans are in `docs/db/`. Runtime Postgres support is future work and must follow those docs.

## Reminder Emails & Backups
- Reminder email digests run via a hosted background service; requires SMTP config in `MailConfig`/`ServerConfig`.
- Backups are LiteDB ZIPs via UI/API.

## Where to Read More
- `README.md`
- `docs/IMPLEMENTATION_PLAN_AUTOMAX.md`
- `docs/db/POSTGRES_MIGRATION_NOTES.md`
- `docs/db/POSTGRES_PROVIDER_IMPLEMENTATION_PLAN.md`
