#nullable enable
using System;

namespace DANCustomTools.Services
{
    public class ConsoleLogService : ILogService
    {
        public void Info(string message)
        {
            Console.WriteLine($"[INFO] {DateTime.Now:HH:mm:ss} - {message}");
        }

        public void Warning(string message)
        {
            Console.WriteLine($"[WARN] {DateTime.Now:HH:mm:ss} - {message}");
        }

        public void Error(string message, Exception? ex = null)
        {
            Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} - {message}");
            if (ex != null)
            {
                Console.WriteLine($"Exception: {ex}");
            }
        }
    }
}