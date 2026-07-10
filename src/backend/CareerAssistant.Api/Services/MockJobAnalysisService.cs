using CareerAssistant.Api.Models;

namespace CareerAssistant.Api.Services;

public class MockJobAnalysisService : IJobAnalysisService
{
    private static readonly TimeSpan SimulatedResponseDelay = TimeSpan.FromSeconds(1.5);

    public async Task<JobAnalysisResult> AnalyseAsync(
        Profile profile,
        JobApplication jobApplication,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(SimulatedResponseDelay, cancellationToken);

        var analysisResult = new JobAnalysisResult
        {
            JobApplicationId = jobApplication.Id,
            MatchScore = 75,
            MissingSkills = "Communication, Teamwork",
            Strengths = "Relevant experience and strong role fit",
            Suggestions = "Highlight leadership and project ownership in your resume.",
            CoverLetterDraft = "I am excited to apply for this role because my experience aligns with the key requirements."
        };

        return analysisResult;
    }
}
