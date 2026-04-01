using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SkyForge.MCP
{
    [Serializable]
    public class MCPClientOptions
    {
        [Tooltip("Hostname oder IP-Adresse des MCP-Servers." )]
        public string Host = "127.0.0.1";

        [Tooltip("HTTP/WebSocket Port des MCP-Servers.")]
        public int Port = 8080;

        [Tooltip("Pfad zum JSON-RPC HTTP Endpoint.")]
        public string HttpPath = "/mcp";

        [Tooltip("Pfad zum JSON-RPC WebSocket Endpoint.")]
        public string WebSocketPath = "/mcp/ws";

        [Tooltip("HTTPS statt HTTP verwenden.")]
        public bool UseSecureHttp;

        [Tooltip("WSS statt WS verwenden.")]
        public bool UseSecureWebSocket;

        [Tooltip("Bevorzugt eine WebSocket-Verbindung. Bei Fehlern wird automatisch auf HTTP-POST zurückgefallen.")]
        public bool PreferWebSocket = true;

        [Tooltip("Optionales Bearer-Token für Authentifizierung.")]
        public string BearerToken;

        [Tooltip("HTTP-Timeout in Sekunden.")]
        public float HttpTimeoutSeconds = 15f;

        [Tooltip("Client-Name für den MCP-Handshake (initialize).")]
        public string ClientName = "SkyForge";

        [Tooltip("Client-Version für den MCP-Handshake (initialize).")]
        public string ClientVersion = Application.unityVersion;

        internal TimeSpan HttpTimeout => TimeSpan.FromSeconds(Mathf.Max(1f, HttpTimeoutSeconds));
    }

    public sealed class MCPClientException : Exception
    {
        public int? Code { get; }

        public MCPClientException(string message, int? code = null)
            : base(message)
        {
            Code = code;
        }

        public MCPClientException(string message, Exception inner, int? code = null)
            : base(message, inner)
        {
            Code = code;
        }
    }

    public class MCPToolDefinition
    {
        public string Name { get; internal set; }
        public string Description { get; internal set; }
        public Dictionary<string, object> Raw { get; internal set; }
    }

    /// <summary>
    /// Lightweight JSON-RPC 2.0 Client für den Unity MCP Server mit optionaler WebSocket-Nutzung.
    /// </summary>
    public sealed class MCPClient : IDisposable
    {
        public event Action<Dictionary<string, object>> NotificationReceived;

        private readonly MCPClientOptions _options;
        private readonly Uri _httpEndpoint;
        private readonly Uri _webSocketEndpoint;
        private readonly HttpClient _httpClient;

        private ClientWebSocket _webSocket;
        private CancellationTokenSource _webSocketCts;
        private Task _receiveTask;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<Dictionary<string, object>>> _pendingRequests = new();
        private readonly object _socketLock = new();
        private int _requestId;
        private bool _disposed;
        private string _sessionId;

        public MCPClient(MCPClientOptions options = null)
        {
            _options = options ?? new MCPClientOptions();

            _httpEndpoint = BuildUri(_options.UseSecureHttp ? "https" : "http", _options.Host, _options.Port, _options.HttpPath);
            _webSocketEndpoint = BuildUri(_options.UseSecureWebSocket ? "wss" : "ws", _options.Host, _options.Port, _options.WebSocketPath);

            var handler = new HttpClientHandler
            {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                // Use default credentials behaviour; no special handling needed.
#endif
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = _options.HttpTimeout
            };

            if (!string.IsNullOrEmpty(_options.BearerToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.BearerToken);
            }
        }

        public bool IsWebSocketConnected
        {
            get
            {
                lock (_socketLock)
                {
                    return _webSocket != null && _webSocket.State == WebSocketState.Open;
                }
            }
        }

        public string SessionId => _sessionId;

        public async Task<string> InitializeAsync(string sessionId = null, IEnumerable<string> capabilities = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            await EnsureWebSocketAsync(cancellationToken).ConfigureAwait(false);

            var payload = new Dictionary<string, object>
            {
                ["client"] = new Dictionary<string, object>
                {
                    ["name"] = _options.ClientName,
                    ["version"] = _options.ClientVersion
                }
            };

            if (!string.IsNullOrEmpty(sessionId))
            {
                payload["session"] = sessionId;
            }

            if (capabilities != null)
            {
                payload["capabilities"] = capabilities.Cast<object>().ToList();
            }

            var response = await SendRequestAsync("initialize", payload, cancellationToken).ConfigureAwait(false);
            if (response is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue("sessionId", out var sessionObj) && sessionObj != null)
                {
                    _sessionId = sessionObj.ToString();
                }
                else if (dict.TryGetValue("session", out var sessionValue) && sessionValue is Dictionary<string, object> sessionDict && sessionDict.TryGetValue("id", out var idObj))
                {
                    _sessionId = idObj?.ToString();
                }
            }

            return _sessionId;
        }

        public async Task<IReadOnlyList<MCPToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var response = await SendRequestAsync("tools/list", null, cancellationToken).ConfigureAwait(false);
            if (response is Dictionary<string, object> dict && dict.TryGetValue("tools", out var toolsObj) && toolsObj is List<object> list)
            {
                var output = new List<MCPToolDefinition>(list.Count);
                foreach (var entry in list)
                {
                    if (entry is Dictionary<string, object> toolDict)
                    {
                        var tool = new MCPToolDefinition
                        {
                            Name = toolDict.TryGetValue("name", out var nameObj) ? nameObj?.ToString() : string.Empty,
                            Description = toolDict.TryGetValue("description", out var descObj) ? descObj?.ToString() : string.Empty,
                            Raw = toolDict
                        };
                        output.Add(tool);
                    }
                }
                return output;
            }

            return Array.Empty<MCPToolDefinition>();
        }

        public async Task<object> CallToolAsync(string name, IDictionary<string, object> arguments = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Tool-Name darf nicht leer sein", nameof(name));
            }

            ThrowIfDisposed();

            var payload = new Dictionary<string, object>
            {
                ["name"] = name
            };

            if (arguments != null)
            {
                payload["arguments"] = arguments;
            }

            return await SendRequestAsync("tools/call", payload, cancellationToken).ConfigureAwait(false);
        }

        public async Task<object> SendRawAsync(string method, object parameters = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(method))
            {
                throw new ArgumentException("JSON-RPC Method darf nicht leer sein", nameof(method));
            }

            ThrowIfDisposed();
            return await SendRequestAsync(method, parameters, cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing || _disposed)
            {
                return;
            }

            _disposed = true;

            lock (_socketLock)
            {
                try
                {
                    _webSocketCts?.Cancel();
                }
                catch (Exception)
                {
                }

                _webSocketCts?.Dispose();
                _webSocketCts = null;

                if (_webSocket != null)
                {
                    try
                    {
                        if (_webSocket.State == WebSocketState.Open)
                        {
                            _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disposing", CancellationToken.None).Forget();
                        }
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        _webSocket.Dispose();
                        _webSocket = null;
                    }
                }
            }

            try
            {
                _httpClient?.Dispose();
            }
            catch (Exception)
            {
            }

            FailAllPending(new MCPClientException("Client disposed."));
        }

        private async Task EnsureWebSocketAsync(CancellationToken cancellationToken)
        {
            if (!_options.PreferWebSocket || _disposed)
            {
                return;
            }

            if (IsWebSocketConnected)
            {
                return;
            }

            lock (_socketLock)
            {
                if (_webSocket != null && _webSocket.State == WebSocketState.Connecting)
                {
                    return;
                }
            }

            ClientWebSocket socket = null;
            try
            {
                socket = new ClientWebSocket();
                if (!string.IsNullOrEmpty(_options.BearerToken))
                {
                    socket.Options.SetRequestHeader("Authorization", $"Bearer {_options.BearerToken}");
                }

                await socket.ConnectAsync(_webSocketEndpoint, cancellationToken).ConfigureAwait(false);

                var cts = new CancellationTokenSource();
                Task receiveTask = Task.Run(() => ReceiveLoopAsync(socket, cts.Token), CancellationToken.None);

                lock (_socketLock)
                {
                    _webSocket = socket;
                    _webSocketCts = cts;
                    _receiveTask = receiveTask;
                }

#if UNITY_EDITOR
                Debug.Log($"[MCPClient] WebSocket verbunden: {_webSocketEndpoint}");
#endif
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[MCPClient] WebSocket-Verbindung fehlgeschlagen ({ex.Message}). Verwende HTTP-Fallback.");
#endif
                if (socket != null)
                {
                    try
                    {
                        socket.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private async Task<object> SendRequestAsync(string method, object parameters, CancellationToken cancellationToken)
        {
            var response = await SendRawRequestAsync(method, parameters, cancellationToken).ConfigureAwait(false);
            return ExtractResult(response);
        }

        private async Task<Dictionary<string, object>> SendRawRequestAsync(string method, object parameters, CancellationToken cancellationToken)
        {
            var requestId = Interlocked.Increment(ref _requestId).ToString();
            var payload = new Dictionary<string, object>
            {
                ["jsonrpc"] = "2.0",
                ["id"] = requestId,
                ["method"] = method
            };

            if (parameters != null)
            {
                payload["params"] = parameters;
            }

            bool sentViaWebSocket = false;
            if (_options.PreferWebSocket)
            {
                await EnsureWebSocketAsync(cancellationToken).ConfigureAwait(false);
                if (IsWebSocketConnected)
                {
                    var tcs = new TaskCompletionSource<Dictionary<string, object>>(TaskCreationOptions.RunContinuationsAsynchronously);
                    if (!_pendingRequests.TryAdd(requestId, tcs))
                    {
                        throw new InvalidOperationException($"Duplicate request id: {requestId}");
                    }

                    try
                    {
                        string json = MiniJson.Serialize(payload);
                        await SendOverWebSocketAsync(json, cancellationToken).ConfigureAwait(false);

                        using var registration = cancellationToken.CanBeCanceled
                            ? cancellationToken.Register(() =>
                            {
                                if (_pendingRequests.TryRemove(requestId, out var pendingTcs))
                                {
                                    pendingTcs.TrySetCanceled(cancellationToken);
                                }
                            })
                            : null;

                        var response = await tcs.Task.ConfigureAwait(false);
                        sentViaWebSocket = true;
                        return response;
                    }
                    catch (Exception ex)
                    {
                        if (_pendingRequests.TryRemove(requestId, out var pending))
                        {
                            pending.TrySetException(ex);
                        }

                        DisableWebSocket($"Send fehlgeschlagen ({ex.Message})");
                    }
                }
            }

            if (!sentViaWebSocket)
            {
                string json = MiniJson.Serialize(payload);
                return await SendOverHttpAsync(json, cancellationToken).ConfigureAwait(false);
            }

            throw new MCPClientException("Unbekannter Fehler beim Senden der MCP-Anfrage.");
        }

        private async Task SendOverWebSocketAsync(string json, CancellationToken cancellationToken)
        {
            byte[] data = Encoding.UTF8.GetBytes(json);
            var segment = new ArraySegment<byte>(data);

            ClientWebSocket socket;
            lock (_socketLock)
            {
                socket = _webSocket;
            }

            if (socket == null)
            {
                throw new MCPClientException("WebSocket ist nicht verbunden.");
            }

            await socket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
        }

        private async Task<Dictionary<string, object>> SendOverHttpAsync(string json, CancellationToken cancellationToken)
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(_httpEndpoint, content, cancellationToken).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new MCPClientException($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {responseText}");
            }

            return ParseJsonRpc(responseText);
        }

        private async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken cancellationToken)
        {
            var buffer = new byte[8192];
            var messageBuffer = new List<byte>(8192);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None).ConfigureAwait(false);
                            DisableWebSocket("Server closed connection");
                            FailAllPending(new MCPClientException("WebSocket connection closed by server."));
                            return;
                        }

                        messageBuffer.AddRange(new ArraySegment<byte>(buffer, 0, result.Count));
                    }
                    while (!result.EndOfMessage);

                    var json = Encoding.UTF8.GetString(messageBuffer.ToArray());
                    messageBuffer.Clear();

                    HandleWebSocketMessage(json);
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown.
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[MCPClient] WebSocket Empfangsloop Fehler: {ex.Message}");
#endif
                DisableWebSocket($"Receive error: {ex.Message}");
                FailAllPending(new MCPClientException("WebSocket receive error", ex));
            }
        }

        private void HandleWebSocketMessage(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            var data = MiniJson.Deserialize(json) as Dictionary<string, object>;
            if (data == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[MCPClient] Unerwartete WebSocket-Nachricht: " + json);
#endif
                return;
            }

            if (!data.TryGetValue("id", out var idObj) || idObj == null)
            {
                NotificationReceived?.Invoke(data);
                return;
            }

            var id = idObj.ToString();
            if (_pendingRequests.TryRemove(id, out var tcs))
            {
                tcs.TrySetResult(data);
            }
        }

        private object ExtractResult(Dictionary<string, object> response)
        {
            if (response == null)
            {
                throw new MCPClientException("Leere Antwort vom MCP-Server");
            }

            if (response.TryGetValue("error", out var errorObj) && errorObj is Dictionary<string, object> errorDict)
            {
                int? code = null;
                if (errorDict.TryGetValue("code", out var codeObj) && codeObj != null)
                {
                    try
                    {
                        code = Convert.ToInt32(codeObj);
                    }
                    catch (Exception)
                    {
                        code = null;
                    }
                }

                var message = errorDict.TryGetValue("message", out var messageObj) ? messageObj?.ToString() : "MCP-Fehler";
                throw new MCPClientException(message ?? "MCP-Fehler", code);
            }

            if (response.TryGetValue("result", out var result))
            {
                return result;
            }

            return null;
        }

        private Dictionary<string, object> ParseJsonRpc(string json)
        {
            var data = MiniJson.Deserialize(json) as Dictionary<string, object>;
            if (data == null)
            {
                throw new MCPClientException("Antwort konnte nicht geparst werden: " + json);
            }

            return data;
        }

        private void DisableWebSocket(string reason)
        {
            lock (_socketLock)
            {
                try
                {
                    _webSocketCts?.Cancel();
                }
                catch (Exception)
                {
                }

                _webSocketCts?.Dispose();
                _webSocketCts = null;

                if (_webSocket != null)
                {
                    try
                    {
                        _webSocket.Dispose();
                    }
                    catch (Exception)
                    {
                    }

                    _webSocket = null;
                }
            }

#if UNITY_EDITOR
            Debug.LogWarning("[MCPClient] WebSocket deaktiviert: " + reason);
#endif
        }

        private void FailAllPending(Exception ex)
        {
            foreach (var pair in _pendingRequests.ToArray())
            {
                if (_pendingRequests.TryRemove(pair.Key, out var tcs))
                {
                    tcs.TrySetException(ex);
                }
            }
        }

        private static Uri BuildUri(string scheme, string host, int port, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = "/";
            }

            if (!path.StartsWith("/", StringComparison.Ordinal))
            {
                path = "/" + path;
            }

            var builder = new UriBuilder
            {
                Scheme = scheme,
                Host = host,
                Port = port,
                Path = path
            };

            return builder.Uri;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MCPClient));
            }
        }
    }

    internal static class TaskExtensions
    {
        public static void Forget(this Task task)
        {
            // Intentionally left blank. Fire-and-forget helper.
        }
    }
}
