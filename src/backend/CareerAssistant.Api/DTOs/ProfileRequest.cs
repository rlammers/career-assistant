using System.ComponentModel.DataAnnotations;

namespace CareerAssistant.Api.DTOs;

public class ProfileRequest
{
    [Required(AllowEmptyStrings = true)]
    [StringLength(InputLimits.ProfileSummaryMaxLength)]
    public string Summary { get; set; } = string.Empty;
    [Required(AllowEmptyStrings = true)]
    [StringLength(InputLimits.ProfileSkillsMaxLength)]
    public string Skills { get; set; } = string.Empty;
    [Required(AllowEmptyStrings = true)]
    [StringLength(InputLimits.ProfileExperienceMaxLength)]
    public string Experience { get; set; } = string.Empty;
}
