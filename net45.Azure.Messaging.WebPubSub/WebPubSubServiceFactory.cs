using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Azure.Messaging.WebPubSub
{
    public class WebPubSubServiceFactory : IDisposable
    {
        private readonly ConcurrentDictionary<string, WebPubSubServiceClient> _hubMap = new ConcurrentDictionary<string, WebPubSubServiceClient>();

        private readonly ILoggerFactory _loggerFactory;
        private readonly string _apiFormat;

        public WebPubSubServiceFactory(ILoggerFactory loggerFactory, string apiFormat)
        {
            _loggerFactory = loggerFactory;
            _apiFormat = apiFormat;
        }

        public WebPubSubServiceClient GetClient(string hubName)
        {
            if (!_hubMap.ContainsKey(hubName))
            {
                string api = string.Format(_apiFormat, hubName);
                _hubMap[hubName] = new WebPubSubServiceClient(_loggerFactory, hubName, api);
            }
            return _hubMap[hubName];
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
                    Parallel.ForEach(_hubMap.Values, x => x.Dispose());
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。

                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        // ~WebPubSubServiceFactory() {
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
