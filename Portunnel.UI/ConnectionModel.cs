using System.ComponentModel.DataAnnotations;

namespace Portunnel.UI;

public class ConnectionModel
{
  [Required(ErrorMessage = "Value is required")]
  [Url]
  public string HostUri {get; set;} = string.Empty;
  
  [Required(ErrorMessage = "Value is required")]
  public string HostApiKey {get; set;} = string.Empty;
  
  [Required(ErrorMessage = "Value is required")]
  [Url]
  public string TargetServiceUri {get; set;} = string.Empty;
  
  public string ConnectedTo {get; set;} = string.Empty;
}