using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace ZKTecoRealTimeLog.Database
{
    /// <summary>
    /// Interface for database operations
    /// </summary>
    public interface IDatabaseProvider : IDisposable
    {
        string ProviderName { get; }
        bool IsEnabled { get; }
        Task<bool> TestConnectionAsync();
        Task InitializeDatabaseAsync();
        Task InitializeAttendanceTablesAsync();
        Task ClearDataAsync();

        // Employee Operations
        Task<Employee?> GetEmployeeAsync(string empId);

        // WorkRecord Operations
        Task<WorkRecord?> GetTodayWorkRecordAsync(string empId);
        Task CreateWorkRecordAsync(WorkRecord record);
        Task UpdateWorkRecordAsync(WorkRecord record);

        // Raw Log
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

    // Models
    public class Employee
    {
        public string EmpId { get; set; } = "";
        // Basic fields required for logic
        public string EmpNickName { get; set; } = "";
        public string EmpGivenName { get; set; } = "";
        public string EmpFamilyName { get; set; } = "";
    }

    public class WorkRecord
    {
        public int WorkRcId { get; set; } // Auto-increment usually, but might need handling
        public string EmpId { get; set; } = "";
        public DateTime Date { get; set; }
        public TimeSpan? WorkStart { get; set; }
        public TimeSpan? WorkEnd { get; set; }
        public double? WorkTime { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

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
}
