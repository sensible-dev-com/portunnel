using Portunnel.Models;

namespace Portunnel.Host;

public interface IRelayClient
{
  Task Registered(string message);
  Task NotRegistered(string message);
  
  Task<ResponseMessage> ForwardHttp(RequestMessage message);
  
  Task ForwardWebSocket(WebSocketMessage message);
}