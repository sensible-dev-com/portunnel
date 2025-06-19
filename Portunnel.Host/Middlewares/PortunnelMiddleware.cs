using System.Net.WebSockets;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Portunnel.Host.Hubs;
using Portunnel.Models;

namespace Portunnel.Host.Middlewares;

public class PortunnelMiddleware(
  IHubContext<RelayHub, IRelayClient> hubContext,
  IConnectionMapper connectionMapper,
  IOptions<PortunnelOptions> options, ILogger<PortunnelMiddleware> logger) : IMiddleware
{
  private readonly ILogger<PortunnelMiddleware> _logger = logger;
  private readonly PortunnelOptions _options = options.Value;

 public async Task InvokeAsync(HttpContext context, RequestDelegate next)
  {
    if (context.Request.Path.StartsWithSegments(_options.Prefix) is false)
    {
      await next(context);
      return;
    }
    
    var pathSegments = context.Request.Path.Value!.TrimStart('/').Split('/');
    if (pathSegments.Length < 2)
    {
      await next(context);
      return;
    }
    
    var serviceId = pathSegments[1];
    
    if (Guid.TryParse(serviceId, out _) is false)
    {
      await next(context);
      return;
    }
    
    var path = string.Join('/', pathSegments.Skip(2));
    
    var connectionId = connectionMapper.GetConnectionId(serviceId);

    if (string.IsNullOrEmpty(connectionId))
    {
      _logger.LogError("Connection Id is not found for service id: {serviceId}", serviceId);
      return;
    }
    
    if (context.WebSockets.IsWebSocketRequest)
    {
      await HandleWebSocketForwarding(context, path, connectionId);
    }
    else
    {
      await HandleHttpForwarding(context, path, connectionId);
    }
  }
  
  private async Task HandleHttpForwarding(HttpContext httpContext, string path, string connectionId)
  {
    byte[] body = [];
    
    if (httpContext.Request.ContentLength > 0)
    {
      using var ms = new MemoryStream();
      await httpContext.Request.Body.CopyToAsync(ms);
      body = ms.ToArray();
    }
            
    var requestMessage = new RequestMessage
    {
      RequestId = Guid.NewGuid().ToString(),
      Method = httpContext.Request.Method,
      Path = $"/{path}",
      Headers = httpContext.Request.Headers.ToDictionary(k => k.Key, v => v.Value.ToString()),
      QueryString = httpContext.Request.QueryString.Value ?? string.Empty,
      Body = body
    };
            
    var result = await hubContext.Clients.Client(connectionId).ForwardHttp(requestMessage);

    foreach (var header in result.Headers.Where(header => !header.Key.StartsWith("Transfer-Encoding")))
      httpContext.Response.Headers.TryAdd(header.Key, header.Value);

    await httpContext.Response.Body.WriteAsync(result.Body);
  }

  private async Task HandleWebSocketForwarding(HttpContext httpContext, string path, string connectionId)
  {
    var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
    var channel = Channel.CreateUnbounded<WebSocketMessage>();

    var socketId = Guid.NewGuid().ToString();
    if (connectionMapper.TryAddWebSocketChannel(socketId, channel.Writer) is false)
    {
      _logger.LogError("Failed to add WebSocket channel for {socketId}", socketId);
      throw new Exception($"Failed to add WebSocket channel for {socketId}");
    }

    if (connectionMapper.TryAddWebSocket(socketId, webSocket) is false)
    {
      _logger.LogError("Failed to add WebSocket {socketId}", socketId);
      throw new Exception($"Failed to add WebSocket {socketId}");
    }
    
    var receiveTask = ReceiveMessageFromClient(webSocket, connectionId, socketId, path);
    var sendTask = SendMessageToClient(webSocket, channel.Reader, socketId);
    
    await Task.WhenAny(receiveTask, sendTask);
  }

  private async Task SendMessageToClient(WebSocket webSocket, ChannelReader<WebSocketMessage> channelReader, string socketId)
  {
    _logger.LogDebug("Get message from hub, read from channel and send it to the client. ");
    
    await foreach (var message in channelReader.ReadAllAsync())
    {
      if (webSocket.State != WebSocketState.Open)
        break;
      
      _logger.LogDebug("Before sending websocket message to the client. Message is {@message}", message);
      
      await webSocket.SendAsync(new ArraySegment<byte>(message.Data), message.MessageType, message.EndOfMessage, CancellationToken.None);
      
      _logger.LogDebug("Websocket message successfully sent to the client.");
    }
    
    _logger.LogDebug("Close websocket after channel have been read.");
    
    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
    
    connectionMapper.TryRemoveWebSocket(socketId, out _);
  }

  private async Task ReceiveMessageFromClient(WebSocket webSocket, string connectionId, string socketId, string path)
  {
    var buffer = new byte[1024 * 4];
    
    while (webSocket.State == WebSocketState.Open)
    {
      var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
      if (result.MessageType == WebSocketMessageType.Close)
        break;

      var message = new WebSocketMessage
      {
        SocketId = socketId,
        Data = buffer.Take(result.Count).ToArray(),
        EndOfMessage = result.EndOfMessage,
        MessageType = result.MessageType,
        Path = path
      };

      _logger.LogDebug(
        "Receive message from client and sent it to the proxy. ConnectionId is {connectionId}, Message is {@requestMessage}",
        connectionId, message);
      
      await hubContext.Clients.Client(connectionId).ForwardWebSocket(message);
    }
    
    _logger.LogDebug("Close websocket after exchange. WebSocket id is: {socketId}", socketId);
    
    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
    
    connectionMapper.TryRemoveWebSocket(socketId, out _);
  }
}