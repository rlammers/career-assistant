using System.Net;
using System.Net.Http.Headers;

namespace CareerAssistant.Api.Tests;

public class ConfigurationStartupTests
{
    [Fact]
    public async Task MockProviderStartsWithoutOpenAiConfiguration()
    {
        using var factory = new CareerAssistantApiFactory(
            new Dictionary<string, string?>
            {
                ["AI:Provider"] = "Mock",
                ["OpenAI:ApiKey"] = " ",
                ["OpenAI:ApiKeyFile"] = " "
            },
            useConfiguredJobAnalysisService: true);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void UnsupportedProviderFailsClearlyOnStartup()
    {
        var exception = Assert.ThrowsAny<Exception>(() =>
        {
            using var factory = new CareerAssistantApiFactory(
            new Dictionary<string, string?>
            {
                ["AI:Provider"] = "MadeUpProvider"
            },
            useConfiguredJobAnalysisService: true);
            using var client = factory.CreateClient();
        });

        AssertExceptionContains(exception, "Unsupported AI provider 'MadeUpProvider'");
    }

    [Fact]
    public void EnabledAuthenticationWithoutRequiredConfigurationFailsClearlyOnStartup()
    {
        var exception = Assert.ThrowsAny<Exception>(() =>
        {
            using var factory = new CareerAssistantApiFactory(
            new Dictionary<string, string?>
            {
                ["Authentication:Enabled"] = "true"
            },
            useConfiguredJobAnalysisService: true);
            using var client = factory.CreateClient();
        });

        AssertExceptionContains(exception, "Authentication:TenantId is required when authentication is enabled.");
    }

    [Fact]
    public async Task EnabledAuthenticationWithCompleteConfigurationStarts()
    {
        using var factory = new CareerAssistantApiFactory(
            AuthenticatedConfiguration(),
            useConfiguredJobAnalysisService: true);
        using var client = factory.CreateClient();

        var healthResponse = await client.GetAsync("/health");
        var profileResponse = await client.GetAsync("/api/profile");

        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, profileResponse.StatusCode);
    }

    [Fact]
    public async Task EnabledAuthenticationRejectsUsersWithoutTheConfiguredAppRole()
    {
        using var factory = new CareerAssistantApiFactory(
            AuthenticatedConfiguration(),
            useTestAuthentication: true);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/profile");
        request.Headers.Add(TestAuthenticationHandler.AppRoleHeaderName, "Other.Role");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EnabledAuthenticationAllowsUsersWithTheConfiguredAppRole()
    {
        using var factory = new CareerAssistantApiFactory(
            AuthenticatedConfiguration(),
            useTestAuthentication: true);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/profile");
        request.Headers.Add(TestAuthenticationHandler.AppRoleHeaderName, "CareerAssistant.Demo.Access");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task EnabledAuthenticationAcceptsOnlyValidTokensFromTheConfiguredTenant()
    {
        using var factory = new CareerAssistantApiFactory(
            AuthenticatedConfiguration(),
            useTestJwtBearerAuthentication: true);
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
        var applicationIdUriAudienceResponse = await SendBearerRequestAsync(
            client,
            TestJwtTokens.Create(audience: "api://22222222-2222-2222-2222-222222222222"));
        var wrongTenantResponse = await SendBearerRequestAsync(
            client,
            TestJwtTokens.Create(tenantId: "33333333-3333-3333-3333-333333333333"));

        Assert.Equal(HttpStatusCode.NotFound, validResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, expiredResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongIssuerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongAudienceResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, applicationIdUriAudienceResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongTenantResponse.StatusCode);

        await AssertSafeAuthenticationFailureAsync(expiredResponse);
        await AssertSafeAuthenticationFailureAsync(wrongIssuerResponse);
        await AssertSafeAuthenticationFailureAsync(wrongAudienceResponse);
        await AssertSafeAuthenticationFailureAsync(applicationIdUriAudienceResponse);
        await AssertSafeAuthenticationFailureAsync(wrongTenantResponse);
    }

    [Fact]
    public void OpenAiProviderWithoutApiKeyFailsClearlyOnStartup()
    {
        var exception = Assert.ThrowsAny<Exception>(() =>
        {
            using var factory = new CareerAssistantApiFactory(
            new Dictionary<string, string?>
            {
                ["AI:Provider"] = "OpenAI",
                ["AI:Model"] = "gpt-test",
                ["OpenAI:ApiKey"] = " ",
                ["OpenAI:ApiKeyFile"] = " "
            },
            useConfiguredJobAnalysisService: true);
            using var client = factory.CreateClient();
        });

        AssertExceptionContains(exception, "OpenAI:ApiKey or OpenAI:ApiKeyFile");
    }

    [Fact]
    public void OpenAiProviderWithMissingApiKeyFileFailsClearlyOnStartup()
    {
        var exception = Assert.ThrowsAny<Exception>(() =>
        {
            using var factory = new CareerAssistantApiFactory(
            new Dictionary<string, string?>
            {
                ["AI:Provider"] = "OpenAI",
                ["AI:Model"] = "gpt-test",
                ["OpenAI:ApiKey"] = " ",
                ["OpenAI:ApiKeyFile"] = "missing-openai-api-key.txt"
            },
            useConfiguredJobAnalysisService: true);
            using var client = factory.CreateClient();
        });

        AssertExceptionContains(exception, "could not read OpenAI:ApiKeyFile");
    }

    private static IReadOnlyDictionary<string, string?> AuthenticatedConfiguration() => new Dictionary<string, string?>
    {
        ["Authentication:Enabled"] = "true",
        ["Authentication:TenantId"] = "11111111-1111-1111-1111-111111111111",
        ["Authentication:ClientId"] = "22222222-2222-2222-2222-222222222222",
        ["Authentication:Audience"] = "22222222-2222-2222-2222-222222222222",
        ["Authentication:Issuer"] = "https://login.microsoftonline.com/11111111-1111-1111-1111-111111111111/v2.0",
        ["Authentication:RequiredAppRole"] = "CareerAssistant.Demo.Access",
        ["Database:MigrateOnStartup"] = "false"
    };

    private static async Task<HttpResponseMessage> SendBearerRequestAsync(HttpClient client, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/profile");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await client.SendAsync(request);
    }

    private static async Task AssertSafeAuthenticationFailureAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        var headers = string.Join(", ", response.Headers.WwwAuthenticate.Select(header => header.ToString()));
        var failureText = $"{headers}\n{body}";

        Assert.DoesNotContain("error_description", failureText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(@"eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+", failureText);
        Assert.DoesNotContain(TestJwtTokens.TenantId, failureText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(TestJwtTokens.Issuer, failureText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(TestJwtTokens.Audience, failureText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CareerAssistant.Demo.Access", failureText, StringComparison.OrdinalIgnoreCase);
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
