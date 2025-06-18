using Portunnel.Models;

namespace Portunnel.Proxy.Services;

public interface IRequestLogger
{
  void LogHttp(RequestMessage message);
  
  void LogWebSocket(WebSocketMessage message);
}