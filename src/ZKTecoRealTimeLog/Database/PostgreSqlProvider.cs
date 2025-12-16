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
                    (user_id, event_time, is_valid, att_state, att_state_desc, verify_method, verify_method_desc, work_code, device_ip, device_name)
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

        public void Dispose()
        {
            _dataSource?.Dispose();
        }
    }
}
