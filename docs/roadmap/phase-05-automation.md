# Phase 5: System-Level Automation

**Priority:** MEDIUM  
**Timeline:** Q4 2025  
**Status:** Partial (Basic automation exists)  
**Dependencies:** Phase 1 (Distro Management), Phase 4 (UI/UX)

---

## Quick Start

Set up simple automation with Windows Task Scheduler.

### Prerequisites


- Phase 1 complete; distro name known (e.g., Ubuntu)

- PowerShell running as a user with permission to create tasks


### Daily Backup at 02:00 (Export tar)

```powershell
$script = "$env:USERPROFILE\Scripts\wsl-backup-ubuntu.ps1"
New-Item -ItemType Directory -Force -Path (Split-Path $script) | Out-Null
@'
$distro    = "Ubuntu"
$backupDir = "$env:USERPROFILE\Backups"
New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
$file = Join-Path $backupDir "$distro-$(Get-Date -Format yyyyMMdd-HHmmss).tar"
wsl --export $distro $file
'@ | Set-Content -Path $script -Encoding UTF8

$action  = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-NoProfile -WindowStyle Hidden -File `"$script`""
$trigger = New-ScheduledTaskTrigger -Daily -At 2:00am
Register-ScheduledTask -TaskName "WSL-Backup-Ubuntu-Daily" -Action $action -Trigger $trigger -Description "Export WSL distro daily"
```

### On-Login Initialization Script

```powershell
$action  = New-ScheduledTaskAction -Execute "wsl.exe" -Argument "-d Ubuntu -- /bin/bash -lc 'echo hello from automation'"

