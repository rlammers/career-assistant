using CareerAssistant.Api.Data;
using CareerAssistant.Api.Options;
using CareerAssistant.Api.Services;
using Microsoft.EntityFrameworkCore;
using OpenAI;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<AiOptions>(builder.Configuration.GetSection("AI"));
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection("OpenAI"));

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

    var apiKey = builder.Configuration["OpenAI:ApiKey"];
    var openAiOptions = builder.Configuration.GetSection("OpenAI").Get<OpenAiOptions>() ?? new OpenAiOptions();
    apiKey = string.IsNullOrWhiteSpace(apiKey) ? openAiOptions.ApiKey : apiKey;

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        throw new InvalidOperationException("OpenAI analysis provider requires OpenAI:ApiKey to be configured.");
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
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // TODO: Return DTOs from controller, then remove this.
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
