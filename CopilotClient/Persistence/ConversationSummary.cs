using System;
using CopilotClient.Models;

namespace CopilotClient.Persistence;

public sealed class ConversationSummary
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public ConversationMode Mode { get; init; }
    public DateTime LastUpdatedAt { get; init; }
}