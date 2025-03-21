using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting
{
    public class HostService : ServiceBase
    {
        private readonly IEnumerable<IHostedService> _services;
        private readonly ILogger _logger;
        private Task _executeTask;
        private CancellationTokenSource _stoppingCts;

        public HostService(IServiceProvider serviceProvider)
        {
            _services = serviceProvider.GetServices<IHostedService>();
            _logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger(Process.GetCurrentProcess().ProcessName);
        }

        public Task StartAsync(CancellationToken cancellation = default(CancellationToken))
        {
            _logger.LogInformation("Start");
            _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            _executeTask = Task.WhenAll(_services.Select(async service => await service.StartAsync(_stoppingCts.Token)));
            if (_executeTask != null && _executeTask.IsCompleted)
            {
                return _executeTask;
            }
            return Task.FromResult(0);
        }

        public async Task StopAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_executeTask == null)
            {
                return;
            }
            try
            {
                _stoppingCts.Cancel();
            }
            finally
            {
                TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                using (cancellationToken.Register(s => ((TaskCompletionSource<object>)s).TrySetCanceled(), tcs))
                {
                    await Task.WhenAny(_executeTask, tcs.Task).ConfigureAwait(false);
                }
            }
            _logger.LogInformation("Stop");
        }

        protected override void OnStart(string[] args)
        {
            _ = Task.Run(async () => await StartAsync());
        }

        protected override void OnStop()
        {
            StopAsync().GetAwaiter().GetResult();
        }
    }
}
