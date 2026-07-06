# ShippingGuard

A Windows service that keeps shipping software running inside a virtual machine.

ShippingGuard monitors configured applications — UPS WorldShip, FedEx Ship Manager, or any other process — and restarts them automatically if they stop or hang. It also detects known blocking dialogs and clicks the right button so the application continues without manual intervention.

---

## How It Works

ShippingGuard installs as a Windows Service and starts automatically when the VM boots. It watches each configured application by process name. If the process disappears it relaunches it. If a window stops responding it kills and relaunches it. If a known dialog appears it handles it according to the configured rules.

The tray application provides a live status view, manual controls, and a simple interface for adding applications to monitor.

---

## Requirements

- Windows 10 or Windows 11 (64-bit)
- The MSI installer includes the .NET runtime — no separate installation required

---

## Installation

1. Run `ShippingGuard-x.x.x.msi` as Administrator
2. After installation the ShippingGuard tray app opens
3. Click **+ Add Application** to configure the first app to monitor

---

## Adding an Application

Click **+ Add Application** in the tray window. Fill in:

- **Display Name** — a friendly name shown in the status list
- **Process Name** — the exe name without `.exe` (e.g. `WorldShip`)
- **Launch Command** — the full command used to start the application, or click **Browse** to find the exe

ShippingGuard will start the application if it is not running and restart it whenever it stops.

---

## Profiles

Each monitored application has a JSON profile stored in:

```
C:\ProgramData\ShippingGuard\profiles\
```

Profiles can be edited directly. Sample profiles for UPS WorldShip and FedEx Ship Manager are included in the `profiles/` directory of this repository as templates.

Each profile supports dialog rules that tell ShippingGuard how to handle specific windows that might block the application. Rules match by window title or visible text and click a named button. Unknown dialogs are logged but not touched.

See `docs/profiles.md` for the full profile reference.

---

## Building From Source

```powershell
# Run the agent and tray locally for testing
cd shippingguard
dotnet run --project src\ShippingGuard.Agent\ShippingGuard.Agent.csproj
# (in a second terminal)
dotnet run --project src\ShippingGuard.Tray\ShippingGuard.Tray.csproj

# Build the MSI installer
.\Build-Release.ps1 -Version 1.0.0
```

Requires .NET 8 SDK and WiX Toolset v5 (`dotnet tool install --global wix`).

---

## License

Copyright (c) 2026 Matthew McDuffie. MIT License — see the root `LICENSE` file.
