using Microsoft.Extensions.DependencyInjection;
using System;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting
{
    public class Host
    {
        private readonly IServiceProvider _serviceProvider;

        public Host(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Run()
        {
            using (HostService service = _serviceProvider.GetService<HostService>())
            {
                if (Environment.UserInteractive)
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    Console.CancelKeyPress += (s, e) =>
                    {
                        Console.WriteLine("Canceling...");
                        cts.Cancel();
                        e.Cancel = true;
                    };
                    service.StartAsync(cts.Token).GetAwaiter().GetResult();
                    while (!cts.IsCancellationRequested)
                    {
                        try
                        {
                            Task.Delay(1000, cts.Token);
                        }
                        catch (OperationCanceledException) { /*ignore*/ }
                    }
                    service.Stop();
                }
                else
                {
                    ServiceBase.Run(service);
                }
            }
        }

        public static HostBuilder CreateBuilder(string[] args) => new HostBuilder(args);
    }
}
