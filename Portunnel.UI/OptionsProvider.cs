using Microsoft.Extensions.Options;
using Portunnel.Proxy.Services;

namespace Portunnel.UI;

public class OptionsProvider(IOptions<PortunnelOptions> options) : IOptionsProvider
{
  public string HostApiKey { get; set; } = options.Value.ServerApiKey ?? string.Empty;
  public string HostUrl { get; set; } = options.Value.ServerUrl ?? string.Empty;
  public string TargetServiceUrl { get; set; } = options.Value.TargetServiceUrl ?? string.Empty;
}