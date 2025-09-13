#nullable enable

using System;
using System.Diagnostics;
using System.IO;

namespace DANCustomTools.Services
{
    public class LogService : ILogService
    {
        private readonly string _logFilePath = Path.Combine(AppContext.BaseDirectory, "application.log");
        private static readonly object _lock = new object();

        private void Log(string level, string message, Exception? ex = null)
        {
            try
            {
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] - {message}";
                if (ex != null)
                {
                    logMessage += $"\nException: {ex}";
                }

                // Log to debug console
                Debug.WriteLine(logMessage);

                // Log to file with a lock to prevent race conditions
                lock (_lock)
                {
                    File.AppendAllText(_logFilePath, logMessage + "\n");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"FATAL: Failed to write to log file: {e.Message}");
            }
        }

        public void Info(string message) => Log("INFO", message);
        public void Warning(string message) => Log("WARNING", message);
        public void Error(string message, Exception? ex = null) => Log("ERROR", message, ex);
    }
}
