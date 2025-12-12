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

        public void Dispose()
        {
            // MySqlConnection doesn't need explicit disposal at provider level
        }
    }
}
