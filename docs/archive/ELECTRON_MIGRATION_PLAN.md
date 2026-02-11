# WSL Tamer Electron Migration Plan

**Version:** 2.0.0 (Electron)  
**Timeline:** 5-7 days for full feature parity  
**Goal:** Migrate from WPF to Electron with complete feature parity + better UI collaboration

---

## 🎯 Migration Objectives

1. **Full Feature Parity** - All current functionality working in Electron
2. **Better UI Collaboration** - Web-based UI that's easy to iterate on
3. **Reliable Testing** - Playwright integration for automated tests
4. **Modern Stack** - TypeScript, React, hot reload, DevTools
5. **Keep What Works** - WSL commands remain the same, just different caller

---

## 📊 Current WPF Feature Inventory

### Core Features to Port
- [x] System tray icon with menu
- [x] Settings window with navigation (General, Distributions, Profiles, Hardware, About)
- [x] WSL service layer (list, start, stop, shutdown, status)
- [x] Distribution management (install, import, clone, move, export, unregister)
- [x] Profile system (.wslconfig generation with all settings)
- [x] Hardware management (USB attach/detach, disk mounting)
- [x] Memory reclaim and global shutdown
- [x] Update checker and auto-update
- [x] Theme support (dark/light)
- [x] Error handling and logging
- [x] Startup settings (launch on boot)

### Current Tech Stack (WPF)
```
- .NET 8.0 / C# / WPF
- Wpf.UI (Fluent Design)
- FlaUI (testing - problematic)
- Custom services: WslService, ProfileManager, HardwareService, ThemeService
```

### New Tech Stack (Electron)
```
- Electron 28+ / Node.js 20+
- TypeScript 5.3+
- React 18+ with hooks
- Tailwind CSS + shadcn/ui (modern, clean components)
- Electron Forge (build tooling)
- Playwright (testing)
- electron-updater (auto-updates)
```

---

## 🗓️ Phase-by-Phase Migration Plan

### **PHASE 1: Foundation Setup (Day 1 - 6-8 hours)**

#### 1.1 Project Structure
```
wsl-tamer-electron/
├── src/
│   ├── main/                    # Electron main process
│   │   ├── index.ts            # App entry, window management
│   │   ├── tray.ts             # System tray icon
│   │   ├── services/           # Business logic
│   │   │   ├── wsl.service.ts         # WSL commands
│   │   │   ├── profile.service.ts     # Profile management
│   │   │   ├── hardware.service.ts    # USB/Disk
│   │   │   └── update.service.ts      # Auto-updates
│   │   ├── ipc/                # IPC handlers
│   │   │   └── handlers.ts
│   │   └── utils/
│   │       ├── logger.ts       # Winston logging
│   │       └── config.ts       # App configuration
│   ├── renderer/               # React UI
│   │   ├── App.tsx
│   │   ├── pages/
│   │   │   ├── GeneralPage.tsx
│   │   │   ├── DistributionsPage.tsx
│   │   │   ├── ProfilesPage.tsx
│   │   │   ├── HardwarePage.tsx
│   │   │   └── AboutPage.tsx
│   │   ├── components/
│   │   │   ├── Layout.tsx      # Main layout with sidebar
│   │   │   ├── DistroCard.tsx
│   │   │   ├── ProfileEditor.tsx
│   │   │   └── ...
│   │   ├── hooks/
│   │   │   ├── useWsl.ts
│   │   │   ├── useProfiles.ts
│   │   │   └── useHardware.ts
│   │   └── styles/
│   │       └── globals.css     # Tailwind + custom styles
│   ├── preload/
│   │   └── index.ts            # Secure IPC bridge
│   └── types/
│       └── index.ts            # Shared TypeScript types
├── tests/
│   ├── e2e/                    # Playwright tests
│   └── unit/                   # Jest tests
├── assets/
│   ├── icons/
│   └── images/
├── forge.config.ts             # Electron Forge config
├── package.json
├── tsconfig.json
├── tailwind.config.js
└── README.md
```

