using CareerAssistant.Api.Data;
using CareerAssistant.Api.DTOs;
using CareerAssistant.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareerAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public ProfileController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<ProfileResponse>> Get()
    {
        var profile = await _dbContext.Profiles
            .OrderBy(p => p.Id)
            .FirstOrDefaultAsync();

        if (profile == null || !HasRequiredProfileFields(profile))
        {
            return NotFound();
        }

        return Ok(ProfileResponse.FromEntity(profile));
    }

    private static bool HasRequiredProfileFields(Profile profile) =>
        !string.IsNullOrWhiteSpace(profile.Summary)
        && !string.IsNullOrWhiteSpace(profile.Skills)
        && !string.IsNullOrWhiteSpace(profile.Experience);

    [HttpPost]
    public async Task<ActionResult<ProfileResponse>> Post(ProfileRequest request)
    {
        var profile = await _dbContext.Profiles
            .OrderBy(p => p.Id)
            .FirstOrDefaultAsync();

        if (profile == null)
        {
            profile = new Profile
            {
                Summary = request.Summary,
                Skills = request.Skills,
                Experience = request.Experience
            };

            _dbContext.Profiles.Add(profile);
        }
        else
        {
            profile.Summary = request.Summary;
            profile.Skills = request.Skills;
            profile.Experience = request.Experience;
        }

        await _dbContext.SaveChangesAsync();

        return Ok(ProfileResponse.FromEntity(profile));
    }
}
