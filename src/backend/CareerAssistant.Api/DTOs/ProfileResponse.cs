using CareerAssistant.Api.Models;

namespace CareerAssistant.Api.DTOs;

public class ProfileResponse
{
    public int Id { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Skills { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;

    public static ProfileResponse FromEntity(Profile profile) => new()
    {
        Id = profile.Id,
        Summary = profile.Summary,
        Skills = profile.Skills,
        Experience = profile.Experience
    };
}
