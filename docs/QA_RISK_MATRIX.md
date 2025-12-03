# QA Risk Matrix (feature-tested-and-debugged)

Scope: Postgres-only runtime (`automax` DB). No schema/interface changes allowed without approval. Type A items are low-risk and already implemented; no Type B pending.

| Feature / Issue | Requirement Ref | Proposed / Applied Fix | Classification | Status |
| --- | --- | --- | --- | --- |
| Admin password update not persisting | Admin password update behavior | Ensure admin edit path hashes and saves new password; integration test covers login with new hash | Type A | Implemented (tests passing) |
| Garage shows blank/0 after Add Vehicle | Vehicle CRUD / Garage list | Align controller/view model binding and retrieval so Year/Make/Model/Plate render; integration test added | Type A | Implemented (tests passing) |
| Reminder email digest toggle stuck disabled | Reminders + digest toggle | Persist toggle in user config; background logic respects flag; tests cover persistence/logic | Type A | Implemented (tests passing) |
| Dashboard minimal / lacks summaries | Dashboard expectations | Add reminders/services/fuel summaries; Bootstrap cards; controller aggregates data; tests validate | Type A | Implemented (tests passing) |
| Layout not consistently Bootstrap-styled | Bootstrap integration | Ensure shared layout/nav uses Bootstrap CDN and containerized content for consistent UX | Type A | Implemented (tests passing) |
| Settings customizability limited | Settings & customizability | Extend user configs (preferred units, default landing, show fuel widget); controller/view + tests | Type A | Implemented (tests passing) |
| Search/filter missing on dashboard/garage | Search requirement | Add server-side filter over make/model/plate; unit test for filtering | Type A | Implemented (tests passing) |
| Explicit query-script output assertions | Reports/queries | Optional future: add direct tests against `automax-mysql-port-queries.pg.sql` outputs | Type A | Deferred (optional, no current failure) |

No Type B (schema/core) items are pending approval at this time.
