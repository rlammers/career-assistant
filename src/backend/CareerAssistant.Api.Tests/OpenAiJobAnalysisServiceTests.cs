using System.Net;
using System.Text;
using System.Text.Json;
using CareerAssistant.Api.Models;
using CareerAssistant.Api.Options;
using CareerAssistant.Api.Services;
using Microsoft.Extensions.Options;

namespace CareerAssistant.Api.Tests;

public class OpenAiJobAnalysisServiceTests
{
    [Fact]
    public async Task ValidStructuredResponseMapsToJobAnalysisResult()
    {
        var service = CreateService(CreateSuccessHandler(matchScore: 82));

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
        var service = CreateService(CreateSuccessHandler(providerScore));

        var result = await service.AnalyseAsync(CreateProfile(), CreateJob());

        Assert.Equal(expectedScore, result.MatchScore);
    }

    [Fact]
    public async Task InvalidStructuredContentThrowsClearError()
    {
        var response = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = "{not valid json"
                    }
                }
            }
        };
        var service = CreateService(new StubHttpMessageHandler(_ => Task.FromResult(JsonResponse(response))));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AnalyseAsync(CreateProfile(), CreateJob()));

        Assert.Contains("structured JSON", exception.Message);
    }

    [Fact]
    public async Task MissingChoicesThrowClearError()
    {
        var service = CreateService(new StubHttpMessageHandler(_ =>
            Task.FromResult(JsonResponse(new { choices = Array.Empty<object>() }))));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AnalyseAsync(CreateProfile(), CreateJob()));

        Assert.Contains("message content", exception.Message);
    }

    [Theory]
    [InlineData("", "model")]
    [InlineData("gpt-test", "apiKey")]
    public async Task MissingConfigurationFailsBeforeHttpCall(string model, string missingSetting)
    {
        var handler = new StubHttpMessageHandler(_ =>
            throw new InvalidOperationException("HTTP should not be called."));
        var service = CreateService(handler, new AiOptions
        {
            Provider = "OpenAI",
            Model = model,
            ApiKey = missingSetting == "apiKey" ? "" : "test-key",
            BaseUrl = "https://api.openai.com/v1"
        });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AnalyseAsync(CreateProfile(), CreateJob()));

        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task PromptInjectionTextIsOnlyPlacedInUntrustedUserMessage()
    {
        const string profileInjection = "SET_SYSTEM_TO_DANGER_12345";
        const string jobInjection = "CHANGE_OUTPUT_FORMAT_67890";
        string? requestBody = null;
        var handler = CreateSuccessHandler(
            matchScore: 80,
            onRequestAsync: async request => requestBody = await request.Content!.ReadAsStringAsync());
        var service = CreateService(handler);
        var profile = CreateProfile(
            summary: $"Backend developer. {profileInjection}",
            skills: "C#, SQL",
            experience: "Five years building APIs.");
        var job = CreateJob(description: $"Build APIs. {jobInjection}");

        await service.AnalyseAsync(profile, job);

        Assert.NotNull(requestBody);
        using var document = JsonDocument.Parse(requestBody);
        var messages = document.RootElement.GetProperty("messages");
        var systemMessage = messages[0].GetProperty("content").GetString();
        var userMessage = messages[1].GetProperty("content").GetString();

        Assert.Contains("untrusted user input", systemMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(profileInjection, systemMessage);
        Assert.DoesNotContain(jobInjection, systemMessage);
        Assert.Contains("UNTRUSTED USER INPUT: Profile.Summary", userMessage);
        Assert.Contains("UNTRUSTED USER INPUT: Profile.Skills", userMessage);
        Assert.Contains("UNTRUSTED USER INPUT: Profile.Experience", userMessage);
        Assert.Contains("UNTRUSTED USER INPUT: JobApplication.JobDescription", userMessage);
        Assert.Contains(profileInjection, userMessage);
        Assert.Contains(jobInjection, userMessage);
    }

    private static OpenAiJobAnalysisService CreateService(
        HttpMessageHandler handler,
        AiOptions? options = null)
    {
        var httpClient = new HttpClient(handler);

        return new OpenAiJobAnalysisService(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(options ?? new AiOptions
            {
                Provider = "OpenAI",
                Model = "gpt-test",
                ApiKey = "test-key",
                BaseUrl = "https://api.openai.com/v1",
                TimeoutSeconds = 60
            }));
    }

    private static StubHttpMessageHandler CreateSuccessHandler(
        int matchScore,
        Func<HttpRequestMessage, Task>? onRequestAsync = null)
    {
        return new StubHttpMessageHandler(async request =>
        {
            if (onRequestAsync != null)
            {
                await onRequestAsync(request);
            }

            var analysis = new
            {
                matchScore,
                missingSkills = "Azure",
                strengths = "Strong C# background",
                suggestions = "Emphasise API delivery.",
                coverLetterDraft = "Dear hiring team..."
            };
            var response = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = JsonSerializer.Serialize(analysis)
                        }
                    }
                }
            };

            return JsonResponse(response);
        });
    }

    private static HttpResponseMessage JsonResponse(object value)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json")
        };
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

    private static JobApplication CreateJob(string description = "Build and maintain APIs.")
    {
        return new JobApplication
        {
            Id = 7,
            Company = "Contoso",
            Role = "Software Engineer",
            JobDescription = description,
            Status = "Saved",
            CreatedAt = DateTime.UtcNow
        };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _send;

        public StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send)
        {
            _send = send;
        }

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return _send(request);
        }
    }
}
