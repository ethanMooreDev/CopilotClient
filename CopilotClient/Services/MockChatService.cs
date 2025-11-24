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
    private static readonly string[] Responses =
    [
        "Got it! Let me think about that for a second...",
        "Interesting question. How would you approach it?",
        "If I were implementing this, I’d start by breaking it into smaller steps.",
        "That sounds like a real-world scenario—tell me more about the constraints.",
        "I hear you. Let’s try to narrow this down."
    ];

    private readonly Random _random = new();

    public async Task<ChatMessage> SendAsync(IEnumerable<ChatMessage> conversation, CancellationToken cancellationToken = default)
    {
        await Task.Delay(TimeSpan.FromSeconds(1.2), cancellationToken);

        var lastUser = conversation.LastOrDefault(m => m.Role == ChatRole.User);
        var baseResponse = Responses[_random.Next(Responses.Length)];

        var content = lastUser is null
            ? baseResponse
            : $"{baseResponse}\n\nYou Said: \"{lastUser.Content}\"";

        return new ChatMessage(ChatRole.Assistant, content);
    }
}
