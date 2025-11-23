# CarCareTracker Manual Test Plan

## 1. Overview
- **What it is:** CarCareTracker is an ASP.NET Core 8 MVC app for tracking vehicles, odometer/gas/service history, reminders (date/odometer), notes, plans, documents, reports, backups, and user management with role-based access.
- **Scope:** Manual end-to-end verification of UI flows, CRUD, filters/pagination, reminder calendar/ICS, documents, reports, settings, admin, backup, reminder emails, security headers, and theme toggle. No performance or load testing.
- **Out of scope:** External SMTP deliverability (beyond basic send attempt), production hardening, third-party CDNs, mobile native apps.
- **Assumptions:** Running locally with LiteDB and default data folder; SMTP may not be configured (expect graceful handling); default root user seeds on first login when no users exist.

## 2. Test Environment & Setup
- [ ] Install .NET SDK 8.x on Windows/Linux/macOS.
- [ ] Clone/place the repo locally.
- [ ] From project root, run:
  - `dotnet restore`
  - `dotnet build`
  - `dotnet run`
- [ ] Access app at `https://localhost:5001` or `http://localhost:5000` (Kestrel defaults).
- [ ] Root seeding: first visit to `/Login` creates root user with username `root` / password `password` if no users exist.

## 3. Core Authentication & Roles
- [ ] **Login (root):** Use `root/password`, expect redirect to Dashboard, nav shows root-only links.
- [ ] **Invalid login:** Wrong password or unknown user → validation error on login page.
- [ ] **Change password (My Account):** Enforce policy (>=8 chars, upper/lower/digit). After change, old password fails, new works.
- [ ] **Admin-created user:** Root creates user in Admin → Create. New user can log in.
- [ ] **Role visibility:** Root sees Settings/Admin/Backup/Reminder Emails. Admin/User should **Forbid** on these root-only pages and not see nav links.

## 4. Home Dashboard
- [ ] **Anonymous:** Visit `/` logged out → MOTD (if set), “not signed in”, metrics zero/empty.
- [ ] **Logged-in:** Shows “Signed in as …” with correct badge (Root/Admin/User). Metrics reflect data.
- [ ] **Upcoming reminders:** Create reminders due in next 30 days (date/odometer). Verify table shows vehicle, description, urgency, tags, status.

## 5. Garage (Vehicle Management, Filters, Pagination)
- [ ] **Add vehicle:** Use “Add Vehicle”; vehicle appears in list.
- [ ] **Edit vehicle:** Change Make/Model/Plate; list reflects changes.
- [ ] **Delete vehicle:** Remove and confirm disappearance.
- [ ] **Search filter:** Enter year/make/model/plate; results filtered accordingly.
- [ ] **Urgent only filter:** Add urgent reminder to a vehicle; “Urgent only” shows only those vehicles.
- [ ] **Pagination:** With >25 vehicles, verify page 1/2 navigation via Next/Previous.
- [ ] **Empty state:** With no vehicles, message prompts to add vehicle.
- Check columns: Last Mileage, Last Service, Last Fill-Up, Active Plans, Notes Count, Documents Count, Total Cost, Cost/Mile, Urgent badge.

## 6. Odometer, Gas, Service
For each module:
- [ ] **Create valid record:** Appears in per-vehicle list.
- [ ] **Validation:** Missing required fields or negative values → form re-displays with errors.
- [ ] **Edit/Delete:** Updates persist; deletion removes record.
- [ ] **Dashboard impact:** Garage/Reports reflect last mileage, last service/fill-up dates, costs, cost/mile.

## 7. Reminders (CRUD, Calendar, ICS)
- [ ] **Create reminders:** Date-based and odometer-based with tags; Description required; DueDate required for date metric.
- [ ] **Validation:** Missing DueDate for date metric → error.
- [ ] **Per-vehicle list:** Create/Edit/Delete behave correctly.
- [ ] **Global calendar:** Shows Vehicle, Description, Due Date, Target Odometer, Urgency, Tags, Status; completed flagged.
- [ ] **ICS export:** Download iCal, import to Google/Outlook; events on correct dates with summary/description (including tags/odometer).
- [ ] **Dashboard:** Reminders due in next 30 days appear in dashboard table and counts.

