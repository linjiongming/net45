using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionDatabaseExtensions
    {
        public static IServiceCollection AddDbRepositories(this IServiceCollection services, string name)
        {
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[name];
            DbProviderFactory factory = DbProviderFactories.GetFactory(settings.ProviderName);
            services.AddTransient<IDbConnection>(provider =>
            {
                DbConnection connection = factory.CreateConnection();
                connection.ConnectionString = settings.ConnectionString;
                return connection;
            });
            Type iType = typeof(IDbRepository);
            System.Collections.Generic.IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
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
