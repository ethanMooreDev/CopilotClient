using CopilotClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CopilotClient.Services;

public static class PromptBuilder
{
    private const int MaxMessages = 20;

    private const int MaxPromptTokensApprox = 6000;
    private const int SystemOverheadTokensApprox = 500;

    public static ServiceConversation Build(Conversation conversation)
    {
        var instruction = ModePromptHelper.GetInstructions(conversation.Mode);

        var recentMessages = BuildTruncatedHistory(conversation);

        //List<ChatMessage> messages = conversation.Messages
        //    .OrderBy(m => m.CreatedAt)
        //    .TakeLast(MaxMessages)
        //    .ToList();

        var messagesForModel = new List<ChatMessage>();

        // If we have a summary, inject it at the top
        if (!string.IsNullOrWhiteSpace(conversation.Summary))
        {
            messagesForModel.Add(new ChatMessage(
                clientId: Guid.NewGuid(),
                role: ChatRole.System,
                content: $"Summary of earlier conversation (for context only, do not repeat verbatim): {conversation.Summary}",
                createdAt: conversation.SummaryUpdatedAt ?? conversation.CreatedAt,
                status: MessageStatus.Sent
            ));
        }

        messagesForModel.AddRange(recentMessages);

        return new ServiceConversation
        {
            Mode = conversation.Mode,
            SystemInstruction = instruction,
            Messages = messagesForModel
        };
    }

    private static IReadOnlyList<ChatMessage> BuildTruncatedHistory(Conversation conversation)
    {
        var result = new List<ChatMessage>();
        int usedTokens = SystemOverheadTokensApprox;

        // Walk backwards from newest to oldest
        foreach (var msg in conversation.Messages.AsEnumerable().Reverse())
        {
            // Ignore pure Typing/Streaming placeholders if any ever slip in
            if (msg.Status is MessageStatus.Typing or MessageStatus.Streaming)
                continue;

            int msgTokens = EstimateTokens(msg.Content);

            if (usedTokens + msgTokens > MaxPromptTokensApprox)
            {
                // Stop: this message would push us over the budget
                break;
            }

            usedTokens += msgTokens;
            result.Add(msg);
        }

        // Reverse to restore chronological order (oldest of the window first)
        result.Reverse();
        return result;
    }

    private static int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        // Very rough: ~4 chars per token on average
        return text.Length / 4 + 1;
    }
}
