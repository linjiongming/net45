# net45.DbRepository



## App.config

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <connectionStrings>
    <add name="conn" connectionString="server=dbserver;database=database;uid=admin;pwd=123456;" providerName="System.Data.SqlClient" />
  </connectionStrings>
</configuration>
```



## Usage

```csha
// DI
services.AddDbRepositories("conn");

//implement
using System.Data;

public class MyRepository : IDbRepository
{
    private readonly IDbConnection _connection;

    public MyRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    // ...
}
```

