namespace CareerAssistant.Api.Models;

public class JobAnalysisResult
{
    public int Id { get; set; }

    public int JobApplicationId { get; set; }

    public int MatchScore { get; set; }

    public string MissingSkills { get; set; } = string.Empty;

    public string Strengths { get; set; } = string.Empty;

    public string Suggestions { get; set; } = string.Empty;

    public string CoverLetterDraft { get; set; } = string.Empty;

    // Navigation property
    public JobApplication? JobApplication { get; set; }
}
