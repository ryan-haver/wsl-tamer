# Phase 4: UI/UX Enhancements

**Priority:** MEDIUM  
**Timeline:** Q4 2025  
**Status:** Planned  
**Dependencies:** Phase 0–3

---

## Quick Start

A few quick wins to improve daily UX today.

### Create a Desktop Shortcut for a Distro

```powershell
$distro   = 'Ubuntu'
$shortcut = "$env:USERPROFILE\Desktop\$distro.lnk"
$shell    = New-Object -ComObject WScript.Shell
$sc       = $shell.CreateShortcut($shortcut)
$sc.TargetPath = "$env:SystemRoot\system32\wsl.exe"
$sc.Arguments  = "-d $distro"
$sc.IconLocation = "$env:SystemRoot\System32\wsl.exe,0"
$sc.Save()
```

### Open a Distro in Windows Terminal

```powershell
wt.exe -w 0 wsl.exe -d Ubuntu
```

### Verify


- Double-clicking the desktop shortcut launches the distro

- Windows Terminal opens the specified distro tab


## Overview

Polish and expand the user experience with tray improvements, integrated terminal, desktop integration, hotkeys, and navigation aids. Streamline common workflows and reduce friction for daily WSL operations.

---

## Features

### 4.1 Tray & Navigation Enhancements 🧭

**Status:** Planned  
**Complexity:** Medium


- [ ] Tray menu enhancements (quick launch, mount controls, pinned favorites)

- [ ] Real-time status indicators

- [ ] Jump list integration (quick actions from taskbar)

- [ ] Collapsible sections and floating save bars (consistency)


---

### 4.2 Embedded Terminal Panel 🖥️

**Status:** Planned  
**Complexity:** Medium


- [ ] Integrated terminal with tabs and split view

- [ ] Per-distro terminal context

- [ ] Keyboard shortcuts for panel management

- [ ] Session persistence options


---

### 4.3 Desktop Integration 🗂️

**Status:** Planned  
**Complexity:** Medium


- [ ] Explorer context menus (import/export, open terminal, mount)

- [ ] Shortcuts for launching distros and actions

- [ ] Context-aware quick actions


---

### 4.4 Global & Distribution Hotkeys ⌨️

**Status:** Planned  
**Complexity:** Medium


- [ ] Global hotkeys (open app, launch default distro, command palette, toggle tray)

- [ ] Per-distribution hotkeys (launch specific distro, execute commands, open in terminal, mount/unmount)

- [ ] Hotkey conflict detection and resolution UI


---

## Technical Implementation


- Tray integration updates with real-time status feed.

- Embedded terminal component with tab/split support.

- Shell integration for Explorer context menus and shortcuts.

- Hotkey recorder and conflict detection service.


---

## User Stories


- Quick access to common actions from tray and taskbar.

- Seamless terminal workflows within the app.

- Desktop integration reduces clicks for routine operations.

- Hotkeys accelerate distro launches and automation.


---

## Success Criteria


- Users complete common actions faster via tray/jump list.

- Terminal panel is stable with tab/split management.

- Explorer integration works across common scenarios.

- Hotkeys are configurable, conflict-aware, and reliable.


---

## Related Phases


- **Prerequisite:** Phase 0–3

- **Related to:** Phase 1 (Distro Management), Phase 5 (Automation)


---

## Implementation Plan

### Task Breakdown

| Task | Estimate | Priority | Dependencies | Status |
|------|----------|----------|--------------|--------|
| Design tray menu redesign | 4h | High | None | Planned |
| Implement enhanced tray functionality | 6h | High | Design | Planned |
| Create embedded terminal component | 16h | High | None | Planned |
| Add terminal tab management | 6h | Medium | Terminal component | Planned |
| Implement split view for terminal | 8h | Medium | Terminal component | Planned |
| Build Explorer context menu handler | 6h | High | None | Planned |
| Create desktop shortcut generator | 4h | Medium | None | Planned |
| Implement global hotkey system | 8h | High | None | Planned |
| Build hotkey configuration UI | 6h | Medium | Hotkey system | Planned |
| Add hotkey conflict detection | 4h | Medium | Hotkey system | Planned |
| Implement jump list integration | 5h | Medium | None | Planned |
| Create command palette | 8h | Medium | None | Planned |
| Add session persistence | 4h | Low | Terminal component | Planned |
| Write unit tests | 8h | High | All features | Planned |
| Usability testing | 6h | High | All features | Planned |
| Documentation | 4h | Medium | Completion | Planned |

