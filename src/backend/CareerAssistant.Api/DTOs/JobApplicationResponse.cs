using CareerAssistant.Api.Models;

namespace CareerAssistant.Api.DTOs;

public class JobApplicationResponse
{
    public int Id { get; set; }
    public string Company { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string JobDescription { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public IReadOnlyCollection<JobAnalysisResponse> AnalysisResults { get; set; } = [];

    public static JobApplicationResponse FromEntity(JobApplication job) => new()
    {
        Id = job.Id,
        Company = job.Company,
        Role = job.Role,
        JobDescription = job.JobDescription,
        Status = job.Status,
        CreatedAt = job.CreatedAt,
        AnalysisResults = job.AnalysisResults.Select(JobAnalysisResponse.FromEntity).ToArray()
    };
}

public class JobAnalysisResponse
{
    public int Id { get; set; }
    public int JobApplicationId { get; set; }
    public int MatchScore { get; set; }
    public string MissingSkills { get; set; } = string.Empty;
    public string Strengths { get; set; } = string.Empty;
    public string Suggestions { get; set; } = string.Empty;
    public string CoverLetterDraft { get; set; } = string.Empty;

    public static JobAnalysisResponse FromEntity(JobAnalysisResult analysis) => new()
    {
        Id = analysis.Id,
        JobApplicationId = analysis.JobApplicationId,
        MatchScore = analysis.MatchScore,
        MissingSkills = analysis.MissingSkills,
        Strengths = analysis.Strengths,
        Suggestions = analysis.Suggestions,
        CoverLetterDraft = analysis.CoverLetterDraft
    };
}
