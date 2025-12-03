// MarkdownBubble.xaml.cs
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using Windows.ApplicationModel.DataTransfer;
using CodeInline = Markdig.Syntax.Inlines.CodeInline;
using EmphasisInline = Markdig.Syntax.Inlines.EmphasisInline;
using Hyperlink = Microsoft.UI.Xaml.Documents.Hyperlink;
using LineBreakInline = Markdig.Syntax.Inlines.LineBreakInline;
using LinkInline = Markdig.Syntax.Inlines.LinkInline;
using LiteralInline = Markdig.Syntax.Inlines.LiteralInline;
using MdContainerInline = Markdig.Syntax.Inlines.ContainerInline;
// Aliases to avoid Inline name collisions
using MdInline = Markdig.Syntax.Inlines.Inline;
using Run = Microsoft.UI.Xaml.Documents.Run;
using TextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

namespace CopilotClient.Controls;

public sealed partial class MarkdownBubble : UserControl
{
    public static readonly DependencyProperty MarkdownTextProperty =
        DependencyProperty.Register(nameof(MarkdownText), typeof(string),
            typeof(MarkdownBubble),
            new PropertyMetadata(string.Empty, OnMarkdownChanged)
        );

    public string MarkdownText
    {
        get => (string)GetValue(MarkdownTextProperty);
        set => SetValue(MarkdownTextProperty, value);
    }

    private static readonly MarkdownPipeline Pipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public MarkdownBubble()
    {
        this.InitializeComponent();
    }

