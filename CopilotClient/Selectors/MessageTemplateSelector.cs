using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CopilotClient.Models;

namespace CopilotClient.Selectors;

public class MessageTemplateSelector : DataTemplateSelector
{
    public DataTemplate? UserTemplate { get; set; }
    public DataTemplate? AssistantTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if(item is ChatMessage message)
        {
            return message.Role switch
            {
                ChatRole.User => UserTemplate ?? base.SelectTemplateCore(item, container),
                ChatRole.Assistant => AssistantTemplate ?? base.SelectTemplateCore(item, container),

                _ => base.SelectTemplateCore(item, container)
            };
        }

        return base.SelectTemplateCore(item, container);
    }

    protected override DataTemplate SelectTemplateCore(object item) => SelectTemplateCore(item, null!);
}
