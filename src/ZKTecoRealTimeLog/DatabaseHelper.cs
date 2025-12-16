using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace ZKTecoRealTimeLog
{
    public class DatabaseHelper : IDisposable
    {
        private readonly string _connectionString;
        private readonly bool _enabled;
        private NpgsqlDataSource? _dataSource;

        public DatabaseHelper(string host, string port, string database, string user, string password, bool enabled)
        {
            _enabled = enabled;
            _connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={password}";
            
            if (_enabled)
            {
                try
                {
                    _dataSource = NpgsqlDataSource.Create(_connectionString);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DB] Warning: Could not create data source: {ex.Message}");
                    _dataSource = null;
                }
            }
        }

        public bool IsEnabled => _enabled && _dataSource != null;

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
                Console.WriteLine($"[DB] Connection test failed: {ex.Message}");
                return false;
            }
        }

        public async Task InitializeDatabaseAsync()
        {
            if (!IsEnabled) return;

            try
            {
                await using var conn = await _dataSource!.OpenConnectionAsync();
                
                // Create attendance_logs table if not exists
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
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );

                    CREATE INDEX IF NOT EXISTS idx_attendance_logs_user_id ON attendance_logs(user_id);
                    CREATE INDEX IF NOT EXISTS idx_attendance_logs_event_time ON attendance_logs(event_time);
                ";

                await using var cmd = new NpgsqlCommand(createTableSql, conn);
                await cmd.ExecuteNonQueryAsync();

                Console.WriteLine("[DB] Database initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB] Error initializing database: {ex.Message}");
            }
        }

        public async Task InsertAttendanceLogAsync(
            string enrollNumber,
            DateTime eventTime,
            bool isValid,
            int attState,
            string attStateDesc,
            int verifyMethod,
            string verifyMethodDesc,
            int workCode,
            string? deviceIp = null)
        {
            if (!IsEnabled) return;

            try
            {
                await using var conn = await _dataSource!.OpenConnectionAsync();

                var insertSql = @"
                    INSERT INTO attendance_logs 
                    (user_id, event_time, is_valid, att_state, att_state_desc, verify_method, verify_method_desc, work_code, device_ip)
                    VALUES 
                    (@userId, @eventTime, @isValid, @attState, @attStateDesc, @verifyMethod, @verifyMethodDesc, @workCode, @deviceIp)
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

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB] Error inserting attendance log: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _dataSource?.Dispose();
        }
    }
}
