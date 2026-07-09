using CareerAssistant.Api.Models;

namespace CareerAssistant.Api.Services;

public class MockJobAnalysisService : IJobAnalysisService
{
    public Task<JobAnalysisResult> AnalyseAsync(
        Profile profile,
        JobApplication jobApplication,
        CancellationToken cancellationToken = default)
    {
        var analysisResult = new JobAnalysisResult
        {
            JobApplicationId = jobApplication.Id,
            MatchScore = 75,
            MissingSkills = "Communication, Teamwork",
            Strengths = "Relevant experience and strong role fit",
            Suggestions = "Highlight leadership and project ownership in your resume.",
            CoverLetterDraft = "I am excited to apply for this role because my experience aligns with the key requirements."
        };

        return Task.FromResult(analysisResult);
    }
}