#### 1.2 Initialize Project
```bash
# Create new electron app with forge
npm init electron-app@latest wsl-tamer-electron -- --template=webpack-typescript

cd wsl-tamer-electron

# Install core dependencies
npm install react react-dom
npm install electron-updater electron-store
npm install winston

# Install dev dependencies
npm install -D @types/react @types/react-dom
npm install -D tailwindcss postcss autoprefixer
npm install -D @playwright/test
npm install -D eslint prettier

# Initialize Tailwind
npx tailwindcss init -p
```

#### 1.3 Configure Build System
- Set up TypeScript paths
- Configure Webpack for React
- Set up hot reload for development
- Configure code signing (for later)

**Deliverable:** Empty Electron app that launches with React renderer

---

### **PHASE 2: Core Infrastructure (Day 1-2 - 8-10 hours)**

#### 2.1 System Tray Implementation
```typescript
// src/main/tray.ts
import { Tray, Menu, BrowserWindow } from 'electron';

export function createTray(mainWindow: BrowserWindow): Tray {
  const tray = new Tray('assets/icons/tray-icon.ico');
  
  const contextMenu = Menu.buildFromTemplate([
    { label: 'Settings', click: () => mainWindow.show() },
    { type: 'separator' },
    { label: 'Memory Reclaim (All)', click: () => handleMemoryReclaim() },
    { label: 'Shutdown WSL', click: () => handleShutdown() },
    { type: 'separator' },
    { label: 'Quit WSL Tamer', click: () => app.quit() }
  ]);
  
  tray.setContextMenu(contextMenu);
  tray.setToolTip('WSL Tamer');
  
  return tray;
}
```

#### 2.2 Window Management
- Main settings window (hidden by default, shows on tray click)
- Window state persistence (size, position)
- Minimize to tray behavior
- Single instance lock

#### 2.3 IPC Bridge Setup
```typescript
// src/preload/index.ts
import { contextBridge, ipcRenderer } from 'electron';

contextBridge.exposeInMainWorld('api', {
  // WSL operations
  wsl: {
    getDistributions: () => ipcRenderer.invoke('wsl:getDistributions'),
    startDistro: (name: string) => ipcRenderer.invoke('wsl:start', name),
    stopDistro: (name: string) => ipcRenderer.invoke('wsl:stop', name),
    // ... etc
  },
  // Profile operations
  profiles: {
    getAll: () => ipcRenderer.invoke('profiles:getAll'),
    save: (profile: Profile) => ipcRenderer.invoke('profiles:save', profile),
    // ... etc
  },
  // Hardware operations
  hardware: {
    listUsbDevices: () => ipcRenderer.invoke('hardware:listUsb'),
    attachUsb: (busId: string, distro: string) => ipcRenderer.invoke('hardware:attachUsb', busId, distro),
    // ... etc
  }
});
```

#### 2.4 Logging System
```typescript
// src/main/utils/logger.ts
import winston from 'winston';
import path from 'path';
import { app } from 'electron';

export const logger = winston.createLogger({
  level: 'info',
  format: winston.format.combine(
    winston.format.timestamp(),
    winston.format.json()
  ),
  transports: [
    new winston.transports.File({ 
      filename: path.join(app.getPath('userData'), 'wsl-tamer.log') 
    }),
    new winston.transports.Console({ format: winston.format.simple() })
  ]
});
```

**Deliverable:** Working tray icon, settings window, IPC bridge, logging

---

### **PHASE 3: WSL Service Layer (Day 2-3 - 8-10 hours)**

#### 3.1 Core WSL Commands
```typescript
// src/main/services/wsl.service.ts
import { exec } from 'child_process';
import { promisify } from 'util';

const execAsync = promisify(exec);

export class WslService {
  async getDistributions(): Promise<WslDistribution[]> {
    const { stdout } = await execAsync('wsl --list --verbose');
    return this.parseDistributions(stdout);
  }

  async startDistribution(name: string): Promise<void> {
    await execAsync(`wsl -d ${name} --exec exit`);
  }

  async stopDistribution(name: string): Promise<void> {
    await execAsync(`wsl --terminate ${name}`);
  }

  async shutdownAll(): Promise<void> {
    await execAsync('wsl --shutdown');
  }

  async getStatus(): Promise<WslStatus> {
    try {
      const { stdout } = await execAsync('wsl --status');
      return this.parseStatus(stdout);
    } catch (error) {
      return { isInstalled: false };
    }
  }

  async reclaimMemory(distroName?: string): Promise<void> {
    if (distroName) {
      await execAsync(`wsl -d ${distroName} --exec sudo sh -c "echo 3 > /proc/sys/vm/drop_caches"`);
    } else {
      // Reclaim all
      const distros = await this.getDistributions();
      await Promise.all(
        distros
          .filter(d => d.state === 'Running')
          .map(d => this.reclaimMemory(d.name))
      );
    }
  }

  // Import, export, clone, move, unregister methods...
}
```

