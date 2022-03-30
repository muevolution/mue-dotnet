using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mue.Server.Tools
{
    public class ToolHost : IHostedService
    {
        private readonly ILogger<ToolHost> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly IServiceProvider _serviceProvider;

        public ToolHost(
            ILogger<ToolHost> logger,
            IConfiguration configuration,
            IHostApplicationLifetime lifetime,
            IServiceProvider serviceProvider
        )
        {
            _logger = logger;
            _configuration = configuration;
            _applicationLifetime = lifetime;
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var taskToRun = _configuration["ToolSettings:Task"];

            if (taskToRun == "init")
            {
                var task = ActivatorUtilities.CreateInstance<InitTask>(_serviceProvider);
                await task.Start(cancellationToken);
            }
            else if (taskToRun == "reload-scripts")
            {
                var task = ActivatorUtilities.CreateInstance<ReloadScriptsTask>(_serviceProvider);
                await task.Start(cancellationToken);
            }
            else
            {
                if (String.IsNullOrEmpty(taskToRun))
                {
                    _logger.LogError("No task specified.");
                }
                else
                {
                    _logger.LogError("Unknown task " + taskToRun);
                }
            }

            _logger.LogInformation("Task finished!");
            _applicationLifetime.StopApplication();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
