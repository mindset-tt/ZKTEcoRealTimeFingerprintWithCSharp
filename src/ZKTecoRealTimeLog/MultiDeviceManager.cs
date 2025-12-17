using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZKTecoRealTimeLog.Database;

namespace ZKTecoRealTimeLog
{
    #region ZKDevice

    /// <summary>
    /// Represents a single ZKTeco device connection
    /// </summary>
    public class ZKDevice : IDisposable
    {
        #region Fields & Properties

        private dynamic? _zkDevice;
        private bool _isConnected = false;
        private readonly int _machineNumber = 1;
        private bool _disposed = false;

        public string Name { get; }
        public string IP { get; }
        public int Port { get; }
        public bool IsConnected => _isConnected;
        public string SerialNumber { get; private set; } = "";

        // Events
        public event Action<ZKDevice, AttendanceEventArgs>? OnAttendance;
        public event Action<ZKDevice>? OnFingerPlaced;
        public event Action<ZKDevice, int>? OnVerify;
        public event Action<ZKDevice, int>? OnCard;
        public event Action<ZKDevice, int>? OnNewUser;
        public event Action<ZKDevice>? OnDisconnected;

        #endregion

        #region Constructor

        public ZKDevice(string name, string ip, int port = 4370)
        {
            Name = name;
            IP = ip;
            Port = port;
        }

        #endregion

        #region Connection

