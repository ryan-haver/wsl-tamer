# 🦁 WSL Tamer

**Tame the beast that is Windows Subsystem for Linux.**

WSL Tamer is a lightweight system tray application designed to give you full control over WSL2's resource usage. No more manually editing .wslconfig files or running PowerShell commands to shut down runaway instances.

## 🚀 Features

### 🧠 Memory & CPU Presets

Switch between profiles instantly without restarting Windows:

* **🍃 Eco Mode:** Caps RAM at 4GB, limits CPU. Great for background tasks or battery life.
* **⚖️ Balanced:** The sweet spot (e.g., 8GB - 12GB). Perfect for daily development.
* **🔥 Unleashed:** Unlocks full system resources for heavy compilation or ML tasks.

### ⚡ Quick Actions

* **Start/Stop WSL:** One-click shutdown to free up resources immediately.
* **Reclaim Memory:** Force Linux to drop caches and return RAM to Windows.
* **Compact Disk:** Shrink the .vhdx virtual disk file to reclaim disk space.

### 🖥️ System Tray Integration

* **Live status indicator:** (Running/Stopped).
* **Right-click context menu:** For all actions.
* **Auto-start:** With Windows.

## 🛠️ Tech Stack

* **Language:** C# / .NET 8 (WPF or WinForms for Tray)
* **Integration:** Interacts directly with wsl.exe and ~/.wslconfig.

## 📦 Installation

*(Coming Soon)*

## 🤝 Contributing

Contributions are welcome!
