using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Microsoft.AspNetCore.SignalR;
using Portunnel.Proxy.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Portunnel.Cli;

var returnCode = 0;

var services = new ServiceCollection();
services.AddLogging(options => options.AddConsole());
services.AddSingleton<IRelayService, RelayService>();
services.AddSingleton<IOptionsProvider, CliOptionsProvider>();
services.AddSingleton<ILogDestination, ConsoleLogDestination>();
services.AddSingleton<IRequestLogger, RequestLogger>();
services.AddSingleton<IUrlValidator, UrlValidator>();

var serviceProvider = services.BuildServiceProvider();
var urlValidator = serviceProvider.GetRequiredService<IUrlValidator>();

var gatewayUrlOption = new Option<Uri>("--gateway-url", description: "Host URL connecting to") { IsRequired = true, Arity = ArgumentArity.ExactlyOne };
var apiKeyOption = new Option<Uri>("--api-key", description: "Host api connection key") { IsRequired = true, Arity = ArgumentArity.ExactlyOne };
var forwardUrlOption = new Option<Uri>("--forward-url", description: "Service URL forwarding to") { IsRequired = true,  Arity = ArgumentArity.ExactlyOne };

ValidateSymbolResult<OptionResult> validator = result =>
{
  if (result.Token is null)
  {
    result.ErrorMessage = "Token is empty";
    return;
  }
  
  var r = result.GetValueOrDefault<Uri>();

  if (r is null)
  {
    result.ErrorMessage = $"{result.Token.Value} is required";
    return;
  }

  if (urlValidator.IsValid(r.ToString(), out var errorMessage))
    return;
  
  result.ErrorMessage = $"Invalid parameter: '{result.Token.Value} {result.Tokens[0]}'\n{errorMessage}";
};

gatewayUrlOption.AddValidator(validator);
forwardUrlOption.AddValidator(validator);

var rootCommand = new RootCommand("Portunnel. Forward http and websocket responses.");
rootCommand.AddOption(gatewayUrlOption); 
rootCommand.AddOption(apiKeyOption); 
rootCommand.AddOption(forwardUrlOption);

rootCommand.SetHandler(async context =>
{
  var token = context.GetCancellationToken();
  
  var optionsProvider = serviceProvider.GetRequiredService<IOptionsProvider>();

  var gatewayUrl = context.ParseResult.GetValueForOption(gatewayUrlOption);
  var hostApiKey = context.ParseResult.GetValueForOption(apiKeyOption);
  var forwardUrl = context.ParseResult.GetValueForOption(forwardUrlOption);

  if (gatewayUrl is null || hostApiKey is null || forwardUrl is null)
    throw new Exception("One or more options is null for some reason");
  
  optionsProvider.HostUrl = gatewayUrl.ToString();
  optionsProvider.HostApiKey = hostApiKey.ToString();
  optionsProvider.TargetServiceUrl = forwardUrl.ToString();
  
  var relayService = context.BindingContext.GetRequiredService<IRelayService>();
  returnCode = await DoRootCommand(relayService, optionsProvider.HostUrl, token);
});

var commandLineBuilder = new CommandLineBuilder(rootCommand);

commandLineBuilder.AddMiddleware(async (context, next) =>
{
  context.BindingContext.AddService<IRelayService>(_ => serviceProvider.GetRequiredService<IRelayService>());
  context.BindingContext.AddService<IOptionsProvider>(_ => serviceProvider.GetRequiredService<IOptionsProvider>());
  context.BindingContext.AddService<ILogger<RelayService>>(_ => serviceProvider.GetRequiredService<ILogger<RelayService>>());
  context.BindingContext.AddService<ILogger>(_ => serviceProvider.GetRequiredService<ILogger>());
  context.BindingContext.AddService<IUrlValidator>(_ => serviceProvider.GetRequiredService<IUrlValidator>());
  
  await next(context);
});

commandLineBuilder.UseDefaults();
var parser = commandLineBuilder.Build();

await parser.InvokeAsync(args);

return returnCode;

async Task<int> DoRootCommand(IRelayService relayService, string hostUrl, CancellationToken cancellationToken)
{
  try
  {
    await relayService.Connect();
    var clientId = await relayService.Register();
    Console.Out.WriteLine($"\e[32mConnection URL is: '{hostUrl}/{clientId}'\e[0m");

    while (relayService.IsConnected())
    {
      cancellationToken.ThrowIfCancellationRequested();
      await Task.Delay(1000, cancellationToken);
    }

    Console.Out.WriteLine("Connection to the host is terminated.");
    return 0;
  }
  catch (OperationCanceledException e)
  {
    Console.Out.WriteLine("Application terminated");
    return 0;
  }
  catch (HubException hubException)
  {
    Console.Out.WriteLine(hubException.Message);
    return 1;
  }
  catch (Exception e)
  {
    Console.Out.WriteLine(e.Message);
    return 1;
  }
}