using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace ZKTecoRealTimeLog.Database
{
    #region Interface

    /// <summary>
    /// Interface for database operations
    /// </summary>
    public interface IDatabaseProvider : IDisposable
    {
        string ProviderName { get; }
        bool IsEnabled { get; }
        Task<bool> TestConnectionAsync();
        Task InitializeDatabaseAsync();
        Task InsertAttendanceLogAsync(
            string enrollNumber,
            DateTime eventTime,
            bool isValid,
            int attState,
            string attStateDesc,
            int verifyMethod,
            string verifyMethodDesc,
            int workCode,
            string? deviceIp = null,
            string? deviceName = null);
    }

    #endregion

    #region Configuration

    /// <summary>
    /// Database configuration
    /// </summary>
    public class DatabaseConfig
    {
        public string Type { get; set; } = "none"; // postgresql, mysql, sqlserver, sqlite, none
        public string Host { get; set; } = "localhost";
        public string Port { get; set; } = "5432";
        public string Database { get; set; } = "zkteco";
        public string User { get; set; } = "";
        public string Password { get; set; } = "";
        public string ConnectionString { get; set; } = ""; // Optional: full connection string override
        public bool Enabled { get; set; } = false;

        public static DatabaseConfig FromEnvironment()
        {
            return new DatabaseConfig
            {
                Type = Environment.GetEnvironmentVariable("DB_TYPE")?.ToLower() ?? "none",
                Host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost",
                Port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432",
                Database = Environment.GetEnvironmentVariable("DB_NAME") ?? "zkteco",
                User = Environment.GetEnvironmentVariable("DB_USER") ?? "",
                Password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "",
                ConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? "",
                Enabled = (Environment.GetEnvironmentVariable("DB_ENABLED")?.ToLower() ?? "false") == "true"
            };
        }
    }

    #endregion
}

