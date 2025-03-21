# net45.Azure.Messaging.WebPubSub



## Usage

```csharp
using Azure.Messaging.WebPubSub;

// DI
services.AddWebPubSubServiceFactory("GetWebPubSubAccessUrlApi");


private readonly WebPubSubServiceFactory _webPubSubServiceFactory;
private readonly WebPubSubServiceClient _webPubSubServiceClient;

ctor(WebPubSubServiceFactory webPubSubServiceFactory)
{
    _webPubSubServiceFactory = webPubSubServiceFactory;
    _webPubSubServiceClient = _webPubSubServiceFactory.GetClient("HubName");
}

    // Start to receiving messages
    _webPubSubServiceClient.ReceiveAsync += WebPubSubServiceClient_ReceiveAsync;
    await _webPubSubServiceClient.StartAsync(cancellationToken);

    // Join group
    await _webPubSubServiceClient.JoinGroupAsync("Group1", cancellationToken);

    // Leave group
    await _webPubSubServiceClient.LeaveGroupAsync("Group1", cancellationToken);

    // Send message to group
    await _webPubSubServiceClient.SendToGroupAsync("Group1", "message content", cancellationToken);

    // Stop receiving messages
    _webPubSubServiceClient.ReceiveAsync -= WebPubSubServiceClient_ReceiveAsync;
    await _webPubSubServiceClient.StopAsync(cancellationToken);

    _webPubSubServiceClient.Dispose();
```

