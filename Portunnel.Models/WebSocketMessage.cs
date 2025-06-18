using System.Net.WebSockets;

namespace Portunnel.Models;

public class WebSocketMessage
{
  public required string SocketId { get; set; }
  public byte[] Data { get; set; } = [];
  public WebSocketMessageType MessageType { get; set; }
  public bool EndOfMessage { get; set; }
  public string Path { get; set; } = string.Empty;
}