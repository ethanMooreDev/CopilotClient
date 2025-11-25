using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;

namespace CopilotClient.Behaviors;

public static class StartStoryboardBehavior
{
    public static readonly DependencyProperty StoryboardKeyProperty =
        DependencyProperty.RegisterAttached(
            "StoryboardKey",
            typeof(string),
            typeof(StartStoryboardBehavior),
            new PropertyMetadata(null, OnStoryboardKeyChanged));

    public static string? GetStoryboardKey(DependencyObject obj) =>
        (string?)obj.GetValue(StoryboardKeyProperty);

    public static void SetStoryboardKey(DependencyObject obj, string? value) =>
        obj.SetValue(StoryboardKeyProperty, value);

    private static void OnStoryboardKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement fe)
        {
            if (e.NewValue is string)
                fe.Loaded += OnLoaded;
            else
                fe.Loaded -= OnLoaded;
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        var fe = (FrameworkElement)sender;
        var key = GetStoryboardKey(fe);
        if (string.IsNullOrEmpty(key))
            return;

        if (fe.Resources.TryGetValue(key, out var resource) &&
            resource is Storyboard storyboard)
        {
            storyboard.Begin();
        }
    }
}
