using CareerAssistant.Api.Data;
using CareerAssistant.Api.DTOs;
using CareerAssistant.Api.Models;
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

    public JobApplicationsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
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

        var analysisResult = new JobAnalysisResult
        {
            JobApplicationId = job.Id,
            MatchScore = 75,
            MissingSkills = "Communication, Teamwork",
            Strengths = "Relevant experience and strong role fit",
            Suggestions = "Highlight leadership and project ownership in your resume.",
            CoverLetterDraft = "I am excited to apply for this role because my experience aligns with the key requirements."
        };

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
