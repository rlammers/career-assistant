namespace CareerAssistant.Api.Services;

public interface IOpenAiChatCompletionClient
{
    Task<string> CompleteAsync(
        string systemMessage,
        string userMessage,
        BinaryData responseSchema,
        CancellationToken cancellationToken = default);
}
