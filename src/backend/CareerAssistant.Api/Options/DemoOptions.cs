namespace CareerAssistant.Api.Options;

public class DemoOptions
{
    public const string SectionName = "Demo";

    public bool Enabled { get; set; }
    public int MaxJobs { get; set; } = 100;
    public int MaxAnalyses { get; set; } = 200;
}
