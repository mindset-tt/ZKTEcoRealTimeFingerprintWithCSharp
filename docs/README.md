# ZKTeco Real-Time Attendance Monitor

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20x86-blue)](https://github.com/)

A robust, enterprise-grade real-time attendance monitoring system for ZKTeco fingerprint devices. Designed for **24/7 continuous operation** with automatic connection recovery.

---

## Download

| Version | Description |
|---------|-------------|
| **v2.0.0 Release** | **Recommended**. Optimized for production use. |
| **v2.0.0 Debug** | Includes verbose logging for troubleshooting. |

> **System Requirements:** Windows 10/11 (x86 or x64)  
> **Dependencies:** The installer automatically registers `zkemkeeper.dll`.

---

## Key Features

| Feature | Description |
|---------|-------------|
| **24/7 Stability** | Built-in Watchdog checks connections every 30s and auto-reconnects if devices go offline. |
| **Real-time Monitoring** | Captures attendance events instantly as they happen. |
| **Multi-Device** | Connect to unlimited ZKTeco devices simultaneously. |
| **Multi-Database** | Supports PostgreSQL, MySQL, SQL Server, SQLite, and Oracle. |
| **Windows Service** | Runs silently in the background, starting automatically with Windows. |
| **Comprehensive Logging** | Logs connection events, database errors, and attendance checks to `logs/` folder. |
| **Clean Uninstall** | Uninstaller automatically cleans up all logs and data files. |

---

## Security & Antivirus

The application is safe and open-source, but because it is **not digitally signed** (signing requires a paid certificate):

1. **Windows SmartScreen** may warn you ("Windows protected your PC"). Click **More info -> Run anyway**.
2. **Antivirus** may flag it as "Unknown" or "Heuristic" because it is a new program that creates a Windows Service.
   - **Solution:** It is a False Positive. You can add an exception for `C:\Program Files (x86)\ZKTeco Attendance\` if needed.
   - **Verification:** You can review the source code yourself to confirm it is safe.

---

## Installation Guide

### Step 1: Install
1. Run `ZKTecoAttendance-Setup.msi`.
2. Follow the wizard.
3. The installer will automatically:
   - Install the application to `C:\Program Files (x86)\ZKTeco Attendance`.
   - Register the ZKTeco SDK.
   - Create a Desktop shortcut.

### Step 2: Configure
Open the `.env` file in the installation folder and configure your devices:

```env
# === Device Configuration ===
DEVICE_1_ENABLED=true
DEVICE_1_NAME=FrontDoor
DEVICE_1_IP=192.168.1.201
DEVICE_1_PORT=4370

# === Database (Optional) ===
POSTGRES_ENABLED=true
```

### Step 3: Run
The application runs automatically as a **Windows Service**.
- **To Start/Stop:** Use "Services" (`services.msc`) -> Find "ZKTeco Attendance Service".
- **To View Logs:** Open `C:\Program Files (x86)\ZKTeco Attendance\logs\`.

---

## Building from Source

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [WiX Toolset v4](https://wixtoolset.org/) (Run `dotnet tool install --global wix`)

### Build Instructions
1. **Prepare Icon**: Place a valid PNG image at `image/zkteco.png`.
2. **Run Build Script**:
   ```cmd
   scripts\build_release.bat
   ```

---

## Troubleshooting

### Device Disconnected?
Check `logs/zkteco_YYYY-MM-DD.log`. You should see:
> `[Watchdog] Connection lost to FrontDoor. Reconnecting...`
> `[Watchdog] Reconnected FrontDoor!`

### Service Won't Start?
1. Run as Administrator.
2. Check if port 4370 is open.
3. Verify `.env` syntax.

### No Icon?
Ensure `image/zkteco.png` is a **valid PNG file**, not a renamed WebP/JPEG.

---

## License
MIT License.
