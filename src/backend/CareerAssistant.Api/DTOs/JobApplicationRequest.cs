using System.ComponentModel.DataAnnotations;

namespace CareerAssistant.Api.DTOs;

public class JobApplicationRequest
{
    [Required]
    [StringLength(InputLimits.CompanyMaxLength)]
    [RegularExpression(@"(?s).*\S.*", ErrorMessage = "Company cannot contain only whitespace.")]
    public string Company { get; set; } = string.Empty;
    [Required]
    [StringLength(InputLimits.RoleMaxLength)]
    [RegularExpression(@"(?s).*\S.*", ErrorMessage = "Role cannot contain only whitespace.")]
    public string Role { get; set; } = string.Empty;
    [Required]
    [StringLength(InputLimits.JobDescriptionMaxLength)]
    [RegularExpression(@"(?s).*\S.*", ErrorMessage = "Job description cannot contain only whitespace.")]
    public string JobDescription { get; set; } = string.Empty;
}
