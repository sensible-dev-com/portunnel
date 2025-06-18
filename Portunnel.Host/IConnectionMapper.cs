using System.Net.WebSockets;
using System.Threading.Channels;
using Portunnel.Models;

namespace Portunnel.Host;

public interface IConnectionMapper
{
  bool TryAddConnectionMapping(string serviceId, string connectionId);
  void RemoveConnectionMapping(string connectionId);
  
  string? GetConnectionId(string serviceId);
  
  bool TryAddWebSocketChannel(string socketId, ChannelWriter<WebSocketMessage> writer);
  bool TryAddWebSocket(string socketId, WebSocket webSocket);
  bool TryGetWebSocketChannel(string socketId, out ChannelWriter<WebSocketMessage>? writer);
  bool TryRemoveWebSocket(string socketId, out WebSocket? webSocket);
}