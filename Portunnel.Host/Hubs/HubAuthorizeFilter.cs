using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Portunnel.Host.Hubs;

public class HubAuthorizeFilter : IHubFilter
{
  public Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
  {
    var httpContext = context.Context.GetHttpContext();
    
    if (httpContext is null)
      throw new Exception($"{nameof(httpContext)} is null");
    
    var portunnelOptions = context.ServiceProvider.GetRequiredService<IOptions<PortunnelOptions>>().Value;
    
    if (httpContext.Request.Headers.TryGetValue("X-Portunnel-Secret", out var secret) &&
        string.Equals(secret, portunnelOptions.SecretKey))
    {
      return next(context);
    }

    throw new UnauthorizedAccessException();
  }
}