#nullable enable
#nullable enable

using System;

namespace DANCustomTools.Services
{
    public interface ILogService
    {
        void Info(string message);
        void Warning(string message);
        void Error(string message, Exception? ex = null);
    }
}
