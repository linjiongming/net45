using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System;

namespace Microsoft.Extensions.Hosting
{
    public class HostBuilder
    {
        public HostBuilder(string[] args)
        {
            Args = args;
            Services = new ServiceCollection();
            Logging = new LoggerFactory();
            if (Environment.UserInteractive)
            {
                Logging.AddConsole();
            }
            Services.AddSingleton(Logging);
            Services.AddSingleton(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));
        }

        public string[] Args { get; set; }
        public IServiceCollection Services { get; set; }
        public ILoggerFactory Logging { get; set; }

        public Host Build()
        {
            Services.AddSingleton<HostService>();
            IServiceProvider serviceProvider = Services.BuildServiceProvider();
            return new Host(serviceProvider);
        }
    }
}
