# net45.Database



## Usage

```csharp
using Microsoft.Extensions.DependencyInjection;

// Injection IDatabase from connectionStrings in app.config
// Auto inject types implementing IRepository
builder.Services.AddDatabase("NorthWind");

private readonly IDatabase _database;

ctor(IDatabase database)
{
    _database = database;
}

using (var conn = _database.CreateConnection())
{
    // use the connection
}
```

