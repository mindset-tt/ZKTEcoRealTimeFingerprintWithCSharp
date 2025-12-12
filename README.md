# ZKTeco Real-Time Attendance Monitor

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20x86-blue)](https://github.com/)

Real-time attendance monitoring system for ZKTeco fingerprint devices with multi-database support.

---

## 📥 Download

| Version | Download | Description |
|---------|----------|-------------|
| **v2.0.0 Release** | [⬇️ ZKTecoAttendance-Setup.msi](../../releases/latest/download/ZKTecoAttendance-Setup.msi) | ✅ Recommended for production |
| **v2.0.0 Debug** | [⬇️ ZKTecoAttendance-Debug.msi](../../releases/latest/download/ZKTecoAttendance-Debug.msi) | 🔧 For troubleshooting (verbose logging) |

> **System Requirements:** Windows 10/11 (x86 or x64)  
> **Note:** The installer includes all dependencies including `zkemkeeper.dll`

---

## ✨ Features

| Feature | Description |
|---------|-------------|
| 🔄 **Real-time Monitoring** | Instant attendance capture as events happen |
| 📡 **Multi-Device** | Connect multiple ZKTeco devices simultaneously |
| 💾 **Multi-Database** | PostgreSQL, MySQL, SQL Server, SQLite, Oracle |
| 🖥️ **Dual Mode** | Windows Service or Console application |
| 📝 **File Logging** | Automatic daily log rotation |
| ⚙️ **Easy Setup** | Simple `.env` configuration file |

---

## 🚀 Quick Start

### Step 1: Install

1. Download the MSI installer above
2. Double-click to install
3. Follow the installation wizard

**Default path:** `C:\Program Files (x86)\ZKTeco Attendance\`

### Step 2: Configure

Open `.env` in the installation folder and configure:

```env
# === Device Configuration ===
DEVICE_IPS=192.168.1.201,192.168.1.202
DEVICE_PORT=4370

# === Database (enable at least one) ===
ENABLE_POSTGRESQL=true
POSTGRESQL_HOST=localhost
POSTGRESQL_PORT=5432
POSTGRESQL_DATABASE=attendance
POSTGRESQL_USER=postgres
POSTGRESQL_PASSWORD=your_password
```

### Step 3: Run

**Console Mode (for testing):**
```cmd
ZKTecoRealTimeLog.exe --console
```

**Windows Service:**
```cmd
net start ZKTecoAttendance
```

---

## 📊 Database Schema

The application auto-creates this table:

```sql
CREATE TABLE machine_attendance_logs (
    id SERIAL PRIMARY KEY,
    user_id VARCHAR(50),
    log_time TIMESTAMP,
    device_ip VARCHAR(50),
    verify_mode INT,
    in_out_mode INT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

---

## 📋 Supported Devices

| Series | Models |
|--------|--------|
| K Series | K40, K20, K14 |
| iClock | iClock 560, iClock 880 |
| SpeedFace | SpeedFace-V5L |
| ProFace | ProFace-X |
| uFace | uFace 800 |
| Others | F18, F22, IN01-A, and zkemkeeper-compatible devices |

---

## ⚙️ Configuration

| Variable | Default | Description |
|----------|---------|-------------|
| `DEVICE_IPS` | `192.168.1.201` | Comma-separated device IPs |
| `DEVICE_PORT` | `4370` | Connection port |
| `ENABLE_POSTGRESQL` | `false` | Enable PostgreSQL logging |
| `ENABLE_MYSQL` | `false` | Enable MySQL logging |
| `ENABLE_SQLSERVER` | `false` | Enable SQL Server logging |
| `ENABLE_SQLITE` | `false` | Enable SQLite logging |
| `ENABLE_ORACLE` | `false` | Enable Oracle logging |

See `.env.example` for complete configuration options.

---

## 📁 File Structure

```
📦 ZKTeco Attendance
├── 📄 ZKTecoRealTimeLog.exe    # Main application
├── 📄 zkemkeeper.dll           # ZKTeco SDK
├── 📄 .env                     # Configuration
├── 📄 .env.example             # Configuration template
├── 📁 logs/                    # Log files
│   └── 📄 attendance_YYYYMMDD.log
└── 📁 data/                    # SQLite database (if used)
```

---

## 🔧 Troubleshooting

### Device Connection Issues
- ✅ Verify device IP is correct
- ✅ Check firewall allows port 4370
- ✅ Ping the device: `ping 192.168.1.201`
- ✅ Ensure device is powered on

### Service Won't Start
- ✅ Check Event Viewer for errors
- ✅ Run console mode first: `ZKTecoRealTimeLog.exe --console`
- ✅ Verify `.env` configuration

### Database Errors
- ✅ Verify database server is running
- ✅ Check connection credentials
- ✅ Ensure user has CREATE TABLE permission

---

## 🛠️ Building from Source

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [WiX Toolset v4](https://wixtoolset.org/) (for MSI)

### Build Commands

```powershell
# Clone
git clone https://github.com/YOUR_USERNAME/zkteco-attendance.git
cd zkteco-attendance

# Build Release & Debug MSI packages
.\build_release.bat
```

---

## 📄 License

MIT License - see [LICENSE](LICENSE) file.

---

## 🤝 Contributing

Contributions welcome! Please open a Pull Request.

---

## 📧 Support

For issues: [Open an Issue](../../issues/new)

---

Made with ❤️ by [Mlfts](https://github.com/Mlfts)
