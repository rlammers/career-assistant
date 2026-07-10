namespace CareerAssistant.Api.Options;

public class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    public bool Enabled { get; set; }

    public string TenantId { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;
}
