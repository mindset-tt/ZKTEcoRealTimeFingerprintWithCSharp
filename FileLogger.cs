using System;
using System.IO;
using System.Text;

namespace ZKTecoRealTimeLog
{
    public class FileLogger : IDisposable
    {
        private readonly string _logFilePath;
        private readonly object _lock = new object();
        private StreamWriter? _writer;
        private readonly bool _enabled;

        public FileLogger(string? logFilePath = null, bool append = false)
        {
            // Default log path
            if (string.IsNullOrWhiteSpace(logFilePath))
            {
                var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
                Directory.CreateDirectory(logDir);
                _logFilePath = Path.Combine(logDir, $"zkteco_{DateTime.Now:yyyyMMdd}.log");
            }
            else
            {
                _logFilePath = logFilePath;
                var dir = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }

            _enabled = true;

            try
            {
                // Open with FileShare.ReadWrite to allow other programs (like Notepad) to read it while open
                var fileStream = new FileStream(_logFilePath, 
                    append ? FileMode.Append : FileMode.Create, 
                    FileAccess.Write, 
                    FileShare.ReadWrite);

                _writer = new StreamWriter(fileStream, Encoding.UTF8)
                {
                    AutoFlush = true
                };

                if (!append)
                {
                    Log("INFO", "File logger initialized (New Session)");
                }
                else
                {
                    Log("INFO", "File logger attached (Appending)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOG] Warning: Could not create log file: {ex.Message}");
                _enabled = false;
            }
        }

        public string LogFilePath => _logFilePath;
        public bool IsEnabled => _enabled && _writer != null;

        public void Log(string level, string message)
        {
            if (!IsEnabled) return;

            lock (_lock)
            {
                try
                {
                    var logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
                    _writer?.WriteLine(logLine);
                }
                catch { }
            }
        }

        public void LogAttendance(
            string enrollNumber,
            DateTime eventTime,
            bool isValid,
            int attState,
            string attStateDesc,
            int verifyMethod,
            string verifyMethodDesc,
            int workCode,
            string? deviceName = null,
            string? deviceIp = null)
        {
            var sb = new StringBuilder();
            sb.Append($"ATTENDANCE | ");
            if (!string.IsNullOrEmpty(deviceName))
                sb.Append($"Device: {deviceName} | ");
            if (!string.IsNullOrEmpty(deviceIp))
                sb.Append($"IP: {deviceIp} | ");
            sb.Append($"User: {enrollNumber} | ");
            sb.Append($"Time: {eventTime:yyyy-MM-dd HH:mm:ss} | ");
            sb.Append($"Valid: {isValid} | ");
            sb.Append($"State: {attStateDesc} ({attState}) | ");
            sb.Append($"Method: {verifyMethodDesc} ({verifyMethod})");
            if (workCode != 0)
                sb.Append($" | WorkCode: {workCode}");

            Log("EVENT", sb.ToString());
        }

        public void LogInfo(string message) => Log("INFO", message);
        public void LogError(string message) => Log("ERROR", message);
        public void LogWarning(string message) => Log("WARN", message);
        public void LogEvent(string message) => Log("EVENT", message);

        public void Dispose()
        {
            lock (_lock)
            {
                _writer?.Dispose();
                _writer = null;
            }
        }
    }
}