    private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (MarkdownBubble)d;
        control.RenderMarkdown(e.NewValue as string ?? string.Empty);
    }

    private void RenderMarkdown(string markdown)
    {
        ContentPanel.Children.Clear();
        var doc = Markdig.Markdown.Parse(markdown, Pipeline);

        foreach (var block in doc)
        {
            switch (block)
            {
                case HeadingBlock h: ContentPanel.Children.Add(CreateHeading(h)); break;
                case ParagraphBlock p: ContentPanel.Children.Add(CreateParagraph(p)); break;
                case ListBlock list: ContentPanel.Children.Add(CreateList(list)); break;
                case QuoteBlock quote: ContentPanel.Children.Add(CreateQuote(quote)); break;
                case FencedCodeBlock fenced: ContentPanel.Children.Add(CreateCodeBlock(fenced)); break;
                case CodeBlock code: ContentPanel.Children.Add(CreateIndentedCodeBlock(code)); break;
                case Table table: ContentPanel.Children.Add(CreateTable(table)); break;
                case ThematicBreakBlock _: ContentPanel.Children.Add(CreateHorizontalRule()); break;
                case HtmlBlock html: ContentPanel.Children.Add(CreateHtmlBlock(html)); break;
            }

        }
    }

    private UIElement CreateHeading(HeadingBlock h)
    {
        var rtb = new RichTextBlock { IsTextSelectionEnabled = true };

        // Pick style based on heading level
        var para = new Paragraph();
        var run = new Run { Text = InlineToPlainText(h.Inline) ?? "" };

        switch (h.Level)
        {
            case 1:
                run.FontSize = 22;
                run.FontWeight = FontWeights.Bold;
                break;
            case 2:
                run.FontSize = 19;
                run.FontWeight = FontWeights.SemiBold;
                break;
            default:
                run.FontSize = 16;
                run.FontWeight = FontWeights.SemiBold;
                break;
        }

        para.Inlines.Add(run);
        rtb.Blocks.Add(para);

        return rtb;
    }


    private UIElement CreateParagraph(ParagraphBlock p)
    {
        var rtb = new RichTextBlock { IsTextSelectionEnabled = true };
        var para = new Paragraph();
        AddInlines(para, p.Inline);
        rtb.Blocks.Add(para);
        return rtb;
    }


    private UIElement CreateList(ListBlock list)
    {
        var panel = new StackPanel { Spacing = 4, Margin = new Thickness(0, 0, 0, 10) };
        int index = 1;

        foreach (ListItemBlock item in list)
        {
            var itemPanel = new StackPanel { Spacing = 4, Orientation = Orientation.Horizontal };

            // Default bullet text
            var bulletText = list.IsOrdered ? $"{index}. " : "• ";
            var bullet = new TextBlock
            {
                Text = bulletText,
                Style = (Style)Resources["MdParagraph"],
                Margin = new Thickness(0, 0, 4, 0)
            };

            var content = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Top };

            foreach (var childBlock in item)
            {
                switch (childBlock)
                {
                    case ParagraphBlock p:
                        {
                            // Extract raw line from source using Span
                            var raw = ExtractRaw(p);

                            // Detect GitHub task list markers at the start of the paragraph text
                            // Accept forms like "- [x] ..." or "* [ ] ..." and tolerate spaces
                            var (isTask, isChecked, label) = ParseTaskFromRaw(raw);

                            if (isTask)
                            {
                                var cb = new CheckBox
                                {
                                    IsChecked = isChecked,
                                    IsEnabled = false,
                                    Content = new TextBlock
                                    {
                                        // Render the rest of the paragraph as formatted markdown (optional)
                                        // If you prefer rich formatting (links/emphasis), swap to CreateParagraph(p)
                                        Text = label,
                                        Style = (Style)Resources["MdParagraph"]
                                    }
                                };
                                content.Children.Add(cb);

                                // Hide the bullet for task items to avoid bullet + checkbox duplication
                                bullet.Text = "";
                            }
                            else
                            {
                                // Normal list paragraph rendering
                                content.Children.Add(CreateParagraph(p));
                            }
                            break;
                        }


                    case ListBlock nested:
                        var nestedPanel = CreateList(nested);
                        (nestedPanel as FrameworkElement)!.Margin = new Thickness(20, 0, 0, 0);
                        content.Children.Add(nestedPanel);
                        break;

                    case FencedCodeBlock code:
                        content.Children.Add(CreateCodeBlock(code));
                        break;
                }
            }

            itemPanel.Children.Add(bullet);
            itemPanel.Children.Add(content);
            panel.Children.Add(itemPanel);
            index++;
        }

        return panel;
    }


    private UIElement CreateCodeBlock(FencedCodeBlock code)
    {
        // code.Info has the language, if applicable. 

        // Overall code block
        var border = new Border
        {
            Background = (Brush)Resources["CodeBackground"],
            CornerRadius = new CornerRadius(6),
        };

        var copyButton = new Button
        {
            Content = "Copy code",
            Padding = new Thickness(4),
            Margin = new Thickness(0),
            Background = (Brush)Resources["CodeBackground"],
            HorizontalAlignment = HorizontalAlignment.Right
        };

        copyButton.Click += (_, __) => 
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            dataPackage.SetText(code.Lines.ToString());
            Clipboard.SetContent(dataPackage);
        };

        var headerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto },
            },
            Padding = new Thickness(10, 4, 4, 0)
        };

        if(!string.IsNullOrEmpty(code.Info))
        {
            var languageLabel = new TextBlock
            {
                Text = code.Info ?? string.Empty,
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                Margin = new Thickness(0, 4, 0, 0),
            };

            Grid.SetColumn(languageLabel, 0);
            headerGrid.Children.Add(languageLabel);
        }
        
        Grid.SetColumn(copyButton, 1);
        headerGrid.Children.Add(copyButton);

        // actual code in block
        var tb = new TextBlock
        {
            Text = code.Lines.ToString(),
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new FontFamily("Consolas"),
            Foreground = (Brush)Resources["TextPrimary"],
            LineHeight = 20,
            Margin = new Thickness(10, 15, 10, 10)
        };
        tb.IsTextSelectionEnabled = true;

        // container for header and code
        var stackPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
        };

        stackPanel.Children.Add(headerGrid);
        stackPanel.Children.Add(tb);

        var scroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = stackPanel,
        };

        border.Child = scroll;
        return border;
    }

    private UIElement CreateIndentedCodeBlock(CodeBlock code)
    {
        // Overall code block container
        var border = new Border
        {
            Background = (Brush)Resources["CodeBackground"],
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(0, 0, 0, 10)
        };

        // Copy button
        var copyButton = new Button
        {
            Content = "Copy code",
            Padding = new Thickness(4),
            Margin = new Thickness(0),
            Background = (Brush)Resources["CodeBackground"],
            HorizontalAlignment = HorizontalAlignment.Right
        };

        copyButton.Click += (_, __) =>
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage
            {
                RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy
            };
            dataPackage.SetText(code.Lines.ToString());
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        };

        // Header grid (no language label for indented blocks)
        var headerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto },
            },
            Padding = new Thickness(10, 4, 4, 0)
        };

        Grid.SetColumn(copyButton, 1);
        headerGrid.Children.Add(copyButton);

        // Actual code text
        var tb = new TextBlock
        {
            Text = code.Lines.ToString(),
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new FontFamily("Consolas"),
            Foreground = (Brush)Resources["TextPrimary"],
            LineHeight = 20,
            Margin = new Thickness(10, 15, 10, 10),
            IsTextSelectionEnabled = true
        };

        // StackPanel to hold header + code
        var stackPanel = new StackPanel { Orientation = Orientation.Vertical };
        stackPanel.Children.Add(headerGrid);
        stackPanel.Children.Add(tb);

        // ScrollViewer for horizontal scrolling
        var scroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = stackPanel
        };

        border.Child = scroll;
        return border;
    }


    private UIElement CreateHorizontalRule()
    {
        return new Border
        {
            BorderBrush = new SolidColorBrush(Colors.Gray),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Margin = new Thickness(0, 10, 0, 10)
        };
    }

    private UIElement CreateHtmlBlock(HtmlBlock html)
    {
        var tb = new TextBlock
        {
            Text = html.Lines.ToString(),
            Foreground = new SolidColorBrush(Colors.DarkGray),
            FontFamily = new FontFamily("Consolas"),
            TextWrapping = TextWrapping.Wrap
        };
        return tb;
    }


    private UIElement CreateQuote(QuoteBlock quote)
    {
        var border = new Border
        {
            BorderThickness = new Thickness(4, 0, 0, 0),
            BorderBrush = (Brush)Resources["QuoteBorder"],
            Padding = new Thickness(10, 4, 0, 4),
            Margin = new Thickness(0, 0, 0, 10)
        };

        // Use a StackPanel so we can host any type of block inside the quote
        var stack = new StackPanel { Spacing = 6 };

        foreach (var block in quote)
        {
            switch (block)
            {
                case ParagraphBlock p:
                    var para = new Paragraph();
                    AddInlines(para, p.Inline);
                    var rtb = new RichTextBlock
                    {
                        IsTextSelectionEnabled = true,
                        FontStyle = Windows.UI.Text.FontStyle.Italic,
                        Foreground = (Brush)Resources["QuoteForeground"]
                    };
                    rtb.Blocks.Add(para);
                    stack.Children.Add(rtb);
                    break;

                case HeadingBlock h:
                    stack.Children.Add(CreateHeading(h));
                    break;

                case ListBlock list:
                    stack.Children.Add(CreateList(list));
                    break;

                case FencedCodeBlock fenced:
                    stack.Children.Add(CreateCodeBlock(fenced));
                    break;

                case CodeBlock code:
                    stack.Children.Add(CreateIndentedCodeBlock(code));
                    break;

                case Table table:
                    stack.Children.Add(CreateTable(table));
                    break;

                case ThematicBreakBlock _:
                    stack.Children.Add(CreateHorizontalRule());
                    break;

                case HtmlBlock html:
                    stack.Children.Add(CreateHtmlBlock(html));
                    break;
            }
        }

        border.Child = stack;
        return border;
    }



    // IMPORTANT: accept Markdig's ContainerInline, not WinUI's Inline
    private void AddInlines(Paragraph para, ContainerInline inlineRoot)
    {
        if (inlineRoot == null) return;

        var current = inlineRoot.FirstChild;

        while (current != null)
        {
            switch (current)
            {
                case LiteralInline lit:
                    para.Inlines.Add(new Run { Text = lit.Content.ToString() });
                    break;

                // Italic / Bold (normal emphasis)
                case EmphasisInline emph when emph.DelimiterChar == '*' || emph.DelimiterChar == '_':
                    var text = InlineToPlainText(emph.FirstChild) ?? "";
                    var run = new Run { Text = text };
                    if (emph.DelimiterCount == 1)
                        run.FontStyle = Windows.UI.Text.FontStyle.Italic;
                    else if (emph.DelimiterCount >= 2)
                        run.FontWeight = FontWeights.SemiBold;
                    para.Inlines.Add(run);
                    break;

                // Strikethrough (~~ or <del>)
                case EmphasisInline emph when emph.DelimiterChar == '~':
                    var strikeRun = new Run { Text = InlineToPlainText(emph.FirstChild) ?? "" };
                    strikeRun.TextDecorations = Windows.UI.Text.TextDecorations.Strikethrough;
                    para.Inlines.Add(strikeRun);
                    break;

                // Handle <ins> ... </ins> as underline
                case HtmlInline html when html.Tag.Equals("<ins>", StringComparison.OrdinalIgnoreCase):
                {
                    if (TryConsumeHtmlSpan(html, "</ins>", out var innerText, out var afterClose))
                    {
                        para.Inlines.Add(new Run
                        {
                            Text = innerText,
                            TextDecorations = Windows.UI.Text.TextDecorations.Underline
                        });

                        // Jump to the node after the closing tag
                        current = afterClose;
                        continue; // Skip default advancement below
                    }

                    // No closing tag found; advance to avoid loops
                    current = current.NextSibling;
                    continue;
                }

                // Handle <del> ... </del> as strikethrough
                case HtmlInline html when html.Tag.Equals("<del>", StringComparison.OrdinalIgnoreCase):
                {
                    if (TryConsumeHtmlSpan(html, "</del>", out var innerText, out var afterClose))
                    {
                        para.Inlines.Add(new Run
                        {
                            Text = innerText,
                            TextDecorations = Windows.UI.Text.TextDecorations.Strikethrough
                        });

                        current = afterClose;
                        continue;
                    }

                    current = current.NextSibling;
                    continue;
                }

                case HtmlInline html when html.Tag.Equals("<b>", StringComparison.OrdinalIgnoreCase):
                    para.Inlines.Add(new Run { Text = InlineToPlainText(html.NextSibling), FontWeight = FontWeights.Bold });
                    break;

                case HtmlInline html when html.Tag.Equals("<i>", StringComparison.OrdinalIgnoreCase):
                    para.Inlines.Add(new Run { Text = InlineToPlainText(html.NextSibling), FontStyle = Windows.UI.Text.FontStyle.Italic });
                    break;


                case CodeInline code:
                {
                    // Create a Border to provide background and padding
                    var border = new Border
                    {
                        Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 40, 40, 40)), // dark gray background
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(4, 0, 4, 0),
                        Margin = new Thickness(2, 0, 2, 0),
                        Child = new TextBlock
                        {
                            Text = code.Content,
                            FontFamily = new FontFamily("Consolas"),
                            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 200, 200, 200))
                        }
                    };

                    // Wrap the Border in InlineUIContainer so it can live inside a Paragraph
                    para.Inlines.Add(new InlineUIContainer { Child = border });
                    break;
                }

                case LinkInline link:
                    if (link.IsImage)
                    {
                        ContentPanel.Children.Add(CreateImage(link));
                    }
                    else
                    {
                        var hyperlink = new Hyperlink();
                        hyperlink.Inlines.Add(new Run { Text = InlineToPlainText(link.FirstChild) ?? link.Url });
                        hyperlink.Click += async (_, __) =>
                        {
                            if (!string.IsNullOrEmpty(link.Url))
                                await Windows.System.Launcher.LaunchUriAsync(new Uri(link.Url));
                        };
                        para.Inlines.Add(hyperlink);
                    }
                    break;

                case LineBreakInline _:
                    para.Inlines.Add(new LineBreak());
                    break;

            }

            current = current.NextSibling;
        }
    }


    private static string? InlineToPlainText(MdInline? inline)
    {
        if (inline == null) return null;
        var sb = new System.Text.StringBuilder();

        // ContainerInline implements IEnumerable<MdInline>, so iterate child chain
        if (inline is MdContainerInline container)
        {
            foreach (var child in container)
            {
                sb.Append(InlineToPlainText(child));
            }
            return sb.ToString();
        }

        switch (inline)
        {
            case LiteralInline lit:
                return lit.Content.ToString();
            case CodeInline code:
                return code.Content;
            case EmphasisInline emph:
                return InlineToPlainText(emph.FirstChild);
            case LineBreakInline _:
                return "\n";
            default:
                return null;
        }
    }

    private UIElement CreateImage(LinkInline imageInline)
    {
        var img = new Image
        {
            Source = new BitmapImage(new Uri(imageInline.Url)),
            Stretch = Stretch.Uniform,
            MaxWidth = 400,
            Margin = new Thickness(0, 8, 0, 8)
        };

        // Optional: use alt text as tooltip
        if (!string.IsNullOrEmpty(imageInline.FirstChild?.ToString()))
            ToolTipService.SetToolTip(img, imageInline.FirstChild.ToString());

        return img;
    }


    private UIElement CreateTable(Table table)
    {
        var grid = new Grid
        {
            BorderBrush = new SolidColorBrush(Colors.Gray),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 8, 0, 8)
        };

        // Define columns
        for (int c = 0; c < table.ColumnDefinitions.Count; c++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        int rowIndex = 0;
        foreach (var rowObj in table)
        {
            if (rowObj is TableRow row)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                int colIndex = 0;
                foreach (var cellObj in row)
                {
                    if (cellObj is TableCell cell)
                    {
                        var rtb = new RichTextBlock { IsTextSelectionEnabled = true };
                        var para = new Paragraph();
                        foreach (var block in cell)
                        {
                            if (block is ParagraphBlock p)
                                AddInlines(para, p.Inline);
                        }
                        rtb.Blocks.Add(para);

                        var border = new Border
                        {
                            BorderBrush = new SolidColorBrush(Colors.Gray),
                            BorderThickness = new Thickness(0.5),
                            Padding = new Thickness(6),
                            Child = rtb
                        };

                        Grid.SetRow(border, rowIndex);
                        Grid.SetColumn(border, colIndex);
                        grid.Children.Add(border);
                        colIndex++;
                    }
                }
                rowIndex++;
            }
        }

        return grid;
    }

    // Helper: consume everything between an opening HtmlInline and its closing tag.
    // Returns inner plain text and the node after the closing tag if found.
    private bool TryConsumeHtmlSpan(HtmlInline openingTag, string closingTag, out string innerText, out Markdig.Syntax.Inlines.Inline afterClose)
    {
        innerText = "";
        afterClose = null;

        var scan = openingTag.NextSibling;

        while (scan != null)
        {
            // Found closing tag -> stop and return
            if (scan is HtmlInline htmlClose &&
                htmlClose.Tag.Equals(closingTag, StringComparison.OrdinalIgnoreCase))
            {
                afterClose = htmlClose.NextSibling;
                return true;
            }

            // Accumulate text from intermediate nodes
            innerText += InlineToPlainText(scan) ?? "";
            scan = scan.NextSibling;
        }

        // No closing tag found
        return false;
    }

    // Extract the raw source text for a leaf block using its Span.
    // Falls back gracefully if Span is default or out of range.
    private string ExtractRaw(LeafBlock block)
    {
        if (MarkdownText == null) return "";
        var span = block.Span;
        if (span.Start >= 0 && span.End <= MarkdownText.Length && span.Start < span.End)
        {
            // Span.End is exclusive in Markdig; use substring accordingly
            return MarkdownText.Substring(span.Start, span.Length);
        }
        return "";
    }

    // Parses "- [x] Task label" / "- [ ] Task label" from raw text.
    // Returns: isTask, isChecked, label (trimmed)
    private (bool isTask, bool isChecked, string label) ParseTaskFromRaw(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return (false, false, "");

        // Normalize leading whitespace
        var line = raw.TrimStart();

        // Find the first "[x]" or "[ ]" after the bullet marker
        // Handles "-", "*", "+", and ordered "1." forms
        // Examples:
        // - [x] Done
        // * [ ] Not done
        // 1. [x] Numbered
        int bracketIdx = line.IndexOf('[');
        if (bracketIdx < 0) return (false, false, "");

        // Ensure pattern immediately starts with [x] or [ ]
        if (bracketIdx + 2 >= line.Length) return (false, false, "");

        char c1 = line[bracketIdx + 1];
        char c2 = line[bracketIdx + 2];
        bool isBracket = c2 == ']';
        bool isKnown = c1 == 'x' || c1 == 'X' || c1 == ' ';

        if (!isBracket || !isKnown) return (false, false, "");

        bool isChecked = (c1 == 'x' || c1 == 'X');

        // Label is everything after the closing bracket
        var after = line.Substring(bracketIdx + 3).TrimStart();
        // If there’s a known separator like spaces or tabs, trim them
        return (true, isChecked, after);
    }

}