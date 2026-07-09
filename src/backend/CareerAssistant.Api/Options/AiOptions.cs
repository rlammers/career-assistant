namespace CareerAssistant.Api.Options;

public class AiOptions
{
    public string Provider { get; set; } = "Mock";

    public string Model { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    public int TimeoutSeconds { get; set; } = 60;
}
