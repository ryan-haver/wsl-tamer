# UI Refactor Plan: TabControl to NavigationView

## Phase 1: Modularization (Extracting Views)
- [x] Create `Views` directory in `src/WslTamer.UI/`.
- [x] Create `GeneralPage.xaml` & `.cs` (Move "General" tab content).
- [x] Create `DistributionsPage.xaml` & `.cs` (Move "Distributions" tab content).
- [x] Create `ProfilesPage.xaml` & `.cs` (Move "Profiles" tab content).
- [x] Create `HardwarePage.xaml` & `.cs` (Move "Hardware" tab content).
- [x] Create `AboutPage.xaml` & `.cs` (Move "About" tab content).
- [x] Move relevant event handlers and logic from `SettingsWindow.xaml.cs` to respective Page code-behinds.

## Phase 2: Navigation Structure
- [x] Modify `SettingsWindow.xaml`:
    - [x] Remove `TabControl`.
    - [x] Add `ui:NavigationView`.
    - [x] Define `NavigationViewItem`s for each page with appropriate Icons.
    - [x] Set `PaneDisplayMode="Left"` (or `LeftCompact` based on preference).
- [x] Modify `SettingsWindow.xaml.cs`:
    - [x] Implement navigation logic to switch content in the `Frame` or `ContentControl`.
    - [x] Ensure dependency injection/service passing to pages works (WslService, ProfileManager, etc.).

## Phase 3: Wiring & Logic Migration
- [x] Update `SettingsWindow` constructor to initialize Pages with required services.
- [x] Ensure `SettingsWindow` handles the main navigation events.
- [x] Verify `ProfilesPage` can still communicate with `SettingsWindow` if needed (or decouple logic).
- [x] Verify `HardwarePage` data grids and refresh logic.

## Phase 4: Testing & Cleanup
- [ ] Verify "General" actions (Start/Stop WSL).
- [ ] Verify "Distributions" list and actions (Run, Stop, etc.).
- [ ] Verify "Profiles" creation and editing.
- [ ] Verify "Hardware" mounting/unmounting.
- [ ] Check UI scaling and responsiveness (Sidebar collapsing).
- [ ] Remove unused code from `SettingsWindow`.
