#nullable enable
using DANCustomTools.Core.Services;
using DANCustomTools.MVVM;
using DANCustomTools.Services;
using System;

namespace DANCustomTools.Core.ViewModels
{
    public abstract class SubToolViewModelBase : ViewModelBase
    {
        protected readonly ILogService LogService;
        private bool _isConnected;
        private string _subToolName;

        public abstract string SubToolName { get; }

        public bool IsConnected
        {
            get => _isConnected;
            protected set => SetProperty(ref _isConnected, value);
        }

        protected SubToolViewModelBase(ILogService logService)
        {
            LogService = logService ?? throw new ArgumentNullException(nameof(logService));
            _subToolName = SubToolName;
        }

        protected virtual void OnConnectionStatusChanged(object? sender, bool isConnected)
        {
            App.Current?.Dispatcher.Invoke(() =>
            {
                IsConnected = isConnected;
                LogService.Info($"{SubToolName} connection status changed: {isConnected}");
            });
        }

        protected abstract void SubscribeToConnectionEvents();
        protected abstract void UnsubscribeFromConnectionEvents();

        public override void Dispose()
        {
            UnsubscribeFromConnectionEvents();
            base.Dispose();
        }
    }
}