namespace Portunnel.Models;

public class RequestLogMessage
{
  public DateTime Time { get; private init; }
  public string? Method { get; private init; }
  public string RequestPathAndQuery { get; private init; } = "/";
  public Dictionary<string, string>? Headers { get; private init; }
  public bool IsWebSocket { get; private init; }
  public bool IsWebSocketEndOfMessage { get; private init; }

  private RequestLogMessage() { }
  
  public static RequestLogMessage Make(RequestMessage message)
  {
    return new RequestLogMessage
    {
      Time = DateTime.Now,
      Method = message.Method,
      Headers = message.Headers,
      RequestPathAndQuery = $"{message.Path}{message.QueryString}",
      IsWebSocket = false,
      IsWebSocketEndOfMessage = false
    };
  }

  public static RequestLogMessage Make(WebSocketMessage message)
  {
    return new RequestLogMessage
    {
      Time = DateTime.Now,
      Method = null,
      Headers = null,
      RequestPathAndQuery = $"/{message.Path}",
      IsWebSocket = true,
      IsWebSocketEndOfMessage = message.EndOfMessage
    };
  }
}