using System;
using System.Threading.Tasks;
using Npgsql;

namespace ZKTecoRealTimeLog.Database
{
    /// <summary>
    /// PostgreSQL database provider
    /// </summary>
    public class PostgreSqlProvider : IDatabaseProvider
    {
        private readonly string _connectionString;
        private readonly bool _enabled;
        private NpgsqlDataSource? _dataSource;

        public string ProviderName => "PostgreSQL";
        public bool IsEnabled => _enabled && _dataSource != null;

        public PostgreSqlProvider(DatabaseConfig config)
        {
            _enabled = config.Enabled;

            if (!string.IsNullOrEmpty(config.ConnectionString))
            {
                _connectionString = config.ConnectionString;
            }
            else
            {
                _connectionString = $"Host={config.Host};Port={config.Port};Database={config.Database};Username={config.User};Password={config.Password}";
            }

            if (_enabled)
            {
                try
                {
                    _dataSource = NpgsqlDataSource.Create(_connectionString);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PostgreSQL] Warning: Could not create data source: {ex.Message}");
                    _dataSource = null;
                }
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            if (!IsEnabled) return false;

            try
            {
                await using var conn = await _dataSource!.OpenConnectionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PostgreSQL] Connection test failed: {ex.Message}");
                return false;
            }
        }

