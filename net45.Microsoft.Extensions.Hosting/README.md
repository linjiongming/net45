# net45.Microsoft.Extensions.Hosting



- Usage

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

static class Program
{
    static void Main(string[] args)
    {
        HostBuilder builder = Host.CreateBuilder(args);
        
        //builder.Logging.Add...
        
        //builder.Services.Add...
        
        builder.Services.AddHostedService<MyService>();
        //builder.Services.AddHostedService<MyService2>();
        
        Host host = builder.Build();
        host.Run();
    }
}

public class MyService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SomeMethodAsync(stoppingToken);
            }
            catch (OperationCanceledException) { /*ignore*/ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing {0}", nameof(MyService));
            }
            finally
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException) { /*ignore*/ }
            }
        }
    }    
}
```

