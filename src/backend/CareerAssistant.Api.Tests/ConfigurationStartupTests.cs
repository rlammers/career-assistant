using System.Net;
using System.Net.Http.Headers;

namespace CareerAssistant.Api.Tests;

public class ConfigurationStartupTests
{
    [Fact]
    public async Task MockProviderStartsWithoutOpenAiConfiguration()
    {
        await WithEnvironmentVariablesAsync(
            new Dictionary<string, string?>
            {
                ["AI__Provider"] = "Mock",
                ["OpenAI__ApiKey"] = " ",
                ["OpenAI__ApiKeyFile"] = " "
            },
            async () =>
            {
                using var factory = new CareerAssistantApiFactory(useConfiguredJobAnalysisService: true);
                using var client = factory.CreateClient();

                var response = await client.GetAsync("/health");

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            });
    }

    [Fact]
    public void UnsupportedProviderFailsClearlyOnStartup()
    {
        var exception = Assert.ThrowsAny<Exception>(() => WithEnvironmentVariables(
            new Dictionary<string, string?>
            {
                ["AI__Provider"] = "MadeUpProvider"
            },
            () =>
            {
                using var factory = new CareerAssistantApiFactory(useConfiguredJobAnalysisService: true);
                using var client = factory.CreateClient();
            }));

        AssertExceptionContains(exception, "Unsupported AI provider 'MadeUpProvider'");
    }

    [Fact]
    public void EnabledAuthenticationWithoutRequiredConfigurationFailsClearlyOnStartup()
    {
        var exception = Assert.ThrowsAny<Exception>(() => WithEnvironmentVariables(
            new Dictionary<string, string?>
            {
                ["Authentication__Enabled"] = "true"
            },
            () =>
            {
                using var factory = new CareerAssistantApiFactory(useConfiguredJobAnalysisService: true);
                using var client = factory.CreateClient();
            }));

        AssertExceptionContains(exception, "Authentication:TenantId is required when authentication is enabled.");
    }

    [Fact]
    public async Task EnabledAuthenticationWithCompleteConfigurationStarts()
    {
        await WithEnvironmentVariablesAsync(
            new Dictionary<string, string?>
            {
                ["Authentication__Enabled"] = "true",
                ["Authentication__TenantId"] = "11111111-1111-1111-1111-111111111111",
                ["Authentication__ClientId"] = "22222222-2222-2222-2222-222222222222",
                ["Authentication__Audience"] = "api://22222222-2222-2222-2222-222222222222",
                ["Authentication__Issuer"] = "https://login.microsoftonline.com/11111111-1111-1111-1111-111111111111/v2.0",
                ["Authentication__RequiredAppRole"] = "CareerAssistant.Demo.Access",
                ["Database__MigrateOnStartup"] = "false"
            },
            async () =>
            {
                using var factory = new CareerAssistantApiFactory(useConfiguredJobAnalysisService: true);
                using var client = factory.CreateClient();

                var healthResponse = await client.GetAsync("/health");
                var profileResponse = await client.GetAsync("/api/profile");

                Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
                Assert.Equal(HttpStatusCode.Unauthorized, profileResponse.StatusCode);
            });
    }

