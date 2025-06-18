using Portunnel.Proxy.Services;

namespace Portunnel.Cli;

public class CliOptionsProvider : IOptionsProvider
{
  public string HostApiKey { get; set; }
  public string HostUrl { get; set; }
  public string TargetServiceUrl { get; set; }
}