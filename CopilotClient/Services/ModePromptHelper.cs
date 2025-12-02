using CopilotClient.Models;

namespace CopilotClient.Services;

public static class ModePromptHelper
{
    public static string GetInstructions(ConversationMode mode)
    {
        return mode switch
        {
            ConversationMode.General => "Answer as a general-purpose assistant.",
            ConversationMode.Explain => "Explain code and concepts in detail, as if mentoring a developer.",
            ConversationMode.Refactor => "Refactor the code for readability, maintainability, and best practices.",
            ConversationMode.BugHunt => "Focus on finding bugs and potential issues in the code.",
            ConversationMode.Optimize => "Optimize the code for performance and efficiency.",
            _ => "Answer as a general-purpose assistant."
        };
    }
}
