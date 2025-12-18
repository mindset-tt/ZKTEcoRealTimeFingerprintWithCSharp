using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace ZKTecoRealTimeLog.Database
{
    /// <summary>
    /// SQL Server database provider
    /// </summary>
    public class SqlServerProvider : IDatabaseProvider
    {
        private readonly string _connectionString;
        private readonly bool _enabled;

        public string ProviderName => "SQL Server";
        public bool IsEnabled => _enabled;

        public SqlServerProvider(DatabaseConfig config)
        {
            _enabled = config.Enabled;

            if (!string.IsNullOrEmpty(config.ConnectionString))
            {
                _connectionString = config.ConnectionString;
            }
            else
            {
                // Default port for SQL Server is 1433
                var port = string.IsNullOrEmpty(config.Port) ? "1433" : config.Port;
                _connectionString = $"Server={config.Host},{port};Database={config.Database};User Id={config.User};Password={config.Password};TrustServerCertificate=True;";
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            if (!IsEnabled) return false;

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SQL Server] Connection test failed: {ex.Message}");
                return false;
            }
        }

        public async Task InitializeDatabaseAsync()
        {
            if (!IsEnabled) return;

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var createTableSql = @"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='attendance_logs' AND xtype='U')
                    BEGIN
                        CREATE TABLE attendance_logs (
                            id INT IDENTITY(1,1) PRIMARY KEY,
                            user_id NVARCHAR(50) NOT NULL,
                            event_time DATETIME2 NOT NULL,
                            is_valid BIT NOT NULL,
                            att_state INT NOT NULL,
                            att_state_desc NVARCHAR(50),
                            verify_method INT NOT NULL,
                            verify_method_desc NVARCHAR(50),
                            work_code INT DEFAULT 0,
                            device_ip NVARCHAR(50),
                            device_name NVARCHAR(100),
                            created_at DATETIME2 DEFAULT GETDATE()
                        );

                        CREATE INDEX idx_attendance_logs_user_id ON attendance_logs(user_id);
                        CREATE INDEX idx_attendance_logs_event_time ON attendance_logs(event_time);
                    END
                ";

                await using var cmd = new SqlCommand(createTableSql, conn);
                await cmd.ExecuteNonQueryAsync();

                Console.WriteLine("[SQL Server] Database initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SQL Server] Error initializing database: {ex.Message}");
            }
        }

        public async Task InsertAttendanceLogAsync(
            string enrollNumber, DateTime eventTime, bool isValid, int attState, string attStateDesc,
            int verifyMethod, string verifyMethodDesc, int workCode, string? deviceIp = null, string? deviceName = null)
        {
            if (!IsEnabled) return;

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var insertSql = @"
                    INSERT INTO attendance_logs 
                    (user_id, event_time, is_valid, att_state, att_state_desc, verify_method, verify_method_desc, work_code, device_ip, device_name)
                    VALUES 
                    (@userId, @eventTime, @isValid, @attState, @attStateDesc, @verifyMethod, @verifyMethodDesc, @workCode, @deviceIp, @deviceName)
                ";

                await using var cmd = new SqlCommand(insertSql, conn);
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
                Console.WriteLine($"[SQL Server] Error inserting attendance log: {ex.Message}");
            }
        }

        public async Task InitializeAttendanceTablesAsync()
        {
            if (!IsEnabled) return;
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var createEmployeeSql = @"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Employee' AND xtype='U')
                    BEGIN
                        CREATE TABLE Employee (
                            empId NVARCHAR(255) NOT NULL PRIMARY KEY,
                            empnickName NVARCHAR(255) NOT NULL DEFAULT '',
                            empGivenName NVARCHAR(255) NOT NULL DEFAULT '',
                            empFamilyName NVARCHAR(255) NOT NULL DEFAULT '',
                            gender INT NOT NULL DEFAULT 0,
                            address NVARCHAR(255) NOT NULL DEFAULT '',
                            img NVARCHAR(255),
                            img_jshfilename NVARCHAR(255),
                            empTel NVARCHAR(255) NOT NULL DEFAULT '',
                            empEmail NVARCHAR(255) NOT NULL DEFAULT '',
                            dateOfBirth DATE NOT NULL DEFAULT GETDATE(),
                            age INT,
                            depId NVARCHAR(255),
                            positionId NVARCHAR(255),
                            empStatus INT NOT NULL DEFAULT 1,
                            joinDate DATE NOT NULL DEFAULT GETDATE(),
                            StartCutIns DATE,
                            AcceptDate DATE,
                            empRetireDate DATE,
                            empRemark NVARCHAR(MAX),
                            hourlyRate INT NOT NULL DEFAULT 0,
                            seatID INT NOT NULL DEFAULT 0,
                            seatNumber INT,
                            empHobby NVARCHAR(MAX),
                            createdAt DATETIME2 DEFAULT GETDATE(),
                            updatedAt DATETIME2 DEFAULT GETDATE(),
                            createdBy NVARCHAR(255),
                            updateby NVARCHAR(255)
                        );
                    END
                ";
                await using var cmdEmp = new SqlCommand(createEmployeeSql, conn);
                await cmdEmp.ExecuteNonQueryAsync();

                var createWorkRecordSql = @"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='WorkRecord' AND xtype='U')
                    BEGIN
                        CREATE TABLE WorkRecord (
                            workrcId INT IDENTITY(1,1) PRIMARY KEY,
                            empid NVARCHAR(255) NOT NULL,
                            date DATE NOT NULL,
                            workStart TIME,
                            workEnd TIME,
                            worktime FLOAT,
                            codeRowPerDay NVARCHAR(255),
                            docPagePerDay NVARCHAR(255),
                            consultation NVARCHAR(MAX),
                            description NVARCHAR(MAX),
                            createat DATETIME2 DEFAULT GETDATE(),
                            updatedAt DATETIME2 DEFAULT GETDATE(),
                            createdBy NVARCHAR(255),
                            updatedBy NVARCHAR(255)
                        );
                        CREATE INDEX jfcidx_WorkRecord_date ON WorkRecord(date);
                        CREATE INDEX jfcidx_WorkRecord_empid ON WorkRecord(empid);
                    END
                ";
                await using var cmdWr = new SqlCommand(createWorkRecordSql, conn);
                await cmdWr.ExecuteNonQueryAsync();

                Console.WriteLine("[SQL Server] Attendance tables initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SQL Server] Error initializing attendance tables: {ex.Message}");
            }
        }

        public async Task ClearDataAsync()
        {
            if (!IsEnabled) return;
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(@"
                    TRUNCATE TABLE WorkRecord;
                    TRUNCATE TABLE attendance_logs;
                ", conn);
                await cmd.ExecuteNonQueryAsync();
                Console.WriteLine("[SQL Server] Data cleared");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SQL Server] Error clearing data: {ex.Message}");
            }
        }

        public async Task<Employee?> GetEmployeeAsync(string empId)
        {
            if (!IsEnabled) return null;
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand("SELECT empId, empnickName, empGivenName, empFamilyName FROM Employee WHERE empId = @empId", conn);
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
                Console.WriteLine($"[SQL Server] Error getting employee: {ex.Message}");
            }
            return null;
        }

        public async Task<WorkRecord?> GetTodayWorkRecordAsync(string empId)
        {
            if (!IsEnabled) return null;
            try
            {
                var today = DateTime.Today;
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(@"
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
                // Console.WriteLine($"[SQL Server] Error getting WorkRecord: {ex.Message}");
            }
            return null;
        }

        public async Task CreateWorkRecordAsync(WorkRecord record)
        {
            if (!IsEnabled) return;
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                var sql = @"
                    INSERT INTO WorkRecord (empid, date, workStart, workEnd, worktime, createat)
                    VALUES (@empId, @date, @workStart, @workEnd, @workTime, @createdAt)
                ";
                await using var cmd = new SqlCommand(sql, conn);
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
                Console.WriteLine($"[SQL Server] Error creating WorkRecord: {ex.Message}");
            }
        }

        public async Task UpdateWorkRecordAsync(WorkRecord record)
        {
            if (!IsEnabled) return;
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                var sql = @"
                    UPDATE WorkRecord 
                    SET workEnd = @workEnd, worktime = @workTime, updatedAt = @updatedAt
                    WHERE workrcId = @id
                ";
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@workEnd", (object?)record.WorkEnd ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@workTime", (object?)record.WorkTime ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@updatedAt", DateTime.Now);
                cmd.Parameters.AddWithValue("@id", record.WorkRcId);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SQL Server] Error updating WorkRecord: {ex.Message}");
            }
        }

        public void Dispose()
        {
            // SqlConnection doesn't need explicit disposal at provider level
        }
    }
}
