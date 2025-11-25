using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CopilotClient.Models;

public class Conversation
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; set; } = "New conversation";
    public List<ChatMessage> Messages { get; set; } = new();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}
