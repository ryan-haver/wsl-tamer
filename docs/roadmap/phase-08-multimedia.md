# Phase 8: Multimedia (Audio)

**Priority:** Low
**Timeline:** Q4 2026
**Status:** Not Started (from Phase 7)
**Dependencies:** WSLg/graphics stack, Distribution management (Phase 1)

---

## Quick Start

Basic audio passthrough checks with PulseAudio.

### Prerequisites


- WSLg enabled or PulseAudio server available


### Install PulseAudio tools (Ubuntu example)

```powershell
wsl -d Ubuntu -- sudo sh -lc 'apt update && apt -y install pulseaudio paprefs pavucontrol'

```

### Check audio devices and play a test sound

```powershell
wsl -d Ubuntu -- sh -lc 'pactl list short sinks; paplay /usr/share/sounds/alsa/Front_Center.wav'

```

### Verify


- Test sound plays through Windows speakers

- `pactl list short sinks` shows available devices


---

## Overview

Enable audio passthrough from WSL to Windows using PulseAudio/PipeWire bridges. Provide per-distro controls for devices, latency, and quality, along with diagnostics and testing tools.

---

## Features


- Global audio passthrough: PulseAudio bridge, PipeWire support, device selection, per-distro volume

- Per-distro settings: enable/disable, device mapping, latency adjustment, quality (sample rate, bit depth)

- Diagnostics: playback and mic tests, routing visualization, troubleshooting wizard


---

## Technical Implementation


- Bridge setup: configure PulseAudio/PipeWire for Windows audio devices

- Device mapping: list and select audio devices per distro

- Controls: latency/quality parameters; per-distro enablement and volume control

- Diagnostics: test signals, capture tests, visualization of routing paths


---

## User Stories


- As a user, I want to play audio from apps in WSL through my Windows speakers

- As a streamer, I want to tune latency and quality settings per distro

- As a learner, I want guided troubleshooting when audio doesn’t work


---

## Success Criteria


- Audio plays from WSL applications through Windows reliably

- Per-distro device selection and settings persist and apply

- Diagnostics identify and help resolve common routing issues


---

## Related Phases


- Phase 6: Network Management & Remote Access – complements remote GUI apps

- Phase 4: Automation – apply audio profiles on distro start

---

## Implementation Plan

### Task Breakdown

| Task | Estimate | Priority | Dependencies | Status |
|------|----------|----------|--------------|--------|
| Research PulseAudio/PipeWire bridging requirements | 4h | Critical | None | Planned |
| Implement AudioBridgeService (PulseAudio) | 8h | High | Research | Planned |
| Implement PipeWire support | 6h | Medium | Bridge service | Planned |
| Create AudioDeviceDiscoveryService | 5h | High | None | Planned |
| Build per-distro audio settings model | 4h | Medium | Discovery | Planned |
| Implement AudioProfileService | 6h | High | Settings model | Planned |
| Build diagnostics/test harness | 6h | Medium | Bridge service | Planned |
| Create audio control UI | 8h | High | Services | Planned |
| Implement troubleshooting wizard | 6h | Medium | Diagnostics | Planned |
| Add automation hooks for profile application | 4h | Low | Phase 5 automation | Planned |
| Write unit tests | 6h | Medium | Services | Planned |
| Integration tests (WSLg + PulseAudio) | 5h | Medium | Services | Planned |
| Manual tests on multiple distros | 4h | Medium | Completion | Planned |
| Documentation/screenshots | 3h | Low | Completion | Planned |

### File/Class Structure

**New Files:**

- `src/WslTamer.Core/Services/AudioBridgeService.cs` – Configure PulseAudio/PipeWire bridges
- `src/WslTamer.Core/Services/AudioDeviceDiscoveryService.cs` – Enumerate sinks/sources
- `src/WslTamer.Core/Services/AudioProfileService.cs` – Persist per-distro settings
- `src/WslTamer.Core/Services/AudioDiagnosticsService.cs` – Playback/mic tests
- `src/WslTamer.Core/Models/AudioProfile.cs` – Device/latency/quality settings
- `src/WslTamer.Core/Models/AudioDeviceInfo.cs` – Device metadata
- `src/WslTamer.Core/Models/AudioTestResult.cs` – Diagnostics output
- `src/WslTamer.UI/Views/AudioControlPanel.xaml` – Main configuration UI
- `src/WslTamer.UI/Views/AudioDiagnosticsDialog.xaml` – Test wizard
- `src/WslTamer.UI/ViewModels/AudioControlViewModel.cs`
- `src/WslTamer.UI/ViewModels/AudioDiagnosticsViewModel.cs`
- `tests/WslTamer.Tests/Services/AudioProfileServiceTests.cs`
- `tests/WslTamer.Tests/Services/AudioBridgeServiceTests.cs`

**Modified Files:**

- `src/WslTamer.UI/Views/DistroDetailsView.xaml` – Add audio panel link
- `src/WslTamer.Core/DependencyInjection.cs` – Register audio services
- `README.md` – Document multimedia support

**New Classes/Interfaces:**

- `IAudioBridgeService`
- `IAudioDeviceDiscoveryService`
- `IAudioProfileService`
- `IAudioDiagnosticsService`
- `AudioBackend` enum (PulseAudio, PipeWire)
- `AudioQualityPreset` enum (LowLatency, Balanced, HighFidelity)

### Testing Strategy

**Unit Tests:**

- Profile creation and persistence per distro
- Device discovery parsing of pactl outputs
- Latency/quality parameter mapping
- Diagnostics result interpretation

**Integration Tests:**

- Configure PulseAudio bridge and play sample audio
- PipeWire test playback and capture
- Apply per-distro settings and verify persistence
- Troubleshooting wizard path for missing devices

**Manual Tests:**

- Play audio from GUI app (e.g., VLC) through Windows speakers
- Adjust latency slider and verify audible effect
- Run mic capture test and inspect waveform
- Switch between PulseAudio and PipeWire
- Use automation hook to apply audio profile on start

### Migration/Upgrade Path

- Settings stored at `%APPDATA%\\WslTamer\\audio-profiles.json`
- No changes applied unless feature enabled
- Compatible with WSLg defaults; falls back gracefully if WSLg absent
- Follows existing automation hooks for start events

### Documentation Updates

- Add "Audio & Multimedia" guide
- Document requirements (WSLg version, PulseAudio packages)
- Provide troubleshooting flowchart
- Include best practices for latency vs quality
- Update README with multimedia section
