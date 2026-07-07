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
    public async Task<ActionResult<Profile>> Get()
    {
        var profile = await _dbContext.Profiles.FirstOrDefaultAsync();

        if (profile == null)
        {
            return NotFound();
        }

        return Ok(profile);
    }

    [HttpPost]
    public async Task<ActionResult<Profile>> Post(ProfileRequest request)
    {
        var profile = await _dbContext.Profiles.FirstOrDefaultAsync();

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

        return Ok(profile);
    }
}
