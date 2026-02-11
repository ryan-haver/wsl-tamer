# Phase 2: Snapshot & Backup System

**Priority:** HIGH  
**Timeline:** Q1–Q2 2025  
**Status:** Partial (Export/Import exists)  
**Dependencies:** Phase 0 (Installation), Phase 1 (Distro Management)

---

## Quick Start

Backup, restore, and optionally sync snapshots to cloud storage.

### Prerequisites


- Disk space sufficient for at least one full export (size of distro rootfs)

- Optional: AzCopy installed for Azure Blob uploads


### Create an Archive Backup (tar)

```powershell
$distro = 'Ubuntu'
$dest   = "$env:USERPROFILE\Backups\$distro-$(Get-Date -Format yyyyMMdd-HHmmss).tar"
New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null
wsl --export $distro "$dest"
```

### Restore from an Archive Backup

```powershell
$new       = "$( $distro )-restored"
$installDir = "$env:USERPROFILE\WSL\$new"
wsl --import $new "$installDir" "$dest" --version 2
wsl -l -v
```

### Optional: Upload Backup to Azure Blob (pilot)

```powershell
# Replace <account>, <container>, and <SAS> with your Azure Storage values
azcopy copy "$dest" "https://<account>.blob.core.windows.net/<container>/$([IO.Path]::GetFileName($dest))?<SAS>"
```

### Verify


- `wsl -l -v` lists the restored distro

- `wsl -d $new -- uname -a` executes in the restored environment


## Overview

Provide robust protection for WSL distros via fast VHDX differencing snapshots and portable archive backups. Add a visual timeline, automation policies, and optional cloud backup to safeguard environments and speed recovery.

---

## Features

### 2.1 Dual Snapshot System 💾

**Status:** Partial  
**Complexity:** High


- [ ] **Fast VHDX snapshots**

  - Near-instant snapshots using VHDX differencing disks

  - Minimal disk space usage (copy-on-write)

  - Quick restore capability (<1 minute)

  - Snapshot chains with metadata



- [ ] **Full archive snapshots**

  - Traditional tar-based backups

  - Portable across machines

  - Compressed storage (gzip, zstd)

  - Integrity verification (checksums)



- [ ] **Snapshot management UI**

  - Visual timeline of snapshots

  - Metadata (size, date, description, tags)

  - Restore from any snapshot

  - Delete with confirmation

  - Versioning and branching

  - Compare snapshots (diff view)



- [ ] **Automated snapshot policies**

  - Before system updates

  - Scheduled daily/weekly snapshots

  - Rotation (keep last N)

  - Pre-command snapshots for destructive ops

  - Snapshot on distro state change


**Technical Stack:**

```text
VHDX: Windows Hyper-V API for differencing disks
Archive: SharpCompress for tar.zst creation
Storage: SQLite for snapshot metadata
UI: Timeline component with restore preview
```

---

### 2.2 Cloud Backup & Sync 🌥️ *NEW*

**Status:** Planned  
**Complexity:** Very High


- [ ] **Cloud providers**: Azure Blob, AWS S3, Google Cloud Storage, OneDrive, Dropbox, S3-compatible (Backblaze B2, Wasabi, MinIO)

- [ ] **Automated cloud backup**: scheduling, incremental uploads, compression, encryption (AES-256), throttling, resume

- [ ] **Cloud restore**: browse, download, integrity verification, cross-machine restore

- [ ] **Retention policies**: daily/weekly/monthly windows, automatic cleanup, cost tracking

- [ ] **Multi-machine sync**: profiles, automation rules, configurations, conflict resolution, selective sync


**User Stories:**


- As a consultant, I want cloud backups before major changes.

- As a student, I want to sync environments between laptop and desktop via OneDrive.

- As an enterprise user, I want nightly backups to an S3 bucket.

- As a hobbyist, I want cost-effective backups to Backblaze B2.


---

## Technical Implementation


- Hyper-V differencing disk APIs for fast snapshot/restore.

- Tar.zst archives via SharpCompress with checksums.

- SQLite metadata store with timeline visualization.

- Cloud SDK integrations (Azure/AWS/GCP/OneDrive/Dropbox).


---

## User Stories


- Rapid recovery from failed updates via pre-update snapshots.

- Portable archives for sharing/migration across machines.

- Automated policies reduce manual backup burden.

- Cloud restore enables disaster recovery and mobility.


---

## Success Criteria


- Snapshot creation and restore complete reliably and quickly.

- Archives verify integrity and restore across machines.

- Timeline UI enables clear navigation and snapshot management.

- Configurable policies run on schedule with rotation.


---

## Related Phases


- **Prerequisite:** Phase 0, Phase 1

- **Related to:** Phase 3 (Monitoring), Phase 5 (Automation)


---

## Implementation Plan

### Task Breakdown

