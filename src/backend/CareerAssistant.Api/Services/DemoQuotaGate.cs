namespace CareerAssistant.Api.Services;

public sealed class DemoQuotaGate
{
    public SemaphoreSlim JobWrites { get; } = new(1, 1);

    public SemaphoreSlim AnalysisWrites { get; } = new(1, 1);
}
