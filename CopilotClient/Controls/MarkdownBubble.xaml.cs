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
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Windows.ApplicationModel.DataTransfer;
using Windows.Globalization;
using Windows.UI;
using ColorCode;
using ColorCode.Common;
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
using Windows.ApplicationModel.Appointments.DataProvider;

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

    // Track cleanup actions for dynamically attached handlers/resources
    private readonly Dictionary<DependencyObject, Action> _cleanupActions = new();

    public MarkdownBubble()
    {
        this.InitializeComponent();

        ContentPanel.SizeChanged += (s, e) =>
        {
            foreach (FrameworkElement child in ContentPanel.Children)
            {
                child.MaxWidth = ContentPanel.ActualWidth;
            }
        };

    }

    private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (MarkdownBubble)d;
        control.RenderMarkdown(e.NewValue as string ?? string.Empty);
    }

    private void RenderMarkdown(string markdown)
    {
        // Clean up handlers/resources from previous content before clearing
        DisposeContentChildren();
        ContentPanel.Children.Clear();
        var doc = Markdig.Markdown.Parse(markdown, Pipeline);

        foreach (var block in doc)
        {
            UIElement element = null;

            switch (block)
            {
                case HeadingBlock h:
                    element = CreateHeading(h);
                    break;
                case ParagraphBlock p:
                    element = CreateParagraph(p);
                    break;
                case ListBlock list:
                    element = CreateList(list);
                    break;
                case QuoteBlock quote:
                    element = CreateQuote(quote);
                    break;
                case FencedCodeBlock fenced:
                    element = CreateCodeBlock(fenced);
                    break;
                case CodeBlock code:
                    element = CreateIndentedCodeBlock(code);
                    break;
                case Table table:
                    element = CreateTable(table);
                    break;
                case ThematicBreakBlock _:
                    element = CreateHorizontalRule();
                    break;
                case HtmlBlock html:
                    element = CreateHtmlBlock(html);
                    break;
            }

            if (element != null)
            {
                // Stretch to available width so RichTextBlock wraps correctly
                ((FrameworkElement) element).HorizontalAlignment = HorizontalAlignment.Stretch;

                ContentPanel.Children.Add(element);
            }
        }
    }

    // Unsubscribe handlers and release resources for current children
    private void DisposeContentChildren()
    {
        // Invoke registered cleanup actions
        foreach (var kv in _cleanupActions)
        {
            try
            {
                kv.Value?.Invoke();
            }
            catch { }
        }
        _cleanupActions.Clear();

        // Additionally clear Inline collections and image sources for UI elements
        foreach (var child in ContentPanel.Children)
        {
            if (child is RichTextBlock rtb)
            {
                foreach (var block in rtb.Blocks)
                {
                    if (block is Paragraph para)
                    {
                        para.Inlines.Clear();
                    }
                }
                rtb.Blocks.Clear();
            }

            if (child is Panel panel)
            {
                // walk panel children to clear nested RichTextBlocks, Images, etc.
                ClearPanelRecursively(panel);
            }

            if (child is Image img)
            {
                // cleanup actions already invoked above; ensure source removed
                if (img.Source is BitmapImage bmp)
                {
                    img.Source = null;
                    try { bmp.UriSource = null; } catch { }
                }
            }
        }
    }

    private void ClearPanelRecursively(Panel panel)
    {
        foreach (var item in panel.Children)
        {
            if (item is RichTextBlock rtb)
            {
                foreach (var block in rtb.Blocks)
                {
                    if (block is Paragraph para)
                    {
                        para.Inlines.Clear();
                    }
                }
                rtb.Blocks.Clear();
            }
            else if (item is Panel childPanel)
            {
                ClearPanelRecursively(childPanel);
            }
            else if (item is Border border && border.Child is FrameworkElement fe)
            {
                if (fe is RichTextBlock innerRtb)
                {
                    foreach (var block in innerRtb.Blocks)
                    {
                        if (block is Paragraph para)
                        {
                            para.Inlines.Clear();
                        }
                    }
                    innerRtb.Blocks.Clear();
                }
                else if (fe is Image img)
                {
                    if (_cleanupActions.TryGetValue(img, out var _))
                    {
                        // cleanup action will handle unsubscribing
                    }
                    img.Source = null;
                }
            }
        }
    }

    private UIElement CreateHeading(HeadingBlock h)
    {
        var rtb = new RichTextBlock { 
            IsTextSelectionEnabled = true,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var para = new Paragraph();
        var run = new Run { Text = InlineToPlainText(h.Inline) ?? "" };

        switch (h.Level)
        {
            case 1:
                run.FontSize = 32;
                run.FontWeight = FontWeights.Bold;
                //para.LineHeight = run.FontSize * 1.25;
                break;
            case 2:
                run.FontSize = 24;
                run.FontWeight = FontWeights.SemiBold;
                //para.LineHeight = run.FontSize * 1.25;
                break;
            case 3:
                run.FontSize = 20;
                run.FontWeight = FontWeights.SemiBold;
                //para.LineHeight = run.FontSize * 1.25;
                break;
            case 4:
                run.FontSize = 16;
                run.FontWeight = FontWeights.SemiBold;
                //para.LineHeight = run.FontSize * 1.5;
                break;
            case 5:
                run.FontSize = 14;
                run.FontWeight = FontWeights.Normal;
                //para.LineHeight = run.FontSize * 1.5;
                break;
            case 6:
                run.FontSize = 12;
                run.FontWeight = FontWeights.Normal;
                //para.LineHeight = run.FontSize * 1.5;
                break;
        }

        para.Inlines.Add(run);
        rtb.Blocks.Add(para);

        if (h.Level == 1 || h.Level == 2)
        {
            return new Border
            {
                BorderBrush = (Brush)Resources["BreakLine"], // GitHub gray
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 0, 0, 4), // mimic GitHub’s .3em padding
                Margin = new Thickness(0, 0, 0, 12),
                Child = rtb
            };
        }

        para.Inlines.Add(new LineBreak());

        return rtb;
    }



    private UIElement CreateParagraph(ParagraphBlock p, bool addLineBreak = true)
    {
        var rtb = new RichTextBlock { 
            IsTextSelectionEnabled = true, 
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Stretch

        };
        var para = new Paragraph() 
        {
            LineHeight = 24
        };

        AddInlines(para, p.Inline);

        if (addLineBreak)
        {
            //para.Inlines.Add(new LineBreak());
        }

        rtb.Blocks.Add(para);
        return rtb;
    }


    private UIElement CreateList(ListBlock list)
    {
        var panel = new StackPanel
        {
            Spacing = 4,
            Margin = new Thickness(0, 0, 0, 10)
        };

        int index = 1;

        foreach (ListItemBlock item in list)
        {
            // Grid with two columns: bullet + content
            var itemGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // bullet
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // content

            // Bullet text
            var bulletText = list.IsOrdered ? $"{index}. " : "• ";
            var bullet = new TextBlock
            {
                Text = bulletText,
                Style = (Style)Resources["MdParagraph"],
                Margin = new Thickness(0, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetColumn(bullet, 0);

            // Content stack
            var content = new StackPanel
            {
                Spacing = 2,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            Grid.SetColumn(content, 1);

            // Render child blocks inside content
            foreach (var childBlock in item)
            {
                switch (childBlock)
                {
                    case ParagraphBlock p:
                        {
                            var raw = ExtractRaw(p);
                            var (isTask, isChecked, label) = ParseTaskFromRaw(raw);

                            if (isTask)
                            {
                                var cb = new CheckBox
                                {
                                    IsChecked = isChecked,
                                    IsEnabled = false,
                                    Content = new TextBlock
                                    {
                                        Text = label,
                                        Style = (Style)Resources["MdParagraph"],
                                        TextWrapping = TextWrapping.Wrap                                    }
                                };
                                content.Children.Add(cb);

                                // Hide bullet for task items
                                bullet.Text = "";
                            }
                            else
                            {
                                var para = CreateParagraph(p, false);
                                if (para is FrameworkElement fe)
                                {
                                    fe.HorizontalAlignment = HorizontalAlignment.Stretch;
                                }
                                content.Children.Add(para);
                            }
                            break;
                        }

                    case ListBlock nested:
                        var nestedPanel = CreateList(nested);
                        if (nestedPanel is FrameworkElement nestedFe)
                            nestedFe.Margin = new Thickness(20, 0, 0, 0);
                        content.Children.Add(nestedPanel);
                        break;

                    case FencedCodeBlock code:
                        content.Children.Add(CreateCodeBlock(code));
                        break;
                }
            }

            itemGrid.Children.Add(bullet);
            itemGrid.Children.Add(content);
            panel.Children.Add(itemGrid);

            index++;
        }

        return panel;
    }



    private UIElement CreateCodeBlock(FencedCodeBlock code)
    {
        string codeText = code.Lines.ToString();
        string language = code.Info ?? "";

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

        RoutedEventHandler copyHandler = (_, __) =>
        {
            var dataPackage = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
            dataPackage.SetText(codeText);
            Clipboard.SetContent(dataPackage);
        };

        copyButton.Click += copyHandler;
        _cleanupActions[copyButton] = () => { try { copyButton.Click -= copyHandler; } catch { } };

        var headerGrid = new Grid
        {
            ColumnDefinitions =
        {
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
            new ColumnDefinition { Width = GridLength.Auto },
        },
            Padding = new Thickness(10, 4, 4, 4),
            BorderThickness = new Thickness(0, 0, 0, 1),
            BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 30, 45, 65))
        };

        if (!string.IsNullOrEmpty(language))
        {
            var languageLabel = new TextBlock
            {
                Text = language,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 4, 0, 0),
            };
            Grid.SetColumn(languageLabel, 0);
            headerGrid.Children.Add(languageLabel);
        }

        Grid.SetColumn(copyButton, 1);
        headerGrid.Children.Add(copyButton);

        // RichTextBlock for syntax highlighting
        var rtb = new RichTextBlock
        {
            IsTextSelectionEnabled = true,
            FontFamily = new FontFamily("Consolas"),
            Margin = new Thickness(10, 15, 10, 10),
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Stretch

        };

        var para = new Paragraph();
        AddCodeRuns(para, codeText, language); // custom tokenizer or ColorCode integration
        rtb.Blocks.Add(para);

        var stackPanel = new StackPanel { Orientation = Orientation.Vertical };
        stackPanel.Children.Add(headerGrid);
        stackPanel.Children.Add(rtb);

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
        string codeText = code.Lines.ToString();

        var border = new Border
        {
            Background = (Brush)Resources["CodeBackground"],
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(0, 0, 0, 10)
        };

        var copyButton = new Button
        {
            Content = "Copy code",
            Padding = new Thickness(4),
            Margin = new Thickness(0),
            Background = (Brush)Resources["CodeBackground"],
            HorizontalAlignment = HorizontalAlignment.Right
        };

        RoutedEventHandler copyHandler = (_, __) =>
        {
            var dataPackage = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
            dataPackage.SetText(codeText);
            Clipboard.SetContent(dataPackage);
        };

        copyButton.Click += copyHandler;
        _cleanupActions[copyButton] = () => { try { copyButton.Click -= copyHandler; } catch { } };

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

        // RichTextBlock instead of TextBlock
        var rtb = new RichTextBlock
        {
            IsTextSelectionEnabled = true,
            FontFamily = new FontFamily("Consolas"),
            Margin = new Thickness(10, 15, 10, 10),
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Stretch

        };

        var para = new Paragraph();
        AddCodeRuns(para, codeText, "plain"); // fallback tokenizer
        rtb.Blocks.Add(para);

        var stackPanel = new StackPanel { Orientation = Orientation.Vertical };
        stackPanel.Children.Add(headerGrid);
        stackPanel.Children.Add(rtb);

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
            BorderBrush = (Brush)Resources["BreakLine"],
            BorderThickness = new Thickness(0, 5, 0, 0),
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
                        Foreground = (Brush)Resources["QuoteForeground"],
                        TextWrapping = TextWrapping.Wrap,
                        HorizontalAlignment = HorizontalAlignment.Stretch

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
                        Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255,40,40,40)), // dark gray background
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(4,0,4,0),
                        Margin = new Thickness(2,0,2,0),
                        Child = new TextBlock
                        {
                            Text = code.Content,
                            FontFamily = new FontFamily("Consolas"),
                            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255,200,200,200))
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

                        Windows.Foundation.TypedEventHandler<Hyperlink, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs> clickHandler = null;
                        clickHandler = async (sender, args) =>
                        {
                            if (!string.IsNullOrEmpty(link.Url))
                                await Windows.System.Launcher.LaunchUriAsync(new Uri(link.Url));
                        };

                        hyperlink.Click += clickHandler;
                        // register cleanup so we can unsubscribe later
                        _cleanupActions[hyperlink] = () => { try { hyperlink.Click -= clickHandler; } catch { } };

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
            Stretch = Stretch.Uniform,
            MaxWidth =400,
            Margin = new Thickness(0,8,0,8)
        };

        try
        {
            img.Source = new BitmapImage(new Uri(imageInline.Url));
        }
        catch
        {
            // If the URI itself is invalid, fall back immediately
            return CreateBrokenImagePlaceholder(imageInline);
        }

        // Optional: use alt text as tooltip
        if (!string.IsNullOrEmpty(imageInline.FirstChild?.ToString()))
            ToolTipService.SetToolTip(img, imageInline.FirstChild.ToString());

        // Handle broken links
        Microsoft.UI.Xaml.ExceptionRoutedEventHandler handler = null;
        handler = (sender, args) =>
        {
            try
            {
                var parent = img.Parent as Panel;
                if (parent != null)
                {
                    int index = parent.Children.IndexOf(img);
                    parent.Children.RemoveAt(index);
                    parent.Children.Insert(index, CreateBrokenImagePlaceholder(imageInline));
                }
            }
            finally
            {
                // remove event and cleanup registration
                try { img.ImageFailed -= handler; } catch { }
                if (_cleanupActions.ContainsKey(img)) _cleanupActions.Remove(img);
            }
        };

        img.ImageFailed += handler;

        // register cleanup action so we can unsubscribe later
        _cleanupActions[img] = () => { try { img.ImageFailed -= handler; } catch { } if (img.Source is BitmapImage bmp) img.Source = null; };

        return img;
    }



    private UIElement CreateTable(Table table)
    {
        var grid = new Grid
        {
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
                        var rtb = new RichTextBlock { 
                            IsTextSelectionEnabled = true, 
                            TextWrapping = TextWrapping.Wrap,
                            HorizontalAlignment = HorizontalAlignment.Stretch

                        };
                        var para = new Paragraph();

                        foreach (var block in cell)
                        {
                            if (block is ParagraphBlock p)
                                AddInlines(para, p.Inline);
                        }

                        // Style header rows differently
                        if (row.IsHeader)
                        {
                            para.FontWeight = FontWeights.Bold;
                        }

                        rtb.Blocks.Add(para);

                        var border = new Border
                        {
                            BorderBrush = new SolidColorBrush(Colors.Gray),
                            BorderThickness = new Thickness(0.5),
                            Padding = new Thickness(6),
                            Child = rtb,
                            Background = row.IsHeader
                                ? new SolidColorBrush(ColorHelper.FromArgb(255, 30, 45, 65)) // header background
                                : rowIndex % 2 == 0
                                    ? new SolidColorBrush(ColorHelper.FromArgb(255, 20, 26, 42))
                                    : null
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

    private UIElement CreateBrokenImagePlaceholder(LinkInline imageInline)
    {
        return new Border
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 20, 28, 40)), // subtle dark background for #0B1120 theme
            Width = 200,
            Height = 100,
            Margin = new Thickness(0, 8, 0, 8),
            Child = new TextBlock
            {
                Text = $"Image not found\n{imageInline.Url}",
                Foreground = new SolidColorBrush(Colors.LightGray),
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = true,
                
            }
        };
    }

    private void AddCodeRuns(Paragraph para, string code, string language)
    {

        if (string.IsNullOrEmpty(language))
        {
            para.Inlines.Add(new Run
            {
                Text = code,
                Foreground = new SolidColorBrush(Colors.LightGray),
                FontFamily = new FontFamily("Consolas")
            });
            return;
        }


        // Resolve language (fallback to C#)
        ILanguage lang = Languages.FindById(language) ?? Languages.CSharp;

        var formatter = new RichTextBlockFormatter(ElementTheme.Dark);

        // Write colored runs directly into the paragraph's InlineCollection
        formatter.FormatInlines(code, lang, para.Inlines);
    }

}