| Task | Estimate | Priority | Dependencies | Status |
|------|----------|----------|--------------|--------|
| Research Hyper-V differencing disk API | 4h | Critical | None | Planned |
| Create SnapshotService (VHDX) | 10h | Critical | API research | Planned |
| Create ArchiveService (tar.zst) | 6h | High | None | Planned |
| Design snapshot metadata schema | 3h | High | None | Planned |
| Implement SQLite metadata store | 5h | High | Schema | Planned |
| Build timeline UI component | 12h | High | Metadata store | Planned |
| Create snapshot comparison/diff view | 8h | Medium | Timeline UI | Planned |
| Implement automated snapshot policies | 6h | High | SnapshotService | Planned |
| Build cloud provider integrations | 16h | Medium | None | Planned |
| Create cloud backup scheduler | 6h | Medium | Cloud integration | Planned |
| Implement incremental upload logic | 8h | Medium | Cloud integration | Planned |
| Add encryption (AES-256) support | 6h | High | ArchiveService | Planned |
| Build retention policy engine | 5h | Medium | Metadata store | Planned |
| Write comprehensive tests | 12h | High | All services | Planned |
| Performance testing (large VHDXs) | 6h | Medium | Snapshot service | Planned |
| Documentation and user guide | 6h | Medium | Completion | Planned |

### File/Class Structure

**New Files:**

- `src/WslTamer.Core/Services/SnapshotService.cs` - VHDX snapshot management

- `src/WslTamer.Core/Services/ArchiveService.cs` - Tar archive creation/restore

- `src/WslTamer.Core/Services/SnapshotMetadataService.cs` - SQLite metadata

- `src/WslTamer.Core/Services/CloudBackupService.cs` - Multi-cloud integration

- `src/WslTamer.Core/Services/RetentionPolicyService.cs` - Automated cleanup

- `src/WslTamer.Core/Services/EncryptionService.cs` - AES-256 encryption

- `src/WslTamer.Core/Models/Snapshot.cs` - Snapshot metadata model

- `src/WslTamer.Core/Models/BackupArchive.cs` - Archive metadata

- `src/WslTamer.Core/Models/CloudBackupConfig.cs` - Cloud settings

- `src/WslTamer.Core/Models/RetentionPolicy.cs` - Retention rules

- `src/WslTamer.Core/Providers/AzureBlobProvider.cs` - Azure implementation

- `src/WslTamer.Core/Providers/S3Provider.cs` - AWS S3 implementation

- `src/WslTamer.Core/Providers/GcsProvider.cs` - Google Cloud Storage

- `src/WslTamer.Core/Providers/OneDriveProvider.cs` - OneDrive integration

- `src/WslTamer.UI/Views/SnapshotTimelineView.xaml` - Timeline component

- `src/WslTamer.UI/Views/SnapshotDetailsDialog.xaml` - Snapshot info/restore

- `src/WslTamer.UI/Views/CloudBackupConfigDialog.xaml` - Cloud settings

- `src/WslTamer.UI/ViewModels/SnapshotTimelineViewModel.cs` - Timeline logic

- `tests/WslTamer.Tests/Services/SnapshotServiceTests.cs`

- `tests/WslTamer.Tests/Services/ArchiveServiceTests.cs`

- `tests/WslTamer.Tests/Services/CloudBackupServiceTests.cs`


**Modified Files:**

- `src/WslTamer.UI/Views/DistroDetailsView.xaml` - Add snapshots tab

- `src/WslTamer.Core/DependencyInjection.cs` - Register services

- `README.md` - Document backup features


**New Classes/Interfaces:**

- `ISnapshotService`

- `IArchiveService`

- `ISnapshotMetadataService`

- `ICloudBackupService`

- `ICloudStorageProvider` - Provider abstraction

- `IRetentionPolicyService`

- `IEncryptionService`

- `SnapshotType` - Enum (VHDX, Archive)

- `CloudProvider` - Enum (Azure, AWS, GCS, OneDrive, Dropbox, S3Compatible)


### Testing Strategy

**Unit Tests:**

- VHDX snapshot creation and restoration

- Archive compression and decompression

- Metadata CRUD operations

- Retention policy evaluation logic

- Encryption/decryption round-trip

- Cloud provider API mocking


**Integration Tests:**

- Full snapshot workflow (create → restore → verify)

- Archive backup workflow (export → compress → encrypt → upload)

- Automated policy execution (scheduled snapshots)

- Cloud upload/download with retry logic

- Snapshot chain integrity validation


**Manual Tests:**

- Create VHDX snapshot and restore (verify data integrity)

- Create tar.zst archive and restore on different machine

- Upload to Azure Blob and restore from cloud

- Test retention policy cleanup

- Verify timeline UI navigation and restore

- Test with large distros (10GB+)


### Migration/Upgrade Path


- Existing manual exports remain valid

- New snapshot metadata stored in `%APPDATA%\WslTamer\snapshots.db`

- Import existing tar backups into metadata system

- Cloud backup opt-in (requires credentials)

- No breaking changes to distro operations


### Documentation Updates


- Add "Backup & Recovery" user guide section

- Document snapshot types and use cases

- Create cloud provider setup guides (Azure, AWS, GCS)

- Add retention policy examples

- Troubleshooting guide for restore failures

- Update CHANGELOG and release notes
