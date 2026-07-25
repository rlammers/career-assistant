using CareerAssistant.Api.Data;
using CareerAssistant.Api.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CareerAssistant.Api.Tests;

public class DatabaseProviderRegistrationTests
{
    [Theory]
    [InlineData("Sqlite", "Microsoft.EntityFrameworkCore.Sqlite")]
    [InlineData("  sqlserver  ", "Microsoft.EntityFrameworkCore.SqlServer")]
    public void ConfiguredDatabaseProviderRegistersApplicationDbContext(string provider, string expectedProviderName)
    {
        var services = new ServiceCollection();
        services.AddCareerAssistantServices(BuildConfiguration(
            new KeyValuePair<string, string?>("Database:Provider", provider)));

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Equal(expectedProviderName, dbContext.Database.ProviderName);
    }

    [Fact]
    public void MissingDatabaseProviderFailsClearly()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            var services = new ServiceCollection();
            services.AddCareerAssistantServices(BuildConfiguration());
        });

        Assert.Equal("Database:Provider is required.", exception.Message);
    }

    [Theory]
    [InlineData(" ", "Database:Provider is required.")]
    [InlineData("Unsupported", "Unsupported database provider: Unsupported")]
    public void InvalidDatabaseProviderFailsClearly(string provider, string expectedMessage)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            var services = new ServiceCollection();
            services.AddCareerAssistantServices(BuildConfiguration(
                new KeyValuePair<string, string?>("Database:Provider", provider)));
        });

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void BlankDefaultConnectionFailsClearly()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            var services = new ServiceCollection();
            services.AddCareerAssistantServices(BuildConfiguration(
                new KeyValuePair<string, string?>("Database:Provider", "Sqlite"),
                new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", " ")));
        });

        Assert.Equal("DefaultConnection is required.", exception.Message);
    }

    private static IConfiguration BuildConfiguration(params KeyValuePair<string, string?>[] overrides)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Data Source=provider-registration-tests.db",
            ["AI:Provider"] = "Mock",
            ["Demo:Enabled"] = "false",
            ["Demo:MaxJobs"] = "100",
            ["Demo:MaxAnalyses"] = "200"
        };

        foreach (var setting in overrides)
        {
            settings[setting.Key] = setting.Value;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }
}
