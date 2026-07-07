namespace CareerAssistant.Api.Models;

public class Profile
{
    public int Id { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string Skills { get; set; } = string.Empty;

    public string Experience { get; set; } = string.Empty;
}
