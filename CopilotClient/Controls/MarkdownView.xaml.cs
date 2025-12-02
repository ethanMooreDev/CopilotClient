using Markdig;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.UI;


namespace CopilotClient.Controls;

public sealed partial class MarkdownView : UserControl
{
    public MarkdownView()
    {
        InitializeComponent();
        Loaded += MarkdownView_Loaded;
    }

    // Markdown text dependency property
    public string Markdown
    {
        get => (string)GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    public static readonly DependencyProperty MarkdownProperty =
        DependencyProperty.Register(
            nameof(Markdown),
            typeof(string),
            typeof(MarkdownView),
            new PropertyMetadata(string.Empty, OnMarkdownChanged));

    private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownView view)
        {
            view.RenderMarkdown();
        }
    }

    private async void MarkdownView_Loaded(object sender, RoutedEventArgs e)
    {
        await MarkdownWebView.EnsureCoreWebView2Async();

        MarkdownWebView.DefaultBackgroundColor = Color.FromArgb(255, 11, 17, 32);

        RenderMarkdown();
    }

    private void RenderMarkdown()
    {
        if (MarkdownWebView.CoreWebView2 == null)
            return;

        var markdown = Markdown ?? string.Empty;

        // Configure Markdig pipeline (tweak as needed)
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        var htmlBody = Markdig.Markdown.ToHtml(markdown, pipeline);

        // Basic HTML doc wrapper with some default styling
        var htmlDocument = $@"
<html>
<head>
<meta charset=""utf-8"">
<style>
html, body {{
    margin: 0;
    padding: 0;
    height: 100%;
    width: 100%;
    font-family: 'Segoe UI', sans-serif;
    font-size: 14px;
    line-height: 1.4;
    color: white;
    background-color: #0B1120;
}}
pre, code {{
    background: #f6f8fa;
    padding: 0.75rem;
    overflow-x: auto;
    background-color: #2d2d2d;
    font-family: Consolas, monospace;
    font-size: 13px;
    line-height: 1.5
}}
table {{
    border-collapse: collapse;
}}
td, th {{
    border: 1px solid #d0d7de;
    padding: 6px 10px;
}}
</style>
</head>
<body>{htmlBody}</body>
</html>";



        // Navigate to the in-memory HTML
        MarkdownWebView.NavigateToString(htmlDocument);

        MarkdownWebView.CoreWebView2.NavigationCompleted += async (_, __) =>
        {
            string heightStr = await MarkdownWebView.ExecuteScriptAsync("document.body.scrollHeight.toString()");
            string widthStr = await MarkdownWebView.ExecuteScriptAsync("document.body.scrollWidth.toString()");

            if (double.TryParse(heightStr.Trim('"'), out double contentHeight) &&
                double.TryParse(widthStr.Trim('"'), out double contentWidth))
            {
                MarkdownWebView.Height = contentHeight;
                MarkdownWebView.Width = contentWidth;
            }
        };
    }
}
