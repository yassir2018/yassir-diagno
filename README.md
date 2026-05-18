# Yassir Diagno

Thermal & Hardware Monitoring application for Windows.

**Tagline**: Diagnostic · Scan · PC Monitor

## Status

🚧 **In development** — v1.0 (commercial WPF rewrite from PowerShell MVP)

## Tech Stack

- **Framework**: .NET 8 + WPF
- **Architecture**: MVVM (Model-View-ViewModel)
- **Language**: C#
- **UI Design**: Light theme, modern card-based dashboard
- **Sensor Library**: TBD (LibreHardwareMonitor → OpenHardwareMonitor migration planned)

## Project Structure

```
YassirDiagno/
├── src/                      # Source code
│   └── YassirDiagno/         # Main WPF app
├── tests/                    # Unit + integration tests
├── installer/                # WiX MSI installer
├── assets/                   # Logos, icons, mockups
├── docs/                     # Documentation
└── .github/workflows/        # CI/CD pipelines
```

## Build

```powershell
dotnet build src/YassirDiagno
dotnet run --project src/YassirDiagno
```

## License

TBD — pending decision on sensor library and business model.
