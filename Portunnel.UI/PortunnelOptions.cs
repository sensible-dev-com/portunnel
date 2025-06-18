namespace Portunnel.UI;

public class PortunnelOptions
{
  public static string ConfigurationSectionName = "Portunnel";
  
  public string? ServerUrl { get; set; }
  
  public string? ServerApiKey { get; set; }
  public string? TargetServiceUrl { get; set; }
}