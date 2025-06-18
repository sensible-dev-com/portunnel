using System.Text;
using Portunnel.Models;
using Portunnel.Proxy.Services;

namespace Portunnel.Cli;

public class ConsoleLogDestination : ILogDestination
{
  // \x1b[CODEm Where CODE is the color code number.
  // \x1b => \e
  
  private const string LightGreen = "\e[92m";
  private const string DarkGreen = "\e[32m";
  private const string Yellow = "\e[33m";
  private const string ResetFG = "\e[0m";
  private const string Italic = "\e[3m";
  private const string ResetItalic = "\e[23m";
  private const string Bold = "\e[1m";
  private const string ResetBold = "\e[22m";
  
  public async Task Log(RequestLogMessage message)
  {
    string output;
    string method;
    var date = message.Time.ToString("O");
    if (message.IsWebSocket)
    {
      method = $"{Bold}{DarkGreen}WebSocket{ResetFG}{ResetBold}";
      output = $"{Yellow}{date}{ResetFG}\t{method}: {message.RequestPathAndQuery} IsEndMessage: {message.IsWebSocketEndOfMessage}";
    }
    else
    {
      method = $"{Bold}{DarkGreen}{message.Method ?? "unknown"}{ResetFG}{ResetBold}";
      
      var sb = new StringBuilder();
      sb.AppendLine($"\t\t\t\t\tHeaders:{Italic}");

      if (message.Headers is not null)
      {
        foreach (var (key, value) in message.Headers)
          sb.AppendLine($"\t\t\t\t\t\t\t{key}: {value}");
      }
      sb.Append(ResetItalic);

      output = $"{Yellow}{date}{ResetFG}\t{method}: {message.RequestPathAndQuery}\n\t{sb}";
    }

    await Console.Out.WriteLineAsync(output);
  }
}