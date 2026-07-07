namespace CareerAssistant.Api.DTOs;

public class JobApplicationRequest
{
    public string Company { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string JobDescription { get; set; } = string.Empty;
}
