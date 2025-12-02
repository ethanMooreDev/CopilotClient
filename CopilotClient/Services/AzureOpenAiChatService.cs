using Azure;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using CopilotClient.Options;
using CopilotClient.Services; // for ServiceConversation
using OpenAI.Chat;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CopilotClient.Services;

public sealed class AzureOpenAiChatService : IChatService
{
    private readonly AzureOpenAIClient _client;
    private readonly AzureOpenAiOptions _options;

    public AzureOpenAiChatService(AzureOpenAiOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint))
            throw new ArgumentException("Azure OpenAI Endpoint is required.", nameof(options));

        if (string.IsNullOrWhiteSpace(options.DeploymentName))
            throw new ArgumentException("Azure OpenAI DeploymentName is required.", nameof(options));

        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new ArgumentException("Azure OpenAI ApiKey is required.", nameof(options));

        var endpoint = new Uri(options.Endpoint);
        var credential = new AzureKeyCredential(options.ApiKey);

        _client = new AzureOpenAIClient(endpoint, credential);

        _options = options;
    }

    public async Task<CopilotClient.Models.ChatMessage> SendAsync(
        ServiceConversation request,
        CancellationToken cancellationToken = default)
    {
        // 1. Build the SDK chat messages
        var chatMessages = BuildChatMessages(request);

        // 2. Call Azure OpenAI
        var chatRequestOptions = new ChatCompletionOptions();

        // You can tweak these later
        chatRequestOptions.Temperature = 0.3f;
        chatRequestOptions.MaxOutputTokenCount = 512;

        // NOTE: this call shape may vary slightly by SDK version;
        // in modern SDKs you typically pass deployment name and options.
        ChatCompletion response = 
            await _client
                .GetChatClient(_options.DeploymentName)
                .CompleteChatAsync(
                    chatMessages,
                    chatRequestOptions,
                    cancellationToken
                );

        var sb = new StringBuilder();
        foreach (var part in response.Content)
        {
            if (part.Kind == ChatMessageContentPartKind.Text && part.Text is not null)
            {
                sb.Append(part.Text);
            }
        }

        var assistantMessage = sb.ToString();

        if (assistantMessage is null || string.IsNullOrWhiteSpace(assistantMessage))
        {
            // Fall back to a failed message
            return new CopilotClient.Models.ChatMessage(
                clientId: Guid.NewGuid(),
                role: CopilotClient.Models.ChatRole.Assistant,
                content: "I didn't receive a valid response from the model.",
                createdAt: DateTime.UtcNow,
                status: CopilotClient.Models.MessageStatus.Failed
            );
        }

        return new CopilotClient.Models.ChatMessage(
            clientId: Guid.NewGuid(),
            role: CopilotClient.Models.ChatRole.Assistant,
            content: assistantMessage,
            createdAt: DateTime.UtcNow,
            status: CopilotClient.Models.MessageStatus.Sent
        );
    }

    private static System.Collections.Generic.IEnumerable<ChatMessage> BuildChatMessages(ServiceConversation request)
    {
        // System instruction first, if present
        if (!string.IsNullOrWhiteSpace(request.SystemInstruction))
        {
            yield return new SystemChatMessage(request.SystemInstruction!);
        }

        // Then the conversation messages
        foreach (var m in request.Messages.OrderBy(m => m.CreatedAt))
        {
            switch (m.Role)
            {
                case CopilotClient.Models.ChatRole.User:
                    yield return new UserChatMessage(m.Content);
                    break;

                case CopilotClient.Models.ChatRole.Assistant:
                    yield return new AssistantChatMessage(m.Content);
                    break;

                case CopilotClient.Models.ChatRole.System:
                    // If you ever persist system messages, handle them here
                    yield return new SystemChatMessage(m.Content);
                    break;

                default:
                    yield return new UserChatMessage(m.Content);
                    break;
            }
        }
    }
}
