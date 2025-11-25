using System;
using System.ComponentModel;

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
    Typing,
    Streaming,
    Sent,
    Failed
}

public class ChatMessage : INotifyPropertyChanged
{
    public Guid ClientId { get; }
    public Guid? ServerId { get; set; }
    public ChatRole Role { get; }
    
    public DateTime CreatedAt { get; }

    private string _content;

    public string Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                _content = value;
                OnPropertyChanged(nameof(Content));
            }
        }
    }

    private MessageStatus _status;
    public MessageStatus Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusString));
            }
        }
    }
    public string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage != value)
            {
                _errorMessage = value;
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }
    }
    public string StatusString
    {
        get => Status switch
        {
            MessageStatus.Sending => "Sending",
            MessageStatus.Typing => "Typing",
            MessageStatus.Streaming => "Streaming",
            MessageStatus.Sent => "Sent",
            MessageStatus.Failed => "Failed",
            _ => "Unknown Status"
        };
    }

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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
