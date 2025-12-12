# ZKTEco K40 Real-Time Log Monitor

A C# application that captures real-time attendance events from **multiple ZKTeco K40 fingerprint devices** and logs them to **multiple databases simultaneously**. Runs as a **Windows Service** or console application.

## Features

- ✅ **Windows Service** - Run as a background service that starts automatically
- ✅ **Multi-device support** - Connect to multiple ZKTeco devices on the same network
- ✅ **Real-time event monitoring** - Captures attendance events as they happen from all devices
- ✅ **Multi-database support** - Log to multiple databases at once (PostgreSQL, MySQL, SQL Server, SQLite, Oracle)
- ✅ **Unified attendance** - All devices record to the same database with device identification
- ✅ **File logging** - Daily rotating log files
- ✅ **Configurable via .env file** - Easy configuration without code changes
- ✅ **Event types** - Fingerprint, card swipe, verification success/failure

## Prerequisites

### 1. ZKTeco SDK (zkemkeeper.dll)

Download the ZKTeco SDK from [ZKTeco website](https://www.zkteco.com/) or your device documentation.

Register the COM component (run as Administrator):
```cmd
regsvr32 zkemkeeper.dll
```

### 2. .NET 9.0 SDK

Download from [Microsoft .NET](https://dotnet.microsoft.com/download/dotnet/9.0)

### 3. Database (Optional)

Install and configure any databases you want to use:
- **PostgreSQL**: [Download](https://www.postgresql.org/download/)
- **MySQL/MariaDB**: [Download MySQL](https://dev.mysql.com/downloads/) or [MariaDB](https://mariadb.org/download/)
- **SQL Server**: [Download](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- **SQLite**: No installation required (file-based)
- **Oracle**: [Download](https://www.oracle.com/database/technologies/oracle-database-software-downloads.html)

## Installation

1. Clone or download this repository
2. Copy `.env.example` to `.env`
3. Configure your settings in `.env`
4. Build and run:

```cmd
dotnet build
dotnet run
```

## Configuration

### Multi-Device Setup

Connect to **multiple devices** on the same network. When a user scans on any device, attendance is recorded to all enabled databases:

```env
# Device 1 - Main Entrance
DEVICE_1_ENABLED=true
DEVICE_1_NAME=Main Entrance
DEVICE_1_IP=192.168.1.201
DEVICE_1_PORT=4370

# Device 2 - Back Door
DEVICE_2_ENABLED=true
DEVICE_2_NAME=Back Door
DEVICE_2_IP=192.168.1.202
DEVICE_2_PORT=4370

# Device 3 - Office
DEVICE_3_ENABLED=true
DEVICE_3_NAME=Office
DEVICE_3_IP=192.168.1.203
DEVICE_3_PORT=4370

# Add more devices: DEVICE_4_*, DEVICE_5_*, etc. (up to 20 devices)
```

### Multi-Database Setup

You can enable **multiple databases simultaneously**! Each database has its own enable flag:

```env
# Enable all 3 databases at once
POSTGRES_ENABLED=true
MYSQL_ENABLED=true
SQLITE_ENABLED=true

# PostgreSQL settings
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
POSTGRES_DATABASE=zkteco
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_password

# MySQL settings
MYSQL_HOST=localhost
MYSQL_PORT=3306
MYSQL_DATABASE=zkteco
MYSQL_USER=root
MYSQL_PASSWORD=your_password

# SQLite settings (no server required)
SQLITE_DATABASE=attendance.db
```

### All Configuration Options

#### Multi-Device Settings
| Variable | Description | Default |
|----------|-------------|---------|
| `DEVICE_N_ENABLED` | Enable device N (true/false) | `true` |
| `DEVICE_N_NAME` | Device friendly name | `Device N` |
| `DEVICE_N_IP` | Device IP address | (required) |
| `DEVICE_N_PORT` | Device port | `4370` |

*N = 1, 2, 3... up to 20*

#### Legacy Single Device (backward compatible)
| Variable | Description | Default |
|----------|-------------|---------|
| `ZKTECO_IP` | Device IP address | `192.168.1.201` |
| `ZKTECO_PORT` | Device port | `4370` |

#### File Logging
| Variable | Description | Default |
|----------|-------------|---------|
| `LOG_FILE_PATH` | Log file path | `logs/zkteco_YYYYMMDD.log` |

#### PostgreSQL
| Variable | Description | Default |
|----------|-------------|---------|
| `POSTGRES_ENABLED` | Enable PostgreSQL | `false` |
| `POSTGRES_HOST` | Server hostname | `localhost` |
| `POSTGRES_PORT` | Server port | `5432` |
| `POSTGRES_DATABASE` | Database name | `zkteco` |
| `POSTGRES_USER` | Username | `postgres` |
| `POSTGRES_PASSWORD` | Password | (empty) |
| `POSTGRES_CONNECTION_STRING` | Full connection string (optional, overrides above) | (empty) |

#### MySQL / MariaDB
| Variable | Description | Default |
|----------|-------------|---------|
| `MYSQL_ENABLED` | Enable MySQL | `false` |
| `MYSQL_HOST` | Server hostname | `localhost` |
| `MYSQL_PORT` | Server port | `3306` |
| `MYSQL_DATABASE` | Database name | `zkteco` |
| `MYSQL_USER` | Username | `root` |
| `MYSQL_PASSWORD` | Password | (empty) |
| `MYSQL_CONNECTION_STRING` | Full connection string (optional) | (empty) |

#### SQL Server
| Variable | Description | Default |
|----------|-------------|---------|
| `SQLSERVER_ENABLED` | Enable SQL Server | `false` |
| `SQLSERVER_HOST` | Server hostname | `localhost` |
| `SQLSERVER_PORT` | Server port | `1433` |
| `SQLSERVER_DATABASE` | Database name | `zkteco` |
| `SQLSERVER_USER` | Username | `sa` |
| `SQLSERVER_PASSWORD` | Password | (empty) |
| `SQLSERVER_CONNECTION_STRING` | Full connection string (optional) | (empty) |

#### SQLite
| Variable | Description | Default |
|----------|-------------|---------|
| `SQLITE_ENABLED` | Enable SQLite | `false` |
| `SQLITE_DATABASE` | Database file path | `zkteco.db` |
| `SQLITE_CONNECTION_STRING` | Full connection string (optional) | (empty) |

#### Oracle
| Variable | Description | Default |
|----------|-------------|---------|
| `ORACLE_ENABLED` | Enable Oracle | `false` |
| `ORACLE_HOST` | Server hostname | `localhost` |
| `ORACLE_PORT` | Server port | `1521` |
| `ORACLE_DATABASE` | Service name | `ORCL` |
| `ORACLE_USER` | Username | `system` |
| `ORACLE_PASSWORD` | Password | (empty) |
| `ORACLE_CONNECTION_STRING` | Full connection string (optional) | (empty) |

## Usage

### Run as Console Application (for testing)

```cmd
# Run in console mode (interactive)
dotnet run -- --console

# Or using the built executable
ZKTecoRealTimeLog.exe --console

# Show help
dotnet run -- --help

# Show supported database types
dotnet run -- --db-types
```

### Install as Windows Service

Run these commands as **Administrator**:

```cmd
# Build the application
dotnet publish -c Release -o C:\Services\ZKTecoAttendance

# Copy your .env file to the service folder
copy .env C:\Services\ZKTecoAttendance\

# Create the Windows Service
sc create "ZKTeco Attendance" binPath= "C:\Services\ZKTecoAttendance\ZKTecoRealTimeLog.exe" start= auto

# Start the service
sc start "ZKTeco Attendance"

# Check service status
sc query "ZKTeco Attendance"
```

### Manage the Windows Service

```cmd
# Stop the service
sc stop "ZKTeco Attendance"

# Restart the service
sc stop "ZKTeco Attendance" && sc start "ZKTeco Attendance"

# Delete the service (when no longer needed)
sc delete "ZKTeco Attendance"

# View logs in Event Viewer
eventvwr.msc
# Navigate to: Windows Logs > Application > Source: ZKTeco Attendance
```

### Example Console Output

```
===========================================
   ZKTeco K40 Real-Time Log Monitor
   Console Mode
===========================================

Press Ctrl+C to stop...

info: ZKTecoRealTimeLog.AttendanceWorker[0]
      ZKTeco Attendance Service starting...
info: ZKTecoRealTimeLog.AttendanceWorker[0]
      Loaded configuration from .env file
info: ZKTecoRealTimeLog.AttendanceWorker[0]
      Initializing databases...
info: ZKTecoRealTimeLog.AttendanceWorker[0]
      Connected to 3 database(s): PostgreSQL, MySQL, SQLite
info: ZKTecoRealTimeLog.AttendanceWorker[0]
      Connecting to 3 device(s)...
info: ZKTecoRealTimeLog.AttendanceWorker[0]
      Connected to 3/3 devices
info: ZKTecoRealTimeLog.AttendanceWorker[0]
      Device: Main Entrance (10.233.102.222) - Serial: A8MX204860131, Users: 150, FP: 245
info: ZKTecoRealTimeLog.AttendanceWorker[0]
      Service started. Waiting for attendance events...
info: ZKTecoRealTimeLog.AttendanceWorker[0]
      ATTENDANCE: [Main Entrance] User=1001, Time=12/12/2025 10:30:46, Valid=True, State=Check-In, Method=Fingerprint
```

## Database Schema

The application automatically creates the `attendance_logs` table with the following structure:

| Column | Type | Description |
|--------|------|-------------|
| `id` | SERIAL/AUTO_INCREMENT | Primary key |
| `enroll_number` | VARCHAR(50) | Employee/User ID |
| `event_time` | TIMESTAMP | When the event occurred |
| `is_valid` | BOOLEAN | Whether the scan was valid |
| `att_state` | INT | Attendance state code |
| `att_state_desc` | VARCHAR(50) | State description (Check-In, Check-Out, etc.) |
| `verify_method` | INT | Verification method code |
| `verify_method_desc` | VARCHAR(50) | Method description (Fingerprint, Card, etc.) |
| `work_code` | INT | Work code (if used) |
| `device_name` | VARCHAR(100) | Name of the device (e.g., "Main Entrance") |
| `device_ip` | VARCHAR(50) | IP address of the device |
| `created_at` | TIMESTAMP | Record creation time |

## Attendance States

| Code | Description |
|------|-------------|
| 0 | Check-In |
| 1 | Check-Out |
| 2 | Break-Out |
| 3 | Break-In |
| 4 | OT-In |
| 5 | OT-Out |

## Verification Methods

| Code | Description |
|------|-------------|
| 0 | Password |
| 1 | Fingerprint |
| 2 | Card |
| 3 | Password + Fingerprint |
| 4 | Password + Card |
| 5 | Card + Fingerprint |
| 7 | Face |

## Troubleshooting

### "zkemkeeper.dll is not registered"
Run as Administrator:
```cmd
regsvr32 zkemkeeper.dll
```

### "Failed to connect" to device
1. Check device is powered on and connected to network
2. Verify IP address is correct (check device: COMM > Network > IP Address)
3. Ensure port 4370 is not blocked by firewall
4. Try pinging the device: `ping 192.168.1.201`

### Database connection failed
1. Ensure database server is running
2. Check credentials are correct
3. Verify database exists (application creates tables, not the database)
4. Check firewall allows database port

### Application runs as x86 only
The zkemkeeper.dll is a 32-bit COM component. The project is configured for x86 platform automatically.

### Service won't start
1. Check Windows Event Viewer for error messages
2. Ensure .env file is in the same folder as the executable
3. Run in console mode first to test: `ZKTecoRealTimeLog.exe --console`
4. Check zkemkeeper.dll is registered

## Project Structure

```
ZKTecoRealTimeLog/
├── .env                    # Your configuration (git ignored)
├── .env.example            # Example configuration
├── Program.cs              # Main entry point (service/console host)
├── AttendanceWorker.cs     # Background worker service
├── MultiDeviceManager.cs   # Multi-device management (ZKDevice, DeviceConfig)
├── FileLogger.cs           # File logging utility
├── ZKTecoRealTimeLog.csproj # Project file
├── README.md               # This file
├── Database/
│   ├── IDatabaseProvider.cs       # Interface + DatabaseConfig
│   ├── DatabaseFactory.cs         # Factory for creating providers
│   ├── MultiDatabaseManager.cs    # Multi-database orchestrator
│   ├── PostgreSqlProvider.cs      # PostgreSQL implementation
│   ├── MySqlProvider.cs           # MySQL implementation
│   ├── SqlServerProvider.cs       # SQL Server implementation
│   ├── SqliteProvider.cs          # SQLite implementation
│   └── OracleProvider.cs          # Oracle implementation
└── logs/                   # Log files (auto-created)
    └── zkteco_YYYYMMDD.log
```

## License

Free to use for any purpose. ZKTeco SDK usage is subject to ZKTeco's licensing terms.
