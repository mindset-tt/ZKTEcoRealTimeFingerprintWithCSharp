using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZKTecoRealTimeLog.Database;

namespace ZKTecoRealTimeLog
{
    /// <summary>
    /// Background worker service for ZKTeco attendance monitoring.
    /// Runs as a Windows Service or console application.
    /// </summary>
    public class AttendanceWorker : BackgroundService
    {
        private readonly ILogger<AttendanceWorker> _logger;
        private MultiDeviceManager? _deviceManager;
        private MultiDatabaseManager? _databases;
        private FileLogger? _fileLogger;

        public AttendanceWorker(ILogger<AttendanceWorker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Important: Yield immediately to allow the service to report as started
            await Task.Yield();

            _logger.LogInformation("ZKTeco Attendance Service starting...");

            try
            {
                // Load .env file
                LoadEnvFile();

                // Initialize file logger - ALWAYS enabled for both console and service mode
                string logFilePath = Environment.GetEnvironmentVariable("LOG_FILE_PATH") ?? "";
                // overwrite = true (append = false), as requested by user ("log will be gone out")
                _fileLogger = new FileLogger(string.IsNullOrWhiteSpace(logFilePath) ? null : logFilePath, append: false);
                
                // Log startup info to file
                _fileLogger?.LogInfo("===========================================");
                _fileLogger?.LogInfo("ZKTeco Attendance Service Starting");
                _fileLogger?.LogInfo($"Version: 2.0.0");
                _fileLogger?.LogInfo($"Base Directory: {AppContext.BaseDirectory}");
                _fileLogger?.LogInfo("===========================================");
                
                if (_fileLogger?.IsEnabled == true)
                {
                    _logger.LogInformation("Log file: {LogPath}", _fileLogger.LogFilePath);
                }
                else
                {
                    _logger.LogWarning("File logging is disabled or failed to initialize");
                }

                // Initialize databases
                _logger.LogInformation("Initializing databases...");
                _fileLogger?.LogInfo("Initializing databases...");
                _databases = new MultiDatabaseManager();
                
                try
                {
                    await _databases.InitializeFromEnvironmentAsync();

                    if (_databases.ActiveCount == 0)
                    {
                        _logger.LogWarning("No databases enabled");
                    }
                    else
                    {
                        _logger.LogInformation("Connected to {Count} database(s): {Names}", 
                            _databases.ActiveCount, string.Join(", ", _databases.EnabledDatabaseNames));
                        _fileLogger?.LogInfo($"Connected to {_databases.ActiveCount} database(s)");
                    }
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Database initialization failed: {Message}", dbEx.Message);
                    _fileLogger?.LogError($"Database initialization failed: {dbEx.Message}");
                    // Continue without databases
                }

                // Load device configurations
                List<DeviceConfig> deviceConfigs;
                try
                {
                    deviceConfigs = MultiDeviceManager.LoadDeviceConfigs();
                }
                catch (Exception cfgEx)
                {
                    _logger.LogError(cfgEx, "Failed to load device configurations: {Message}", cfgEx.Message);
                    _fileLogger?.LogError($"Failed to load device configurations: {cfgEx.Message}");
                    deviceConfigs = new List<DeviceConfig>();
                }
                
                if (deviceConfigs.Count == 0)
                {
                    _logger.LogError("No devices configured! Set DEVICE_1_IP, DEVICE_1_PORT in .env file");
                    _fileLogger?.LogError("No devices configured");
                    
                    // Keep service running but idle - don't exit
                    _logger.LogWarning("Service running in idle mode. Configure devices and restart the service.");
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        await Task.Delay(5000, stoppingToken);
                    }
                    return;
                }

                _logger.LogInformation("Connecting to {Count} device(s)...", deviceConfigs.Count);
                _fileLogger?.LogInfo($"Connecting to {deviceConfigs.Count} device(s)");

                // Initialize multi-device manager
                try
                {
                    _deviceManager = new MultiDeviceManager();

                    // Subscribe to events
                    _deviceManager.OnAttendance += HandleAttendanceEvent;
                    _deviceManager.OnFingerPlaced += HandleFingerPlaced;
                    _deviceManager.OnVerify += HandleVerify;
                    _deviceManager.OnCard += HandleCard;
                    _deviceManager.OnNewUser += HandleNewUser;
                    _deviceManager.OnDisconnected += HandleDisconnected;

                    // Connect to all devices
                    _deviceManager.ConnectAll(deviceConfigs);
                }
                catch (Exception devEx)
                {
                    _logger.LogError(devEx, "Failed to initialize device manager: {Message}", devEx.Message);
                    _fileLogger?.LogError($"Failed to initialize device manager: {devEx.Message}");
                    
                    // Keep service running
                    _logger.LogWarning("Service running without devices. Check zkemkeeper.dll registration.");
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        await Task.Delay(5000, stoppingToken);
                    }
                    return;
                }

                if (_deviceManager.ConnectedCount == 0)
                {
                    _logger.LogWarning("Failed to connect to any device. Service will keep running.");
                    _fileLogger?.LogWarning("Failed to connect to any device");
                }
                else
                {
                    _logger.LogInformation("Connected to {Count}/{Total} devices", 
                        _deviceManager.ConnectedCount, deviceConfigs.Count);
                    _fileLogger?.LogInfo($"Connected to {_deviceManager.ConnectedCount} device(s)");

                    // Log device info
                    foreach (var info in _deviceManager.GetAllDeviceInfo())
                    {
                        _logger.LogInformation("Device: {Name} ({IP}) - Serial: {Serial}, Users: {Users}, FP: {FP}",
                            info.Name, info.IP, info.SerialNumber, info.UserCount, info.FingerprintCount);
                    }
                }

