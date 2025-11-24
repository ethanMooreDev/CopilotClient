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

    public ChatMessage(ChatRole role, string content)
    {
        Role = role; 
        Content = content;
    }

    public bool IsFromUser => Role == ChatRole.User;
}
