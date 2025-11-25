using System;

namespace CopilotClient.Models;

public enum ChatRole
{
    User,
    Assistant,
    System
}

public enum MessageStatus
{
    Sending,
    Sent,
    Failed
}

public class ChatMessage
{
    public Guid ClientId { get; }
    public Guid? ServerId { get; set; }
    public ChatRole Role { get; }
    public string Content { get; }
    public DateTime CreatedAt { get; }
    public MessageStatus Status { get; set; }
    public string? ErrorMessage { get; }

    // Constructor with caller-supplied timestamp
    // Main constructor for full control (e.g., when loading history)
    public ChatMessage(
        Guid clientId,
        ChatRole role,
        string content,
        DateTime createdAt,
        MessageStatus status,
        Guid? serverId = null)
    {
        ClientId = clientId;
        Role = role;
        Content = content;
        CreatedAt = createdAt;
        Status = status;
        ServerId = serverId;
    }

    // Convenience constructor for "new outgoing user message", created now
    public static ChatMessage CreateNewUserMessage(string content) =>
        new ChatMessage(
            clientId: Guid.NewGuid(),
            role: ChatRole.User,
            content: content,
            createdAt: DateTime.UtcNow,
            status: MessageStatus.Sending,
            serverId: null
        );

}
