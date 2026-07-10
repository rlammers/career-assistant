using CareerAssistant.Api.Data;
using CareerAssistant.Api.Options;
using CareerAssistant.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using OpenAI;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<AiOptions>(builder.Configuration.GetSection("AI"));
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.AddOptions<AuthenticationOptions>()
    .Bind(builder.Configuration.GetSection(AuthenticationOptions.SectionName))
    .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.TenantId), "Authentication:TenantId is required when authentication is enabled.")
    .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.ClientId), "Authentication:ClientId is required when authentication is enabled.")
    .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.Audience), "Authentication:Audience is required when authentication is enabled.")
    .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.Issuer), "Authentication:Issuer is required when authentication is enabled.")
    .ValidateOnStart();
builder.Services.AddOptions<DemoOptions>()
    .Bind(builder.Configuration.GetSection(DemoOptions.SectionName))
    .Validate(options => options.MaxJobs > 0, "Demo:MaxJobs must be greater than zero.")
    .Validate(options => options.MaxAnalyses > 0, "Demo:MaxAnalyses must be greater than zero.")
    .ValidateOnStart();
builder.Services.AddSingleton<DemoQuotaGate>();

var authenticationOptions = builder.Configuration.GetSection(AuthenticationOptions.SectionName).Get<AuthenticationOptions>() ?? new AuthenticationOptions();

if (authenticationOptions.Enabled)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

var aiOptions = builder.Configuration.GetSection("AI").Get<AiOptions>() ?? new AiOptions();
var aiProvider = string.IsNullOrWhiteSpace(aiOptions.Provider) ? "Mock" : aiOptions.Provider;

if (string.Equals(aiProvider, "Mock", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IJobAnalysisService, MockJobAnalysisService>();
}
else if (string.Equals(aiProvider, "OpenAI", StringComparison.OrdinalIgnoreCase))
{
    if (string.IsNullOrWhiteSpace(aiOptions.Model))
    {
        throw new InvalidOperationException("OpenAI analysis provider requires AI:Model to be configured.");
    }

    var openAiOptions = builder.Configuration.GetSection("OpenAI").Get<OpenAiOptions>() ?? new OpenAiOptions();
    var apiKey = builder.Configuration["OpenAI:ApiKey"];
    apiKey = string.IsNullOrWhiteSpace(apiKey) ? openAiOptions.ApiKey : apiKey;

    var apiKeyFile = builder.Configuration["OpenAI:ApiKeyFile"];
    apiKeyFile = string.IsNullOrWhiteSpace(apiKeyFile) ? openAiOptions.ApiKeyFile : apiKeyFile;

    if (string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(apiKeyFile))
    {
        if (!File.Exists(apiKeyFile))
        {
            throw new InvalidOperationException($"OpenAI analysis provider could not read OpenAI:ApiKeyFile at '{apiKeyFile}'.");
        }

        apiKey = File.ReadAllText(apiKeyFile).Trim();
    }

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        throw new InvalidOperationException("OpenAI analysis provider requires OpenAI:ApiKey or OpenAI:ApiKeyFile to be configured.");
    }

    builder.Services.AddSingleton(serviceProvider =>
    {
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiOptions>>().Value;
        var timeoutSeconds = options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 60;
        var clientOptions = new OpenAIClientOptions
        {
            NetworkTimeout = TimeSpan.FromSeconds(timeoutSeconds),
            RetryPolicy = new ClientRetryPolicy(maxRetries: 0)
        };

        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            clientOptions.Endpoint = new Uri(options.BaseUrl);
        }

        return new OpenAIClient(
            new ApiKeyCredential(apiKey),
            clientOptions);
    });
    builder.Services.AddSingleton<IOpenAiChatCompletionClient>(serviceProvider =>
        new OpenAiSdkChatCompletionClient(
            serviceProvider.GetRequiredService<OpenAIClient>(),
            aiOptions.Model));
    builder.Services.AddScoped<IJobAnalysisService, OpenAiJobAnalysisService>();
}
else
{
    throw new InvalidOperationException($"Unsupported AI provider '{aiProvider}'. Supported providers are Mock and OpenAI.");
}

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var frontendOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173"];

        policy
            .WithOrigins(frontendOrigins)
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
            .WithHeaders("Content-Type");
    });
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

var forwardedHeadersEnabled = builder.Configuration.GetValue("ForwardedHeaders:Enabled", false);
var migrateOnStartup = builder.Configuration.GetValue("Database:MigrateOnStartup", true);

app.Logger.LogInformation(
    "Starting Career Assistant API in {Environment}. AI provider: {AIProvider}. Migrate on startup: {MigrateOnStartup}. Forwarded headers: {ForwardedHeadersEnabled}.",
    app.Environment.EnvironmentName,
    aiProvider,
    migrateOnStartup,
    forwardedHeadersEnabled);

if (forwardedHeadersEnabled)
{
    var forwardedHeadersOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    };

    foreach (var proxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
    {
        if (!IPAddress.TryParse(proxy, out var address))
        {
            throw new InvalidOperationException($"ForwardedHeaders:KnownProxies contains an invalid IP address: '{proxy}'.");
        }

        forwardedHeadersOptions.KnownProxies.Add(address);
    }

    foreach (var network in builder.Configuration.GetSection("ForwardedHeaders:KnownIPNetworks").Get<string[]>() ?? [])
    {
        if (!System.Net.IPNetwork.TryParse(network, out var ipNetwork))
        {
            throw new InvalidOperationException($"ForwardedHeaders:KnownIPNetworks contains an invalid CIDR network: '{network}'.");
        }

        forwardedHeadersOptions.KnownIPNetworks.Add(ipNetwork);
    }

    app.UseForwardedHeaders(forwardedHeadersOptions);
}

if (migrateOnStartup)
{
    app.Logger.LogInformation("Applying database migrations.");

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

if (authenticationOptions.Enabled)
{
    app.UseAuthentication();
}

app.UseAuthorization();

app.MapMethods("/health", ["GET", "HEAD"], () => Results.Ok(new { status = "Healthy" }))
    .AllowAnonymous();
app.MapControllers();

app.Run();

public partial class Program
{
}
