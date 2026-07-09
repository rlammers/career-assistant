using CareerAssistant.Api.Data;
using CareerAssistant.Api.DTOs;
using CareerAssistant.Api.Models;
using CareerAssistant.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    public JobApplicationsController(ApplicationDbContext dbContext, IJobAnalysisService jobAnalysisService)
    {
        _dbContext = dbContext;
        _jobAnalysisService = jobAnalysisService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobApplication>>> Get()
    {
        var jobs = await _dbContext.JobApplications
            .Include(j => j.AnalysisResults)
            .ToListAsync();

        return Ok(jobs);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<JobApplication>> Get(int id)
    {
        var job = await _dbContext.JobApplications
            .Include(j => j.AnalysisResults)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null)
        {
            return NotFound();
        }

        return Ok(job);
    }

    [HttpPost]
    public async Task<ActionResult<JobApplication>> Post(JobApplicationRequest request)
    {
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

        return CreatedAtAction(nameof(Get), new { id = job.Id }, job);
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<JobApplication>> PatchStatus(int id, JobStatusUpdateRequest request)
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

        return Ok(job);
    }

    [HttpPost("{id}/analyse")]
    public async Task<ActionResult<JobAnalysisResultResponse>> Analyse(int id)
    {
        var profile = await _dbContext.Profiles.FirstOrDefaultAsync();
        var job = await _dbContext.JobApplications.FindAsync(id);

        if (job == null)
        {
            return NotFound();
        }

        if (profile == null)
        {
            return BadRequest("Profile must be created before analysis.");
        }

        JobAnalysisResult analysisResult;

        try
        {
            analysisResult = await _jobAnalysisService.AnalyseAsync(profile, job, HttpContext.RequestAborted);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                title: "Job analysis failed.",
                detail: ex.Message,
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
}
