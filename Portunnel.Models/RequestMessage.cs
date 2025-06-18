namespace Portunnel.Models;

public class RequestMessage
{
  public string? SocketId { get; set; }
  public required string RequestId { get; set; }
  public required string Method { get; set; }
  public required string Path { get; set; }
  public required Dictionary<string, string> Headers { get; set; } = new();
  public required byte[] Body { get; set; }
  public required string QueryString { get; set; }
}