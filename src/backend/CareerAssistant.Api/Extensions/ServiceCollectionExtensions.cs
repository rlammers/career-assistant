using CareerAssistant.Api.Data;
using CareerAssistant.Api.Options;
using CareerAssistant.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OpenAI;
using System.ClientModel;
using System.ClientModel.Primitives;

namespace CareerAssistant.Api.Extensions;

internal static class ServiceCollectionExtensions
{
    public static void AddCareerAssistantServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authenticationOptions = configuration.GetSection(AuthenticationOptions.SectionName).Get<AuthenticationOptions>() ?? new();
        var aiOptions = configuration.GetSection("AI").Get<AiOptions>() ?? new();
        var aiProvider = string.IsNullOrWhiteSpace(aiOptions.Provider) ? "Mock" : aiOptions.Provider;

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));
        services.AddCareerAssistantOptions(configuration);
        services.AddConfiguredAuthentication(authenticationOptions);
        services.AddAuthorization();
        services.AddJobAnalysisService(configuration, aiOptions, aiProvider);
        services.AddConfiguredCors(configuration);
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

    }

    private static void AddCareerAssistantOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiOptions>(configuration.GetSection("AI"));
        services.AddOptions<AuthenticationOptions>()
            .Bind(configuration.GetSection(AuthenticationOptions.SectionName))
            .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.TenantId), "Authentication:TenantId is required when authentication is enabled.")
            .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.ClientId), "Authentication:ClientId is required when authentication is enabled.")
            .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.Audience), "Authentication:Audience is required when authentication is enabled.")
            .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.Issuer), "Authentication:Issuer is required when authentication is enabled.")
            .ValidateOnStart();
        services.AddOptions<DemoOptions>()
            .Bind(configuration.GetSection(DemoOptions.SectionName))
            .Validate(options => options.MaxJobs > 0, "Demo:MaxJobs must be greater than zero.")
            .Validate(options => options.MaxAnalyses > 0, "Demo:MaxAnalyses must be greater than zero.")
            .ValidateOnStart();
        services.AddSingleton<DemoQuotaGate>();
    }

    private static void AddConfiguredAuthentication(this IServiceCollection services, AuthenticationOptions authenticationOptions)
    {
        if (!authenticationOptions.Enabled)
        {
            return;
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = $"https://login.microsoftonline.com/{authenticationOptions.TenantId}/v2.0";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authenticationOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = authenticationOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true
                };
            });
    }

    private static void AddJobAnalysisService(
        this IServiceCollection services,
        IConfiguration configuration,
        AiOptions aiOptions,
        string aiProvider)
    {
        if (string.Equals(aiProvider, "Mock", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IJobAnalysisService, MockJobAnalysisService>();
            return;
        }

        if (!string.Equals(aiProvider, "OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unsupported AI provider '{aiProvider}'. Supported providers are Mock and OpenAI.");
        }

        if (string.IsNullOrWhiteSpace(aiOptions.Model))
        {
            throw new InvalidOperationException("OpenAI analysis provider requires AI:Model to be configured.");
        }

        var apiKey = GetOpenAiApiKey(configuration);

        services.AddSingleton(serviceProvider =>
        {
            var configuredAiOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiOptions>>().Value;
            var timeoutSeconds = configuredAiOptions.TimeoutSeconds > 0 ? configuredAiOptions.TimeoutSeconds : 60;
            var clientOptions = new OpenAIClientOptions
            {
                NetworkTimeout = TimeSpan.FromSeconds(timeoutSeconds),
                RetryPolicy = new ClientRetryPolicy(maxRetries: 0)
            };

            if (!string.IsNullOrWhiteSpace(configuredAiOptions.BaseUrl))
            {
                clientOptions.Endpoint = new Uri(configuredAiOptions.BaseUrl);
            }

            return new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions);
        });
        services.AddSingleton<IOpenAiChatCompletionClient>(serviceProvider =>
            new OpenAiSdkChatCompletionClient(
                serviceProvider.GetRequiredService<OpenAIClient>(),
                aiOptions.Model));
        services.AddScoped<IJobAnalysisService, OpenAiJobAnalysisService>();
    }

    private static string GetOpenAiApiKey(IConfiguration configuration)
    {
        var openAiOptions = configuration.GetSection("OpenAI").Get<OpenAiOptions>() ?? new();
        var apiKey = configuration["OpenAI:ApiKey"];
        apiKey = string.IsNullOrWhiteSpace(apiKey) ? openAiOptions.ApiKey : apiKey;

        var apiKeyFile = configuration["OpenAI:ApiKeyFile"];
        apiKeyFile = string.IsNullOrWhiteSpace(apiKeyFile) ? openAiOptions.ApiKeyFile : apiKeyFile;

        if (string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(apiKeyFile))
        {
            if (!File.Exists(apiKeyFile))
            {
                throw new InvalidOperationException($"OpenAI analysis provider could not read OpenAI:ApiKeyFile at '{apiKeyFile}'.");
            }

            apiKey = File.ReadAllText(apiKeyFile).Trim();
        }

        return string.IsNullOrWhiteSpace(apiKey)
            ? throw new InvalidOperationException("OpenAI analysis provider requires OpenAI:ApiKey or OpenAI:ApiKeyFile to be configured.")
            : apiKey;
    }

    private static void AddConfiguredCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options => options.AddPolicy(StartupConfiguration.CorsPolicyName, policy =>
        {
            var frontendOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

            policy.WithOrigins(frontendOrigins).AllowAnyHeader().AllowAnyMethod();
        }));
    }
}
