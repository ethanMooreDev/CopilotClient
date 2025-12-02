using CopilotClient.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CopilotClient.Services;

public class MockChatService : IChatService
{
    public async Task<ChatMessage> SendAsync(ServiceConversation request, CancellationToken cancellationToken = default)
    {
        var messages = request.Messages.ToList();

        // Last user message
        var lastUser = messages.LastOrDefault(m => m.Role == ChatRole.User);
        var userContent = lastUser?.Content ?? "(no user message)";

        await Task.Delay(500, cancellationToken); // simulate latency

        string content = request.Mode switch
        {
            ConversationMode.Explain =>
                "Mode: Explain\n\n" +
                "Here's an explanation of your request/code:\n" +
                userContent,

            ConversationMode.Refactor =>
                "Mode: Refactor\n\n" +
                "Here's how I might refactor this (conceptually):\n" +
                userContent,

            ConversationMode.BugHunt =>
                "Mode: BugHunt\n\n" +
                "Potential issues / edge cases:\n" +
                "- (placeholder issue #1)\n" +
                "- (placeholder issue #2)\n\nOriginal:\n" +
                userContent,

            ConversationMode.Optimize =>
                "Mode: Optimize\n\n" +
                "Optimization ideas:\n" +
                "- (placeholder optimization #1)\n\nOriginal:\n" +
                userContent,

            _ =>
                "Mode: General\n\n" +
                userContent
        };

        return new ChatMessage(
            clientId: Guid.NewGuid(),
            role: ChatRole.Assistant,
            content: content,
            createdAt: DateTime.UtcNow,
            status: MessageStatus.Sent
        );
    }
}
