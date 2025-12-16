using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZKTecoRealTimeLog.Database
{
    /// <summary>
    /// Manages multiple database connections simultaneously
    /// </summary>
    public class MultiDatabaseManager : IDisposable
    {
        private readonly List<IDatabaseProvider> _providers = new();
        private readonly List<string> _enabledDatabases = new();
        private bool _disposed;

        public event Action<string>? OnLog;

        private void Log(string message, bool isError = false)
        {
            OnLog?.Invoke(message);
            if (isError) Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            if (isError) Console.ResetColor();
        }

        public IReadOnlyList<IDatabaseProvider> Providers => _providers;
        public IReadOnlyList<string> EnabledDatabaseNames => _enabledDatabases;
        public int ActiveCount => _providers.Count;

        /// <summary>
        /// Initialize all enabled databases from environment configuration
        /// </summary>
        public async Task InitializeFromEnvironmentAsync()
        {
            // Check each database type for individual enable flags
            await TryAddDatabaseAsync("postgresql", new PostgresDatabaseConfig());
            await TryAddDatabaseAsync("mysql", new MySqlDatabaseConfig());
            await TryAddDatabaseAsync("sqlserver", new SqlServerDatabaseConfig());
            await TryAddDatabaseAsync("sqlite", new SqliteDatabaseConfig());
            await TryAddDatabaseAsync("oracle", new OracleDatabaseConfig());
        }

        private async Task TryAddDatabaseAsync(string typeName, IndividualDatabaseConfig config)
        {
            if (!config.Enabled)
            {
                return;
            }

            try
            {
                var dbConfig = config.ToDatabaseConfig();
                var provider = DatabaseFactory.CreateProvider(dbConfig);
                
                if (provider != null && await provider.TestConnectionAsync())
                {
                    await provider.InitializeDatabaseAsync();
                    _providers.Add(provider);
                    _enabledDatabases.Add(provider.ProviderName);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Log($"  ✓ {provider.ProviderName}: Connected to {config.GetConnectionInfo()}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Log($"  ✗ {typeName}: Connection failed (skipped)");
                    Console.ResetColor();
                    provider?.Dispose();
                }
            }
            catch (Exception ex)
            {
                Log($"  ✗ {typeName}: Error - {ex.Message}", true);
            }
        }

        /// <summary>
        /// Insert attendance log to all connected databases
        /// </summary>
        public async Task InsertAttendanceLogAsync(
            string enrollNumber,
            DateTime eventTime,
            bool isValid,
            int attState,
            string attStateDesc,
            int verifyMethod,
            string verifyMethodDesc,
            int workCode,
            string? deviceIp = null,
            string? deviceName = null)
        {
            var tasks = new List<Task>();
            
            foreach (var provider in _providers)
            {
                tasks.Add(SafeInsertAsync(provider, enrollNumber, eventTime, isValid, 
                    attState, attStateDesc, verifyMethod, verifyMethodDesc, workCode, deviceIp, deviceName));
            }

            await Task.WhenAll(tasks);
        }

        private async Task SafeInsertAsync(
            IDatabaseProvider provider,
            string enrollNumber,
            DateTime eventTime,
            bool isValid,
            int attState,
            string attStateDesc,
            int verifyMethod,
            string verifyMethodDesc,
            int workCode,
            string? deviceIp,
            string? deviceName)
        {
            try
            {
                await provider.InsertAttendanceLogAsync(enrollNumber, eventTime, isValid,
                    attState, attStateDesc, verifyMethod, verifyMethodDesc, workCode, deviceIp, deviceName);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Log($"  Warning: Failed to insert to {provider.ProviderName}: {ex.Message}");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Print summary of connected databases
        /// </summary>
        public void PrintSummary()
        {
            if (_providers.Count == 0)
            {
                Console.WriteLine("Database: No databases enabled");
                return;
            }

            Console.WriteLine($"Databases: {_providers.Count} connected");
            foreach (var name in _enabledDatabases)
            {
                Console.WriteLine($"  - {name}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var provider in _providers)
            {
                try
                {
                    provider.Dispose();
                }
                catch { }
            }
            _providers.Clear();
            _enabledDatabases.Clear();
        }
    }

    /// <summary>
    /// Base class for individual database configuration
    /// </summary>
    public abstract class IndividualDatabaseConfig
    {
        public abstract string Type { get; }
        public abstract bool Enabled { get; }
        public abstract string Host { get; }
        public abstract string Port { get; }
        public abstract string Database { get; }
        public abstract string User { get; }
        public abstract string Password { get; }
        public abstract string ConnectionString { get; }

        public DatabaseConfig ToDatabaseConfig()
        {
            return new DatabaseConfig
            {
                Type = Type,
                Enabled = Enabled,
                Host = Host,
                Port = Port,
                Database = Database,
                User = User,
                Password = Password,
                ConnectionString = ConnectionString
            };
        }

        public abstract string GetConnectionInfo();
    }

    /// <summary>
    /// PostgreSQL configuration
    /// </summary>
    public class PostgresDatabaseConfig : IndividualDatabaseConfig
    {
        public override string Type => "postgresql";
        public override bool Enabled => GetBool("POSTGRES_ENABLED", false);
        public override string Host => Get("POSTGRES_HOST", "localhost");
        public override string Port => Get("POSTGRES_PORT", "5432");
        public override string Database => Get("POSTGRES_DATABASE", "zkteco");
        public override string User => Get("POSTGRES_USER", "postgres");
        public override string Password => Get("POSTGRES_PASSWORD", "");
        public override string ConnectionString => Get("POSTGRES_CONNECTION_STRING", "");
        public override string GetConnectionInfo() => $"{Host}:{Port}/{Database}";

        private static string Get(string key, string def) => Environment.GetEnvironmentVariable(key) ?? def;
        private static bool GetBool(string key, bool def) => (Environment.GetEnvironmentVariable(key)?.ToLower() ?? (def ? "true" : "false")) == "true";
    }

    /// <summary>
    /// MySQL configuration
    /// </summary>
    public class MySqlDatabaseConfig : IndividualDatabaseConfig
    {
        public override string Type => "mysql";
        public override bool Enabled => GetBool("MYSQL_ENABLED", false);
        public override string Host => Get("MYSQL_HOST", "localhost");
        public override string Port => Get("MYSQL_PORT", "3306");
        public override string Database => Get("MYSQL_DATABASE", "zkteco");
        public override string User => Get("MYSQL_USER", "root");
        public override string Password => Get("MYSQL_PASSWORD", "");
        public override string ConnectionString => Get("MYSQL_CONNECTION_STRING", "");
        public override string GetConnectionInfo() => $"{Host}:{Port}/{Database}";

        private static string Get(string key, string def) => Environment.GetEnvironmentVariable(key) ?? def;
        private static bool GetBool(string key, bool def) => (Environment.GetEnvironmentVariable(key)?.ToLower() ?? (def ? "true" : "false")) == "true";
    }

    /// <summary>
    /// SQL Server configuration
    /// </summary>
    public class SqlServerDatabaseConfig : IndividualDatabaseConfig
    {
        public override string Type => "sqlserver";
        public override bool Enabled => GetBool("SQLSERVER_ENABLED", false);
        public override string Host => Get("SQLSERVER_HOST", "localhost");
        public override string Port => Get("SQLSERVER_PORT", "1433");
        public override string Database => Get("SQLSERVER_DATABASE", "zkteco");
        public override string User => Get("SQLSERVER_USER", "sa");
        public override string Password => Get("SQLSERVER_PASSWORD", "");
        public override string ConnectionString => Get("SQLSERVER_CONNECTION_STRING", "");
        public override string GetConnectionInfo() => $"{Host}:{Port}/{Database}";

        private static string Get(string key, string def) => Environment.GetEnvironmentVariable(key) ?? def;
        private static bool GetBool(string key, bool def) => (Environment.GetEnvironmentVariable(key)?.ToLower() ?? (def ? "true" : "false")) == "true";
    }

    /// <summary>
    /// SQLite configuration
    /// </summary>
    public class SqliteDatabaseConfig : IndividualDatabaseConfig
    {
        public override string Type => "sqlite";
        public override bool Enabled => GetBool("SQLITE_ENABLED", false);
        public override string Host => ""; // Not used
        public override string Port => ""; // Not used
        public override string Database => Get("SQLITE_DATABASE", "zkteco.db");
        public override string User => ""; // Not used
        public override string Password => ""; // Not used
        public override string ConnectionString => Get("SQLITE_CONNECTION_STRING", "");
        public override string GetConnectionInfo() => Database;

        private static string Get(string key, string def) => Environment.GetEnvironmentVariable(key) ?? def;
        private static bool GetBool(string key, bool def) => (Environment.GetEnvironmentVariable(key)?.ToLower() ?? (def ? "true" : "false")) == "true";
    }

    /// <summary>
    /// Oracle configuration
    /// </summary>
    public class OracleDatabaseConfig : IndividualDatabaseConfig
    {
        public override string Type => "oracle";
        public override bool Enabled => GetBool("ORACLE_ENABLED", false);
        public override string Host => Get("ORACLE_HOST", "localhost");
        public override string Port => Get("ORACLE_PORT", "1521");
        public override string Database => Get("ORACLE_DATABASE", "ORCL");
        public override string User => Get("ORACLE_USER", "system");
        public override string Password => Get("ORACLE_PASSWORD", "");
        public override string ConnectionString => Get("ORACLE_CONNECTION_STRING", "");
        public override string GetConnectionInfo() => $"{Host}:{Port}/{Database}";

        private static string Get(string key, string def) => Environment.GetEnvironmentVariable(key) ?? def;
        private static bool GetBool(string key, bool def) => (Environment.GetEnvironmentVariable(key)?.ToLower() ?? (def ? "true" : "false")) == "true";
    }
}
