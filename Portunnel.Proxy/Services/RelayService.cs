using System.Net.WebSockets;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR.Client;
using Portunnel.Models;

namespace Portunnel.Proxy.Services;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(RequestMessage))]
[JsonSerializable(typeof(ResponseMessage))]
[JsonSerializable(typeof(WebSocketMessage))]
public partial class SourceGenerationContext : JsonSerializerContext;

public class RelayService : IRelayService
{
  private readonly ILogger<RelayService> _logger;
  private readonly IOptionsProvider _options;
  private readonly IRequestLogger _requestLogger;
  private HubConnection? _hubConnection;

  private string _clientId = string.Empty;
  private readonly Dictionary<string, WebSocket> _webSocketConnections = new();
  
  public RelayService(ILogger<RelayService> logger, IOptionsProvider options, IRequestLogger requestLogger)
  {
    _logger = logger;
    _options = options;
    _requestLogger = requestLogger;
  }
  
  public async Task Connect()
  {
    await Connect(_options.HostUrl);
  }

  private async Task Connect(string url)
  {
    _hubConnection = new HubConnectionBuilder()
      .WithUrl(url, options =>
      {
        options.Headers["X-Portunnel-Secret"] = _options.HostApiKey;
      })
      .WithAutomaticReconnect().ConfigureLogging(l => l.AddConsole())
      .AddJsonProtocol(options =>
      {
        options.PayloadSerializerOptions.TypeInfoResolver = SourceGenerationContext.Default;
      })
      .Build();

    _hubConnection.Closed += async (error) =>
    {
      await _hubConnection.StopAsync();
      _clientId = string.Empty;
    };
    
    _hubConnection.On<string>("Registered", (message) =>
    {
      Console.WriteLine($"{message}");
    });
    
    _hubConnection.On<string>("NotRegistered", (message) =>
    {
      Console.WriteLine($"{message}");
    });
    
    var uri = new Uri(_options.TargetServiceUrl);
    
    var uriBuilder = new UriBuilder(uri);
    uriBuilder.Scheme = uriBuilder.Scheme == Uri.UriSchemeHttps ? Uri.UriSchemeWss : Uri.UriSchemeWs;
    
    var webSocketUri = uriBuilder.Uri;
    
    _hubConnection.On<RequestMessage, ResponseMessage>("ForwardHttp", async message =>
    {
      _requestLogger.LogHttp(message);
      
      var httpClient = new HttpClient { BaseAddress = uri };

      var req = new HttpRequestMessage();

      foreach (var header in message.Headers)
      {
        if (req.Headers.TryAddWithoutValidation(header.Key, header.Value) is false)
          req.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
      }
      
      req.Method = HttpMethod.Parse(message.Method);
      
      if (message.Body.Length > 0)
        req.Content = new ByteArrayContent(message.Body);

      req.RequestUri = new Uri(uri, $"{message.Path}{message.QueryString}");

      var httpResponseMessage = await httpClient.SendAsync(req);

      if (httpResponseMessage.IsSuccessStatusCode is false)
        return new ResponseMessage(); 
          
      var responseMessage = new ResponseMessage
      {
        RequestId = message.RequestId,
        Body = await httpResponseMessage.Content.ReadAsByteArrayAsync(),
        Headers = httpResponseMessage.Headers.Concat(httpResponseMessage.Content.Headers)
          .ToDictionary(k => k.Key, v => string.Join(',', v.Value))
      };

      return responseMessage;
    });
    
    _hubConnection.On<WebSocketMessage>("ForwardWebSocket", async message =>
    {
      _requestLogger.LogWebSocket(message);
      
      _logger.LogTrace("Get message from hub. Message is {@message}", message);
      
      if (_webSocketConnections.TryGetValue(message.SocketId, out var webSocket) is false)
      {
        webSocket = new ClientWebSocket();
        
        await ((ClientWebSocket)webSocket).ConnectAsync(new Uri(webSocketUri, message.Path), CancellationToken.None);
        
        _webSocketConnections.TryAdd(message.SocketId, webSocket);
      }

      _ = ReceiveFromInternalWebSocket(webSocket, message.SocketId);

      if (webSocket.State == WebSocketState.Open)
      {
        await webSocket.SendAsync(new ArraySegment<byte>(message.Data), message.MessageType, message.EndOfMessage, // TODO split and merge large data
          CancellationToken.None);
      }
      
    });
    
    await _hubConnection.StartAsync();
  }

  private async Task ReceiveFromInternalWebSocket(WebSocket webSocket, string socketId)
  {
    var buffer = new byte[1024 * 4];

    while (webSocket.State == WebSocketState.Open)
    {
      var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
      var message = new WebSocketMessage
      {
        SocketId = socketId,
        Data = buffer.Take(result.Count).ToArray(),
        EndOfMessage = result.EndOfMessage, // TODO split and merge large data
        MessageType = result.MessageType,
      };
      
      await _hubConnection.InvokeAsync("ForwardWebSocketToClient", message);
    }
    
    await _hubConnection.InvokeAsync("CloseWebSocket", socketId);
    
    _webSocketConnections.Remove(socketId);
  }

  public async Task<string> Register()
  {
    if (_hubConnection is null || _hubConnection.State != HubConnectionState.Connected)
      throw new InvalidOperationException("There is no connection to the hub");
    
    if (string.IsNullOrEmpty(_clientId) is false)
      throw new InvalidOperationException("There is already a connection to the hub");
    
    _clientId = Guid.NewGuid().ToString();
    
    await _hubConnection.InvokeAsync("Register", _clientId);
    
    return _clientId;
  }

  public async Task Unregister()
  {
    if (_hubConnection is null || _hubConnection.State != HubConnectionState.Connected)
      throw new InvalidOperationException("There is no connection to the hub");
    
    if (string.IsNullOrEmpty(_clientId))
      throw new InvalidOperationException("There is no client ID");
    
    await _hubConnection.InvokeAsync("Unregister", _clientId);
    _clientId = string.Empty;
  }

  public async Task Disconnect()
  {
    if (_hubConnection is not null)
      await _hubConnection.StopAsync();
  }

  public bool IsConnected()
  {
    return _hubConnection?.State == HubConnectionState.Connected;
  }
}