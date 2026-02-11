# Implementation Checklist

**Purpose:** Ensure every phase has a detailed implementation plan before development begins.

---

## Pre-Development Checklist

Before starting work on any roadmap phase, complete the following steps **in order**:

### ✅ 1. Phase Document Preparation

- [ ] Review phase document in `docs/roadmap/phase-XX-name.md`
- [ ] Verify all sections are complete:
  - [ ] Priority, Timeline, Status, Dependencies
  - [ ] Overview, Features list
  - [ ] Technical Implementation (high-level)
  - [ ] User Stories
  - [ ] Success Criteria
  - [ ] Quick Start section

### ✅ 2. Create Detailed Implementation Plan

Add a new section to the phase document: `## Implementation Plan`

Include the following subsections:

#### 2.1 Task Breakdown
- [ ] List all discrete tasks (UI, backend, tests, docs)
- [ ] Assign estimated effort (hours/days)
- [ ] Identify dependencies between tasks
- [ ] Set priority order (critical path)

#### 2.2 File/Class Structure
- [ ] List all new files to create (with paths)
- [ ] List all existing files to modify
- [ ] Document new classes, interfaces, services
- [ ] Specify namespaces and project locations

#### 2.3 Testing Strategy
- [ ] Unit tests: List test classes and key test cases
- [ ] Integration tests: List scenarios
- [ ] Manual testing: Document test scripts
- [ ] Acceptance criteria: Map tests to success criteria

#### 2.4 Migration/Upgrade Path
- [ ] Document breaking changes (if any)
- [ ] Create migration scripts (if needed)
- [ ] Plan backward compatibility approach
- [ ] Document upgrade procedure for users

#### 2.5 Documentation Updates
- [ ] List README updates
- [ ] User guide sections to add/modify
- [ ] API documentation requirements
- [ ] Release notes template

### ✅ 3. Technical Review

- [ ] Review implementation plan with team/self
- [ ] Validate technical approach
- [ ] Identify potential risks/blockers
- [ ] Confirm dependencies are satisfied
- [ ] Get approval to proceed (if team-based)

### ✅ 4. Setup Development Branch

```powershell
# Create feature branch
git checkout -b phase-XX-feature-name

# Create issue tracking (GitHub)
gh issue create --title "Phase XX: Feature Name" --body "See docs/roadmap/phase-XX-name.md"
```

- [ ] Branch created and checked out
- [ ] GitHub issue created and linked
- [ ] Project board updated (if using)

### ✅ 5. Implementation Kickoff

- [ ] Create todo list from task breakdown
- [ ] Set up test stubs/fixtures
- [ ] Begin development following plan

---

## During Development

- [ ] Update phase document Status field (`Planned` → `In Progress`)
- [ ] Check off completed tasks in Implementation Plan
- [ ] Document any deviations from plan
- [ ] Run tests continuously (TDD approach)
- [ ] Update Quick Start section if commands change

---

## Pre-Merge Checklist

Before merging phase implementation:

- [ ] All tasks in Implementation Plan completed
- [ ] All tests passing (unit + integration)
- [ ] Manual testing completed (Quick Start verified)
- [ ] Code review completed (if team-based)
- [ ] Documentation updated (README, user guide)
- [ ] Phase document Status updated to `Complete`
- [ ] Release notes drafted
- [ ] Git tags created (if releasing)

---

## Implementation Plan Template

Copy this template into your phase document:

```markdown
## Implementation Plan

### Task Breakdown

| Task | Estimate | Priority | Dependencies | Status |
|------|----------|----------|--------------|--------|
| Create UI mockups | 2h | High | None | Planned |
| Implement service class | 4h | High | None | Planned |
| Add unit tests | 3h | Medium | Service class | Planned |
| Update UI binding | 2h | Medium | Service class | Planned |
| Manual testing | 1h | Low | All above | Planned |

### File/Class Structure

**New Files:**
- `src/WslTamer.Core/Services/FeatureService.cs`
- `src/WslTamer.UI/Views/FeatureWindow.xaml`
- `tests/WslTamer.Tests/Services/FeatureServiceTests.cs`

**Modified Files:**
- `src/WslTamer.UI/MainWindow.xaml.cs` (add menu item)
- `README.md` (update feature list)

**New Classes/Interfaces:**
- `IFeatureService` (interface)
- `FeatureService` (implementation)
- `FeatureViewModel` (UI binding)

### Testing Strategy

**Unit Tests:**
- `FeatureService_Initialize_SetsDefaultValues`
- `FeatureService_Execute_ReturnsSuccess`
- `FeatureService_ExecuteWithInvalidInput_ThrowsException`

**Integration Tests:**
- End-to-end feature flow with real WSL calls

**Manual Tests:**
- Follow Quick Start section commands
- Verify UI responsiveness
- Test error handling

### Migration/Upgrade Path

- No breaking changes
- Backward compatible with existing config
- No migration script needed

### Documentation Updates

- Update README.md feature list
- Add user guide section: "Using Feature X"
- Update CHANGELOG.md
- Draft release notes for v2.X.Y
```

---

## Enforcement

1. **Self-Review:** Before starting a phase, check this file and verify all steps
2. **PR Template:** Add checklist reference to pull request template
3. **GitHub Actions:** (Future) Add CI check to verify phase docs have Implementation Plan section
4. **Code Review:** Reviewers verify implementation matches plan

---

## Example: Phase 0 Implementation Plan

See `docs/roadmap/phase-00-installation.md` (to be updated) for a reference example.

