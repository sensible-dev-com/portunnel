using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Portunnel.Host.Hubs;
using Portunnel.Host.Middlewares;

namespace Portunnel.Host;

public static class Extensions
{
  private static readonly Action<PortunnelOptions> DefaultPortunnelOptions = options =>
  {
    options.Prefix = "/portunnel";
  };

  public static WebApplicationBuilder AddPortunnel(this WebApplicationBuilder builder,
    Action<PortunnelOptions>? configureOptions = null)
  {
    builder.Services.AddSingleton<IConnectionMapper, ConnectionMapper>();
    builder.Services.AddSignalR(options => { options.AddFilter(new HubAuthorizeFilter()); })
      .AddHubOptions((HubOptions<RelayHub> options) =>
      {
        options.MaximumReceiveMessageSize = options.MaximumReceiveMessageSize;
      });

    builder.Services.AddTransient<PortunnelMiddleware>();

    builder.Services.Configure(DefaultPortunnelOptions);

    builder.Services.AddOptionsWithValidateOnStart<PortunnelOptions>()
      .Bind(builder.Configuration.GetSection(PortunnelOptions.ConfigurationSectionName))
      .ValidateDataAnnotations();

    if (configureOptions is not null)
      builder.Services.Configure(configureOptions);

    builder.Services.AddOptions<HubOptions<RelayHub>>()
      .PostConfigure<IOptions<PortunnelOptions>>((hubOptions, portunnelOptions) =>
      {
        if (portunnelOptions.Value.MaximumReceiveMessageSize is not null)
          hubOptions.MaximumReceiveMessageSize = portunnelOptions.Value.MaximumReceiveMessageSize;
      });

    return builder;
  }

  public static IApplicationBuilder UsePortunnel(this IApplicationBuilder app)
  {
    if (app is not IEndpointRouteBuilder endpointRouteBuilder)
      throw new RouteCreationException(nameof(app));

    var portunnelOptions = app.ApplicationServices.GetRequiredService<IOptions<PortunnelOptions>>().Value;

    endpointRouteBuilder.MapHub<RelayHub>(portunnelOptions.Prefix);
    app.UseWebSockets();
    app.UseMiddleware<PortunnelMiddleware>();

    return app;
  }
}