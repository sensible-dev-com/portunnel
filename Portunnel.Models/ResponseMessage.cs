namespace Portunnel.Models;

public class ResponseMessage
{
  public string RequestId { get; set; }
  public Dictionary<string, string> Headers { get; set; } = new();
  public byte[] Body { get; set; } = [];
}