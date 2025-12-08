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
        var lastUser = messages.LastOrDefault(m => m.Role == ChatRole.User);
        var userContent = lastUser?.Content ?? "(no user message)";

        await Task.Delay(500, cancellationToken);

        var instruction = request.SystemInstruction ?? "(no instruction)";

        string content = request.Mode switch
        {
            ConversationMode.Explain =>
                $"[Explain Mode]\n{instruction}\n\nUser said:\n{userContent}",

            ConversationMode.Refactor =>
                $"[Refactor Mode]\n{instruction}\n\nOriginal code:\n{userContent}",

            ConversationMode.BugHunt =>
                $"[BugHunt Mode]\n{instruction}\n\nAnalyzing for issues in:\n{userContent}",

            ConversationMode.Optimize =>
                $"[Optimize Mode]\n{instruction}\n\nLooking for optimizations in:\n{userContent}",

            _ =>
                $"[General Mode]\n{instruction}\n\n{userContent}"
        };

        return new ChatMessage(
            clientId: Guid.NewGuid(),
            role: ChatRole.Assistant,
            content: content,
            createdAt: DateTime.UtcNow,
            status: MessageStatus.Sent
        );
    }

    public async IAsyncEnumerable<string> StreamAsync(ServiceConversation request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = request.Messages.ToList();
        var lastUser = messages.LastOrDefault(m => m.Role == ChatRole.User);
        var userContent = lastUser?.Content ?? "(no user message)";

        var instruction = request.SystemInstruction ?? "(no instruction)";

        // Simple mock: just yield the canned reply in one chunk
        var reply = request.Mode switch
        {
            ConversationMode.Explain =>
                $"[Explain Mode]\n{instruction}\n\nUser said:\n{userContent}",

            ConversationMode.Refactor =>
                $"[Refactor Mode]\n{instruction}\n\nOriginal code:\n{userContent}",

            ConversationMode.BugHunt =>
                $"[BugHunt Mode]\n{instruction}\n\nAnalyzing for issues in:\n{userContent}",

            ConversationMode.Optimize =>
                $"[Optimize Mode]\n{instruction}\n\nLooking for optimizations in:\n{userContent}",

            _ =>
                $"[General Mode]\n{instruction}\n\n{userContent}"
        };
        yield return reply;
        await Task.CompletedTask;
    }
}
