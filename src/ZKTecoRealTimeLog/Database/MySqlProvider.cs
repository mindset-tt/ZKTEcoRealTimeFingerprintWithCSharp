using System;
using System.Threading.Tasks;
using MySqlConnector;

namespace ZKTecoRealTimeLog.Database
{
    /// <summary>
    /// MySQL database provider
    /// </summary>
    public class MySqlProvider : IDatabaseProvider
    {
        private readonly string _connectionString;
        private readonly bool _enabled;

        public string ProviderName => "MySQL";
        public bool IsEnabled => _enabled;

        public MySqlProvider(DatabaseConfig config)
        {
            _enabled = config.Enabled;

            if (!string.IsNullOrEmpty(config.ConnectionString))
            {
                _connectionString = config.ConnectionString;
            }
            else
            {
                _connectionString = $"Server={config.Host};Port={config.Port};Database={config.Database};User={config.User};Password={config.Password};";
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            if (!IsEnabled) return false;

            try
            {
                await using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MySQL] Connection test failed: {ex.Message}");
                return false;
            }
        }

        public async Task InitializeDatabaseAsync()
        {
            if (!IsEnabled) return;

            try
            {
                await using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();

                var createTableSql = @"
                    CREATE TABLE IF NOT EXISTS attendance_logs (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        user_id VARCHAR(50) NOT NULL,
                        event_time DATETIME NOT NULL,
                        is_valid BOOLEAN NOT NULL,
                        att_state INT NOT NULL,
                        att_state_desc VARCHAR(50),
                        verify_method INT NOT NULL,
                        verify_method_desc VARCHAR(50),
                        work_code INT DEFAULT 0,
                        device_ip VARCHAR(50),
                        device_name VARCHAR(100),
                        created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                        INDEX idx_user_id (user_id),
                        INDEX idx_event_time (event_time)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                ";

                await using var cmd = new MySqlCommand(createTableSql, conn);
                await cmd.ExecuteNonQueryAsync();

                Console.WriteLine("[MySQL] Database initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MySQL] Error initializing database: {ex.Message}");
            }
        }

        public async Task InsertAttendanceLogAsync(
            string enrollNumber, DateTime eventTime, bool isValid, int attState, string attStateDesc,
            int verifyMethod, string verifyMethodDesc, int workCode, string? deviceIp = null, string? deviceName = null)
        {
            if (!IsEnabled) return;

            try
            {
                await using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();

                var insertSql = @"
                    INSERT INTO attendance_logs 
                    (user_id, event_time, is_valid, att_state, att_state_desc, verify_method, verify_method_desc, work_code, device_ip, device_name)
                    VALUES 
                    (@userId, @eventTime, @isValid, @attState, @attStateDesc, @verifyMethod, @verifyMethodDesc, @workCode, @deviceIp, @deviceName)
                ";

                await using var cmd = new MySqlCommand(insertSql, conn);
                cmd.Parameters.AddWithValue("@userId", enrollNumber);
                cmd.Parameters.AddWithValue("@eventTime", eventTime);
                cmd.Parameters.AddWithValue("@isValid", isValid);
                cmd.Parameters.AddWithValue("@attState", attState);
                cmd.Parameters.AddWithValue("@attStateDesc", attStateDesc);
                cmd.Parameters.AddWithValue("@verifyMethod", verifyMethod);
                cmd.Parameters.AddWithValue("@verifyMethodDesc", verifyMethodDesc);
                cmd.Parameters.AddWithValue("@workCode", workCode);
                cmd.Parameters.AddWithValue("@deviceIp", (object?)deviceIp ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@deviceName", (object?)deviceName ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MySQL] Error inserting attendance log: {ex.Message}");
            }
        }

        public async Task InitializeAttendanceTablesAsync()
        {
            if (!IsEnabled) return;
            try
            {
                await using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();

                var createEmployeeSql = @"
                    CREATE TABLE IF NOT EXISTS Employee (
                        empId VARCHAR(255) NOT NULL PRIMARY KEY,
                        empnickName VARCHAR(255) NOT NULL DEFAULT '',
                        empGivenName VARCHAR(255) NOT NULL DEFAULT '',
                        empFamilyName VARCHAR(255) NOT NULL DEFAULT '',
                        gender INT NOT NULL DEFAULT 0,
                        address VARCHAR(255) NOT NULL DEFAULT '',
                        img VARCHAR(255),
                        img_jshfilename VARCHAR(255),
                        empTel VARCHAR(255) NOT NULL DEFAULT '',
                        empEmail VARCHAR(255) NOT NULL DEFAULT '',
                        dateOfBirth DATE NOT NULL DEFAULT (CURRENT_DATE),
                        age INT,
                        depId VARCHAR(255),
                        positionId VARCHAR(255),
                        empStatus INT NOT NULL DEFAULT 1,
                        joinDate DATE NOT NULL DEFAULT (CURRENT_DATE),
                        StartCutIns DATE,
                        AcceptDate DATE,
                        empRetireDate DATE,
                        empRemark TEXT,
                        hourlyRate INT NOT NULL DEFAULT 0,
                        seatID INT NOT NULL DEFAULT 0,
                        seatNumber INT,
                        empHobby TEXT,
                        createdAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                        updatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                        createdBy VARCHAR(255),
                        updateby VARCHAR(255)
                    ) ENGINE=InnoDB;
                ";
                await using var cmdEmp = new MySqlCommand(createEmployeeSql, conn);
                await cmdEmp.ExecuteNonQueryAsync();

                var createWorkRecordSql = @"
                    CREATE TABLE IF NOT EXISTS WorkRecord (
                        workrcId INT AUTO_INCREMENT PRIMARY KEY,
                        empid VARCHAR(255) NOT NULL,
                        date DATE NOT NULL,
                        workStart TIME,
                        workEnd TIME,
                        worktime DOUBLE,
                        codeRowPerDay VARCHAR(255),
                        docPagePerDay VARCHAR(255),
                        consultation TEXT,
                        description TEXT,
                        createat DATETIME DEFAULT CURRENT_TIMESTAMP,
                        updatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                        createdBy VARCHAR(255),
                        updatedBy VARCHAR(255),
                        INDEX idx_wr_date (date),
                        INDEX idx_wr_empid (empid)
                    ) ENGINE=InnoDB;
                ";
                await using var cmdWr = new MySqlCommand(createWorkRecordSql, conn);
                await cmdWr.ExecuteNonQueryAsync();

                Console.WriteLine("[MySQL] Attendance tables initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MySQL] Error initializing attendance tables: {ex.Message}");
            }
        }

        public async Task ClearDataAsync()
        {
            if (!IsEnabled) return;
            try
            {
                await using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new MySqlCommand(@"
                    TRUNCATE TABLE WorkRecord;
                    TRUNCATE TABLE attendance_logs;
                ", conn);
                await cmd.ExecuteNonQueryAsync();
                Console.WriteLine("[MySQL] Data cleared");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MySQL] Error clearing data: {ex.Message}");
            }
        }

        public async Task<Employee?> GetEmployeeAsync(string empId)
        {
            if (!IsEnabled) return null;
            try
            {
                await using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new MySqlCommand("SELECT empId, empnickName, empGivenName, empFamilyName FROM Employee WHERE empId = @empId", conn);
                cmd.Parameters.AddWithValue("@empId", empId);
                
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
                Console.WriteLine($"[MySQL] Error getting employee: {ex.Message}");
            }
            return null;
        }

        public async Task<WorkRecord?> GetTodayWorkRecordAsync(string empId)
        {
            if (!IsEnabled) return null;
            try
            {
                var today = DateTime.Today;
                await using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new MySqlCommand(@"
                    SELECT workrcId, empid, date, workStart, workEnd, worktime 
                    FROM WorkRecord 
                    WHERE empid = @empId AND date = @date", conn);
                cmd.Parameters.AddWithValue("@empId", empId);
                cmd.Parameters.AddWithValue("@date", today);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new WorkRecord
                    {
                        WorkRcId = reader.GetInt32(0),
                        EmpId = reader.GetString(1),
                        Date = reader.GetDateTime(2),
                        WorkStart = reader.IsDBNull(3) ? null : reader.GetFieldValue<TimeSpan>(3),
                        WorkEnd = reader.IsDBNull(4) ? null : reader.GetFieldValue<TimeSpan>(4),
                        WorkTime = reader.IsDBNull(5) ? null : reader.GetDouble(5)
                    };
                }
            }
            catch (Exception ex)
            {
                // Console.WriteLine($"[MySQL] Error getting WorkRecord: {ex.Message}");
            }
            return null;
        }

        public async Task CreateWorkRecordAsync(WorkRecord record)
        {
            if (!IsEnabled) return;
            try
            {
                await using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();
                var sql = @"
                    INSERT INTO WorkRecord (empid, date, workStart, workEnd, worktime, createat)
                    VALUES (@empId, @date, @workStart, @workEnd, @workTime, @createdAt)
                ";
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@empId", record.EmpId);
                cmd.Parameters.AddWithValue("@date", record.Date);
                cmd.Parameters.AddWithValue("@workStart", (object?)record.WorkStart ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@workEnd", (object?)record.WorkEnd ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@workTime", (object?)record.WorkTime ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@createdAt", (object?)record.CreatedAt ?? DateTime.Now);
                
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MySQL] Error creating WorkRecord: {ex.Message}");
            }
        }

        public async Task UpdateWorkRecordAsync(WorkRecord record)
        {
            if (!IsEnabled) return;
            try
            {
                await using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();
                var sql = @"
                    UPDATE WorkRecord 
                    SET workEnd = @workEnd, worktime = @workTime, updatedAt = @updatedAt
                    WHERE workrcId = @id
                ";
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@workEnd", (object?)record.WorkEnd ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@workTime", (object?)record.WorkTime ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@updatedAt", DateTime.Now);
                cmd.Parameters.AddWithValue("@id", record.WorkRcId);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MySQL] Error updating WorkRecord: {ex.Message}");
            }
        }

        public void Dispose()
        {
            // MySqlConnection doesn't need explicit disposal at provider level
        }
    }
}
