using CareerAssistant.Api.Options;
using Microsoft.Extensions.Configuration;

namespace CareerAssistant.Api.Extensions;

internal sealed record StartupConfiguration(
    bool AuthenticationEnabled,
    string AiProvider,
    bool MigrateOnStartup,
    bool ForwardedHeadersEnabled)
{
    public const string CorsPolicyName = "AllowFrontend";

    public static StartupConfiguration From(IConfiguration configuration)
    {
        var authenticationOptions = configuration.GetSection(AuthenticationOptions.SectionName).Get<AuthenticationOptions>() ?? new();
        var aiOptions = configuration.GetSection("AI").Get<AiOptions>() ?? new();
        var aiProvider = string.IsNullOrWhiteSpace(aiOptions.Provider) ? "Mock" : aiOptions.Provider;

        return new StartupConfiguration(
            authenticationOptions.Enabled,
            aiProvider,
            configuration.GetValue<bool>("Database:MigrateOnStartup"),
            configuration.GetValue("ForwardedHeaders:Enabled", false));
    }
}
