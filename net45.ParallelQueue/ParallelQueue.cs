using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Concurrent
{
    /// <summary>
    /// <see cref="Install-Package MSFT.ParallelExtensionsExtras"/>
    /// </summary>
    public class ParallelQueue<T> : IDisposable
    {
        private readonly BlockingCollection<T> _collection = new BlockingCollection<T>();
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();
        private readonly ILogger _logger;
        private readonly ParallelOptions _options;
        private readonly Task _executeTask;

        public delegate Task DequeueEventHandler(T item, CancellationToken cancellation = default(CancellationToken));
        public event DequeueEventHandler DequeueAsync;

        public ParallelQueue(ILoggerFactory loggerFactory, ParallelOptions options)
        {
            _logger = loggerFactory.CreateLogger(GetType().Name);
            _options = options;
            _executeTask = ExecuteAsync(_stoppingCts.Token);
        }

        public int Count => _collection.Count;

        public void Enqueue(T item, CancellationToken cancellation = default(CancellationToken)) => _collection.Add(item, cancellation);

        public bool Any(Func<T, bool> predicate) => _collection.Any(predicate);

        protected async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _options.CancellationToken = stoppingToken;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    while (_collection.Count > 0)
                    {
                        Parallel.ForEach(
                            _collection.GetConsumingPartitioner(),
                            _options,
                            async item => await DequeueAsync?.Invoke(item, stoppingToken));
                    }
                }
                catch (OperationCanceledException) { /*ignore*/ }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing {0}", GetType().Name);
                    try
                    {
                        await Task.Delay(4700, stoppingToken);
                    }
                    catch (OperationCanceledException) { /*ignore*/ }
                }
                finally
                {
                    try
                    {
                        await Task.Delay(300, stoppingToken);
                    }
                    catch (OperationCanceledException) { /*ignore*/ }
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)。
                    _stoppingCts.Cancel();
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。

                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        // ~ParallelQueue() {
        //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        //   Dispose(false);
        // }

        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
