# net45.ParallelQueue



## Usage

```csharp
using System.Collections.Concurrent;

// DI
services.AddParallelQueue<MyModel>(10);


private readonly ParallelQueue<MyModel> _myQueue;

ctor(ParallelQueue<MyModel> myQueue)
{
    _myQueue = myQueue;
    _myQueue.DequeueAsync += MyQueue_DequeueAsync;
}

private async Task MyQueue_DequeueAsync(MyModel model, CancellationToken cancellation = default(CancellationToken))
{
    // Dequeue
}

// Enqueue
_myQueue.Enqueue(model);
```

