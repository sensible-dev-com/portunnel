namespace Portunnel.Proxy.Services;

public interface IOptionsProvider
{
  string HostApiKey {get; set;}
  string HostUrl {get; set;}
  string TargetServiceUrl {get; set;}
}