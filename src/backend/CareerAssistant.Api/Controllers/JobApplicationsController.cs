using CareerAssistant.Api.Data;
using CareerAssistant.Api.DTOs;
using CareerAssistant.Api.Models;
using CareerAssistant.Api.Options;
using CareerAssistant.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CareerAssistant.Api.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobApplicationsController : ControllerBase
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.Ordinal)
    {
        "Saved",
        "Applied",
        "Interview",
        "Offer",
        "Rejected"
    };

    private readonly ApplicationDbContext _dbContext;
    private readonly IJobAnalysisService _jobAnalysisService;
    private readonly DemoOptions _demoOptions;
    private readonly DemoQuotaGate _demoQuotaGate;
    private readonly ILogger<JobApplicationsController> _logger;

    public JobApplicationsController(
        ApplicationDbContext dbContext,
        IJobAnalysisService jobAnalysisService,
        IOptions<DemoOptions> demoOptions,
        DemoQuotaGate demoQuotaGate,
        ILogger<JobApplicationsController> logger)
    {
        _dbContext = dbContext;
        _jobAnalysisService = jobAnalysisService;
        _demoOptions = demoOptions.Value;
        _demoQuotaGate = demoQuotaGate;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobApplicationResponse>>> Get()
    {
        var jobs = await _dbContext.JobApplications
            .Include(j => j.AnalysisResults)
            .ToListAsync();

        return Ok(jobs.Select(JobApplicationResponse.FromEntity));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<JobApplicationResponse>> Get(int id)
    {
        var job = await _dbContext.JobApplications
            .Include(j => j.AnalysisResults)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null)
        {
            return NotFound();
        }

        return Ok(JobApplicationResponse.FromEntity(job));
    }

    [HttpPost]
    public async Task<ActionResult<JobApplicationResponse>> Post(JobApplicationRequest request)
    {
        if (!_demoOptions.Enabled)
        {
            return await CreateJobAsync(request);
        }

        await _demoQuotaGate.JobWrites.WaitAsync(HttpContext.RequestAborted);

        try
        {
            return await CreateJobAsync(request);
        }
        finally
        {
            _demoQuotaGate.JobWrites.Release();
        }
    }

    private async Task<ActionResult<JobApplicationResponse>> CreateJobAsync(JobApplicationRequest request)
    {
        if (_demoOptions.Enabled
            && await _dbContext.JobApplications.CountAsync() >= _demoOptions.MaxJobs)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Demo job limit reached.",
                Detail = $"The public demo stores at most {_demoOptions.MaxJobs} jobs. Delete a job or wait for the next demo reset.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var job = new JobApplication
        {
            Company = request.Company,
            Role = request.Role,
            JobDescription = request.JobDescription,
            Status = "Saved",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.JobApplications.Add(job);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = job.Id }, JobApplicationResponse.FromEntity(job));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<JobApplicationResponse>> Put(int id, JobApplicationRequest request)
    {
        var job = await _dbContext.JobApplications
            .Include(j => j.AnalysisResults)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null)
        {
            return NotFound();
        }

        job.Company = request.Company;
        job.Role = request.Role;
        job.JobDescription = request.JobDescription;
        await _dbContext.SaveChangesAsync();

        return Ok(JobApplicationResponse.FromEntity(job));
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<JobApplicationResponse>> PatchStatus(int id, JobStatusUpdateRequest request)
    {
        if (!AllowedStatuses.Contains(request.Status))
        {
            return BadRequest("Status must be one of: Saved, Applied, Interview, Offer, Rejected.");
        }

        var job = await _dbContext.JobApplications.FindAsync(id);

        if (job == null)
        {
            return NotFound();
        }

        job.Status = request.Status;
        await _dbContext.SaveChangesAsync();

        return Ok(JobApplicationResponse.FromEntity(job));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var job = await _dbContext.JobApplications.FindAsync(id);

        if (job == null)
        {
            return NotFound();
        }

        _dbContext.JobApplications.Remove(job);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/analyse")]
    public async Task<ActionResult<JobAnalysisResultResponse>> Analyse(int id)
    {
        var profile = await _dbContext.Profiles
            .OrderBy(p => p.Id)
            .FirstOrDefaultAsync();
        var job = await _dbContext.JobApplications.FindAsync(id);

        if (job == null)
        {
            return NotFound();
        }

        if (profile == null || !HasRequiredProfileFields(profile))
        {
            return BadRequest("Profile must be created before analysis.");
        }

        if (_demoOptions.Enabled)
        {
            await _demoQuotaGate.AnalysisWrites.WaitAsync(HttpContext.RequestAborted);
        }

        try
        {
            return await CreateAnalysisAsync(profile, job);
        }
        finally
        {
            if (_demoOptions.Enabled)
            {
                _demoQuotaGate.AnalysisWrites.Release();
            }
        }
    }

    private async Task<ActionResult<JobAnalysisResultResponse>> CreateAnalysisAsync(
        Profile profile,
        JobApplication job)
    {

        if (_demoOptions.Enabled
            && await _dbContext.JobAnalysisResults.CountAsync() >= _demoOptions.MaxAnalyses)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Demo analysis limit reached.",
                Detail = $"The public demo stores at most {_demoOptions.MaxAnalyses} analyses. Wait for the next demo reset.",
                Status = StatusCodes.Status409Conflict
            });
        }

        JobAnalysisResult analysisResult;

        try
        {
            analysisResult = await _jobAnalysisService.AnalyseAsync(profile, job, HttpContext.RequestAborted);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogError(exception, "Job analysis request failed.");

            return Problem(
                title: "Job analysis failed.",
                detail: "The job analysis could not be generated. Please try again.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        analysisResult.JobApplicationId = job.Id;
        analysisResult.MatchScore = Math.Clamp(analysisResult.MatchScore, 0, 100);
        analysisResult.MissingSkills ??= string.Empty;
        analysisResult.Strengths ??= string.Empty;
        analysisResult.Suggestions ??= string.Empty;
        analysisResult.CoverLetterDraft ??= string.Empty;

        _dbContext.JobAnalysisResults.Add(analysisResult);
        await _dbContext.SaveChangesAsync();

        var response = new JobAnalysisResultResponse
        {
            JobApplicationId = analysisResult.JobApplicationId,
            MatchScore = analysisResult.MatchScore,
            MissingSkills = analysisResult.MissingSkills,
            Strengths = analysisResult.Strengths,
            Suggestions = analysisResult.Suggestions,
            CoverLetterDraft = analysisResult.CoverLetterDraft
        };

        return Ok(response);
    }

    private static bool HasRequiredProfileFields(Profile profile) =>
        !string.IsNullOrWhiteSpace(profile.Summary)
        && !string.IsNullOrWhiteSpace(profile.Skills)
        && !string.IsNullOrWhiteSpace(profile.Experience);
}
