namespace CareerAssistant.Api.Models;

public class JobApplication
{
    public int Id { get; set; }

    public string Company { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string JobDescription { get; set; } = string.Empty;

    public string Status { get; set; } = "Saved";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ICollection<JobAnalysisResult> AnalysisResults { get; set; } = new List<JobAnalysisResult>();
}