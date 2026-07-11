using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CareerAssistant.Api.Models;
using CareerAssistant.Api.Services;
using CareerAssistant.Api.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using CareerAssistant.Api.DTOs;

namespace CareerAssistant.Api.Tests;

public class CoreWorkflowTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task HealthEndpointSupportsGetAndHead()
    {
        using var factory = new CareerAssistantApiFactory();
        using var client = factory.CreateClient();

        var getResponse = await client.GetAsync("/health");
        var headRequest = new HttpRequestMessage(HttpMethod.Head, "/health");
        var headResponse = await client.SendAsync(headRequest);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, headResponse.StatusCode);
    }

    [Fact]
    public async Task ProfileCanBeCreatedAndRetrieved()
    {
        using var factory = new CareerAssistantApiFactory();
        using var client = factory.CreateClient();
        var request = new
        {
            Summary = "Backend developer",
            Skills = "C#, SQL, React",
            Experience = "Five years building web APIs"
        };

        var createResponse = await client.PostAsJsonAsync("/api/profile", request);
        createResponse.EnsureSuccessStatusCode();

        var getResponse = await client.GetAsync("/api/profile");
        getResponse.EnsureSuccessStatusCode();
        var profile = await ReadResponseAsync<ProfileResponse>(getResponse);

        Assert.Equal(request.Summary, profile.Summary);
        Assert.Equal(request.Skills, profile.Skills);
        Assert.Equal(request.Experience, profile.Experience);
    }

    [Fact]
    public async Task JobApplicationCanBeCreated()
    {
        using var factory = new CareerAssistantApiFactory();
        using var client = factory.CreateClient();

        var job = await CreateJobAsync(client);

        Assert.True(job.Id > 0);
        Assert.Equal("Contoso", job.Company);
        Assert.Equal("Software Engineer", job.Role);
        Assert.Equal("Build and maintain APIs.", job.JobDescription);
        Assert.Equal("Saved", job.Status);
    }

    [Fact]
    public async Task JobApplicationAndItsAnalysisResultsCanBeDeleted()
    {
        using var factory = new CareerAssistantApiFactory();
        using var client = factory.CreateClient();
        await CreateProfileAsync(client);
        var job = await CreateJobAsync(client);
        var analysisResponse = await client.PostAsync($"/api/jobs/{job.Id}/analyse", content: null);
        analysisResponse.EnsureSuccessStatusCode();

        var deleteResponse = await client.DeleteAsync($"/api/jobs/{job.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/jobs/{job.Id}")).StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await dbContext.JobAnalysisResults.AnyAsync(result => result.JobApplicationId == job.Id));
    }

    [Fact]
    public async Task JobApplicationStatusCanBeUpdated()
    {
        using var factory = new CareerAssistantApiFactory();
        using var client = factory.CreateClient();
        var job = await CreateJobAsync(client);

        var updateRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/jobs/{job.Id}/status")
        {
            Content = JsonContent.Create(new { Status = "Applied" })
        };
        var updateResponse = await client.SendAsync(updateRequest);
        updateResponse.EnsureSuccessStatusCode();
        var updatedJob = await ReadResponseAsync<JobApplicationResponse>(updateResponse);

        Assert.Equal("Applied", updatedJob.Status);

        var persistedResponse = await client.GetAsync($"/api/jobs/{job.Id}");
        persistedResponse.EnsureSuccessStatusCode();
        var persistedJob = await ReadResponseAsync<JobApplicationResponse>(persistedResponse);

        Assert.Equal("Applied", persistedJob.Status);
    }

    [Fact]
    public async Task CorsPreflightAllowsEditingJobs()
    {
        using var factory = new CareerAssistantApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/jobs/1");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "PUT");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains(
            "PUT",
            string.Join(',', response.Headers.GetValues("Access-Control-Allow-Methods")));
    }

    [Fact]
    public async Task JobApplicationFieldsCanBeUpdated()
    {
        using var factory = new CareerAssistantApiFactory();
        using var client = factory.CreateClient();
        var job = await CreateJobAsync(client);

        var updateResponse = await client.PutAsJsonAsync($"/api/jobs/{job.Id}", new
        {
            Company = "Fabrikam",
            Role = "Senior Software Engineer",
            JobDescription = "Design and maintain APIs."
        });
        updateResponse.EnsureSuccessStatusCode();
        var updatedJob = await ReadResponseAsync<JobApplicationResponse>(updateResponse);

        Assert.Equal("Fabrikam", updatedJob.Company);
        Assert.Equal("Senior Software Engineer", updatedJob.Role);
        Assert.Equal("Design and maintain APIs.", updatedJob.JobDescription);
        Assert.Equal(job.Status, updatedJob.Status);
        Assert.Equal(job.CreatedAt, updatedJob.CreatedAt);

        var persistedJob = await ReadResponseAsync<JobApplicationResponse>(
            await client.GetAsync($"/api/jobs/{job.Id}"));
        Assert.Equal(updatedJob.Company, persistedJob.Company);
        Assert.Equal(updatedJob.Role, persistedJob.Role);
        Assert.Equal(updatedJob.JobDescription, persistedJob.JobDescription);
    }

    [Fact]
    public async Task InvalidJobApplicationStatusReturnsBadRequestAndDoesNotChangeStatus()
    {
        using var factory = new CareerAssistantApiFactory();
        using var client = factory.CreateClient();
        var job = await CreateJobAsync(client);

        var updateRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/jobs/{job.Id}/status")
        {
            Content = JsonContent.Create(new { Status = "NotARealStatus" })
        };

        var updateResponse = await client.SendAsync(updateRequest);

        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);

        var persistedResponse = await client.GetAsync($"/api/jobs/{job.Id}");
        persistedResponse.EnsureSuccessStatusCode();
        var persistedJob = await ReadResponseAsync<JobApplicationResponse>(persistedResponse);

        Assert.Equal("Saved", persistedJob.Status);
    }

    [Fact]
    public async Task AiAnalysisEndpointReturnsSuccessfulResponse()
    {
        using var factory = new CareerAssistantApiFactory();
        using var client = factory.CreateClient();
        await CreateProfileAsync(client);
        var job = await CreateJobAsync(client);

        var response = await client.PostAsync($"/api/jobs/{job.Id}/analyse", content: null);
        response.EnsureSuccessStatusCode();
        var analysis = await ReadResponseAsync<JobAnalysisResultResponse>(response);

        Assert.Equal(job.Id, analysis.JobApplicationId);
        Assert.InRange(analysis.MatchScore, 0, 100);
        Assert.False(string.IsNullOrWhiteSpace(analysis.MissingSkills));
        Assert.False(string.IsNullOrWhiteSpace(analysis.Strengths));
        Assert.False(string.IsNullOrWhiteSpace(analysis.Suggestions));
        Assert.False(string.IsNullOrWhiteSpace(analysis.CoverLetterDraft));
    }

    [Fact]
    public async Task AiAnalysisRequiresProfile()
    {
        using var factory = new CareerAssistantApiFactory();
        using var client = factory.CreateClient();
        var job = await CreateJobAsync(client);

        var response = await client.PostAsync($"/api/jobs/{job.Id}/analyse", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var message = await response.Content.ReadAsStringAsync();
        Assert.Contains("Profile must be created before analysis.", message);
    }

    [Fact]
    public async Task AiAnalysisResultIsPersisted()
    {
        using var factory = new CareerAssistantApiFactory();
        using var client = factory.CreateClient();
        await CreateProfileAsync(client);
        var job = await CreateJobAsync(client);

        var analysisResponse = await client.PostAsync($"/api/jobs/{job.Id}/analyse", content: null);
        analysisResponse.EnsureSuccessStatusCode();

        var jobResponse = await client.GetAsync($"/api/jobs/{job.Id}");
        jobResponse.EnsureSuccessStatusCode();
        var persistedJob = await ReadResponseAsync<JobApplicationResponse>(jobResponse);
        var analysis = Assert.Single(persistedJob.AnalysisResults);

        Assert.Equal(job.Id, analysis.JobApplicationId);
        Assert.Equal(75, analysis.MatchScore);
        Assert.Equal("Communication, Teamwork", analysis.MissingSkills);
        Assert.Equal("Relevant experience and strong role fit", analysis.Strengths);
        Assert.Equal("Highlight leadership and project ownership in your resume.", analysis.Suggestions);
        Assert.Equal(
            "I am excited to apply for this role because my experience aligns with the key requirements.",
            analysis.CoverLetterDraft);
    }

    [Fact]
    public async Task FailedAiAnalysisDoesNotPersistAnalysisResult()
    {
        using var factory = new CareerAssistantApiFactory(
            jobAnalysisService: new FailingJobAnalysisService());
        using var client = factory.CreateClient();
        await CreateProfileAsync(client);
        var job = await CreateJobAsync(client);

        var analysisResponse = await client.PostAsync($"/api/jobs/{job.Id}/analyse", content: null);

        Assert.Equal(HttpStatusCode.InternalServerError, analysisResponse.StatusCode);
        var errorContent = await analysisResponse.Content.ReadAsStringAsync();
        Assert.Contains("The job analysis could not be generated. Please try again.", errorContent);
        Assert.DoesNotContain("Analysis provider returned malformed content.", errorContent);

        var jobResponse = await client.GetAsync($"/api/jobs/{job.Id}");
        jobResponse.EnsureSuccessStatusCode();
        var persistedJob = await ReadResponseAsync<JobApplicationResponse>(jobResponse);

        Assert.Empty(persistedJob.AnalysisResults);
    }

    [Fact]
    public async Task InvalidJobIdsReturnNotFound()
    {
        using var factory = new CareerAssistantApiFactory();
        using var client = factory.CreateClient();
        const int missingJobId = 999;

        var getResponse = await client.GetAsync($"/api/jobs/{missingJobId}");

        var updateRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/jobs/{missingJobId}/status")
        {
            Content = JsonContent.Create(new { Status = "Applied" })
        };
        var updateResponse = await client.SendAsync(updateRequest);

        var editResponse = await client.PutAsJsonAsync($"/api/jobs/{missingJobId}", new
        {
            Company = "Missing",
            Role = "Missing",
            JobDescription = "Missing"
        });

        var analyseResponse = await client.PostAsync($"/api/jobs/{missingJobId}/analyse", content: null);
        var deleteResponse = await client.DeleteAsync($"/api/jobs/{missingJobId}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, editResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, analyseResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }

    [Theory]
    [InlineData("Summary", InputLimits.ProfileSummaryMaxLength)]
    [InlineData("Skills", InputLimits.ProfileSkillsMaxLength)]
    [InlineData("Experience", InputLimits.ProfileExperienceMaxLength)]
    public async Task OversizedProfileFieldsReturnValidationErrors(string field, int maximumLength)
    {
        using var factory = new CareerAssistantApiFactory();
        using var client = factory.CreateClient();
        var request = new Dictionary<string, string>
        {
            ["Summary"] = "Summary",
            ["Skills"] = "C#",
            ["Experience"] = "Experience",
            [field] = new string('x', maximumLength + 1)
        };

        var response = await client.PostAsJsonAsync("/api/profile", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("Company", InputLimits.CompanyMaxLength)]
    [InlineData("Role", InputLimits.RoleMaxLength)]
    [InlineData("JobDescription", InputLimits.JobDescriptionMaxLength)]
    public async Task OversizedJobFieldsReturnValidationErrors(string field, int maximumLength)
    {
        using var factory = new CareerAssistantApiFactory();
        using var client = factory.CreateClient();
        var request = new Dictionary<string, string>
        {
            ["Company"] = "Contoso",
            ["Role"] = "Developer",
            ["JobDescription"] = "Description",
            [field] = new string('x', maximumLength + 1)
        };

        var response = await client.PostAsJsonAsync("/api/jobs", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("Company")]
    [InlineData("Role")]
    [InlineData("JobDescription")]
    public async Task RequiredJobFieldsRejectWhitespace(string field)
    {
        using var factory = new CareerAssistantApiFactory();
        using var client = factory.CreateClient();
        var request = new Dictionary<string, string>
        {
            ["Company"] = "Contoso",
            ["Role"] = "Developer",
            ["JobDescription"] = "Description",
            [field] = "   "
        };

        var response = await client.PostAsJsonAsync("/api/jobs", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DemoJobQuotaReturnsConflictWithoutStoringAnotherJob()
    {
        using var factory = new CareerAssistantApiFactory(new Dictionary<string, string?>
        {
            ["Demo:Enabled"] = "true",
            ["Demo:MaxJobs"] = "1"
        });
        using var client = factory.CreateClient();
        await CreateJobAsync(client);

        var response = await client.PostAsJsonAsync("/api/jobs", new
        {
            Company = "Fabrikam",
            Role = "Developer",
            JobDescription = "This job exceeds the configured demo quota."
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().JobApplications.CountAsync());
    }

    [Fact]
    public async Task DemoAnalysisQuotaReturnsConflictBeforeCallingProviderAgain()
    {
        var provider = new CountingJobAnalysisService();
        using var factory = new CareerAssistantApiFactory(
            new Dictionary<string, string?>
            {
                ["Demo:Enabled"] = "true",
                ["Demo:MaxAnalyses"] = "1"
            },
            provider);
        using var client = factory.CreateClient();
        await CreateProfileAsync(client);
        var job = await CreateJobAsync(client);
        (await client.PostAsync($"/api/jobs/{job.Id}/analyse", content: null)).EnsureSuccessStatusCode();

        var response = await client.PostAsync($"/api/jobs/{job.Id}/analyse", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(1, provider.CallCount);
    }

    private static async Task CreateProfileAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/profile", new
        {
            Summary = "Backend developer",
            Skills = "C#, SQL, React",
            Experience = "Five years building web APIs"
        });

        response.EnsureSuccessStatusCode();
    }

    private static async Task<JobApplicationResponse> CreateJobAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/jobs", new
        {
            Company = "Contoso",
            Role = "Software Engineer",
            JobDescription = "Build and maintain APIs."
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadResponseAsync<JobApplicationResponse>(response);
    }

    private static async Task<T> ReadResponseAsync<T>(HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions);

        Assert.NotNull(value);

        return value;
    }

    private sealed record ProfileResponse(
        int Id,
        string Summary,
        string Skills,
        string Experience);

    private sealed record JobApplicationResponse(
        int Id,
        string Company,
        string Role,
        string JobDescription,
        string Status,
        DateTime CreatedAt,
        IReadOnlyList<PersistedJobAnalysisResultResponse> AnalysisResults);

    private sealed record JobAnalysisResultResponse(
        int JobApplicationId,
        int MatchScore,
        string MissingSkills,
        string Strengths,
        string Suggestions,
        string CoverLetterDraft);

    private sealed record PersistedJobAnalysisResultResponse(
        int Id,
        int JobApplicationId,
        int MatchScore,
        string MissingSkills,
        string Strengths,
        string Suggestions,
        string CoverLetterDraft);

    private sealed class FailingJobAnalysisService : IJobAnalysisService
    {
        public Task<JobAnalysisResult> AnalyseAsync(
            Profile profile,
            JobApplication jobApplication,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Analysis provider returned malformed content.");
        }
    }

    private sealed class CountingJobAnalysisService : IJobAnalysisService
    {
        public int CallCount { get; private set; }

        public Task<JobAnalysisResult> AnalyseAsync(
            Profile profile,
            JobApplication jobApplication,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new JobAnalysisResult
            {
                MatchScore = 75,
                MissingSkills = "None",
                Strengths = "Test strength",
                Suggestions = "Test suggestion",
                CoverLetterDraft = "Test draft"
            });
        }
    }
}
