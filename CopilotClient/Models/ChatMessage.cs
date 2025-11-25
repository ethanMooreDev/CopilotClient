using System;

namespace CopilotClient.Models;

public enum ChatRole
{
    User,
    Assistant,
    System
}

public class ChatMessage
{
    public ChatRole Role { get; }
    public string Content { get; }

    public DateTime CreatedAt { get; }

    public ChatMessage(ChatRole role, string content, DateTime createdAt)
    {
        Role = role; 
        Content = content;
        CreatedAt = createdAt;
    }

    public bool IsFromUser => Role == ChatRole.User;
}
