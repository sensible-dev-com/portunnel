using System.ComponentModel.DataAnnotations;

namespace Portunnel.Host;

public class PortunnelOptions
{
  public static string ConfigurationSectionName = "Portunnel";
  
  [Required]
  public required string Prefix { get; set; }
  
  [Required]
  public required string SecretKey { get; set; }
  
  public long? MaximumReceiveMessageSize { get; set; }
}