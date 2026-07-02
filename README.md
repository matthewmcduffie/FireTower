# FireTower

A native Windows utility for monitoring, recovering, and automatically restarting virtual machines.

---

## The Problem

Running mission-critical shipping software — UPS WorldShip, FedEx Ship Manager, and similar — inside virtual machines is common practice. It works well until it doesn't. An unscheduled Windows update, a power interruption, or an unexpected crash takes the VM down. The shipping operation stops. Someone has to notice, find a keyboard, and manually bring it back up.

FireTower fixes that. It watches your virtual machines and brings them back automatically when they go down, whether you are in the building or not.

---

## What It Does

- Monitors VirtualBox (more cominig) virtual machines continuously
- Detects when a VM stops running for any reason
- Restarts it automatically, without any manual intervention
- Runs as a Windows Service — starts at boot, keeps running when no one is logged in
- Includes a management tray application for configuration, discovery, logs, and status

---

## Requirements

- Windows 10 or Windows 11 (64-bit)
- [Oracle VirtualBox](https://www.virtualbox.org/) installed with VBoxManage accessible
- [.NET 8 Windows Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) — required if using the framework-dependent installer; not required if using the self-contained build

---

## Installation

1. Download the latest MSI installer from the [Releases](../../releases) page
2. Run the installer as Administrator (Windows may complain)
3. When installation completes, the FireTower should launch for configuration
4. If the tray application shows **"Service Unavailable"** on first launch, open **Windows Services**, find **FireTower VM Watchdog**, and start it manually — it will start automatically on every subsequent boot

---

## Usage

### Adding Virtual Machines

1. Open FireTower from the system tray or Start Menu
2. Go to **Discovery** — FireTower scans VirtualBox and lists all available virtual machines
3. Check the machines you want monitored and click **Add to Monitoring**
4. Navigate to **Virtual Machines** to confirm they appear and begin monitoring

### Manual Controls

Select a VM in the Virtual Machines page to access **Start**, **Stop**, **Restart**, and **Force Restart** buttons. These send commands directly to VirtualBox via the service.

### Recovery Behavior

By default, FireTower detects a stopped VM within 10 seconds and restarts it immediately. If a VM keeps crashing, it retries with force restart on the second attempt. Recovery behavior is configurable per VM through Health Profiles and Recovery Profiles in the Settings page.

### Configuration

All settings are stored in `C:\ProgramData\FireTower\config\` as JSON files and can be edited directly or through the Settings page in the tray application.

---

## Building From Source

```powershell
# Development (service + tray, both in one command)
.\Start-Dev.ps1

# Release MSI
.\Build-Release.ps1 -Version 1.0.0
```

Requires .NET 8 SDK and WiX Toolset v5 (`dotnet tool install --global wix`).

---

## License

Copyright (c) 2026 Matthew McDuffie

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

**The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.**

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).
