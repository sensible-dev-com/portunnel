using Portunnel.Models;

namespace Portunnel.Proxy.Services;

public class RequestLogger(IEnumerable<ILogDestination> logDestinations) : IRequestLogger
{
  private readonly IEnumerable<ILogDestination> _logDestinations = logDestinations;

  public void LogHttp(RequestMessage message)
  {
    var requestLogMessage = RequestLogMessage.Make(message);
    
    foreach (var destination in _logDestinations)
      Task.Run(() => destination.Log(requestLogMessage));
  }

  public void LogWebSocket(WebSocketMessage message)
  {
    var requestLogMessage = RequestLogMessage.Make(message);
    
    foreach (var destination in _logDestinations)
      Task.Run(() => destination.Log(requestLogMessage));
  }
}