        public bool Connect()
        {
            try
            {
                // Ensure any previous instance is cleaned up
                Disconnect();

                // Create COM object dynamically
                Type? zkType = Type.GetTypeFromProgID("zkemkeeper.ZKEM.1");
                if (zkType == null)
                {
                    zkType = Type.GetTypeFromProgID("zkemkeeper.ZKEM");
                }

                if (zkType == null)
                {
                    return false;
                }

                _zkDevice = Activator.CreateInstance(zkType);

                if (_zkDevice == null)
                {
                    return false;
                }

                // Connect to device
                _isConnected = _zkDevice.Connect_Net(IP, Port);

                if (_isConnected)
                {
                    // Get serial number
                    _zkDevice.GetSerialNumber(_machineNumber, out string serial);
                    SerialNumber = serial ?? "";

                    // Register events
                    RegisterEvents();

                    // Enable real-time monitoring
                    _zkDevice.RegEvent(_machineNumber, 65535);

                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public void Disconnect()
        {
            if (_zkDevice != null)
            {
                try
                {
                    _zkDevice.Disconnect();
                }
                catch { }

                try
                {
                    // Release COM object to prevent memory leaks
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(_zkDevice);
                }
                catch { }
                
                _zkDevice = null;
            }
            _isConnected = false;
        }

        public bool Ping()
        {
            if (!_isConnected || _zkDevice == null) return false;

            try
            {
                int year = 0, month = 0, day = 0, hour = 0, minute = 0, second = 0;
                // Reading device time is a lightweight way to check connection
                return _zkDevice!.GetDeviceTime(_machineNumber, ref year, ref month, ref day, ref hour, ref minute, ref second);
            }
            catch
            {
                return false;
            }
        }

        public bool Reconnect()
        {
            Disconnect();
            Thread.Sleep(1000); // Wait a bit before reconnecting
            return Connect();
        }

        #endregion

        #region Device Info

        public DeviceInfo? GetDeviceInfo()
        {
            if (_zkDevice == null || !_isConnected) return null;

            try
            {
                string serialNumber = "";
                string firmwareVersion = "";
                int userCount = 0;
                int fpCount = 0;
                int logCount = 0;

                _zkDevice!.GetSerialNumber(_machineNumber, out serialNumber);
                _zkDevice!.GetFirmwareVersion(_machineNumber, ref firmwareVersion);
                _zkDevice!.GetDeviceStatus(_machineNumber, 1, ref userCount);
                _zkDevice!.GetDeviceStatus(_machineNumber, 2, ref fpCount);
                _zkDevice!.GetDeviceStatus(_machineNumber, 6, ref logCount);

                return new DeviceInfo
                {
                    Name = Name,
                    IP = IP,
                    Port = Port,
                    SerialNumber = serialNumber,
                    FirmwareVersion = firmwareVersion,
                    UserCount = userCount,
                    FingerprintCount = fpCount,
                    AttendanceLogCount = logCount
                };
            }
            catch
            {
                return null;
            }
        }

        public List<AttendanceLog> ReadAllLogs()
        {
            var logs = new List<AttendanceLog>();
            if (_zkDevice == null || !_isConnected) return logs;

            try
            {
                if (_zkDevice!.ReadGeneralLogData(_machineNumber))
                {
                    string enrollNumber = "";
                    int verifyMode = 0;
                    int inOutMode = 0;
                    int year = 0, month = 0, day = 0, hour = 0, minute = 0, second = 0;
                    int workCode = 0;

                    while (_zkDevice!.SSR_GetGeneralLogData(_machineNumber, out enrollNumber, out verifyMode,
                        out inOutMode, out year, out month, out day, out hour, out minute, out second, ref workCode))
                    {
                        logs.Add(new AttendanceLog
                        {
                            EnrollNumber = enrollNumber,
                            EventTime = new DateTime(year, month, day, hour, minute, second),
                            VerifyMethod = verifyMode,
                            AttState = inOutMode,
                            WorkCode = workCode,
                            DeviceName = Name,
                            DeviceIP = IP
                        });
                    }
                }
            }
            catch { }

            return logs;
        }

        #endregion

        #region Events

        private void RegisterEvents()
        {
            if (_zkDevice == null) return;

            try
            {
                // Attendance transaction event
                _zkDevice.OnAttTransactionEx += new Action<string, int, int, int, int, int, int, int, int, int, int>(
                    (enrollNumber, isInValid, attState, verifyMethod, year, month, day, hour, minute, second, workCode) =>
                    {
                        var args = new AttendanceEventArgs
                        {
                            EnrollNumber = enrollNumber,
                            IsValid = isInValid == 0,
                            AttState = attState,
                            VerifyMethod = verifyMethod,
                            EventTime = new DateTime(year, month, day, hour, minute, second),
                            WorkCode = workCode,
                            DeviceName = Name,
                            DeviceIP = IP
                        };
                        OnAttendance?.Invoke(this, args);
                    });

                // Finger placed on sensor
                _zkDevice.OnFinger += new Action(() =>
                {
                    OnFingerPlaced?.Invoke(this);
                });

                // Verification result
                _zkDevice.OnVerify += new Action<int>((userId) =>
                {
                    OnVerify?.Invoke(this, userId);
                });

                // Card/HID event
                _zkDevice.OnHIDNum += new Action<int>((cardNumber) =>
                {
                    OnCard?.Invoke(this, cardNumber);
                });

                // New user registered
                _zkDevice.OnNewUser += new Action<int>((enrollNumber) =>
                {
                    OnNewUser?.Invoke(this, enrollNumber);
                });

                // Device disconnected
                _zkDevice.OnDisConnected += new Action(() =>
                {
                    _isConnected = false;
                    OnDisconnected?.Invoke(this);
                });
            }
            catch { }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Disconnect();
        }

        #endregion
    }

    #endregion

    #region Models

    /// <summary>
    /// Attendance event arguments
    /// </summary>
    public class AttendanceEventArgs
    {
        public string EnrollNumber { get; set; } = "";
        public bool IsValid { get; set; }
        public int AttState { get; set; }
        public int VerifyMethod { get; set; }
        public DateTime EventTime { get; set; }
        public int WorkCode { get; set; }
        public string DeviceName { get; set; } = "";
        public string DeviceIP { get; set; } = "";

        public string AttStateDescription => AttState switch
        {
            0 => "Check-In",
            1 => "Check-Out",
            2 => "Break-Out",
            3 => "Break-In",
            4 => "OT-In",
            5 => "OT-Out",
            _ => $"State{AttState}"
        };

        public string VerifyMethodDescription => VerifyMethod switch
        {
            0 => "Password",
            1 => "Fingerprint",
            2 => "Card",
            3 => "Password+FP",
            4 => "Password+Card",
            5 => "FP+Card",
            6 => "FP+Pwd+Card",
            7 => "Face",
            _ => $"Unknown({VerifyMethod})"
        };
    }

    /// <summary>
    /// Attendance log record
    /// </summary>
    public class AttendanceLog
    {
        public string EnrollNumber { get; set; } = "";
        public DateTime EventTime { get; set; }
        public int VerifyMethod { get; set; }
        public int AttState { get; set; }
        public int WorkCode { get; set; }
        public string DeviceName { get; set; } = "";
        public string DeviceIP { get; set; } = "";
    }

    /// <summary>
    /// Device information
    /// </summary>
    public class DeviceInfo
    {
        public string Name { get; set; } = "";
        public string IP { get; set; } = "";
        public int Port { get; set; }
        public string SerialNumber { get; set; } = "";
        public string FirmwareVersion { get; set; } = "";
        public int UserCount { get; set; }
        public int FingerprintCount { get; set; }
        public int AttendanceLogCount { get; set; }
    }

    /// <summary>
    /// Device configuration from environment
    /// </summary>
    public class DeviceConfig
    {
        public string Name { get; set; } = "";
        public string IP { get; set; } = "";
        public int Port { get; set; } = 4370;
        public bool Enabled { get; set; } = false;
    }

    #endregion

    #region MultiDeviceManager

    /// <summary>
    /// Manages multiple ZKTeco device connections
    /// </summary>
    public class MultiDeviceManager : IDisposable
    {
        #region Fields & Properties

        private readonly List<ZKDevice> _devices = new();
        private readonly object _lock = new object();
        private bool _disposed = false;

        public IReadOnlyList<ZKDevice> Devices => _devices;
        public int ConnectedCount => _devices.FindAll(d => d.IsConnected).Count;

        // Events (aggregated from all devices)
        public event Action<ZKDevice, AttendanceEventArgs>? OnAttendance;
        public event Action<ZKDevice>? OnFingerPlaced;
        public event Action<ZKDevice, int>? OnVerify;
        public event Action<ZKDevice, int>? OnCard;
        public event Action<ZKDevice, int>? OnNewUser;
        public event Action<ZKDevice>? OnDisconnected;

        // Log event for internal messages
        public event Action<string>? OnLog;

        #endregion

        #region Private Helpers

        private void Log(string message)
        {
            OnLog?.Invoke(message);
            Console.WriteLine(message);
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Load device configurations from environment variables
        /// </summary>
        public static List<DeviceConfig> LoadDeviceConfigs()
        {
            var configs = new List<DeviceConfig>();

            // Try numbered devices first (DEVICE_1_*, DEVICE_2_*, etc.)
            for (int i = 1; i <= 20; i++) // Support up to 20 devices
            {
                string prefix = $"DEVICE_{i}_";
                string? ip = Environment.GetEnvironmentVariable($"{prefix}IP");
                
                if (!string.IsNullOrWhiteSpace(ip))
                {
                    bool enabled = (Environment.GetEnvironmentVariable($"{prefix}ENABLED")?.ToLower() ?? "true") == "true";
                    
                    if (enabled)
                    {
                        configs.Add(new DeviceConfig
                        {
                            Name = Environment.GetEnvironmentVariable($"{prefix}NAME") ?? $"Device {i}",
                            IP = ip,
                            Port = int.TryParse(Environment.GetEnvironmentVariable($"{prefix}PORT"), out int port) ? port : 4370,
                            Enabled = true
                        });
                    }
                }
            }

            // If no numbered devices found, try legacy single device config
            if (configs.Count == 0)
            {
                string? ip = Environment.GetEnvironmentVariable("ZKTECO_IP");
                if (!string.IsNullOrWhiteSpace(ip))
                {
                    configs.Add(new DeviceConfig
                    {
                        Name = "Main Device",
                        IP = ip,
                        Port = int.TryParse(Environment.GetEnvironmentVariable("ZKTECO_PORT"), out int port) ? port : 4370,
                        Enabled = true
                    });
                }
            }

            return configs;
        }

        #endregion

        #region Connection Management

        /// <summary>
        /// Connect to all configured devices
        /// </summary>
        public void ConnectAll(List<DeviceConfig> configs)
        {
            foreach (var config in configs)
            {
                if (!config.Enabled) continue;

                var device = new ZKDevice(config.Name, config.IP, config.Port);
                
                // Subscribe to device events
                device.OnAttendance += (d, args) => OnAttendance?.Invoke(d, args);
                device.OnFingerPlaced += (d) => OnFingerPlaced?.Invoke(d);
                device.OnVerify += (d, userId) => OnVerify?.Invoke(d, userId);
                device.OnCard += (d, cardNum) => OnCard?.Invoke(d, cardNum);
                device.OnNewUser += (d, userId) => OnNewUser?.Invoke(d, userId);
                device.OnDisconnected += (d) => OnDisconnected?.Invoke(d);

                Console.Write($"  Connecting to {config.Name} ({config.IP}:{config.Port})... ");

                if (device.Connect())
                {
                    lock (_lock)
                    {
                        _devices.Add(device);
                    }
                    Console.ForegroundColor = ConsoleColor.Green;
                    Log($"✓ Connected {config.Name} (S/N: {device.SerialNumber})");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Log($"✗ Failed to connect to {config.Name}");
                    Console.ResetColor();
                    device.Dispose();
                }
            }
        }

        /// <summary>
        /// Disconnect all devices
        /// </summary>
        public void DisconnectAll()
        {
            lock (_lock)
            {
                foreach (var device in _devices)
                {
                    device.Disconnect();
                }
            }
        }

        /// <summary>
        /// Get info for all connected devices
        /// </summary>
        public List<DeviceInfo> GetAllDeviceInfo()
        {
            var infos = new List<DeviceInfo>();
            lock (_lock)
            {
                foreach (var device in _devices)
                {
                    var info = device.GetDeviceInfo();
                    if (info != null)
                    {
                        infos.Add(info);
                    }
                }
            }
            return infos;
        }

        /// <summary>
        /// Read all logs from all devices
        /// </summary>
        public List<AttendanceLog> ReadAllLogs()
        {
            var allLogs = new List<AttendanceLog>();
            lock (_lock)
            {
                foreach (var device in _devices)
                {
                    if (device.IsConnected)
                    {
                        var logs = device.ReadAllLogs();
                        allLogs.AddRange(logs);
                    }
                }
            }
            return allLogs;
        }

        #endregion

        #region Watchdog

        /// <summary>
        /// Maintain connections: Reconnect if dropped, Ping if connected
        /// </summary>
        public void MaintainConnections()
        {
            lock (_lock)
            {
                foreach (var device in _devices)
                {
                    if (!device.IsConnected)
                    {
                        Log($"[Watchdog] Attempting to reconnect {device.Name}...");
                        if (device.Connect())
                        {
                            Log($"[Watchdog] Reconnected {device.Name}!");
                        }
                    }
                    else
                    {
                         // It thinks it's connected, let's verify
                         if (!device.Ping())
                         {
                             Log($"[Watchdog] Connection lost to {device.Name}. Reconnecting...");
                             if (device.Reconnect())
                             {
                                 Log($"[Watchdog] Reconnected {device.Name}!");
                             }
                             else
                             {
                                 Log($"[Watchdog] Failed to reconnect {device.Name}.");
                             }
                         }
                    }
                }
            }
        }

        /// <summary>
        /// Print summary of connected devices
        /// </summary>
        public void PrintSummary()
        {
            Console.WriteLine($"\nConnected Devices: {ConnectedCount}");
            lock (_lock)
            {
                foreach (var device in _devices)
                {
                    if (device.IsConnected)
                    {
                        Console.WriteLine($"  • {device.Name}: {device.IP}:{device.Port} (S/N: {device.SerialNumber})");
                    }
                }
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            lock (_lock)
            {
                foreach (var device in _devices)
                {
                    device.Dispose();
                }
                _devices.Clear();
            }
        }

        #endregion
    }

    #endregion
}

