using CareerAssistant.Api.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace CareerAssistant.Api.Extensions;

internal static class WebApplicationExtensions
{
    public static void LogStartupConfiguration(this WebApplication app, StartupConfiguration startupConfiguration)
    {
        app.Logger.LogInformation(
            "Starting Career Assistant API in {Environment}. AI provider: {AIProvider}. Migrate on startup: {MigrateOnStartup}. Forwarded headers: {ForwardedHeadersEnabled}.",
            app.Environment.EnvironmentName,
            startupConfiguration.AiProvider,
            startupConfiguration.MigrateOnStartup,
            startupConfiguration.ForwardedHeadersEnabled);
    }

    public static void UseConfiguredForwardedHeaders(this WebApplication app, bool enabled, IConfiguration configuration)
    {
        if (!enabled)
        {
            return;
        }

        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        };

        AddKnownProxies(options, configuration);
        AddKnownNetworks(options, configuration);
        app.UseForwardedHeaders(options);
    }

    public static void ApplyDatabaseMigrations(this WebApplication app, bool migrateOnStartup)
    {
        if (!migrateOnStartup)
        {
            return;
        }

        app.Logger.LogInformation("Applying database migrations.");

        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
    }

    public static void UseCareerAssistantPipeline(this WebApplication app, bool authenticationEnabled)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCors(StartupConfiguration.CorsPolicyName);

        if (authenticationEnabled)
        {
            app.UseAuthentication();
        }

        app.UseAuthorization();
    }

    public static void MapCareerAssistantEndpoints(this WebApplication app)
    {
        app.MapMethods("/health", ["GET", "HEAD"], () => Results.Ok(new { status = "Healthy" }))
            .AllowAnonymous();
        app.MapControllers();
    }

    private static void AddKnownProxies(ForwardedHeadersOptions options, IConfiguration configuration)
    {
        foreach (var proxy in configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
        {
            if (!IPAddress.TryParse(proxy, out var address))
            {
                throw new InvalidOperationException($"ForwardedHeaders:KnownProxies contains an invalid IP address: '{proxy}'.");
            }

            options.KnownProxies.Add(address);
        }
    }

    private static void AddKnownNetworks(ForwardedHeadersOptions options, IConfiguration configuration)
    {
        foreach (var network in configuration.GetSection("ForwardedHeaders:KnownIPNetworks").Get<string[]>() ?? [])
        {
            if (!System.Net.IPNetwork.TryParse(network, out var ipNetwork))
            {
                throw new InvalidOperationException($"ForwardedHeaders:KnownIPNetworks contains an invalid CIDR network: '{network}'.");
            }

            options.KnownIPNetworks.Add(ipNetwork);
        }
    }
}