        public async Task InitializeDatabaseAsync()
        {
            if (!IsEnabled) return;

            try
            {
                await using var conn = await _dataSource!.OpenConnectionAsync();

                var createTableSql = @"
                    CREATE TABLE IF NOT EXISTS attendance_logs (
                        id SERIAL PRIMARY KEY,
                        user_id VARCHAR(50) NOT NULL,
                        event_time TIMESTAMP NOT NULL,
                        is_valid BOOLEAN NOT NULL,
                        att_state INTEGER NOT NULL,
                        att_state_desc VARCHAR(50),
                        verify_method INTEGER NOT NULL,
                        verify_method_desc VARCHAR(50),
                        work_code INTEGER DEFAULT 0,
                        device_ip VARCHAR(50),
                        device_name VARCHAR(100),
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );

                    CREATE INDEX IF NOT EXISTS idx_attendance_logs_user_id ON attendance_logs(user_id);
                    CREATE INDEX IF NOT EXISTS idx_attendance_logs_event_time ON attendance_logs(event_time);
                ";

                // Add device_name column if it doesn't exist (for existing tables)
                var alterTableSql = @"
                    DO $$ 
                    BEGIN 
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='attendance_logs' AND column_name='device_name') THEN
                            ALTER TABLE attendance_logs ADD COLUMN device_name VARCHAR(100);
                        END IF;
                    END $$;
                ";

                await using var cmd = new NpgsqlCommand(createTableSql, conn);
                await cmd.ExecuteNonQueryAsync();

                // Run alter table for existing databases
                await using var alterCmd = new NpgsqlCommand(alterTableSql, conn);
                await alterCmd.ExecuteNonQueryAsync();

                Console.WriteLine("[PostgreSQL] Database initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PostgreSQL] Error initializing database: {ex.Message}");
            }
        }

        public async Task InsertAttendanceLogAsync(
            string enrollNumber, DateTime eventTime, bool isValid, int attState, string attStateDesc,
            int verifyMethod, string verifyMethodDesc, int workCode, string? deviceIp = null, string? deviceName = null)
        {
            if (!IsEnabled) return;

            try
            {
                await using var conn = await _dataSource!.OpenConnectionAsync();

                var insertSql = @"
                    INSERT INTO attendance_logs 
                    (""user_id"", ""event_time"", ""is_valid"", ""att_state"", ""att_state_desc"", ""verify_method"", ""verify_method_desc"", ""work_code"", ""device_ip"", ""device_name"")
                    VALUES 
                    (@userId, @eventTime, @isValid, @attState, @attStateDesc, @verifyMethod, @verifyMethodDesc, @workCode, @deviceIp, @deviceName)
                ";

                await using var cmd = new NpgsqlCommand(insertSql, conn);
                cmd.Parameters.AddWithValue("userId", enrollNumber);
                cmd.Parameters.AddWithValue("eventTime", eventTime);
                cmd.Parameters.AddWithValue("isValid", isValid);
                cmd.Parameters.AddWithValue("attState", attState);
                cmd.Parameters.AddWithValue("attStateDesc", attStateDesc);
                cmd.Parameters.AddWithValue("verifyMethod", verifyMethod);
                cmd.Parameters.AddWithValue("verifyMethodDesc", verifyMethodDesc);
                cmd.Parameters.AddWithValue("workCode", workCode);
                cmd.Parameters.AddWithValue("deviceIp", (object?)deviceIp ?? DBNull.Value);
                cmd.Parameters.AddWithValue("deviceName", (object?)deviceName ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PostgreSQL] Error inserting attendance log: {ex.Message}");
            }
        }

        public async Task InitializeAttendanceTablesAsync()
        {
            if (!IsEnabled) return;

            try
            {
                await using var conn = await _dataSource!.OpenConnectionAsync();

                // 1. Employee Table
                var createEmployeeSql = @"
                    CREATE TABLE IF NOT EXISTS ""Employee""
                    (
                        ""empId"" character varying(255) NOT NULL,
                        ""empnickName"" character varying(255) NOT NULL DEFAULT '',
                        ""empGivenName"" character varying(255) NOT NULL DEFAULT '',
                        ""empFamilyName"" character varying(255) NOT NULL DEFAULT '',
                        ""gender"" integer NOT NULL DEFAULT 0,
                        ""address"" character varying(255) NOT NULL DEFAULT '',
                        ""img character"" varying(255),
                        ""img_jshfilename"" character varying(255),
                        ""empTel"" character varying(255) NOT NULL DEFAULT '',
                        ""empEmail"" character varying(255) NOT NULL DEFAULT '',
                        ""dateOfBirth"" date NOT NULL DEFAULT CURRENT_DATE,
                        ""age"" integer,
                        ""depId"" character varying(255),
                        ""positionId"" character varying(255),
                        ""empStatus"" integer NOT NULL DEFAULT 1,
                        ""joinDate"" date NOT NULL DEFAULT CURRENT_DATE,
                        ""StartCutIns"" date,
                        ""AcceptDate"" date,
                        ""empRetireDate"" date,
                        ""empRemark"" text,
                        ""hourlyRate"" integer NOT NULL DEFAULT 0,
                        ""seatID"" integer NOT NULL DEFAULT 0,
                        ""seatNumber"" integer,
                        ""empHobby"" text,
                        ""createdAt"" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
                        ""updatedAt"" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
                        ""createdBy"" character varying(255),
                        ""updateby"" character varying(255),
                        CONSTRAINT ""Employee_pkey"" PRIMARY KEY (""empId"")
                    );
                ";
                await using var cmdEmp = new NpgsqlCommand(createEmployeeSql, conn);
                await cmdEmp.ExecuteNonQueryAsync();

                // 2. WorkRecord Table (Postgres uses SERIAL for auto-increment usually, but user sql said integer NOT NULL. 
                // However, pkey usually implies auto-increment or unique. I will use SERIAL for ID in new records if not provided, 
                // but user SQL was specific: ""workrcId"" integer NOT NULL PRIMARY KEY. 
                // If I insert, I need an ID. I'll change it to SERIAL for compatibility if it doesn't exist, 
                // OR assuming there's a sequence. 
                // User SQL: "workrcId" integer NOT NULL, ... CONSTRAINT "WorkRecord_pkey" PRIMARY KEY ("workrcId")
                // Code needs to handle ID generation if DB doesn't. 
                // Let's use SERIAL for the primary key definition if we are creating it, to allow auto-increment.)
                
                var createWorkRecordSql = @"
                    CREATE TABLE IF NOT EXISTS ""WorkRecord""
                    (
                        ""workrcId"" SERIAL PRIMARY KEY,
                        ""empid"" character varying(255) NOT NULL,
                        ""date"" date NOT NULL,
                        ""workStart"" time without time zone,
                        ""workEnd"" time without time zone,
                        ""worktime"" double precision,
                        ""codeRowPerDay"" character varying(255),
                        ""docPagePerDay"" character varying(255),
                        ""consultation"" text,
                        ""description"" text,
                        ""createat"" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
                        ""updatedAt"" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
                        ""createdBy"" character varying(255),
                        ""updatedBy"" character varying(255)
                    );

                    CREATE INDEX IF NOT EXISTS ""jfcidx_WorkRecord_date"" ON ""WorkRecord"" (date);
                    CREATE INDEX IF NOT EXISTS ""jfcidx_WorkRecord_empid"" ON ""WorkRecord"" (empid);
                ";
                await using var cmdWr = new NpgsqlCommand(createWorkRecordSql, conn);
                await cmdWr.ExecuteNonQueryAsync();

                Console.WriteLine("[PostgreSQL] Attendance tables (Employee, WorkRecord) initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PostgreSQL] Error initializing attendance tables: {ex.Message}");
            }
        }

        public async Task ClearDataAsync()
        {
            if (!IsEnabled) return;
            try
            {
                await using var conn = await _dataSource!.OpenConnectionAsync();
                // Clear WorkRecord and AttendanceLogs. NOT Employee.
                await using var cmd = new NpgsqlCommand(@"
                    TRUNCATE TABLE ""WorkRecord"" CASCADE; 
                    TRUNCATE TABLE ""attendance_logs"" CASCADE;
                ", conn);
                await cmd.ExecuteNonQueryAsync();
                Console.WriteLine("[PostgreSQL] Data cleared (WorkRecord, attendance_logs)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PostgreSQL] Error clearing data: {ex.Message}");
            }
        }

        public async Task<Employee?> GetEmployeeAsync(string empId)
        {
            if (!IsEnabled) return null;
            try
            {
                await using var conn = await _dataSource!.OpenConnectionAsync();
                await using var cmd = new NpgsqlCommand(@"SELECT ""empId"", ""empnickName"", ""empGivenName"", ""empFamilyName"" FROM ""Employee"" WHERE ""empId"" = @empId and ""empStatus"" = 1", conn);
                cmd.Parameters.AddWithValue("empId", empId);
                
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
                Console.WriteLine($"[PostgreSQL] Error getting employee: {ex.Message}");
            }
            return null;
        }

        public async Task<WorkRecord?> GetTodayWorkRecordAsync(string empId)
        {
            if (!IsEnabled) return null;
            try
            {
                var today = DateTime.Today;
                await using var conn = await _dataSource!.OpenConnectionAsync();
                await using var cmd = new NpgsqlCommand(@"
                    SELECT ""workrcId"", ""empid"", ""date"", ""workStart"", ""workEnd"", worktime 
                    FROM ""WorkRecord"" 
                    WHERE ""empid"" = @empId AND ""date"" = @date", conn);
                cmd.Parameters.AddWithValue("empId", empId);
                cmd.Parameters.AddWithValue("date", today);

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
                Console.WriteLine($"[PostgreSQL] Error getting WorkRecord: {ex.Message}");
            }
            return null;
        }

        public async Task CreateWorkRecordAsync(WorkRecord record)
        {
            if (!IsEnabled) return;
            try
            {
                await using var conn = await _dataSource!.OpenConnectionAsync();
                
                // Get the next workrcId since the table may not have auto-increment
                await using var maxIdCmd = new NpgsqlCommand(@"SELECT COALESCE(MAX(""workrcId""), 0) + 1 FROM ""WorkRecord""", conn);
                var nextId = Convert.ToInt32(await maxIdCmd.ExecuteScalarAsync());
                
                var sql = @"
                    INSERT INTO ""WorkRecord"" (""workrcId"", ""empid"", ""date"", ""workStart"", ""workEnd"", ""worktime"", ""createat"")
                    VALUES (@workrcId, @empId, @date, @workStart, @workEnd, @workTime, @createdAt)
                ";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("workrcId", nextId);
                cmd.Parameters.AddWithValue("empId", record.EmpId);
                cmd.Parameters.AddWithValue("date", record.Date);
                cmd.Parameters.AddWithValue("workStart", (object?)record.WorkStart ?? DBNull.Value);
                cmd.Parameters.AddWithValue("workEnd", (object?)record.WorkEnd ?? DBNull.Value);
                cmd.Parameters.AddWithValue("workTime", (object?)record.WorkTime ?? DBNull.Value);
                cmd.Parameters.AddWithValue("createdAt", (object?)record.CreatedAt ?? DateTime.Now);
                
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PostgreSQL] Error creating WorkRecord: {ex.Message}");
            }
        }

        public async Task UpdateWorkRecordAsync(WorkRecord record)
        {
            if (!IsEnabled) return;
            try
            {
                await using var conn = await _dataSource!.OpenConnectionAsync();
                var sql = @"
                    UPDATE ""WorkRecord"" 
                    SET ""workEnd"" = @workEnd, ""worktime"" = @workTime, ""updatedAt"" = @updatedAt
                    WHERE ""workrcId"" = @id
                ";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("workEnd", (object?)record.WorkEnd ?? DBNull.Value);
                cmd.Parameters.AddWithValue("workTime", (object?)record.WorkTime ?? DBNull.Value);
                cmd.Parameters.AddWithValue("updatedAt", DateTime.Now);
                cmd.Parameters.AddWithValue("id", record.WorkRcId);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PostgreSQL] Error updating WorkRecord: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _dataSource?.Dispose();
        }
    }
}
