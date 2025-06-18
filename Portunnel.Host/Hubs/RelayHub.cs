using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Portunnel.Models;

namespace Portunnel.Host.Hubs;

public class RelayHub(IConnectionMapper connectionMapper, ILogger<RelayHub> logger) : Hub<IRelayClient>
{
  private readonly IConnectionMapper _connectionMapper = connectionMapper;
  private readonly ILogger<RelayHub> _logger = logger;

  public async Task Register(string serviceId)
  {
    if (_connectionMapper.TryAddConnectionMapping(serviceId, Context.ConnectionId))
    {
      await Clients.Caller.Registered($"Service with id={serviceId} registered successfully");
      return;
    }
    
    await Clients.Caller.NotRegistered($"Service with id={serviceId} could not be registered");
  }
  
  public Task Unregister(string serviceId)
  {
    var connectionId = _connectionMapper.GetConnectionId(serviceId);
    _connectionMapper.RemoveConnectionMapping(connectionId);
    return Task.CompletedTask;
  }

  public async Task CloseWebSocket(string socketId)
  {
    _connectionMapper.TryGetWebSocketChannel(socketId, out var channel);

    _logger.LogDebug("Closing WebSocket. Channel is null: {channel}", channel is null);
    
    channel?.Complete();

    _connectionMapper.TryRemoveWebSocket(socketId, out var webSocket);
    
    _logger.LogDebug("Fetching websocket to remove. WebSocket is null: {socket}", webSocket is null);

    if (webSocket is not null)
      await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
  }

  public async Task ForwardWebSocketToClient(WebSocketMessage message)
  {
    _logger.LogDebug("Get websocket from internal client and write it to the channel. Message is {@message}", message);

    if (_connectionMapper.TryGetWebSocketChannel(message.SocketId, out var writer) is false)
    {
      _logger.LogError("WebSocket channel for socket Id {socketId} not found", message.SocketId);
      throw new Exception($"WebSocket channel for socket Id {message.SocketId} not found");
    }

    await writer.WriteAsync(message);
  }

  public override async Task OnDisconnectedAsync(Exception? exception)
  {
    _connectionMapper.RemoveConnectionMapping(Context.ConnectionId);

    if (exception is not null)
      _logger.LogError("{exception}", exception.Message);
    
    await base.OnDisconnectedAsync(exception);
  }
}