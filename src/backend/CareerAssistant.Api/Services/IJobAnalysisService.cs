using CareerAssistant.Api.Models;

namespace CareerAssistant.Api.Services;

public interface IJobAnalysisService
{
    Task<JobAnalysisResult> AnalyseAsync(
        Profile profile,
        JobApplication jobApplication,
        CancellationToken cancellationToken = default);
}
