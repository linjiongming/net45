using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Messaging.WebPubSub
{
    public class WebPubSubServiceClient : IDisposable
    {
        public const string SubProtocol = "json.webpubsub.azure.v1";
        private readonly ILogger _logger;
        private readonly string _hubName;
        private readonly string _getUrlApi;
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _startStopSemaphore = new SemaphoreSlim(1, 1);

        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cts;
        private Task _receiveTask;

        public delegate Task WebPubSubReceiveEventHandler(WebPubSubServiceClient client, WebPubSubMessage message, CancellationToken cancellation = default(CancellationToken));
        public event WebPubSubReceiveEventHandler ReceiveAsync;

        public WebPubSubServiceClient(ILoggerFactory loggerFactory, string hubName, string getUrlApi)
        {
            _logger = loggerFactory.CreateLogger($"{nameof(WebPubSubServiceClient)}({hubName})");
            _hubName = hubName;
            _getUrlApi = getUrlApi;
            _httpClient = new HttpClient();
        }

        public string HubName => _hubName;

        protected async Task<ClientWebSocket> GetWebSocketAsync(CancellationToken cancellation = default(CancellationToken))
        {
            if (_webSocket == null)
            {
                _webSocket = new ClientWebSocket();
                _webSocket.Options.AddSubProtocol(SubProtocol);
            }
            if (_webSocket.State != WebSocketState.Open && _webSocket.State != WebSocketState.Connecting)
            {
                _webSocket.Dispose();
                _webSocket = new ClientWebSocket();
                _webSocket.Options.AddSubProtocol(SubProtocol);
                using (HttpResponseMessage response = await _httpClient.GetAsync(_getUrlApi, cancellation))
                {
                    response.EnsureSuccessStatusCode();
                    string url = await response.Content.ReadAsStringAsync();
                    cancellation.ThrowIfCancellationRequested();
                    await _webSocket.ConnectAsync(new Uri(url), cancellation);
                }
            }
            return _webSocket;
        }

        protected async Task SendAsync(string content, CancellationToken cancellation = default(CancellationToken))
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(content));
            ClientWebSocket ws = await GetWebSocketAsync(cancellation);
            await ws.SendAsync(buffer, WebSocketMessageType.Text, true, cancellation);
        }

        protected async Task CircularReceiveAsync(CancellationToken cancellation = default(CancellationToken))
        {
            while (!cancellation.IsCancellationRequested)
            {
                try
                {
                    ClientWebSocket ws = await GetWebSocketAsync(cancellation);

                    byte[] buffer = new byte[1024 * 4]; // 缓冲区大小
                    WebSocketReceiveResult result;
                    StringBuilder receivedMessage = new StringBuilder();

                    do
                    {
                        result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellation);
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            string partialMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            receivedMessage.Append(partialMessage);
                        }
                    } while (!result.EndOfMessage);

                    string json = receivedMessage.ToString();
                    _logger.LogInformation("Receive:{0}", json);
                    WebPubSubMessage message = JsonConvert.DeserializeObject<WebPubSubMessage>(json);

                    await ReceiveAsync(this, message, cancellation);
                }
                catch (OperationCanceledException) { /*ignore*/ }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error receiving");
                }
                finally
                {
                    try { await Task.Delay(300, cancellation); } catch { }
                }
            }
        }

        public async Task StartAsync(CancellationToken cancellation = default(CancellationToken))
        {
            bool releaseGuard = false;
            try
            {
                await _startStopSemaphore.WaitAsync(cancellation).ConfigureAwait(false);
                releaseGuard = true;
                if (_receiveTask == null)
                {
                    _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
                    _receiveTask = Task.Run(async () => await CircularReceiveAsync(_cts.Token), _cts.Token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting");
            }
            finally
            {
                if (releaseGuard)
                {
                    _startStopSemaphore.Release();
                }
            }
        }

        public async Task JoinGroupAsync(string group, CancellationToken cancellation = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(group)) throw new ArgumentNullException("group");
            string json = JsonConvert.SerializeObject(new { type = "joinGroup", group });
            await SendAsync(json, cancellation);
            _logger.LogInformation($"Join group[{group}]");
        }

        public async Task LeaveGroupAsync(string group, CancellationToken cancellation = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(group)) throw new ArgumentNullException("group");
            string json = JsonConvert.SerializeObject(new { type = "leaveGroup", group });
            await SendAsync(json, cancellation);
            _logger.LogInformation($"Leave group[{group}]");
        }

        public async Task SendToGroupAsync(string group, string data, CancellationToken cancellation = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(group)) throw new ArgumentNullException("group");
            if (string.IsNullOrWhiteSpace(data)) throw new ArgumentNullException("data");
            ClientWebSocket ws = await GetWebSocketAsync(cancellation);
            string json = JsonConvert.SerializeObject(new { type = "sendToGroup", group, data });
            ArraySegment<byte> buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));
            await ws.SendAsync(buffer, WebSocketMessageType.Text, true, cancellation);
        }

        public async Task StopAsync(CancellationToken cancellation = default(CancellationToken))
        {
            bool releaseGuard = false;
            try
            {
                await _startStopSemaphore.WaitAsync(cancellation).ConfigureAwait(false);
                releaseGuard = true;
                if (_cts != null)
                {
                    _cts.Cancel();
                    _cts.Dispose();
                    _cts = null;
                }
                if (_receiveTask != null)
                {
                    try
                    {
                        await _receiveTask.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { /*ignore*/ }
                    finally
                    {
                        _receiveTask.Dispose();
                        _receiveTask = null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping");
            }
            finally
            {
                if (releaseGuard)
                {
                    _startStopSemaphore.Release();
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
                    StopAsync().Wait();
                    if (_webSocket != null)
                    {
                        if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.Connecting)
                        {
                            _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "dispose", CancellationToken.None).Wait();
                        }
                        _webSocket.Dispose();
                        _webSocket = null;
                    }
                    _httpClient.Dispose();
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。

                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        // ~WebPubSubServiceClient() {
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