$trigger = New-ScheduledTaskTrigger -AtLogOn
Register-ScheduledTask -TaskName "WSL-Init-Ubuntu-OnLogin" -Action $action -Trigger $trigger -Description "Run init on login"
```

### Verify

```powershell
Get-ScheduledTask -TaskName "WSL-*" | Format-Table TaskName, State, LastRunTime
Start-ScheduledTask -TaskName "WSL-Backup-Ubuntu-Daily"   # test run
```

## Overview

Expand automation capabilities with event-driven triggers, scheduled tasks, a visual workflow builder, and a library of script templates to streamline maintenance, backups, and environment management.

---

## Features

### 5.1 Event-Driven Automation 🔄

**Status:** Planned  
**Complexity:** High


- [ ] On system boot → Start specific distros

- [ ] On USB detected → Auto-mount to distro

- [ ] On network connected → Start services

- [ ] On user login → Run initialization scripts


---

### 5.2 Scheduled Tasks 🗓️

**Status:** Planned  
**Complexity:** Medium


- [ ] Cron-like scheduling for WSL commands

- [ ] Maintenance windows (automated updates)

- [ ] Automated backups/snapshots

- [ ] Resource optimization schedules


---

### 5.3 Workflow Builder 🧩

**Status:** Planned  
**Complexity:** High


- [ ] Visual workflow designer (drag-drop)

- [ ] Conditional logic (if/then/else)

- [ ] Loops and retries with backoff

- [ ] Error handling and rollback


---

### 5.4 Script Templates Library 📚

**Status:** Planned  
**Complexity:** Medium


- [ ] Common maintenance scripts

- [ ] Update all distros (apt/dnf/pacman)

- [ ] Clean up disk space

- [ ] Health checks and diagnostics

- [ ] Security hardening scripts


---

## Technical Implementation


- Event bus and trigger listeners (boot, USB, network, login).

- Scheduler service for cron-like jobs and maintenance windows.

- Workflow engine with visual designer, conditionals, and retries.

- Script templates packaged and versioned; execution with logging.


---

## User Stories


- Automated start and initialization on boot/login.

- Scheduled updates and backups without manual intervention.

- Visual workflows for complex multi-step operations.

- Reusable maintenance scripts for common tasks.


---

## Success Criteria


- Triggers fire reliably and are configurable per distro.

- Scheduler runs tasks with logs and error handling.

- Workflows can be created, executed, and rolled back safely.

- Script templates cover common scenarios and are extensible.


---

## Related Phases


- **Prerequisite:** Phase 1, Phase 4

- **Related to:** Phase 2 (Snapshots), Phase 3 (Monitoring)


---

## Implementation Plan

### Task Breakdown

| Task | Estimate | Priority | Dependencies | Status |
|------|----------|----------|--------------|--------|
| Design event bus architecture | 4h | Critical | None | Planned |
| Create EventBusService | 6h | Critical | Architecture | Planned |
| Implement event triggers (boot, USB, network, login) | 8h | High | EventBus | Planned |
| Create SchedulerService (cron-like) | 10h | High | None | Planned |
| Build scheduling UI | 8h | High | Scheduler | Planned |
| Design workflow engine architecture | 6h | High | None | Planned |
| Create WorkflowEngine core | 12h | High | Architecture | Planned |
| Build visual workflow designer | 16h | Medium | Workflow engine | Planned |
| Implement conditional logic (if/then/else) | 6h | Medium | Workflow engine | Planned |
| Add loops and retry logic | 6h | Medium | Workflow engine | Planned |
| Create script template library | 8h | Medium | None | Planned |
| Build template editor UI | 6h | Medium | Template library | Planned |
| Implement rollback/error handling | 8h | High | Workflow engine | Planned |
| Add execution logging and history | 5h | Medium | Scheduler/Workflow | Planned |
| Write comprehensive tests | 12h | High | All services | Planned |
| Performance testing | 4h | Medium | Workflow engine | Planned |
| Documentation and examples | 6h | Medium | Completion | Planned |

### File/Class Structure

**New Files:**

- `src/WslTamer.Core/Services/EventBusService.cs` - Event pub/sub

- `src/WslTamer.Core/Services/EventTriggerService.cs` - System event handlers

- `src/WslTamer.Core/Services/SchedulerService.cs` - Task scheduling

- `src/WslTamer.Core/Services/WorkflowEngine.cs` - Workflow execution

- `src/WslTamer.Core/Services/ScriptTemplateService.cs` - Template management

- `src/WslTamer.Core/Services/ExecutionLogService.cs` - Execution history

- `src/WslTamer.Core/Models/AutomationEvent.cs` - Event model

- `src/WslTamer.Core/Models/EventTrigger.cs` - Trigger definition

- `src/WslTamer.Core/Models/ScheduledTask.cs` - Task model

- `src/WslTamer.Core/Models/Workflow.cs` - Workflow definition

- `src/WslTamer.Core/Models/WorkflowStep.cs` - Step model

- `src/WslTamer.Core/Models/ScriptTemplate.cs` - Template model

- `src/WslTamer.Core/Models/ExecutionLog.cs` - Log entry

- `src/WslTamer.UI/Views/AutomationView.xaml` - Automation hub

- `src/WslTamer.UI/Views/EventTriggersView.xaml` - Trigger configuration

- `src/WslTamer.UI/Views/SchedulerView.xaml` - Task scheduler UI

- `src/WslTamer.UI/Views/WorkflowDesigner.xaml` - Visual designer

- `src/WslTamer.UI/Views/ScriptTemplateLibrary.xaml` - Template browser

- `src/WslTamer.UI/ViewModels/AutomationViewModel.cs`

- `src/WslTamer.UI/ViewModels/WorkflowDesignerViewModel.cs`

- `src/WslTamer.UI/Controls/WorkflowNode.xaml` - Workflow step control

- `tests/WslTamer.Tests/Services/EventBusServiceTests.cs`

- `tests/WslTamer.Tests/Services/SchedulerServiceTests.cs`

- `tests/WslTamer.Tests/Services/WorkflowEngineTests.cs`


**Modified Files:**

- `src/WslTamer.UI/MainWindow.xaml` - Add automation tab

- `src/WslTamer.Core/DependencyInjection.cs` - Register services

- `README.md` - Document automation features


**New Classes/Interfaces:**

- `IEventBusService`

- `IEventTriggerService`

- `ISchedulerService`

- `IWorkflowEngine`

- `IScriptTemplateService`

- `IExecutionLogService`

- `EventType` - Enum (Boot, UsbDetected, NetworkConnected, UserLogin)

- `TriggerCondition` - Enum (Always, OnSuccess, OnFailure)

- `WorkflowStepType` - Enum (Command, Condition, Loop, Wait, Snapshot)


### Testing Strategy

**Unit Tests:**

- Event bus publish/subscribe

- Trigger condition evaluation

- Cron expression parsing and scheduling

- Workflow step execution order

- Conditional logic (if/then/else)

- Loop iteration and termination

- Retry logic with backoff

- Error handling and rollback

- Template variable substitution


**Integration Tests:**

- End-to-end trigger → action flow

- Scheduled task execution at correct times

- Workflow with multiple steps and conditions

- Error in workflow triggers rollback

- Script template execution

- Event bus under high load


**Manual Tests:**

- Configure boot trigger and verify execution on restart

- Schedule daily backup and verify execution

- Build workflow with loops and conditions

- Test rollback on intentional failure

- Execute script template with parameters

- Monitor execution logs and history

- Test with multiple concurrent workflows


### Migration/Upgrade Path


- New automation features (opt-in)

- Existing manual operations unaffected

- Workflows stored in `%APPDATA%\WslTamer\workflows\`

- Script templates in `%APPDATA%\WslTamer\templates\`

- Execution logs in SQLite database

- No breaking changes to existing functionality


### Documentation Updates


- Add "Automation" user guide section

- Document event triggers and conditions

- Create workflow authoring guide with examples

- Add cron expression reference

- Document script template syntax

- Create troubleshooting guide for automation

- Add video walkthrough for workflow designer

- Update README automation features

- Release notes for Q4 2025
