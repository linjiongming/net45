namespace System.Data
{
    public interface IDatabase
    {
        IDbConnection CreateConnection();
    }
}