### File/Class Structure

**New Files:**

- `src/WslTamer.UI/Services/TrayService.cs` - Enhanced tray management

- `src/WslTamer.UI/Services/HotkeyService.cs` - Global hotkey registration

- `src/WslTamer.UI/Services/ShellIntegrationService.cs` - Explorer integration

- `src/WslTamer.UI/Services/JumpListService.cs` - Taskbar jump lists

- `src/WslTamer.UI/Controls/EmbeddedTerminal.xaml` - Terminal control

- `src/WslTamer.UI/Controls/TerminalTabControl.xaml` - Tab management

- `src/WslTamer.UI/Controls/CommandPalette.xaml` - Quick command UI

- `src/WslTamer.UI/Dialogs/HotkeyConfigDialog.xaml` - Hotkey settings

- `src/WslTamer.UI/ViewModels/TrayViewModel.cs` - Tray logic

- `src/WslTamer.UI/ViewModels/TerminalViewModel.cs` - Terminal binding

- `src/WslTamer.UI/ViewModels/HotkeyConfigViewModel.cs` - Hotkey config

- `src/WslTamer.Core/Models/Hotkey.cs` - Hotkey definition

- `src/WslTamer.Core/Models/TrayAction.cs` - Tray action model

- `src/WslTamer.Core/Native/ShellExtension.cs` - Native shell APIs

- `src/WslTamer.Core/Native/HotkeyInterop.cs` - Win32 hotkey APIs

- `tests/WslTamer.Tests/Services/HotkeyServiceTests.cs`

- `tests/WslTamer.Tests/Services/TrayServiceTests.cs`


**Modified Files:**

- `src/WslTamer.UI/MainWindow.xaml` - Add terminal panel

- `src/WslTamer.UI/App.xaml.cs` - Register hotkeys on startup

- `src/WslTamer.Core/DependencyInjection.cs` - Register services

- `README.md` - Document UI features


**New Classes/Interfaces:**

- `ITrayService`

- `IHotkeyService`

- `IShellIntegrationService`

- `IJumpListService`

- `ITerminalSession` - Terminal session abstraction

- `HotkeyModifiers` - Enum (Ctrl, Alt, Shift, Win)

- `TrayActionType` - Enum (Launch, Mount, Unmount, Shutdown)


### Testing Strategy

**Unit Tests:**

- Hotkey registration and unregistration

- Hotkey conflict detection logic

- Tray menu action binding

- Jump list creation and updates

- Terminal session management


**Integration Tests:**

- Hotkey triggers correct action

- Tray menu launches distros

- Explorer context menu invokes actions

- Terminal executes commands and captures output

- Tab management (create, close, switch)


**Manual Tests:**

- Test global hotkeys across applications

- Verify tray menu real-time status updates

- Right-click in Explorer and test context menu

- Open terminal, split view, execute commands

- Test hotkey conflicts with common apps

- Verify jump list from taskbar

- Test command palette search and execution


### Migration/Upgrade Path


- Enhanced UI features (opt-in for hotkeys)

- Existing tray functionality preserved

- Hotkey configuration stored in settings

- Shell integration requires elevation (one-time)

- Terminal panel can be hidden/docked

- No breaking changes to core functionality


### Documentation Updates


- Add "User Interface" guide with screenshots

- Document hotkey configuration and defaults

- Create terminal usage guide

- Add Explorer integration setup instructions

- Document accessibility considerations

- Update README with UI capabilities

- Release notes for Q4 2025
