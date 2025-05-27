using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DatabaseServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, string name)
        {
            ConnectionStringSettings connectionStringSettings = ConfigurationManager.ConnectionStrings[name];
            DbProviderFactory dbProviderFactory = DbProviderFactories.GetFactory(connectionStringSettings.ProviderName);
            services.AddSingleton<IDatabase>(new Database(dbProviderFactory, connectionStringSettings.ConnectionString));
            services.AddScoped(provider => provider.GetRequiredService<IDatabase>().CreateConnection());
            Type iType = typeof(IDbRepository);
            IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.ExportedTypes)
                .Where(t => t.IsClass && !t.IsAbstract && iType.IsAssignableFrom(t));
            foreach (Type type in types)
            {
                services.AddScoped(type, type);
            }
            return services;
        }
    }
}
