using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Threading.Channels;
using Portunnel.Models;

namespace Portunnel.Host;

public class ConnectionMapper : IConnectionMapper
{
  private readonly ConcurrentDictionary<string, ChannelWriter<WebSocketMessage>?> _channelWriters = new();
  private readonly ConcurrentDictionary<string, string?> _connections = new();
  private readonly ConcurrentDictionary<string, WebSocket?> _webSocketConnections = new();

  public bool TryAddConnectionMapping(string serviceId, string connectionId)
  {
    return _connections.TryAdd(serviceId.ToUpper(), connectionId);
  }

  public void RemoveConnectionMapping(string connectionId)
  {
    var serviceId = _connections.FirstOrDefault(c => c.Value == connectionId).Key;

    if (string.IsNullOrWhiteSpace(serviceId))
      return;

    _connections.TryRemove(serviceId, out _);
  }

  public string? GetConnectionId(string serviceId)
  {
    return _connections.GetValueOrDefault(serviceId.ToUpper());
  }

  public bool TryAddWebSocketChannel(string socketId, ChannelWriter<WebSocketMessage> writer)
  {
    return _channelWriters.TryAdd(socketId, writer);
  }

  public bool TryAddWebSocket(string socketId, WebSocket webSocket)
  {
    return _webSocketConnections.TryAdd(socketId, webSocket);
  }

  public bool TryGetWebSocketChannel(string socketId, out ChannelWriter<WebSocketMessage>? writer)
  {
    return _channelWriters.TryGetValue(socketId, out writer);
  }

  public bool TryRemoveWebSocket(string socketId, out WebSocket? webSocket)
  {
    if (_channelWriters.TryRemove(socketId, out var channel) is false)
    {
      webSocket = null;
      return false;
    }
    
    channel?.Complete();
    
    return _webSocketConnections.TryRemove(socketId, out webSocket);
  }
}