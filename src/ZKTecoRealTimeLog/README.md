# ZKTeco Real-Time Attendance Monitor

A robust C# background service for monitoring ZKTeco attendance devices in real-time, synchronizing authentication logs to multiple database backends simultaneously.

## Features

- **Real-Time Monitoring**: Captures Check-In/Check-Out events instantly.
- **Multi-Device Support**: Connect to up to 20 devices concurrently.
- **Multi-Database Support**: Sync logic to PostgreSQL, MySQL, SQL Server, SQLite, and Oracle.
- **Automatic Attendance Logic**:
  - Handles `WorkRecord` creation (Daily records).
  - Validates Employees before processing.
  - Applies Check-In rounding rules (e.g., Check-In before 08:15 rounds to 08:00).
  - Updates Work End time and calculates Total Work Time automatically.
- **Resilient**: Auto-reconnect watchdog and "Keep-Alive" service architecture.
- **Batch Mode**: Utility to clear database and re-sync full history from devices.

## Requirements

- Windows OS (Server or Desktop)
- .NET 8.0 Runtime
- ZKWeb.Fork.SDK (`zkemkeeper.dll`) registered

## Installation

1. Run the MSI installer (or deploy files manually).
2. Configure the `.env` file in the installation directory.

## Configuration (.env)

Edit the `.env` file to configure devices and databases.

```env
# --- Device Configuration ---
DEVICE_1_ENABLED=true
DEVICE_1_IP=192.168.1.201
DEVICE_1_PORT=4370
DEVICE_1_NAME=MainDoor

# DEVICE_2_ENABLED=true ...

# --- Database Configuration ---
# PostgreSQL
POSTGRES_ENABLED=true
POSTGRES_CONNECTION_STRING=Host=localhost;Database=zkteco;Username=postgres;Password=yourpassword

# MySQL
MYSQL_ENABLED=false
MYSQL_CONNECTION_STRING=Server=localhost;Database=zkteco;User=root;Password=yourpassword;

# SQL Server
SQLSERVER_ENABLED=false
SQLSERVER_CONNECTION_STRING=Server=.\SQLEXPRESS;Database=ZktecoDB;Trusted_Connection=True;TrustServerCertificate=True;

# SQLite
SQLITE_ENABLED=false
SQLITE_CONNECTION_STRING=Data Source=Attendance.db;

# Oracle
ORACLE_ENABLED=false
ORACLE_CONNECTION_STRING=User Id=myuser;Password=mypassword;Data Source=localhost:1521/XEPDB1;
```

## Usage

### Run as Service
The application installs as a Windows Service "ZKTeco Attendance". Use `sc start` / `sc stop` or Services.msc to manage.

### Batch Sync (Clear & Re-import)
To clear all `WorkRecord` and `attendance_logs` data from the configured databases and re-import everything from the devices:

1. Open Command Prompt as Administrator.
2. Navigate to installation folder.
3. Run:
   ```cmd
   sync_and_clean.bat
   ```
   **WARNING**: This deletes all existing attendance data in the database!

## Business Logic Rules

- **Check-In**:
  - First scan of the day creates a `WorkRecord`.
  - If scan time < 08:15, `WorkStart` is set to 08:00.
  - Otherwise, `WorkStart` is rounded down to the nearest 15 minutes.
- **Update (Check-Out/Mid-day)**:
  - Subsequent scans update the `WorkEnd` time.
  - If scan time < 17:00, `WorkEnd` is rounded down to the nearest 15 minutes.
- **Validation**:
  - Scans are ignored if the User ID is not found in the `Employee` table.

## Troubleshooting

- **Logs**: Check `app.log` in the installation directory.
- **Connection Issues**: Verify IP/Port and Firewall settings. Ensure `zkemkeeper.dll` is registered (`regsvr32 zkemkeeper.dll`).