                _logger.LogInformation("Service started. Waiting for attendance events...");

                // Keep running until cancellation requested
                while (!stoppingToken.IsCancellationRequested)
                {
                    // Watchdog: Check connections every 30 seconds
                    if (_deviceManager != null)
                    {
                        try
                        {
                            _deviceManager.MaintainConnections();
                        }
                        catch (Exception wdEx)
                        {
                            _logger.LogError(wdEx, "Watchdog error: {Message}", wdEx.Message);
                        }
                    }
                    
                    await Task.Delay(30000, stoppingToken); // Check every 30 seconds
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service error: {Message}", ex.Message);
                _fileLogger?.LogError($"Service error: {ex.Message}");
            }
            finally
            {
                await Cleanup();
            }
        }

        private async Task Cleanup()
        {
            _logger.LogInformation("Service stopping...");
            _fileLogger?.LogInfo("Service stopping");

            if (_deviceManager != null)
            {
                _deviceManager.DisconnectAll();
                _deviceManager.Dispose();
                _deviceManager = null;
            }

            _fileLogger?.Dispose();
            _databases?.Dispose();

            await Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stop requested...");
            await base.StopAsync(cancellationToken);
        }

        private void HandleAttendanceEvent(ZKDevice device, AttendanceEventArgs args)
        {
            _logger.LogInformation(
                "ATTENDANCE: [{Device}] User={User}, Time={Time}, Valid={Valid}, State={State}, Method={Method}",
                device.Name, args.EnrollNumber, args.EventTime, args.IsValid, 
                args.AttStateDescription, args.VerifyMethodDescription);

            // Log to file
            _fileLogger?.LogAttendance(args.EnrollNumber, args.EventTime, args.IsValid, 
                args.AttState, args.AttStateDescription, args.VerifyMethod, args.VerifyMethodDescription, 
                args.WorkCode, device.Name, device.IP);

            // Save to all enabled databases
            _ = Task.Run(async () =>
            {
                try
                {
                    if (_databases != null && _databases.ActiveCount > 0)
                    {
                        await _databases.InsertAttendanceLogAsync(
                            args.EnrollNumber, args.EventTime, args.IsValid, args.AttState, args.AttStateDescription,
                            args.VerifyMethod, args.VerifyMethodDescription, args.WorkCode, device.IP, device.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Database insert error: {Message}", ex.Message);
                    _fileLogger?.LogError($"DB insert error: {ex.Message}");
                }
            });
        }

        private void HandleFingerPlaced(ZKDevice device)
        {
            _logger.LogDebug("[{Device}] Finger placed on sensor", device.Name);
            _fileLogger?.LogEvent($"[{device.Name}] Finger placed on sensor");
        }

        private void HandleVerify(ZKDevice device, int userId)
        {
            if (userId == -1)
            {
                _logger.LogWarning("[{Device}] Verification FAILED - User not recognized", device.Name);
                _fileLogger?.LogEvent($"[{device.Name}] Verification FAILED");
            }
            else
            {
                _logger.LogInformation("[{Device}] Verification successful - User ID: {UserId}", device.Name, userId);
                _fileLogger?.LogEvent($"[{device.Name}] Verification successful - User ID: {userId}");
            }
        }

        private void HandleCard(ZKDevice device, int cardNumber)
        {
            if (cardNumber == 0)
            {
                _logger.LogWarning("[{Device}] Card verification FAILED", device.Name);
                _fileLogger?.LogEvent($"[{device.Name}] Card verification FAILED");
            }
            else
            {
                _logger.LogInformation("[{Device}] Card swiped - Card Number: {CardNumber}", device.Name, cardNumber);
                _fileLogger?.LogEvent($"[{device.Name}] Card swiped - Card Number: {cardNumber}");
            }
        }

        private void HandleNewUser(ZKDevice device, int enrollNumber)
        {
            _logger.LogInformation("[{Device}] New user registered - User ID: {UserId}", device.Name, enrollNumber);
            _fileLogger?.LogEvent($"[{device.Name}] New user registered - User ID: {enrollNumber}");
        }

        private void HandleDisconnected(ZKDevice device)
        {
            _logger.LogWarning("[{Device}] Device disconnected!", device.Name);
            _fileLogger?.LogWarning($"[{device.Name}] Device disconnected");
        }

        private void LoadEnvFile()
        {
            string envPath = Path.Combine(AppContext.BaseDirectory, ".env");
            if (!File.Exists(envPath))
            {
                envPath = ".env";
            }

            if (File.Exists(envPath))
            {
                try
                {
                    foreach (var line in File.ReadAllLines(envPath))
                    {
                        var trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                            continue;

                        var parts = trimmed.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim();
                            var value = parts[1].Trim();
                            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                            {
                                Environment.SetEnvironmentVariable(key, value);
                            }
                        }
                    }
                    _logger.LogInformation("Loaded configuration from .env file");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Could not load .env file: {Message}", ex.Message);
                }
            }
        }
    }
}
