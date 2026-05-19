# Yassir Diagno

**Thermal & Hardware Monitoring application for Windows.**

> Diagnostic · Scan · PC Monitor

A modern WPF .NET 8 monitoring tool that competes with HWiNFO, AIDA64, and HWMonitor.
Light/Dark themes, real-time charts, per-core CPU breakdown, stress test, CSV logging,
diagnostic reports, and Windows toast notifications on critical temperatures.

---

## ✨ Features

### 8 sections, all functional
- **Accueil (Dashboard)** — 6 cards with live sparklines (CPU, GPU, Power, SSD, Battery, Santé globale)
- **Système** — OS, version, RAM usage, BIOS info, machine model, uptime
- **Matériel** — Per-core CPU breakdown (load + clock per core), full hardware inventory
- **Performance** — Full-size historical charts (CPU/GPU/SSD/Power/Battery) with min/max/avg stats and 60s/5min/10min windows
- **Processus** — Top processes by CPU/RAM with kill button (refresh every 2s)
- **Outils** — Quick scan diagnostic + CPU stress test (configurable duration)
- **Rapports** — Generate report, export TXT/HTML
- **Paramètres** — Theme toggle, polling interval, sparklines, notifications, auto-start, CSV logging

### Hardware Monitoring
- CPU silicon temperature (Tctl/Tdie/Package)
- CPU power consumption (W)
- CPU per-core load + clock frequency
- GPU temperature (ACPI thermal zone) + load
- SSD composite temperature
- Chassis (ACPI external zone) temperature
- Battery charge level + health + status
- ACPI thermal zones (CPU/GPU/Chassis)

### Smart Features
- 🌙 **Dark / Light themes** with instant switching
- 📌 **System tray** integration (minimize/run in background)
- 🔔 **Toast notifications** when CPU ≥ 85°C or SSD ≥ 75°C (5-min cooldown)
- 💾 **CSV logging** for long-term sensor history
- ⚙️ **Auto-start with Windows** (registry-based)
- 📊 **Santé globale score** computed from weighted thresholds
- 🎯 **Diagnostic engine** with severity-coded findings + recommendations
- 📄 **Report export** to TXT or styled HTML

---

## 🛠 Tech Stack

- **.NET 8** + WPF (Windows Desktop)
- **C# 12** with nullable + implicit usings
- **MVVM** with [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) (ObservableProperty, RelayCommand source generators)
- **Dependency Injection** via Microsoft.Extensions.Hosting
- **Sensors**: [LibreHardwareMonitorLib](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) (NuGet 0.9.6)
- **Tray icon**: [H.NotifyIcon.Wpf](https://github.com/HavenDV/H.NotifyIcon)
- **Toasts**: Microsoft.Toolkit.Uwp.Notifications
- **WMI** via System.Management

---

## 📂 Project Structure

```
YassirDiagno/
├── src/YassirDiagno/                  # Main WPF app
│   ├── Models/                        # Records (SensorReading, HardwareSnapshot, etc.)
│   ├── ViewModels/                    # MVVM view-models
│   ├── Views/                         # UserControls + Windows
│   ├── Services/                      # IServices + implementations
│   │   └── Hardware/                  # Hardware monitoring layer
│   ├── Resources/Themes/              # Light + Dark theme dictionaries
│   ├── Resources/Icons/               # App icon (multi-resolution .ico)
│   ├── Converters/                    # WPF value converters
│   └── app.manifest                   # requireAdministrator + PerMonitorV2 DPI
├── installer/                          # WiX MSI installer
│   ├── YassirDiagno.wxs               # WiX 5 package definition
│   └── Build-Installer.ps1            # Publish + harvest + build script
├── assets/                             # Logos, generate-icon.ps1
├── tests/                              # (placeholder for unit tests)
└── docs/                               # (placeholder for documentation)
```

---

## 🚀 Build from source

### Prerequisites
- .NET 8 SDK
- Windows 10 1809 (10.0.17763) or later
- Admin privileges (required for hardware sensor access via WinRing0 driver)

### Run
```powershell
dotnet build src/YassirDiagno
dotnet run --project src/YassirDiagno
```

### Build installer
```powershell
dotnet tool install --global wix --version 5.0.2
wix extension add -g WixToolset.UI.wixext/5.0.2
.\installer\Build-Installer.ps1
```
Output: `installer/out/YassirDiagno-1.0.0-x64.msi` (~80 MB self-contained)

---

## 📋 Roadmap

- [ ] Multi-language: FR (default), EN, AR
- [ ] LiveCharts2 fancy graphs (replace Polyline) in Performance page
- [ ] Custom alert thresholds in Settings
- [ ] Cloud sync of history (optional Pro feature)
- [ ] Code-signed installer (Authenticode certificate)
- [ ] Microsoft Store listing
- [ ] Auto-update mechanism

---

## ⚖️ License

This project bundles [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) which is licensed under MPL-2.0 + portions under GPL.
The application itself is the work of **Yassir Mellakh** (2026).

For commercial distribution, the GPL-bound LibreHardwareMonitor portions will be replaced with a compatible alternative (planned).

---

## 🙏 Acknowledgements

- [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) — sensor reading
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) — MVVM source generators
- [H.NotifyIcon.Wpf](https://github.com/HavenDV/H.NotifyIcon) — tray icon
- [WiX Toolset](https://wixtoolset.org/) — MSI installer
