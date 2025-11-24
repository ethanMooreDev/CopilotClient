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
    {
        """
        **Got it!**  
        Let me think about that for a moment… 🤔
        """,

        """
        Here's one way to break that down:
        1. Understand the problem  
        2. Identify inputs & outputs  
        3. Sketch the logic  
        4. Write a prototype
        """,

        """
        Here’s a quick example:

        ```csharp
        void DoWork()
        {
            Step1();
            Step2();
        }
        ```
        """
    };


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
