#nullable enable
using DANCustomTools.Core.Abstractions;
using DANCustomTools.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DANCustomTools.Core.Services
{
    public class ToolInitializer : IToolInitializer
    {
        private readonly IToolConfigurationService _configurationService;
        private bool _isInitialized = false;

        public bool IsInitialized => _isInitialized;

        public ToolInitializer(IToolConfigurationService configurationService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
            if (_isInitialized)
                return;

            try
            {
                _configurationService.ConfigureTools(serviceProvider);
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                var logService = serviceProvider.GetService<ILogService>();
                logService?.Error($"Tool initialization failed: {ex.Message}", ex);
                throw;
            }
        }
    }
}