## 8. Notes & Plans
- [ ] **Add note/plan:** Description required; Estimated cost non-negative (plans).
- [ ] **Edit/Delete:** Changes persist; deletion removes entry.
- [ ] **Counts:** Garage/Reports NoteCount and ActivePlanCount update accordingly.

## 9. Documents (Uploads, Limits, Types)
- [ ] **Upload allowed file under limit:** Small .pdf/.jpg uploads; success message; file listed; download opens.
- [ ] **Disallowed extension:** Expect “file type not allowed.”
- [ ] **Too large file:** Use > configured MB (e.g., 12MB if limit 10MB); expect size error.
- [ ] **Delete:** File removed from list.
- [ ] **Settings upload limit:** Change Max upload size in Settings; verify enforcement.

## 10. Reports (Table, Filters, Pagination, CSV)
- [ ] **Render:** Columns match Garage values.
- [ ] **Filters:** Search by Make/Model/Plate/Year; “Urgent only” filters urgent vehicles.
- [ ] **Pagination:** With >25 vehicles, verify navigation.
- [ ] **Summary:** Totals/averages match visible rows.
- [ ] **CSV export:** Download; headers present; values match full dataset (not just current page); text fields escaped.

## 11. Settings (MOTD, Auth, Locale, Upload Limit, Reminder Emails)
- [ ] **MOTD:** Set/update; appears on Home.
- [ ] **EnableAuth toggle:** Note expected behavior if changed.
- [ ] **Locale override:** Change (e.g., en-CA/en-US); verify date/number formats.
- [ ] **Max document upload size:** Changing affects uploads (see section 9).
- [ ] **Reminder email settings:** Enable/disable, adjust days-ahead; Reminder Emails page reflects state.

## 12. Admin (User Management)
- [ ] **List users:** Root first; roles visible.
- [ ] **Create user:** With/without admin; password policy enforced.
- [ ] **Edit user:** Update email/admin flag (non-root); optional password change works.
- [ ] **Root protection:** Root delete blocked.
- [ ] **Delete non-root:** User removed and cannot log in.

## 13. My Account (Change Password)
- [ ] Successful change (policy compliant).
- [ ] Incorrect current password → error.
- [ ] Non-compliant new password → validation error.
- [ ] Post-change login uses new password only.

## 14. Backup
- [ ] **Backup page:** Accessible to root; warning text shown.
- [ ] **Download backup:** ZIP downloads; contains `data` tree (LiteDB, config, documents).
- [ ] Optional: document restore steps if attempted manually.

## 15. Reminder Emails (Manual Trigger)
- [ ] **Reminder Emails page:** Shows enabled/disabled and days-ahead.
- [ ] **Disabled state:** Preview indicates none; send short-circuits with message.
- [ ] **Enabled state:** With user emails and upcoming reminders, preview per-user digests.
- [ ] **Send now:** Trigger send; success message. If SMTP configured, check inbox; otherwise, check logs.

## 16. Error Pages (401/403/404)
- [ ] **401:** Access protected page logged out → login redirect or 401 page with guidance.
- [ ] **403:** Non-root hits root-only URL → Access Denied page.
- [ ] **404:** Non-existent route → 404 page with navigation links.

## 17. Security Headers
- [ ] Open DevTools → Network → select response → Headers.
- [ ] Confirm headers on multiple pages (Home, Garage, Login):  
  - X-Content-Type-Options: nosniff  
  - X-Frame-Options: SAMEORIGIN  
  - Referrer-Policy: strict-origin-when-cross-origin  
  - X-XSS-Protection: 0  
  - Content-Security-Policy includes `default-src 'self'` etc.

## 18. Theme Toggle (Light/Dark Mode)
- [ ] **Toggle:** Click “Toggle theme”; body switches light/dark (background, text, navbar, cards, tables).
- [ ] **Persistence:** Reload/close/reopen; theme remains via localStorage.
- [ ] **Pages:** Verify legibility/usability on Home, Garage, Reports, Settings, Admin, Backup, Reminder Emails, Documents in both themes.

## 19. Cross-Browser / Device Checklist
- [ ] Test in at least two browsers (e.g., Chrome + Edge/Firefox).
- [ ] Test desktop and narrow viewport (responsive tables, navbar).
- [ ] Confirm critical flows (Login, Garage, Reminders, Documents, Reports, Backup) in both themes and viewports.
