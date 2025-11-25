using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CopilotClient.Models;

namespace CopilotClient.Selectors;

public class MessageTemplateSelector : DataTemplateSelector
{
    public DataTemplate? UserTemplate { get; set; }
    public DataTemplate? AssistantTemplate { get; set; }
    public DataTemplate? AssistantTypingTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is ChatMessage message && container is FrameworkElement)
        {
            if (message.Role == ChatRole.User)
                return UserTemplate!;

            if (message.Role == ChatRole.Assistant)
            {
                if (message.Status == MessageStatus.Typing && string.IsNullOrEmpty(message.Content))
                    return AssistantTypingTemplate!;

                return AssistantTemplate!;
            }
        }

        return base.SelectTemplateCore(item, container);
    }

    protected override DataTemplate SelectTemplateCore(object item) => SelectTemplateCore(item, null!);
}
