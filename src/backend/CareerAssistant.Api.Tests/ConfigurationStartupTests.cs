using System.Net;

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
