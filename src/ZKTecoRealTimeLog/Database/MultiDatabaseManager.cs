using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZKTecoRealTimeLog.Database
{
    #region MultiDatabaseManager

    /// <summary>
    /// Manages multiple database connections simultaneously
    /// </summary>
    public class MultiDatabaseManager : IDisposable
    {
        #region Fields & Properties

        private readonly List<IDatabaseProvider> _providers = new();
        private readonly List<string> _enabledDatabases = new();
        private bool _disposed;

        public event Action<string>? OnLog;

        public IReadOnlyList<IDatabaseProvider> Providers => _providers;
        public IReadOnlyList<string> EnabledDatabaseNames => _enabledDatabases;
        public int ActiveCount => _providers.Count;

        #endregion

        #region Private Methods

        private void Log(string message, bool isError = false)
        {
            OnLog?.Invoke(message);
            if (isError) Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            if (isError) Console.ResetColor();
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

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize all enabled databases from environment configuration
        /// </summary>
        public async Task InitializeFromEnvironmentAsync()
        {
            await TryAddDatabaseAsync("postgresql", new PostgresDatabaseConfig());
            await TryAddDatabaseAsync("mysql", new MySqlDatabaseConfig());
            await TryAddDatabaseAsync("sqlserver", new SqlServerDatabaseConfig());
            await TryAddDatabaseAsync("sqlite", new SqliteDatabaseConfig());
            await TryAddDatabaseAsync("oracle", new OracleDatabaseConfig());
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

        #endregion
    }

    #endregion

    #region Database Configurations

    /// <summary>
    /// Base class for individual database configuration
    /// </summary>
    public abstract class IndividualDatabaseConfig
    {
        protected static string Env(string key, string def = "") =>
            Environment.GetEnvironmentVariable(key) ?? def;
        
        protected static bool EnvBool(string key, bool def = false) =>
            (Environment.GetEnvironmentVariable(key)?.ToLower() ?? (def ? "true" : "false")) == "true";

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
        public override bool Enabled => EnvBool("POSTGRES_ENABLED");
        public override string Host => Env("POSTGRES_HOST", "localhost");
        public override string Port => Env("POSTGRES_PORT", "5432");
        public override string Database => Env("POSTGRES_DATABASE", "zkteco");
        public override string User => Env("POSTGRES_USER", "postgres");
        public override string Password => Env("POSTGRES_PASSWORD");
        public override string ConnectionString => Env("POSTGRES_CONNECTION_STRING");
        public override string GetConnectionInfo() => $"{Host}:{Port}/{Database}";
    }

    /// <summary>
    /// MySQL configuration
    /// </summary>
    public class MySqlDatabaseConfig : IndividualDatabaseConfig
    {
        public override string Type => "mysql";
        public override bool Enabled => EnvBool("MYSQL_ENABLED");
        public override string Host => Env("MYSQL_HOST", "localhost");
        public override string Port => Env("MYSQL_PORT", "3306");
        public override string Database => Env("MYSQL_DATABASE", "zkteco");
        public override string User => Env("MYSQL_USER", "root");
        public override string Password => Env("MYSQL_PASSWORD");
        public override string ConnectionString => Env("MYSQL_CONNECTION_STRING");
        public override string GetConnectionInfo() => $"{Host}:{Port}/{Database}";
    }

    /// <summary>
    /// SQL Server configuration
    /// </summary>
    public class SqlServerDatabaseConfig : IndividualDatabaseConfig
    {
        public override string Type => "sqlserver";
        public override bool Enabled => EnvBool("SQLSERVER_ENABLED");
        public override string Host => Env("SQLSERVER_HOST", "localhost");
        public override string Port => Env("SQLSERVER_PORT", "1433");
        public override string Database => Env("SQLSERVER_DATABASE", "zkteco");
        public override string User => Env("SQLSERVER_USER", "sa");
        public override string Password => Env("SQLSERVER_PASSWORD");
        public override string ConnectionString => Env("SQLSERVER_CONNECTION_STRING");
        public override string GetConnectionInfo() => $"{Host}:{Port}/{Database}";
    }

    /// <summary>
    /// SQLite configuration
    /// </summary>
    public class SqliteDatabaseConfig : IndividualDatabaseConfig
    {
        public override string Type => "sqlite";
        public override bool Enabled => EnvBool("SQLITE_ENABLED");
        public override string Host => "";
        public override string Port => "";
        public override string Database => Env("SQLITE_DATABASE", "zkteco.db");
        public override string User => "";
        public override string Password => "";
        public override string ConnectionString => Env("SQLITE_CONNECTION_STRING");
        public override string GetConnectionInfo() => Database;
    }

    /// <summary>
    /// Oracle configuration
    /// </summary>
    public class OracleDatabaseConfig : IndividualDatabaseConfig
    {
        public override string Type => "oracle";
        public override bool Enabled => EnvBool("ORACLE_ENABLED");
        public override string Host => Env("ORACLE_HOST", "localhost");
        public override string Port => Env("ORACLE_PORT", "1521");
        public override string Database => Env("ORACLE_DATABASE", "ORCL");
        public override string User => Env("ORACLE_USER", "system");
        public override string Password => Env("ORACLE_PASSWORD");
        public override string ConnectionString => Env("ORACLE_CONNECTION_STRING");
        public override string GetConnectionInfo() => $"{Host}:{Port}/{Database}";
    }

    #endregion
}

