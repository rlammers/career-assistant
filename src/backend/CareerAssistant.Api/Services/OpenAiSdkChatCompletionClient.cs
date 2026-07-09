using OpenAI;
using OpenAI.Chat;

namespace CareerAssistant.Api.Services;

public class OpenAiSdkChatCompletionClient : IOpenAiChatCompletionClient
{
    private const string ResponseSchemaName = "job_analysis_result";

    private readonly OpenAIClient _client;
    private readonly string _model;

    public OpenAiSdkChatCompletionClient(OpenAIClient client, string model)
    {
        _client = client;
        _model = model;
    }

    public async Task<string> CompleteAsync(
        string systemMessage,
        string userMessage,
        BinaryData responseSchema,
        CancellationToken cancellationToken = default)
    {
        var chatClient = _client.GetChatClient(_model);
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemMessage),
            new UserChatMessage(userMessage)
        };
        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: ResponseSchemaName,
                jsonSchema: responseSchema,
                jsonSchemaIsStrict: true)
        };

        var completion = await chatClient.CompleteChatAsync(messages, options, cancellationToken);
        var content = completion.Value.Content.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("OpenAI analysis response did not include a message content payload.");
        }

        return content;
    }
}
