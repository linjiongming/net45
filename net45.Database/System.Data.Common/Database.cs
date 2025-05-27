namespace System.Data.Common
{
    public class Database : IDatabase
    {
        private readonly DbProviderFactory _providerFactory;
        private readonly string _connectionString;

        public Database(DbProviderFactory providerFactory, string connectionString)
        {
            _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public IDbConnection CreateConnection()
        {
            IDbConnection conn = _providerFactory.CreateConnection();
            conn.ConnectionString = _connectionString;
            return conn;
        }
    }
}
