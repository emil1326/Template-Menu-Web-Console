# Project Behavior Summary Guidelines

This file is the canonical summary of how the console architecture behaves today.
Use it to review PRs and keep feature changes aligned.

## 1) App model

- Every module is an `App` (`Core/App.cs`).
- Apps can contain child apps with `RegisterSubApp(App)`.
- Hierarchy depth is unlimited.
- The core initializes and cleans the full tree (`InitializeTree`, `CleanupTree`).

## 2) Page model

- A single app can expose many pages (`MenuPage`, `MenuChar`, custom UI components).
- Pages are local navigation units only.
- Settings/Info ownership is app-level, not page-level.

## 3) Settings and Info ownership

- `GetSettingsEntries()` and `GetInfoLines()` belong to each app.
- A parent app can display/modify descendant settings via scoped aggregation.
- Rule: expose Settings/Info actions once at app root pages.
- Do not duplicate Settings/Info actions on every CRUD/search page.

## 4) Navigation invariants

- `q` quits only on top-level router page.
- `q` in all other pages means "back".
- `Ctrl+B` tries to go back to previous page.
- If back is unavailable, the app warns and replays page root (and falls back to home only when no page is registered).
- `Ctrl+H` always returns to the router home page.

## 4.1) Settings UX invariants

- Settings selection reads full line input: indices above 9 are supported.
- Bool settings toggle on selection (no extra value prompt).
- Post-edit confirmation pauses are removed; page refreshes directly.

## 5) Router responsibilities

- Router is the root app and module switcher.
- Router can show scoped Settings/Info for the whole tree.
- Router should not contain domain business logic.

## 6) Domain app responsibilities

- Domain app root menu links to domain pages.
- Domain app root includes Settings/Info for that app scope.
- Child pages focus on business actions only (create/read/update/delete/search/etc.).

## 7) Error handling invariants

- Expected failures return `Result` / `Result<T>` with `AppError`.
- User/developer-relevant failures should use `AppError`.
- Avoid raw exceptions in normal control flow.

## 8) Documentation update checklist

When behavior changes, update all of these files together:

- `README.md`
- `doc/userapp-guide.md`
- `doc/summary-guidelines.md`
- `doc/error-guidelines.md`
- `doc/data-access-architecture.md` (if data flow contracts changed)

If a change touches navigation or ownership rules, explicitly update sections 2, 3, and 4 in this file.
