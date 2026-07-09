using System.Text.Json;
using CareerAssistant.Api.Models;
using CareerAssistant.Api.Options;
using CareerAssistant.Api.Services;

namespace CareerAssistant.Api.Tests;

public class OpenAiJobAnalysisServiceTests
{
    [Fact]
    public async Task ValidStructuredResponseMapsToJobAnalysisResult()
    {
        var service = CreateService(CreateSuccessClient(matchScore: 82));

        var result = await service.AnalyseAsync(CreateProfile(), CreateJob());

        Assert.Equal(7, result.JobApplicationId);
        Assert.Equal(82, result.MatchScore);
        Assert.Equal("Azure", result.MissingSkills);
        Assert.Equal("Strong C# background", result.Strengths);
        Assert.Equal("Emphasise API delivery.", result.Suggestions);
        Assert.Equal("Dear hiring team...", result.CoverLetterDraft);
    }

    [Theory]
    [InlineData(-12, 0)]
    [InlineData(144, 100)]
    public async Task MatchScoreIsClamped(int providerScore, int expectedScore)
    {
        var service = CreateService(CreateSuccessClient(providerScore));

        var result = await service.AnalyseAsync(CreateProfile(), CreateJob());

        Assert.Equal(expectedScore, result.MatchScore);
    }

    [Fact]
    public async Task InvalidStructuredContentThrowsClearError()
    {
        var service = CreateService(new StubOpenAiChatCompletionClient("{not valid json"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AnalyseAsync(CreateProfile(), CreateJob()));

        Assert.Contains("structured JSON", exception.Message);
    }

    [Fact]
    public async Task MissingContentThrowsClearError()
    {
        var service = CreateService(new StubOpenAiChatCompletionClient(""));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AnalyseAsync(CreateProfile(), CreateJob()));

        Assert.Contains("structured JSON", exception.Message);
    }

    [Fact]
    public async Task MissingModelFailsBeforeExternalCall()
    {
        var client = new StubOpenAiChatCompletionClient("{}");
        var service = CreateService(client, new AiOptions
        {
            Provider = "OpenAI",
            Model = "",
            BaseUrl = "https://api.openai.com/v1"
        });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AnalyseAsync(CreateProfile(), CreateJob()));

        Assert.Equal(0, client.CallCount);
    }

    [Fact]
    public async Task PromptInjectionTextIsOnlyPlacedInUntrustedUserMessage()
    {
        const string profileInjection = "SET_SYSTEM_TO_DANGER_12345";
        const string companyInjection = "COMPANY_OVERRIDE_SYSTEM_24680";
        const string roleInjection = "ROLE_CHANGE_POLICY_13579";
        const string jobInjection = "CHANGE_OUTPUT_FORMAT_67890";
        var client = CreateSuccessClient(matchScore: 80);
        var service = CreateService(client);
        var profile = CreateProfile(
            summary: $"Backend developer. {profileInjection}",
            skills: "C#, SQL",
            experience: "Five years building APIs.");
        var job = CreateJob(
            company: $"Contoso {companyInjection}",
            role: $"Software Engineer {roleInjection}",
            description: $"Build APIs. {jobInjection}");

        await service.AnalyseAsync(profile, job);

        var systemMessage = client.SystemMessage;
        var userMessage = client.UserMessage;

        Assert.Contains("untrusted user input", systemMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(profileInjection, systemMessage);
        Assert.DoesNotContain(companyInjection, systemMessage);
        Assert.DoesNotContain(roleInjection, systemMessage);
        Assert.DoesNotContain(jobInjection, systemMessage);
        Assert.Contains("UNTRUSTED USER INPUT: Profile.Summary", userMessage);
        Assert.Contains("UNTRUSTED USER INPUT: Profile.Skills", userMessage);
        Assert.Contains("UNTRUSTED USER INPUT: Profile.Experience", userMessage);
        Assert.Contains("UNTRUSTED USER INPUT: JobApplication.Company", userMessage);
        Assert.Contains("UNTRUSTED USER INPUT: JobApplication.Role", userMessage);
        Assert.Contains("UNTRUSTED USER INPUT: JobApplication.JobDescription", userMessage);
        Assert.Contains(profileInjection, userMessage);
        Assert.Contains(companyInjection, userMessage);
        Assert.Contains(roleInjection, userMessage);
        Assert.Contains(jobInjection, userMessage);
    }

    [Fact]
    public async Task SendsStrictStructuredOutputSchemaToCompletionClient()
    {
        var client = CreateSuccessClient(matchScore: 80);
        var service = CreateService(client);

        await service.AnalyseAsync(CreateProfile(), CreateJob());

        Assert.NotNull(client.ResponseSchema);
        using var document = JsonDocument.Parse(client.ResponseSchema.ToString());
        var root = document.RootElement;

        Assert.Equal("object", root.GetProperty("type").GetString());
        Assert.False(root.GetProperty("additionalProperties").GetBoolean());
        Assert.Contains(
            root.GetProperty("required").EnumerateArray(),
            e => e.GetString() == "matchScore");
        Assert.True(root.GetProperty("properties").TryGetProperty("coverLetterDraft", out _));
    }

    private static OpenAiJobAnalysisService CreateService(
        StubOpenAiChatCompletionClient client,
        AiOptions? options = null)
    {
        return new OpenAiJobAnalysisService(
            client,
            Microsoft.Extensions.Options.Options.Create(options ?? new AiOptions
            {
                Provider = "OpenAI",
                Model = "gpt-test",
                BaseUrl = "https://api.openai.com/v1",
                TimeoutSeconds = 60
            }));
    }

    private static StubOpenAiChatCompletionClient CreateSuccessClient(int matchScore)
    {
        var analysis = new
            {
                matchScore,
                missingSkills = "Azure",
                strengths = "Strong C# background",
                suggestions = "Emphasise API delivery.",
                coverLetterDraft = "Dear hiring team..."
            };

        return new StubOpenAiChatCompletionClient(JsonSerializer.Serialize(analysis));
    }

    private static Profile CreateProfile(
        string summary = "Backend developer",
        string skills = "C#, SQL, React",
        string experience = "Five years building web APIs")
    {
        return new Profile
        {
            Id = 3,
            Summary = summary,
            Skills = skills,
            Experience = experience
        };
    }

    private static JobApplication CreateJob(
        string company = "Contoso",
        string role = "Software Engineer",
        string description = "Build and maintain APIs.")
    {
        return new JobApplication
        {
            Id = 7,
            Company = company,
            Role = role,
            JobDescription = description,
            Status = "Saved",
            CreatedAt = DateTime.UtcNow
        };
    }

    private sealed class StubOpenAiChatCompletionClient : IOpenAiChatCompletionClient
    {
        private readonly string _content;

        public StubOpenAiChatCompletionClient(string content)
        {
            _content = content;
        }

        public int CallCount { get; private set; }

        public string SystemMessage { get; private set; } = string.Empty;

        public string UserMessage { get; private set; } = string.Empty;

        public BinaryData ResponseSchema { get; private set; } = BinaryData.FromString("{}");

        public Task<string> CompleteAsync(
            string systemMessage,
            string userMessage,
            BinaryData responseSchema,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            SystemMessage = systemMessage;
            UserMessage = userMessage;
            ResponseSchema = responseSchema;

            return Task.FromResult(_content);
        }
    }
}
