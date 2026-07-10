using CareerAssistant.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCareerAssistantServices(builder.Configuration);

var app = builder.Build();
var startupConfiguration = StartupConfiguration.From(builder.Configuration);

app.LogStartupConfiguration(startupConfiguration);
app.UseConfiguredForwardedHeaders(startupConfiguration.ForwardedHeadersEnabled, builder.Configuration);
app.ApplyDatabaseMigrations(startupConfiguration.MigrateOnStartup);
app.UseCareerAssistantPipeline(startupConfiguration.AuthenticationEnabled);
app.MapCareerAssistantEndpoints();

app.Run();

public partial class Program
{
}
