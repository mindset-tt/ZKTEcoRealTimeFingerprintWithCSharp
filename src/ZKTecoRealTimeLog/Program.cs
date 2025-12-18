using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

namespace ZKTecoRealTimeLog
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Check for console mode
            bool isConsoleMode = args.Contains("--console") || args.Contains("-c");
            bool showHelp = args.Contains("--help") || args.Contains("-h") || args.Contains("/?");
            bool showDbTypes = args.Contains("--db-types");

            if (showHelp)
            {
                ShowHelp();
                return;
            }

            if (showDbTypes)
            {
                Database.DatabaseFactory.PrintSupportedDatabases();
                return;
            }

            if (args.Contains("--batch-sync"))
            {
                await RunBatchSyncAsync();
                return;
            }

            if (isConsoleMode)
            {
                // Run in console mode (interactive)
                Console.WriteLine("===========================================");
                Console.WriteLine("   ZKTeco Real-Time Attendance Monitor");
                Console.WriteLine("   Console Mode");
                Console.WriteLine("===========================================");
                Console.WriteLine();
            }

            var builder = Host.CreateApplicationBuilder(args);

            // Configure logging
            builder.Logging.ClearProviders();
            
            if (isConsoleMode)
            {
                // Console mode: show logs in console
                builder.Logging.AddConsole();
                builder.Logging.SetMinimumLevel(LogLevel.Information);
            }
            else
            {
                // Service mode: log to both Event Log and Console (for file capture)
                builder.Logging.AddEventLog(new EventLogSettings
                {
                    SourceName = "ZKTeco Attendance",
                    LogName = "Application"
                });
                builder.Logging.SetMinimumLevel(LogLevel.Information);
            }

            // Add the worker service
            builder.Services.AddHostedService<AttendanceWorker>();

            // Configure as Windows Service (when not in console mode)
            if (!isConsoleMode)
            {
                builder.Services.AddWindowsService(options =>
                {
                    options.ServiceName = "ZKTeco Attendance Service";
                });
            }

            var host = builder.Build();

            if (isConsoleMode)
            {
                Console.WriteLine("Press Ctrl+C to stop...\n");
            }

            await host.RunAsync();
        }


        static async Task RunBatchSyncAsync()
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("   ZKTeco Real-Time Attendance Monitor");
            Console.WriteLine("   BATCH SYNC MODE");
            Console.WriteLine("===========================================");
            Console.WriteLine();

            // 1. Load .env
            string envPath = Path.Combine(AppContext.BaseDirectory, ".env");
            if (File.Exists(envPath))
            {
                foreach (var line in File.ReadAllLines(envPath))
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                            Environment.SetEnvironmentVariable(key, value);
                    }
                }
            }

            // 2. Initialize Databases
            using var databases = new Database.MultiDatabaseManager();
            await databases.InitializeFromEnvironmentAsync();

            if (databases.ActiveCount == 0)
            {
                Console.WriteLine("Error: No databases enabled. Configure .env file.");
                return;
            }

            // 3. Clear Data (Safety Warning handled by script usually, but here we do it)
            Console.WriteLine("Wait 5 seconds... Press Ctrl+C to cancel if this is mistake.");
            await Task.Delay(5000); // Safety delay

            Console.WriteLine("Clearing existing data...");
            await databases.ClearAllDataAsync();

            // 4. Connect Devices
            using var deviceManager = new MultiDeviceManager();
            var configs = MultiDeviceManager.LoadDeviceConfigs();
            
            if (configs.Count == 0)
            {
                Console.WriteLine("Error: No devices configured.");
                return;
            }

            deviceManager.ConnectAll(configs);
            if (deviceManager.ConnectedCount == 0)
            {
                Console.WriteLine("Error: Could not connect to any devices.");
                return;
            }

            // 5. Sync Data
            Console.WriteLine($"Fetching logs from {deviceManager.ConnectedCount} devices...");
            
            // Allow buffers to download? (Actually ReadAllLogs uses blocking calls usually)
            // But sometimes need a moment after connect.
            await Task.Delay(2000); 

            var logs = deviceManager.ReadAllLogs();
            Console.WriteLine($"Found {logs.Count} Total Logs.");

            Console.WriteLine("Processing logs...");
            
            // Sort by time to ensure correct "First in / Last out" logic
            var orderedLogs = logs.OrderBy(l => l.EventTime).ToList();
            
            int count = 0;
            foreach (var log in orderedLogs)
            {
                // Simulate event processing
                count++;
                if (count % 100 == 0) Console.Write($".");

                await databases.InsertAttendanceLogAsync(
                    log.EnrollNumber, 
                    log.EventTime, 
                    true, // Assuming logs from device are valid
                    log.AttState, 
                    "Unknown", // Description might need helper 
                    log.VerifyMethod, 
                    "Unknown", 
                    log.WorkCode, 
                    log.DeviceIP, 
                    log.DeviceName
                );
            }
            Console.WriteLine();
            Console.WriteLine("Batch Sync Completed Successfully.");
        }

        static void ShowHelp()
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("   ZKTeco Real-Time Attendance Monitor");
            Console.WriteLine("   Multi-Device Edition v2.0");
            Console.WriteLine("===========================================");
            Console.WriteLine();
            Console.WriteLine("Usage: ZKTecoRealTimeLog [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --console, -c   Run in console mode (interactive)");
            Console.WriteLine("  --help, -h      Show this help message");
            Console.WriteLine("  --db-types      Show supported database types");
            Console.WriteLine("  --batch-sync    Clear DB and Sync all history from devices");
            Console.WriteLine();
            Console.WriteLine("Windows Service Commands (run as Administrator):");
            Console.WriteLine();
            Console.WriteLine("  Install service:");
            Console.WriteLine("    sc create \"ZKTeco Attendance\" binPath= \"C:\\path\\to\\ZKTecoRealTimeLog.exe\"");
            Console.WriteLine();
            Console.WriteLine("  Start service:");
            Console.WriteLine("    sc start \"ZKTeco Attendance\"");
            Console.WriteLine();
            Console.WriteLine("  Stop service:");
            Console.WriteLine("    sc stop \"ZKTeco Attendance\"");
            Console.WriteLine();
            Console.WriteLine("  Delete service:");
            Console.WriteLine("    sc delete \"ZKTeco Attendance\"");
            Console.WriteLine();
            Console.WriteLine("  View service status:");
            Console.WriteLine("    sc query \"ZKTeco Attendance\"");
            Console.WriteLine();
            Console.WriteLine("Multi-Device Configuration (.env file):");
            Console.WriteLine();
            Console.WriteLine("  # Device 1");
            Console.WriteLine("  DEVICE_1_ENABLED=true");
            Console.WriteLine("  DEVICE_1_NAME=Entrance");
            Console.WriteLine("  DEVICE_1_IP=192.168.1.201");
            Console.WriteLine("  DEVICE_1_PORT=4370");
            Console.WriteLine();
            Console.WriteLine("  # Device 2");
            Console.WriteLine("  DEVICE_2_ENABLED=true");
            Console.WriteLine("  DEVICE_2_NAME=Exit");
            Console.WriteLine("  DEVICE_2_IP=192.168.1.202");
            Console.WriteLine();
            Console.WriteLine("  # Add more devices: DEVICE_3_*, DEVICE_4_*, etc. (up to 20)");
            Console.WriteLine();
            Console.WriteLine("Database Configuration:");
            Console.WriteLine("  POSTGRES_ENABLED=true/false");
            Console.WriteLine("  MYSQL_ENABLED=true/false");
            Console.WriteLine("  SQLSERVER_ENABLED=true/false");
            Console.WriteLine("  SQLITE_ENABLED=true/false");
            Console.WriteLine("  ORACLE_ENABLED=true/false");
            Console.WriteLine();
            Console.WriteLine("Supported Platforms:");
            Console.WriteLine("  x86 (32-bit) - Default, required for standard zkemkeeper.dll");
            Console.WriteLine("  x64 (64-bit) - Requires 64-bit zkemkeeper.dll from ZKTeco");
            Console.WriteLine("  ARM64        - Requires ARM64 zkemkeeper.dll from ZKTeco");
            Console.WriteLine();
            Console.WriteLine("Prerequisites:");
            Console.WriteLine("  Register zkemkeeper.dll: regsvr32 zkemkeeper.dll (as Admin)");
        }
    }
}
