using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Azure.Messaging.WebPubSub
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWebPubSubServiceFactory(this IServiceCollection services, string getAccessUrlApi)
        {
            // Azure 的默认最低 TLS 版本为 TLS 1.2， net45必须手动开启
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            services.AddSingleton(provider => new WebPubSubServiceFactory(provider.GetService<ILoggerFactory>(), getAccessUrlApi));
            return services;
        }
    }
}
