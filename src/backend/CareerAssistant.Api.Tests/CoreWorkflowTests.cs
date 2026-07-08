using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CareerAssistant.Api.Tests;

public class CoreWorkflowTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

        var analyseResponse = await client.PostAsync($"/api/jobs/{missingJobId}/analyse", content: null);

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, analyseResponse.StatusCode);
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
}
