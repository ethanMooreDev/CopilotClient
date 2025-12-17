using Azure;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using CopilotClient.Options;
using CopilotClient.Services; // for ServiceConversation
using OpenAI.Chat;
using Polly;
using Polly.Retry;
using System;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CopilotClient.Services;

public sealed class AzureOpenAiChatService : IChatService
{
    private readonly AzureOpenAIClient _client;
    private readonly ChatClient _chatClient;

    private readonly AzureOpenAiOptions _options;

    private readonly AsyncRetryPolicy _retryPolicy;

    public AzureOpenAiChatService(AzureOpenAiOptions options, AiRetryPolicy aiRetryPolicy)
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
        _chatClient = _client.GetChatClient(options.DeploymentName);

        _options = options;
        _retryPolicy = aiRetryPolicy.RetryPolicy;
    }

    public async Task<CopilotClient.Models.ChatMessage> SendAsync(ServiceConversation request, CancellationToken cancellationToken = default)
    {
        try
        {
            var chatMessages = BuildChatMessages(request);

            var chatRequestOptions = new ChatCompletionOptions
            {
                Temperature = request.Mode == CopilotClient.Models.ConversationMode.Explain ? _options.ExplainTemperature : _options.Temperature,
                MaxOutputTokenCount = request.Mode == CopilotClient.Models.ConversationMode.Explain ? _options.ExplainMaxOutputTokens : _options.MaxOutputTokens,

            };

            ChatCompletion response = await _retryPolicy.ExecuteAsync(
                async ct => await _chatClient.CompleteChatAsync(
                    chatMessages,
                    chatRequestOptions,
                    ct),
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

            if (string.IsNullOrWhiteSpace(assistantMessage))
            {
                return BuildFailedMessage("I didn't receive a valid response from the model.");
            }

            return new CopilotClient.Models.ChatMessage(
                clientId: Guid.NewGuid(),
                role: CopilotClient.Models.ChatRole.Assistant,
                content: assistantMessage,
                createdAt: DateTime.UtcNow,
                status: CopilotClient.Models.MessageStatus.Sent
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return BuildFailedMessage(
            "An error occurred while calling Azure OpenAI.",
            ex.Message);
        }
    }

    public async IAsyncEnumerable<string> StreamAsync(
    ServiceConversation request,
    [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatMessages = BuildChatMessages(request);
        #pragma warning disable AOAI001

        ChatCompletionOptions chatRequestOptions = ModelReaderWriter.Read<ChatCompletionOptions>(BinaryData.FromString("{}")!)!;

        //var chatRequestOptions = new ChatCompletionOptions()
        //{
        //    Temperature = _options.Temperature,
        //    MaxOutputTokenCount = _options.MaxOutputTokens
        //};

        //chatRequestOptions.Temperature = _options.Temperature;
        chatRequestOptions.MaxOutputTokenCount = _options.MaxOutputTokens;
        
        chatRequestOptions.SetNewMaxCompletionTokensPropertyEnabled(true);


        
        #pragma warning restore AOAI001

        // This returns an async stream of updates from the model
        var streamingResult = _chatClient.CompleteChatStreamingAsync(
            chatMessages,
            chatRequestOptions,
            cancellationToken);

        await foreach (var update in streamingResult.WithCancellation(cancellationToken))
        {
            // Each update can have multiple content parts
            foreach (var part in update.ContentUpdate)
            {
                if (part.Kind == ChatMessageContentPartKind.Text && part.Text is not null)
                {
                    yield return part.Text;
                }
            }
        }
    }

    public async Task<string> SummarizeAsync(
    IEnumerable<CopilotClient.Models.ChatMessage> messages,
    CancellationToken cancellationToken = default)
    {
        // Build a system prompt specifically for summarization
        var system = new SystemChatMessage(
            "You are summarizing an earlier part of a programming-focused conversation. " +
            "Write a concise summary that captures key questions, answers, and decisions, " +
            "in 4–8 bullet points. Do not invent details.");

        var historyMessages = messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => m.Role switch
            {
                CopilotClient.Models.ChatRole.User =>
                    (ChatMessage)new UserChatMessage(m.Content),
                CopilotClient.Models.ChatRole.Assistant =>
                    new AssistantChatMessage(m.Content),
                _ =>
                    new UserChatMessage(m.Content)
            })
            .ToList();

        var all = new List<ChatMessage> { system };
        all.AddRange(historyMessages);

        var options = new ChatCompletionOptions
        {
            Temperature = 0.2f,
            MaxOutputTokenCount = 256
        };

        ChatCompletion response = await _retryPolicy.ExecuteAsync(
            async ct => await _chatClient.CompleteChatAsync(all, options, ct),
            cancellationToken
        );

        var sb = new System.Text.StringBuilder();
        foreach (var part in response.Content)
        {
            if (part.Kind == ChatMessageContentPartKind.Text && part.Text is not null)
            {
                sb.Append(part.Text);
            }
        }

        return sb.ToString().Trim();
    }

    private static CopilotClient.Models.ChatMessage BuildFailedMessage(string message, string? details = null)
    {
        var full = details is null ? message : $"{message}\n\nDetails: {details}";

        var msg = new CopilotClient.Models.ChatMessage(
            clientId: Guid.NewGuid(),
            role: CopilotClient.Models.ChatRole.Assistant,
            content: full,
            createdAt: DateTime.UtcNow,
            status: CopilotClient.Models.MessageStatus.Failed
        );

        msg.ErrorMessage = full;
        return msg;
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
                    yield return new SystemChatMessage(m.Content);
                    break;

                default:
                    yield return new UserChatMessage(m.Content);
                    break;
            }
        }
    }
}
