using System.Collections.Generic;
using CopilotClient.Models;

namespace CopilotClient.Services;

public sealed class ServiceConversation
{
    public ConversationMode Mode { get; init; }
    public IReadOnlyList<ChatMessage> Messages { get; init; } = new List<ChatMessage>();
}