    [Fact]
    public async Task EnabledAuthenticationRejectsUsersWithoutTheConfiguredAppRole()
    {
        await WithEnvironmentVariablesAsync(
            AuthenticatedEnvironmentVariables(),
            async () =>
            {
                using var factory = new CareerAssistantApiFactory(useTestAuthentication: true);
                using var client = factory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Get, "/api/profile");
                request.Headers.Add(TestAuthenticationHandler.AppRoleHeaderName, "Other.Role");

                var response = await client.SendAsync(request);

                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            });
    }

    [Fact]
    public async Task EnabledAuthenticationAllowsUsersWithTheConfiguredAppRole()
    {
        await WithEnvironmentVariablesAsync(
            AuthenticatedEnvironmentVariables(),
            async () =>
            {
                using var factory = new CareerAssistantApiFactory(useTestAuthentication: true);
                using var client = factory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Get, "/api/profile");
                request.Headers.Add(TestAuthenticationHandler.AppRoleHeaderName, "CareerAssistant.Demo.Access");

                var response = await client.SendAsync(request);

                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            });
    }

    [Fact]
    public async Task EnabledAuthenticationAcceptsOnlyValidTokensFromTheConfiguredTenant()
    {
        await WithEnvironmentVariablesAsync(
            AuthenticatedEnvironmentVariables(),
            async () =>
            {
                using var factory = new CareerAssistantApiFactory(useTestJwtBearerAuthentication: true);
                using var client = factory.CreateClient();

                var validResponse = await SendBearerRequestAsync(client, TestJwtTokens.Create());
                var expiredResponse = await SendBearerRequestAsync(
                    client,
                    TestJwtTokens.Create(expires: DateTime.UtcNow.AddMinutes(-6)));
                var wrongIssuerResponse = await SendBearerRequestAsync(
                    client,
                    TestJwtTokens.Create(issuer: "https://login.microsoftonline.com/other-tenant/v2.0"));
                var wrongAudienceResponse = await SendBearerRequestAsync(
                    client,
                    TestJwtTokens.Create(audience: "api://other-api"));
                var wrongTenantResponse = await SendBearerRequestAsync(
                    client,
                    TestJwtTokens.Create(tenantId: "33333333-3333-3333-3333-333333333333"));

                Assert.Equal(HttpStatusCode.NotFound, validResponse.StatusCode);
                Assert.Equal(HttpStatusCode.Unauthorized, expiredResponse.StatusCode);
                Assert.Equal(HttpStatusCode.Unauthorized, wrongIssuerResponse.StatusCode);
                Assert.Equal(HttpStatusCode.Unauthorized, wrongAudienceResponse.StatusCode);
                Assert.Equal(HttpStatusCode.Unauthorized, wrongTenantResponse.StatusCode);
                Assert.DoesNotContain(
                    "error_description",
                    string.Join(", ", wrongIssuerResponse.Headers.WwwAuthenticate.Select(header => header.ToString())),
                    StringComparison.OrdinalIgnoreCase);
            });
    }

    [Fact]
    public void OpenAiProviderWithoutApiKeyFailsClearlyOnStartup()
    {
        var exception = Assert.ThrowsAny<Exception>(() => WithEnvironmentVariables(
            new Dictionary<string, string?>
            {
                ["AI__Provider"] = "OpenAI",
                ["AI__Model"] = "gpt-test",
                ["OpenAI__ApiKey"] = " ",
                ["OpenAI__ApiKeyFile"] = " "
            },
            () =>
            {
                using var factory = new CareerAssistantApiFactory(useConfiguredJobAnalysisService: true);
                using var client = factory.CreateClient();
            }));

        AssertExceptionContains(exception, "OpenAI:ApiKey or OpenAI:ApiKeyFile");
    }

    [Fact]
    public void OpenAiProviderWithMissingApiKeyFileFailsClearlyOnStartup()
    {
        var exception = Assert.ThrowsAny<Exception>(() => WithEnvironmentVariables(
            new Dictionary<string, string?>
            {
                ["AI__Provider"] = "OpenAI",
                ["AI__Model"] = "gpt-test",
                ["OpenAI__ApiKey"] = " ",
                ["OpenAI__ApiKeyFile"] = "missing-openai-api-key.txt"
            },
            () =>
            {
                using var factory = new CareerAssistantApiFactory(useConfiguredJobAnalysisService: true);
                using var client = factory.CreateClient();
            }));

        AssertExceptionContains(exception, "could not read OpenAI:ApiKeyFile");
    }

    private static async Task WithEnvironmentVariablesAsync(
        IReadOnlyDictionary<string, string?> variables,
        Func<Task> action)
    {
        var originals = SetEnvironmentVariables(variables);

        try
        {
            await action();
        }
        finally
        {
            RestoreEnvironmentVariables(originals);
        }
    }

    private static void WithEnvironmentVariables(
        IReadOnlyDictionary<string, string?> variables,
        Action action)
    {
        var originals = SetEnvironmentVariables(variables);

        try
        {
            action();
        }
        finally
        {
            RestoreEnvironmentVariables(originals);
        }
    }

    private static IReadOnlyDictionary<string, string?> AuthenticatedEnvironmentVariables() => new Dictionary<string, string?>
    {
        ["Authentication__Enabled"] = "true",
        ["Authentication__TenantId"] = "11111111-1111-1111-1111-111111111111",
        ["Authentication__ClientId"] = "22222222-2222-2222-2222-222222222222",
        ["Authentication__Audience"] = "api://22222222-2222-2222-2222-222222222222",
        ["Authentication__Issuer"] = "https://login.microsoftonline.com/11111111-1111-1111-1111-111111111111/v2.0",
        ["Authentication__RequiredAppRole"] = "CareerAssistant.Demo.Access",
        ["Database__MigrateOnStartup"] = "false"
    };

    private static async Task<HttpResponseMessage> SendBearerRequestAsync(HttpClient client, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/profile");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await client.SendAsync(request);
    }

    private static Dictionary<string, string?> SetEnvironmentVariables(IReadOnlyDictionary<string, string?> variables)
    {
        var originals = new Dictionary<string, string?>();

        foreach (var variable in variables)
        {
            originals[variable.Key] = Environment.GetEnvironmentVariable(variable.Key);
            Environment.SetEnvironmentVariable(variable.Key, variable.Value);
        }

        return originals;
    }

    private static void RestoreEnvironmentVariables(IReadOnlyDictionary<string, string?> originals)
    {
        foreach (var original in originals)
        {
            Environment.SetEnvironmentVariable(original.Key, original.Value);
        }
    }

    private static void AssertExceptionContains(Exception exception, string expectedMessage)
    {
        var current = exception;

        while (current != null)
        {
            if (current.Message.Contains(expectedMessage, StringComparison.Ordinal))
            {
                return;
            }

            current = current.InnerException;
        }

        Assert.Fail($"Expected exception message to contain '{expectedMessage}', but got: {exception}");
    }
}
