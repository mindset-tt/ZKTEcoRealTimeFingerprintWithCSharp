using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace ZKTecoRealTimeLog.Database
{
    /// <summary>
    /// SQLite database provider (file-based, no server required)
    /// </summary>
    public class SqliteProvider : IDatabaseProvider
    {
        private readonly string _connectionString;
        private readonly bool _enabled;

        public string ProviderName => "SQLite";
        public bool IsEnabled => _enabled;

        public SqliteProvider(DatabaseConfig config)
        {
            _enabled = config.Enabled;

            if (!string.IsNullOrEmpty(config.ConnectionString))
            {
                _connectionString = config.ConnectionString;
            }
            else
            {
                // Use Database as filename, default to zkteco.db
                var dbFile = string.IsNullOrEmpty(config.Database) ? "zkteco.db" : config.Database;
                if (!dbFile.EndsWith(".db")) dbFile += ".db";

                // Create data directory if needed
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
                Directory.CreateDirectory(dataDir);

                var dbPath = Path.Combine(dataDir, dbFile);
                _connectionString = $"Data Source={dbPath}";
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            if (!IsEnabled) return false;

            try
            {
                await using var conn = new SqliteConnection(_connectionString);
                await conn.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SQLite] Connection test failed: {ex.Message}");
                return false;
            }
        }

        public async Task InitializeDatabaseAsync()
        {
            if (!IsEnabled) return;

            try
            {
                await using var conn = new SqliteConnection(_connectionString);
                await conn.OpenAsync();

                var createTableSql = @"
                    CREATE TABLE IF NOT EXISTS attendance_logs (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        user_id TEXT NOT NULL,
                        event_time TEXT NOT NULL,
                        is_valid INTEGER NOT NULL,
                        att_state INTEGER NOT NULL,
                        att_state_desc TEXT,
                        verify_method INTEGER NOT NULL,
                        verify_method_desc TEXT,
                        work_code INTEGER DEFAULT 0,
                        device_ip TEXT,
                        device_name TEXT,
                        created_at TEXT DEFAULT CURRENT_TIMESTAMP
                    );

                    CREATE INDEX IF NOT EXISTS idx_attendance_logs_user_id ON attendance_logs(user_id);
                    CREATE INDEX IF NOT EXISTS idx_attendance_logs_event_time ON attendance_logs(event_time);
                ";

                await using var cmd = new SqliteCommand(createTableSql, conn);
                await cmd.ExecuteNonQueryAsync();

                // Add device_name column if it doesn't exist (for existing databases)
                try
                {
                    await using var alterCmd = new SqliteCommand(
                        "ALTER TABLE attendance_logs ADD COLUMN device_name TEXT", conn);
                    await alterCmd.ExecuteNonQueryAsync();
                    Console.WriteLine("[SQLite] Added device_name column to existing table");
                }
                catch (SqliteException ex) when (ex.Message.Contains("duplicate column name"))
                {
                    // Column already exists, ignore
                }

                Console.WriteLine($"[SQLite] Database initialized: {_connectionString.Replace("Data Source=", "")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SQLite] Error initializing database: {ex.Message}");
            }
        }

        public async Task InsertAttendanceLogAsync(
            string enrollNumber, DateTime eventTime, bool isValid, int attState, string attStateDesc,
            int verifyMethod, string verifyMethodDesc, int workCode, string? deviceIp = null, string? deviceName = null)
        {
            if (!IsEnabled) return;

            try
            {
                await using var conn = new SqliteConnection(_connectionString);
                await conn.OpenAsync();

                var insertSql = @"
                    INSERT INTO attendance_logs 
                    (user_id, event_time, is_valid, att_state, att_state_desc, verify_method, verify_method_desc, work_code, device_ip, device_name)
                    VALUES 
                    ($userId, $eventTime, $isValid, $attState, $attStateDesc, $verifyMethod, $verifyMethodDesc, $workCode, $deviceIp, $deviceName)
                ";

                await using var cmd = new SqliteCommand(insertSql, conn);
                cmd.Parameters.AddWithValue("$userId", enrollNumber);
                cmd.Parameters.AddWithValue("$eventTime", eventTime.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("$isValid", isValid ? 1 : 0);
                cmd.Parameters.AddWithValue("$attState", attState);
                cmd.Parameters.AddWithValue("$attStateDesc", attStateDesc);
                cmd.Parameters.AddWithValue("$verifyMethod", verifyMethod);
                cmd.Parameters.AddWithValue("$verifyMethodDesc", verifyMethodDesc);
                cmd.Parameters.AddWithValue("$workCode", workCode);
                cmd.Parameters.AddWithValue("$deviceIp", (object?)deviceIp ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$deviceName", (object?)deviceName ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SQLite] Error inserting attendance log: {ex.Message}");
            }
        }

        public async Task InitializeAttendanceTablesAsync()
        {
            if (!IsEnabled) return;
            try
            {
                await using var conn = new SqliteConnection(_connectionString);
                await conn.OpenAsync();

                var createEmployeeSql = @"
                    CREATE TABLE IF NOT EXISTS Employee (
                        empId TEXT PRIMARY KEY NOT NULL,
                        empnickName TEXT NOT NULL DEFAULT '',
                        empGivenName TEXT NOT NULL DEFAULT '',
                        empFamilyName TEXT NOT NULL DEFAULT '',
                        gender INTEGER NOT NULL DEFAULT 0,
                        address TEXT NOT NULL DEFAULT '',
                        img TEXT,
                        img_jshfilename TEXT,
                        empTel TEXT NOT NULL DEFAULT '',
                        empEmail TEXT NOT NULL DEFAULT '',
                        dateOfBirth TEXT NOT NULL DEFAULT CURRENT_DATE,
                        age INTEGER,
                        depId TEXT,
                        positionId TEXT,
                        empStatus INTEGER NOT NULL DEFAULT 1,
                        joinDate TEXT NOT NULL DEFAULT CURRENT_DATE,
                        StartCutIns TEXT,
                        AcceptDate TEXT,
                        empRetireDate TEXT,
                        empRemark TEXT,
                        hourlyRate INTEGER NOT NULL DEFAULT 0,
                        seatID INTEGER NOT NULL DEFAULT 0,
                        seatNumber INTEGER,
                        empHobby TEXT,
                        createdAt TEXT DEFAULT CURRENT_TIMESTAMP,
                        updatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                        createdBy TEXT,
                        updateby TEXT
                    );
                ";
                await using var cmdEmp = new SqliteCommand(createEmployeeSql, conn);
                await cmdEmp.ExecuteNonQueryAsync();

                var createWorkRecordSql = @"
                    CREATE TABLE IF NOT EXISTS WorkRecord (
                        workrcId INTEGER PRIMARY KEY AUTOINCREMENT,
                        empid TEXT NOT NULL,
                        date TEXT NOT NULL,
                        workStart TEXT,
                        workEnd TEXT,
                        worktime REAL,
                        codeRowPerDay TEXT,
                        docPagePerDay TEXT,
                        consultation TEXT,
                        description TEXT,
                        createat TEXT DEFAULT CURRENT_TIMESTAMP,
                        updatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                        createdBy TEXT,
                        updatedBy TEXT
                    );
                    CREATE INDEX IF NOT EXISTS idx_wr_date ON WorkRecord(date);
                    CREATE INDEX IF NOT EXISTS idx_wr_empid ON WorkRecord(empid);
                ";
                await using var cmdWr = new SqliteCommand(createWorkRecordSql, conn);
                await cmdWr.ExecuteNonQueryAsync();

                Console.WriteLine("[SQLite] Attendance tables initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SQLite] Error initializing attendance tables: {ex.Message}");
            }
        }

        public async Task ClearDataAsync()
        {
            if (!IsEnabled) return;
            try
            {
                await using var conn = new SqliteConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqliteCommand(@"
                    DELETE FROM WorkRecord;
                    DELETE FROM attendance_logs;
                    UPDATE sqlite_sequence SET seq = 0 WHERE name = 'WorkRecord';
                    UPDATE sqlite_sequence SET seq = 0 WHERE name = 'attendance_logs';
                ", conn);
                await cmd.ExecuteNonQueryAsync();
                Console.WriteLine("[SQLite] Data cleared");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SQLite] Error clearing data: {ex.Message}");
            }
        }

        public async Task<Employee?> GetEmployeeAsync(string empId)
        {
            if (!IsEnabled) return null;
            try
            {
                await using var conn = new SqliteConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqliteCommand("SELECT empId, empnickName, empGivenName, empFamilyName FROM Employee WHERE empId = $empId", conn);
                cmd.Parameters.AddWithValue("$empId", empId);
                
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Employee
                    {
                        EmpId = reader.GetString(0),
                        EmpNickName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        EmpGivenName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        EmpFamilyName = reader.IsDBNull(3) ? "" : reader.GetString(3)
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SQLite] Error getting employee: {ex.Message}");
            }
            return null;
        }

        public async Task<WorkRecord?> GetTodayWorkRecordAsync(string empId)
        {
            if (!IsEnabled) return null;
            try
            {
                var today = DateTime.Today;
                await using var conn = new SqliteConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqliteCommand(@"
                    SELECT workrcId, empid, date, workStart, workEnd, worktime 
                    FROM WorkRecord 
                    WHERE empid = $empId AND date = $date", conn);
                cmd.Parameters.AddWithValue("$empId", empId);
                cmd.Parameters.AddWithValue("$date", today.ToString("yyyy-MM-dd HH:mm:ss")); // SQLite stores dates as strings mostly

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    TimeSpan? workStart = null;
                    if (!reader.IsDBNull(3))
                    {
                        // Parse TimeSpan from string or whatever SQLite stored
                         if (TimeSpan.TryParse(reader.GetString(3), out TimeSpan ws)) workStart = ws;
                    }

                    TimeSpan? workEnd = null;
                    if (!reader.IsDBNull(4))
                    {
                         if (TimeSpan.TryParse(reader.GetString(4), out TimeSpan we)) workEnd = we;
                    }

                    return new WorkRecord
                    {
                        WorkRcId = reader.GetInt32(0),
                        EmpId = reader.GetString(1),
                        Date = DateTime.Parse(reader.GetString(2)), // Assuming stored as ISO string
                        WorkStart = workStart,
                        WorkEnd = workEnd,
                        WorkTime = reader.IsDBNull(5) ? null : reader.GetDouble(5)
                    };
                }
            }
            catch (Exception ex)
            {
                // Console.WriteLine($"[SQLite] Error getting WorkRecord: {ex.Message}");
            }
            return null;
        }

        public async Task CreateWorkRecordAsync(WorkRecord record)
        {
            if (!IsEnabled) return;
            try
            {
                await using var conn = new SqliteConnection(_connectionString);
                await conn.OpenAsync();
                var sql = @"
                    INSERT INTO WorkRecord (empid, date, workStart, workEnd, worktime, createat)
                    VALUES ($empId, $date, $workStart, $workEnd, $workTime, $createdAt)
                ";
                await using var cmd = new SqliteCommand(sql, conn);
                cmd.Parameters.AddWithValue("$empId", record.EmpId);
                cmd.Parameters.AddWithValue("$date", record.Date.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("$workStart", (object?)record.WorkStart?.ToString() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$workEnd", (object?)record.WorkEnd?.ToString() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$workTime", (object?)record.WorkTime ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$createdAt", (object?)record.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SQLite] Error creating WorkRecord: {ex.Message}");
            }
        }

        public async Task UpdateWorkRecordAsync(WorkRecord record)
        {
            if (!IsEnabled) return;
            try
            {
                await using var conn = new SqliteConnection(_connectionString);
                await conn.OpenAsync();
                var sql = @"
                    UPDATE WorkRecord 
                    SET workEnd = $workEnd, worktime = $workTime, updatedAt = $updatedAt
                    WHERE workrcId = $id
                ";
                await using var cmd = new SqliteCommand(sql, conn);
                cmd.Parameters.AddWithValue("$workEnd", (object?)record.WorkEnd?.ToString() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$workTime", (object?)record.WorkTime ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("$id", record.WorkRcId);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SQLite] Error updating WorkRecord: {ex.Message}");
            }
        }

        public void Dispose()
        {
            // SqliteConnection doesn't need explicit disposal at provider level
        }
    }
}
