using System.Collections.Generic;
using System.Linq;
using CopilotClient.Models;

namespace CopilotClient.Services;

public static class PromptBuilder
{
    private const int MaxMessages = 20;

    public static ServiceConversation Build(Conversation conversation)
    {
        var instruction = ModePromptHelper.GetInstructions(conversation.Mode);

        List<ChatMessage> messages = conversation.Messages
            .OrderBy(m => m.CreatedAt)
            .TakeLast(MaxMessages)
            .ToList();

        return new ServiceConversation
        {
            Mode = conversation.Mode,
            SystemInstruction = instruction,
            Messages = messages
        };
    }
}