#### 3.2 Distribution Operations
- Install from Microsoft Store (launch ms-windows-store:// URLs)
- Import from tar
- Export to tar
- Clone distribution
- Move distribution location
- Unregister distribution

#### 3.3 IPC Handlers
```typescript
// src/main/ipc/handlers.ts
import { ipcMain } from 'electron';
import { wslService } from '../services/wsl.service';

export function registerWslHandlers() {
  ipcMain.handle('wsl:getDistributions', async () => {
    return await wslService.getDistributions();
  });

  ipcMain.handle('wsl:start', async (_, name: string) => {
    return await wslService.startDistribution(name);
  });

  // ... register all handlers
}
```

**Deliverable:** Full WSL command functionality working from main process

---

### **PHASE 4: UI Framework (Day 3-4 - 10-12 hours)**

#### 4.1 Base Layout & Navigation
```tsx
// src/renderer/App.tsx
import { useState } from 'react';
import { Sidebar } from './components/Sidebar';
import { GeneralPage } from './pages/GeneralPage';
import { DistributionsPage } from './pages/DistributionsPage';
// ... other pages

export default function App() {
  const [currentPage, setCurrentPage] = useState('general');

  return (
    <div className="flex h-screen bg-gray-50 dark:bg-gray-900">
      <Sidebar currentPage={currentPage} onNavigate={setCurrentPage} />
      <main className="flex-1 overflow-auto">
        {currentPage === 'general' && <GeneralPage />}
        {currentPage === 'distributions' && <DistributionsPage />}
        {currentPage === 'profiles' && <ProfilesPage />}
        {currentPage === 'hardware' && <HardwarePage />}
        {currentPage === 'about' && <AboutPage />}
      </main>
    </div>
  );
}
```

#### 4.2 Component Library Setup
Use shadcn/ui for consistent, modern components:
- Buttons, inputs, selects
- Cards, dialogs, dropdowns
- Tables, tabs, accordions
- Toast notifications
- Loading states

#### 4.3 Theme System
```tsx
// src/renderer/hooks/useTheme.ts
import { useEffect, useState } from 'react';

export function useTheme() {
  const [theme, setTheme] = useState<'light' | 'dark'>('light');

  useEffect(() => {
    // Listen for system theme changes
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
      setTheme(e.matches ? 'dark' : 'light');
    });
  }, []);

  useEffect(() => {
    document.documentElement.classList.toggle('dark', theme === 'dark');
  }, [theme]);

  return { theme, setTheme };
}
```

**Deliverable:** Beautiful, responsive UI shell with navigation

---

### **PHASE 5: Page Implementation (Day 4-5 - 10-12 hours)**

#### 5.1 General Page
```tsx
// src/renderer/pages/GeneralPage.tsx
export function GeneralPage() {
  const { status, loading } = useWslStatus();
  
  return (
    <div className="p-6 space-y-6">
      <section>
        <h2 className="text-2xl font-bold mb-4">WSL Status</h2>
        <StatusCard status={status} loading={loading} />
      </section>

      <section>
        <h2 className="text-2xl font-bold mb-4">Quick Actions</h2>
        <div className="grid grid-cols-2 gap-4">
          <Button onClick={handleMemoryReclaimAll}>
            Memory Reclaim (All)
          </Button>
          <Button onClick={handleShutdown} variant="destructive">
            Shutdown WSL
          </Button>
        </div>
      </section>

      <section>
        <h2 className="text-2xl font-bold mb-4">Settings</h2>
        <StartupSettings />
        <UpdateSettings />
      </section>
    </div>
  );
}
```

#### 5.2 Distributions Page
- Distribution list with cards/table view
- Status indicators (running, stopped)
- Quick actions (start, stop, terminal, file explorer)
- Distribution operations (clone, move, export, unregister)
- Install new distribution dialog

#### 5.3 Profiles Page
- Profile list
- Create/edit/delete profiles
- Profile editor with all .wslconfig options:
  - Memory, processors, swap
  - Networking, kernel, boot command
  - GPU, nested virtualization
  - etc.
- Apply profile to distribution
- Live preview of generated .wslconfig

#### 5.4 Hardware Page
- USB device list with attach/detach
- Physical disk list with mount/unmount
- Device status indicators
- Safety warnings

#### 5.5 About Page
- Current version
- Update checker
- Release notes
- Links (GitHub, docs, license)
- System info

**Deliverable:** All pages fully functional with real data

---

### **PHASE 6: Profile System (Day 5 - 4-6 hours)**

#### 6.1 Profile Service
```typescript
// src/main/services/profile.service.ts
import fs from 'fs/promises';
import path from 'path';
import os from 'os';

export class ProfileService {
  private profilesDir = path.join(app.getPath('userData'), 'profiles');

  async getAllProfiles(): Promise<Profile[]> {
    const files = await fs.readdir(this.profilesDir);
    return Promise.all(
      files
        .filter(f => f.endsWith('.json'))
        .map(f => this.loadProfile(path.basename(f, '.json')))
    );
  }

  async saveProfile(profile: Profile): Promise<void> {
    const filePath = path.join(this.profilesDir, `${profile.name}.json`);
    await fs.writeFile(filePath, JSON.stringify(profile, null, 2));
  }

  async generateWslConfig(profile: Profile): Promise<string> {
    // Generate .wslconfig content from profile settings
    return `[wsl2]
memory=${profile.memory}GB
processors=${profile.processors}
swap=${profile.swap}GB
...`;
  }

  async applyProfile(profileName: string, distroName?: string): Promise<void> {
    const profile = await this.loadProfile(profileName);
    const config = await this.generateWslConfig(profile);
    const wslConfigPath = path.join(os.homedir(), '.wslconfig');
    await fs.writeFile(wslConfigPath, config);
  }
}
```

#### 6.2 Profile Editor UI
- Form with all settings
- Validation
- Real-time preview
- Save/cancel/delete

**Deliverable:** Complete profile management system

---

### **PHASE 7: Hardware Integration (Day 6 - 4-6 hours)**

#### 7.1 USB Service
```typescript
// src/main/services/hardware.service.ts
export class HardwareService {
  async listUsbDevices(): Promise<UsbDevice[]> {
    // Call usbipd wsl list
    const { stdout } = await execAsync('usbipd wsl list');
    return this.parseUsbDevices(stdout);
  }

  async attachUsbDevice(busId: string, distro: string): Promise<void> {
    await execAsync(`usbipd wsl attach --busid ${busId} --distribution ${distro}`);
  }

  async detachUsbDevice(busId: string): Promise<void> {
    await execAsync(`usbipd wsl detach --busid ${busId}`);
  }

  async listPhysicalDisks(): Promise<PhysicalDisk[]> {
    // Use GET-CimInstance or wmic
    const { stdout } = await execAsync(
      'powershell -Command "GET-CimInstance -query \\"SELECT * from Win32_DiskDrive\\" | ConvertTo-Json"'
    );
    return JSON.parse(stdout);
  }

  async mountDisk(diskPath: string, distro: string): Promise<void> {
    await execAsync(`wsl --mount ${diskPath} --distribution ${distro}`);
  }
}
```

#### 7.2 Hardware UI
- Device lists with real-time status
- Attach/detach buttons
- Error handling for permission issues

**Deliverable:** Full hardware management functionality

---

### **PHASE 8: Auto-Update System (Day 6 - 3-4 hours)**

#### 8.1 Update Service
```typescript
// src/main/services/update.service.ts
import { autoUpdater } from 'electron-updater';

export class UpdateService {
  constructor() {
    autoUpdater.checkForUpdatesAndNotify();
    
    autoUpdater.on('update-available', (info) => {
      // Notify renderer
      mainWindow.webContents.send('update:available', info);
    });

    autoUpdater.on('update-downloaded', (info) => {
      // Notify renderer
      mainWindow.webContents.send('update:downloaded', info);
    });
  }

  async checkForUpdates(): Promise<UpdateInfo> {
    return await autoUpdater.checkForUpdates();
  }

  async downloadUpdate(): Promise<void> {
    return await autoUpdater.downloadUpdate();
  }

  quitAndInstall(): void {
    autoUpdater.quitAndInstall();
  }
}
```

#### 8.2 Update UI
- Check for updates button
- Download progress
- Install prompt

**Deliverable:** Working auto-update system

---

### **PHASE 9: Testing & Polish (Day 7 - 6-8 hours)**

#### 9.1 Playwright E2E Tests
```typescript
// tests/e2e/general.spec.ts
import { test, expect } from '@playwright/test';
import { ElectronApplication, _electron as electron } from 'playwright';

test.describe('General Page', () => {
  let app: ElectronApplication;

  test.beforeEach(async () => {
    app = await electron.launch({ args: ['.'] });
  });

  test.afterEach(async () => {
    await app.close();
  });

  test('should display WSL status', async () => {
    const window = await app.firstWindow();
    await expect(window.locator('[data-testid="wsl-status"]')).toBeVisible();
  });

  test('should reclaim memory', async () => {
    const window = await app.firstWindow();
    await window.click('[data-testid="memory-reclaim-all"]');
    await expect(window.locator('[data-testid="success-toast"]')).toBeVisible();
  });
});
```

#### 9.2 Unit Tests
- Service layer tests
- Component tests
- Utility function tests

#### 9.3 UI Polish
- Animations and transitions
- Loading states
- Empty states
- Error states
- Keyboard shortcuts
- Accessibility (ARIA labels)

#### 9.4 Error Handling
- Global error boundary
- Toast notifications
- Retry logic
- Fallback UI

**Deliverable:** Fully tested, polished application

---

### **PHASE 10: Build & Release (Day 7 - 2-3 hours)**

#### 10.1 Installer Configuration
```javascript
// forge.config.ts
module.exports = {
  packagerConfig: {
    name: 'WSL Tamer',
    executableName: 'wsl-tamer',
    icon: './assets/icons/app',
    asar: true,
  },
  makers: [
    {
      name: '@electron-forge/maker-squirrel',
      config: {
        name: 'wsl_tamer',
        setupIcon: './assets/icons/app.ico',
      },
    },
    {
      name: '@electron-forge/maker-zip',
      platforms: ['darwin', 'win32'],
    },
  ],
  publishers: [
    {
      name: '@electron-forge/publisher-github',
      config: {
        repository: {
          owner: 'ryan-haver',
          name: 'wsl-tamer',
        },
        prerelease: false,
      },
    },
  ],
};
```

#### 10.2 GitHub Actions CI/CD
```yaml
# .github/workflows/build.yml
name: Build and Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
        with:
          node-version: 20
      - run: npm install
      - run: npm run test
      - run: npm run make
      - name: Release
        uses: softprops/action-gh-release@v1
        with:
          files: out/make/**/*
```

#### 10.3 Code Signing
- Set up code signing certificate
- Configure signing in forge config

**Deliverable:** Signed, installable MSI/EXE

---

## 🔄 Migration Strategy: Running Both Apps in Parallel

### Week 1: Parallel Testing
```
Day 1-7: Run WPF v1.x and Electron v2.0.0-beta side-by-side
- Test all features in Electron
- Compare behavior
- Fix any discrepancies
- User acceptance testing
```

### Week 2: Soft Launch
```
Day 8-14: Release Electron v2.0.0-beta
- GitHub pre-release
- Beta testers feedback
- Bug fixes
- Keep WPF as fallback
```

### Week 3: Full Release
```
Day 15+: Release Electron v2.0.0 stable
- Deprecate WPF version
- Migration notice in WPF app
- Archive WPF codebase
- Focus on Electron roadmap
```

---

## 📦 Dependency List

```json
{
  "dependencies": {
    "electron": "^28.0.0",
    "electron-updater": "^6.1.7",
    "electron-store": "^8.1.0",
    "react": "^18.2.0",
    "react-dom": "^18.2.0",
    "winston": "^3.11.0",
    "classnames": "^2.3.2",
    "date-fns": "^3.0.0"
  },
  "devDependencies": {
    "@electron-forge/cli": "^7.2.0",
    "@electron-forge/maker-squirrel": "^7.2.0",
    "@electron-forge/maker-zip": "^7.2.0",
    "@electron-forge/plugin-webpack": "^7.2.0",
    "@playwright/test": "^1.40.0",
    "@types/react": "^18.2.45",
    "@types/react-dom": "^18.2.18",
    "typescript": "^5.3.3",
    "tailwindcss": "^3.4.0",
    "postcss": "^8.4.32",
    "autoprefixer": "^10.4.16",
    "eslint": "^8.56.0",
    "prettier": "^3.1.1"
  }
}
```

---

## 🎨 UI Design System

### Colors (Windows 11 Fluent inspired)
```css
/* Light theme */
--bg-primary: #ffffff
--bg-secondary: #f5f5f5
--text-primary: #1f1f1f
--text-secondary: #605e5c
--accent: #0078d4
--success: #107c10
--warning: #ff8c00
--danger: #d13438

/* Dark theme */
--bg-primary: #1f1f1f
--bg-secondary: #2d2d2d
--text-primary: #ffffff
--text-secondary: #c8c6c4
--accent: #60cdff
--success: #6ccb5f
--warning: #faa06b
--danger: #f1707b
```

### Typography
```
Headings: Segoe UI Variable Display
Body: Segoe UI Variable Text
Monospace: Cascadia Code
```

### Components
- Cards with subtle shadows
- Rounded corners (8px)
- Smooth transitions (200ms)
- Hover states
- Focus indicators

---

## 🚀 Long-Term Roadmap (Post-Migration)

### v2.1.0 - Enhanced Features
- [ ] Distribution templates
- [ ] Backup/restore system
- [ ] Scheduled tasks (auto shutdown, memory reclaim)
- [ ] Performance monitoring dashboard
- [ ] Custom themes

### v2.2.0 - Advanced Workflows
- [ ] Bulk operations
- [ ] Distribution groups
- [ ] Snapshot system
- [ ] Export/import app settings
- [ ] CLI companion tool

### v2.3.0 - Cloud Integration
- [ ] OneDrive backup sync
- [ ] Profile sharing
- [ ] Team/enterprise features
- [ ] Remote WSL management

### v3.0.0 - Cross-Platform
- [ ] macOS support (for Docker Desktop WSL backend)
- [ ] Linux support (for WSL on Windows Server)
- [ ] Web dashboard (optional)

---

## 📋 Success Criteria

### Feature Parity Checklist
- [ ] All WPF features working in Electron
- [ ] Same or better performance
- [ ] No regressions
- [ ] Better UI/UX
- [ ] Automated tests passing

### Quality Gates
- [ ] Zero critical bugs
- [ ] <5 minor bugs
- [ ] 80%+ test coverage
- [ ] Lighthouse score >90
- [ ] Load time <2s

### User Acceptance
- [ ] Beta tester approval
- [ ] GitHub issues resolved
- [ ] Documentation complete
- [ ] Migration guide ready

---

## ⚠️ Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| WSL commands behave differently | Extensive testing, same commands as WPF |
| Performance issues | Optimize IPC, lazy load data, virtual lists |
| Windows-only APIs missing | Use native Node.js modules, PowerShell fallback |
| Update system breaks | Thorough testing, staged rollout |
| Migration bugs | Keep WPF available for 2 weeks as fallback |

---

## 📞 Support During Migration

### Development Environment
- Node.js 20.x LTS
- npm 10.x
- Windows 11 (primary target)
- WSL 2 with test distributions

### Communication
- Daily progress updates
- Feature demos
- Issue tracking in GitHub
- Beta testing feedback loop

---

## 🎯 Next Steps

1. **You approve this plan** ✅
2. **I create the Electron project** (30 mins)
3. **We start Phase 1** (Day 1 begins)
4. **Daily iterations with visual feedback** (you can see and test everything)
5. **Full feature parity in 5-7 days** 🚀

---

**Ready to start? Say the word and I'll scaffold the Electron project structure right now.**
