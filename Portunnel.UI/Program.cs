using Portunnel.UI.Components;
using Portunnel.Proxy.Services;
using Portunnel.UI;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddSingleton<IRelayService, RelayService>();
builder.Services.AddSingleton<IOptionsProvider, OptionsProvider>();
builder.Services.AddSingleton<IUrlValidator, UrlValidator>();

// Add services to the container.
builder.Services.AddRazorComponents()
  .AddInteractiveServerComponents();

builder.Services.AddOptionsWithValidateOnStart<PortunnelOptions>()
  .Bind(builder.Configuration.GetSection(PortunnelOptions.ConfigurationSectionName))
  .ValidateDataAnnotations();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Error", createScopeForErrors: true);
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  // app.UseHsts();
}

// app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
  .AddInteractiveServerRenderMode();

app.Run();