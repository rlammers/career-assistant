using System.ComponentModel.DataAnnotations;

namespace CareerAssistant.Api.DTOs;

public class ProfileRequest
{
    [Required]
    [StringLength(InputLimits.ProfileSummaryMaxLength)]
    public string Summary { get; set; } = string.Empty;
    [Required]
    [StringLength(InputLimits.ProfileSkillsMaxLength)]
    public string Skills { get; set; } = string.Empty;
    [Required]
    [StringLength(InputLimits.ProfileExperienceMaxLength)]
    public string Experience { get; set; } = string.Empty;
}
