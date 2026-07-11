using System.Data;
using System.Data.Common;
using CareerAssistant.Api.Data;
using CareerAssistant.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace CareerAssistant.Api.Tests;

public class CareerAssistantApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly IReadOnlyDictionary<string, string?> _configuration;
    private readonly IJobAnalysisService? _jobAnalysisService;
    private readonly bool _useConfiguredJobAnalysisService;
    private readonly bool _useTestAuthentication;

    public CareerAssistantApiFactory(
        IReadOnlyDictionary<string, string?>? configuration = null,
        IJobAnalysisService? jobAnalysisService = null,
        bool useConfiguredJobAnalysisService = false,
        bool useTestAuthentication = false)
    {
        _configuration = configuration ?? new Dictionary<string, string?>();
        _jobAnalysisService = jobAnalysisService;
        _useConfiguredJobAnalysisService = useConfiguredJobAnalysisService;
        _useTestAuthentication = useTestAuthentication;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (_connection.State != ConnectionState.Open)
        {
            _connection.Open();
        }

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var testConfiguration = new Dictionary<string, string?>
            {
                ["AI:Provider"] = "Mock",
                ["AI:Model"] = "test-mock",
                ["OpenAI:ApiKey"] = string.Empty,
                ["Database:MigrateOnStartup"] = "false",
                ["Demo:Enabled"] = "false"
            };

            foreach (var setting in _configuration)
            {
                testConfiguration[setting.Key] = setting.Value;
            }

            configuration.AddInMemoryCollection(testConfiguration);
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });

        builder.ConfigureServices(services =>
        {
            if (_useTestAuthentication)
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.SchemeName,
                    _ => { });
            }

            if (!_useConfiguredJobAnalysisService)
            {
                services.RemoveAll<IJobAnalysisService>();
                services.AddScoped<IJobAnalysisService>(_ => _jobAnalysisService ?? new MockJobAnalysisService());
            }

            var dbContextOptionsDescriptor = services.SingleOrDefault(
                service => service.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (dbContextOptionsDescriptor != null)
            {
                services.Remove(dbContextOptionsDescriptor);
            }

            var dbConnectionDescriptor = services.SingleOrDefault(
                service => service.ServiceType == typeof(DbConnection));

            if (dbConnectionDescriptor != null)
            {
                services.Remove(dbConnectionDescriptor);
            }

            services.AddSingleton<DbConnection>(_connection);

            services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            {
                var connection = serviceProvider.GetRequiredService<DbConnection>();
                options.UseSqlite(connection);
            });

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
