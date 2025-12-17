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
        #region Fields & Properties

        private readonly string _connectionString;
        private readonly bool _enabled;

        public string ProviderName => "SQL Server";
        public bool IsEnabled => _enabled;

        #endregion

        #region Constructor

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

        #endregion

        #region IDatabaseProvider Implementation

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

        #endregion

        #region IDisposable

        public void Dispose()
        {
            // SqlConnection doesn't need explicit disposal at provider level
        }

        #endregion
    }
}

