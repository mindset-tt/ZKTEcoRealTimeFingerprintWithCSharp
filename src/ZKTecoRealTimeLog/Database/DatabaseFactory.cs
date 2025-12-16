using System;

namespace ZKTecoRealTimeLog.Database
{
    /// <summary>
    /// Factory for creating database providers
    /// </summary>
    public static class DatabaseFactory
    {
        /// <summary>
        /// Supported database types
        /// </summary>
        public static readonly string[] SupportedDatabases = new[]
        {
            "postgresql",
            "mysql",
            "sqlserver",
            "sqlite",
            "oracle",
            "none"
        };

        /// <summary>
        /// Create a database provider based on configuration
        /// </summary>
        public static IDatabaseProvider? CreateProvider(DatabaseConfig config)
        {
            if (!config.Enabled || string.IsNullOrEmpty(config.Type) || config.Type == "none")
            {
                return null;
            }

            return config.Type.ToLower() switch
            {
                "postgresql" or "postgres" or "pgsql" => new PostgreSqlProvider(config),
                "mysql" or "mariadb" => new MySqlProvider(config),
                "sqlserver" or "mssql" => new SqlServerProvider(config),
                "sqlite" or "sqlite3" => new SqliteProvider(config),
                "oracle" => new OracleProvider(config),
                _ => throw new ArgumentException($"Unsupported database type: {config.Type}. Supported types: {string.Join(", ", SupportedDatabases)}")
            };
        }

        /// <summary>
        /// Get the default port for a database type
        /// </summary>
        public static string GetDefaultPort(string dbType)
        {
            return dbType.ToLower() switch
            {
                "postgresql" or "postgres" or "pgsql" => "5432",
                "mysql" or "mariadb" => "3306",
                "sqlserver" or "mssql" => "1433",
                "oracle" => "1521",
                _ => ""
            };
        }

        /// <summary>
        /// Print supported databases to console
        /// </summary>
        public static void PrintSupportedDatabases()
        {
            Console.WriteLine("Supported database types:");
            Console.WriteLine("  postgresql (aliases: postgres, pgsql) - Port 5432");
            Console.WriteLine("  mysql (alias: mariadb) - Port 3306");
            Console.WriteLine("  sqlserver (alias: mssql) - Port 1433");
            Console.WriteLine("  sqlite (alias: sqlite3) - File-based, no server required");
            Console.WriteLine("  oracle - Port 1521");
            Console.WriteLine("  none - Disable database logging");
        }
    }
}
