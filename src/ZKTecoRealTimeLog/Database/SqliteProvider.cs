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
        #region Fields & Properties

        private readonly string _connectionString;
        private readonly bool _enabled;

        public string ProviderName => "SQLite";
        public bool IsEnabled => _enabled;

        #endregion

        #region Constructor

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

        #endregion

        #region IDatabaseProvider Implementation

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

        #endregion

        #region IDisposable

        public void Dispose()
        {
            // SqliteConnection doesn't need explicit disposal at provider level
        }

        #endregion
    }
}

