# Round 4: Backend, CSS & Test Quality Review

Deep enhancement of the Rust backend, CSS architecture, and test coverage — the untouched areas from Rounds 1–3 (which focused on the TypeScript frontend).

---

## Phase 1: Security & Safety Hardening

Critical fixes that prevent data loss or shell injection.

### 1A. Shell Injection in `mount_folder`

[hardware_service.rs](file:///c:/scripts/wsl-tamer/wsl-tamer-tauri/src/services/hardware_service.rs#L163-L180)

`mount_folder()` interpolates user-provided `linux_path` and derived `mount_source` directly into a shell command string via `format!()`. Even though `validate_linux_path` rejects some dangerous chars, this is defense-in-depth — refactor to use positional args or stdin piping (same pattern already used in `write_distro_config`).

```diff
- let script = format!("mkdir -p '{}' && mount --bind '{}' '{}'",
-     linux_path, mount_source, linux_path);
- run_wsl_command(&["-d", distro, "-u", "root", "--", "sh", "-c", &script])
+ // Pass paths as environment variables, not interpolated shell
+ run_wsl_command(&[
+     "-d", distro, "-u", "root", "--",
+     "sh", "-c", "mkdir -p \"$1\" && mount --bind \"$2\" \"$1\"",
+     "--", linux_path, &mount_source
+ ])
```

### 1B. Data-Loss Risk in `move_distribution`

[wsl_service.rs](file:///c:/scripts/wsl-tamer/wsl-tamer-tauri/src/services/wsl_service.rs#L287-L306)

Current flow: export → **unregister** → import. If the import fails after unregister, the distro is **gone** (temp file still exists but requires manual recovery). Fix: import first under a temp name, verify, then unregister.

```diff
- Self::unregister_distribution(name)?;
- let result = Self::import_distribution(name, new_location, &temp_path);
+ // Import at new location under temp name first
+ let temp_name = format!("{}_moving", name);
+ Self::import_distribution(&temp_name, new_location, &temp_path)?;
+ // Only unregister original after successful import
+ Self::unregister_distribution(name)?;
+ // Rename temp to original name (re-import)
+ // ...handle rename logic...
```

> [!WARNING]
> WSL doesn't support renaming distros, so the safest approach is export → import-at-new-location → verify the new distro exists → then unregister old. Same pattern for `clone_distribution` temp cleanup.

---

## Phase 2: Rust Architecture Cleanup

### 2A. Eliminate Duplicate Types

`TriggerType` and `AutomationRule` are defined **twice**:
- [models/profile.rs](file:///c:/scripts/wsl-tamer/wsl-tamer-tauri/src-tauri/src/models/profile.rs#L85-L104) — canonical location
- [services/automation_engine.rs](file:///c:/scripts/wsl-tamer/wsl-tamer-tauri/src-tauri/src/services/automation_engine.rs#L8-L26) — duplicate

**Fix:** Delete duplicates from `automation_engine.rs`, import from `crate::models`.

### 2B. Move Monitoring Structs to Models

`SystemMetrics`, `DistroMetrics`, `WslMemoryBreakdown` are defined in [commands/monitoring.rs](file:///c:/scripts/wsl-tamer/wsl-tamer-tauri/src-tauri/src/commands/monitoring.rs#L6-L47) — commands should be thin handlers, not type definitions.

**Fix:** Move these 3 structs to a new `models/monitoring.rs`, re-export from `models/mod.rs`.

### 2C. Deduplicate Config IPC Commands

Two separate command sets exist for the same operations:

| Operation | `commands/wsl.rs` | `commands/config.rs` |
|-----------|-------------------|---------------------|
| Read .wslconfig | `read_wslconfig` | `get_wslconfig` |
| Write .wslconfig | `write_wslconfig` | `save_wslconfig` |
| Read distro config | `read_distro_config` | `get_distro_config` |
| Write distro config | `write_distro_config` | `save_distro_config` |

**Fix:** Remove the raw variants from `commands/wsl.rs` (keep the typed/validated versions in `commands/config.rs`). Update frontend service layer if needed.

### 2D. Remove Dead Code

- [process.rs](file:///c:/scripts/wsl-tamer/wsl-tamer-tauri/src-tauri/src/utils/process.rs#L28-L30): `run_wsl_list_command` — just calls `run_wsl_command`, never used directly
- [automation_engine.rs](file:///c:/scripts/wsl-tamer/wsl-tamer-tauri/src-tauri/src/services/automation_engine.rs#L46-L50): `AutomationEngine` struct and its `rules` + `last_check` fields — the `evaluate_rules` method is never called from any IPC command; all rule evaluation goes through the stateless `evaluate_rule` function

### 2E. Modernize Deprecated APIs

- [automation_engine.rs](file:///c:/scripts/wsl-tamer/wsl-tamer-tauri/src-tauri/src/services/automation_engine.rs#L95-L98): `Get-WmiObject` → `Get-CimInstance` (deprecated since PowerShell 3.0, removed in PS 7+)

```diff
- "(Get-WmiObject -Class Win32_Battery).BatteryStatus"
+ "(Get-CimInstance -ClassName Win32_Battery).BatteryStatus"
```

### 2F. Replace `once_cell` with `std::sync::OnceLock`

[wsl_service.rs](file:///c:/scripts/wsl-tamer/wsl-tamer-tauri/src-tauri/src/services/wsl_service.rs#L9) uses `once_cell::sync::Lazy` for the distro cache. `std::sync::LazyLock` is stable as of Rust 1.80 — can drop the external dependency.

---

## Phase 3: Rust Error Handling & Quality

### 3A. Structured Error Type

Currently all backend errors are `Result<T, String>`. Introduce a proper error enum:

#### [NEW] `src-tauri/src/error.rs`

```rust
#[derive(Debug, thiserror::Error)]
pub enum AppError {
    #[error("WSL command failed: {0}")]
    Wsl(String),
    #[error("PowerShell error: {0}")]
    PowerShell(String),
    #[error("IO error: {0}")]
    Io(#[from] std::io::Error),
    #[error("Config error: {0}")]
    Config(String),
    #[error("Validation error: {0}")]
    Validation(String),
}
```

> [!IMPORTANT]
> This is a **large refactor** touching every command and service. Can be done incrementally — start with `AppError` + `impl From<AppError> for String` to keep backward compat with Tauri's `Result<T, String>`.

### 3B. Consistent `format!` Error Messages

Some errors use inconsistent prefixes. Standardize to `"[Operation] [context]: [detail]"` pattern.

---

## Phase 4: CSS Architecture

The single [App.css](file:///c:/scripts/wsl-tamer/wsl-tamer-tauri/src/App.css) file is **2,482 lines** — a monolith covering everything from design tokens to page-specific styles. This makes maintenance difficult and increases merge conflict risk.

### 4A. Split Into Modular Files

```
src/styles/
├── variables.css      ← CSS custom properties (theme tokens)
├── base.css           ← Reset, body, typography, scrollbar
├── layout.css         ← App shell, sidebar, main-content
├── components.css     ← Card, button, form, modal, toast, alert, tooltip
├── pages/
│   ├── general.css
│   ├── distributions.css
│   ├── profiles.css
│   ├── configuration.css
│   ├── hardware.css
│   ├── automation.css
│   ├── settings.css
│   └── about.css
└── index.css          ← Barrel import that preserves ordering
```

### 4B. CSS Cleanup

- Remove dead selectors (audit against actual classnames used in TSX)
- Consolidate duplicate padding/margin patterns
- Verify dark mode coverage for all components

---

## Phase 5: Test Coverage Expansion

### Current Coverage

| Area | Files | Tests |
|------|-------|-------|
| **Rust backend** | 20 files | 3 test modules (`wsl_service`, `profile_manager`, `automation_engine`) + `sanitize` + `config` = ~30 unit tests |
| **Frontend** | 18 files | 6 test files (`ErrorBoundary`, `ToastContext`, `useSettings`, `automationService`, `errorUtils`, `formatUtils`) |

### 5A. Rust Backend Tests

Add tests for untested service methods:

| Module | Missing Tests |
|--------|--------------|
| `wsl_service.rs` | `parse_online_distributions`, `get_wslconfig_path` |
| `hardware_service.rs` | `parse_usb_devices`, `format_size` |
| `monitoring.rs` | `parse_memory_value`, `get_wsl_memory_breakdown` parsing |
| `process.rs` | `CommandExt` trait (compile-time only) |

### 5B. Frontend Component Tests

Prioritized by risk (components handling user data or side effects):

| Priority | Component | Why |
|----------|-----------|-----|
| P0 | `DistroConfigEditor` | Writes system config |
| P0 | `MonitoringDashboard` | Complex data flow, polling |
| P1 | `Sidebar` | Navigation, active state |
| P1 | `InstallWizard` | Multi-step flow |
| P2 | `SnapshotManager` | Destructive operations |
| P2 | `UpdateNotification` | Async update flow |

### 5C. Frontend Page Tests

| Priority | Page | Why |
|----------|------|-----|
| P0 | `ConfigurationPage` | Writes .wslconfig |
| P0 | `DistributionsPage` | Start/stop/delete distros |
| P1 | `ProfilesPage` | CRUD with unsaved changes |
| P1 | `AutomationPage` | Rule management |

---

## Phase 6: Accessibility Audit

### 6A. Semantic HTML & ARIA

Audit all components for:
- Missing `aria-label` on icon-only buttons
- Missing `role` attributes on custom widgets (toggles, dropdowns)
- Proper heading hierarchy (`h1` → `h2` → `h3`)
- `aria-live` regions for status updates (toasts, loading states)

### 6B. Keyboard Navigation

- Tab order through sidebar nav items
- Enter/Space activation on all interactive elements
- Escape to close modals
- Focus trap in modal overlays

### 6C. Color Contrast

- Verify WCAG AA (4.5:1) for text on both light and dark themes
- Check status indicators (red/green dots) have non-color differentiation

---

## Verification Plan

### Automated

```bash
# Rust backend
cargo test
cargo clippy -- -D warnings

# TypeScript
npx tsc --noEmit
npm test

# CSS audit (manual grep for dead selectors)
```

### Manual

- Verify `mount_folder` shell injection fix works with paths containing spaces
- Verify `move_distribution` safely aborts if import fails (no data loss)
- Keyboard-navigate through every page
- Screen reader test on key flows

---

## Execution Priority

> [!TIP]
> Recommended execution order by impact-to-effort ratio:

| Order | Phase | Effort | Impact |
|-------|-------|--------|--------|
| 1 | Phase 1 (Security) | Low | 🔴 Critical |
| 2 | Phase 2A-2E (Arch cleanup) | Medium | 🟡 High |
| 3 | Phase 3A (Error type) | High | 🟡 High |
| 4 | Phase 5A (Rust tests) | Medium | 🟢 Medium |
| 5 | Phase 4 (CSS split) | Medium | 🟢 Medium |
| 6 | Phase 5B-C (FE tests) | High | 🟢 Medium |
| 7 | Phase 6 (Accessibility) | Medium | 🟢 Medium |
