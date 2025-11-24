# 🦁 WSL Tamer

**Tame the beast that is Windows Subsystem for Linux.**

WSL Tamer is a lightweight system tray application designed to give you full control over WSL2's resource usage. No more manually editing .wslconfig files or running PowerShell commands to shut down runaway instances.

## 🚀 Features

### 🧠 Smart Resource Profiles

Switch between profiles instantly without restarting Windows:

* **🍃 Eco Mode:** Caps RAM at 4GB, limits CPU. Great for background tasks or battery life.
* **⚖️ Balanced:** The sweet spot (e.g., 8GB - 12GB). Perfect for daily development.
* **🔥 Unleashed:** Unlocks full system resources for heavy compilation or ML tasks.

### 🤖 Automation & Triggers

* **Process Triggers:** Automatically switch profiles when you launch specific apps (e.g., switch to "Unleashed" when `code.exe` starts).
* **Power Triggers:** Automatically switch to "Eco Mode" when on battery power.

### 🐧 Distro Management

* **Dashboard:** View all installed distributions and their running state.
* **Control:** Launch, Stop, or Set Default distributions directly from the UI.

### ⚡ Quick Actions

* **Start/Stop WSL:** One-click shutdown to free up resources immediately.
* **Reclaim Memory:** Force Linux to drop caches and return RAM to Windows.
* **Compact Disk:** Shrink the .vhdx virtual disk file to reclaim disk space.

### 🖥️ System Tray Integration

* **Dynamic Icon:** Visual indicator of WSL state (Green = Running, Gray = Stopped).
* **Right-click context menu:** For all actions.
* **Auto-start:** Option to start automatically with Windows.

## 🛠️ Tech Stack

* **Language:** C# / .NET 8 (WPF)
* **Integration:** Interacts directly with wsl.exe and ~/.wslconfig.

## 📦 Installation

1. Go to the [Releases](https://github.com/ryan-haver/wsl-tamer/releases) page.
2. Download the latest `WslTamer.msi`.
3. Run the installer.

## 🗺️ Roadmap

Check out our [ROADMAP.md](ROADMAP.md) to see what's planned for future releases.

## 🤝 Contributing

Contributions are welcome!
