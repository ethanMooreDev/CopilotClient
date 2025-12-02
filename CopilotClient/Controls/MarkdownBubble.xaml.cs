// MarkdownBubble.xaml.cs
using Markdig;
using Markdig.Syntax;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using System;

// Markdig inline aliases (avoid "Inline" naming conflicts)
using MdInline = Markdig.Syntax.Inlines.Inline;
using MdContainerInline = Markdig.Syntax.Inlines.ContainerInline;
using LiteralInline = Markdig.Syntax.Inlines.LiteralInline;
using EmphasisInline = Markdig.Syntax.Inlines.EmphasisInline;
using CodeInline = Markdig.Syntax.Inlines.CodeInline;
using LinkInline = Markdig.Syntax.Inlines.LinkInline;
using LineBreakInline = Markdig.Syntax.Inlines.LineBreakInline;
using Microsoft.UI.Text;

namespace CopilotClient.Controls;

public sealed partial class MarkdownBubble : UserControl
{
    // Dependency Property: MarkdownText
    public static readonly DependencyProperty MarkdownTextProperty =
        DependencyProperty.Register(nameof(MarkdownText), typeof(string),
            typeof(MarkdownBubble),
            new PropertyMetadata(string.Empty, OnMarkdownChanged));

    public string MarkdownText
    {
        get => (string)GetValue(MarkdownTextProperty);
        set => SetValue(MarkdownTextProperty, value);
    }

    private static readonly MarkdownPipeline Pipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public MarkdownBubble()
    {
        InitializeComponent();
    }

    private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (MarkdownBubble)d;
        control.RenderMarkdown(e.NewValue as string ?? string.Empty);
    }

    // Render everything into a single RichTextBlock for contiguous selection
    private void RenderMarkdown(string markdown)
    {
        MarkdownRichText.Blocks.Clear();

        var doc = Markdig.Markdown.Parse(markdown, Pipeline);

        foreach (var block in doc)
        {
            switch (block)
            {
                case HeadingBlock h:
                    MarkdownRichText.Blocks.Add(CreateHeadingParagraph(h));
                    break;

                case ParagraphBlock p:
                    MarkdownRichText.Blocks.Add(CreateParagraph(p.Inline));
                    break;

                case ListBlock list:
                    AddList(list);
                    break;

                case FencedCodeBlock code:
                    MarkdownRichText.Blocks.Add(CreateCodeParagraph(code));
                    break;

                case QuoteBlock quote:
                    AddQuote(quote);
                    break;

                default:
                    // Fallback: dump block inline text
                    if (block is LeafBlock leaf && leaf.Inline is MdContainerInline inlines)
                        MarkdownRichText.Blocks.Add(CreateParagraph(inlines));
                    break;
            }
        }
    }

    private Paragraph CreateHeadingParagraph(HeadingBlock h)
    {
        var para = new Paragraph();
        var text = InlineToPlainText(h.Inline) ?? string.Empty;

        var run = new Run { Text = text };
        run.FontWeight = FontWeights.SemiBold;
        run.FontSize = h.Level switch
        {
            1 => 22,
            2 => 19,
            _ => 16
        };

        para.Inlines.Add(run);
        // add bottom spacing via a linebreak
        para.Inlines.Add(new LineBreak());
        return para;
    }

    private Paragraph CreateParagraph(MdContainerInline inlineRoot)
    {
        var para = new Paragraph();
        AddInlines(para, inlineRoot);
        return para;
    }

    private void AddList(ListBlock list)
    {
        int index = 1;
        foreach (ListItemBlock item in list)
        {
            // Compose a single paragraph per list item to keep selection contiguous
            var liPara = new Paragraph();

            string bullet = list.IsOrdered ? $"{index}. " : "• ";
            liPara.Inlines.Add(new Run { Text = bullet });

            foreach (var child in item)
            {
                switch (child)
                {
                    case ParagraphBlock p:
                        AddInlines(liPara, p.Inline);
                        break;

                    case ListBlock nested:
                        // Indent nested list: add linebreak and some spacing prefix
                        liPara.Inlines.Add(new LineBreak());
                        liPara.Inlines.Add(new Run { Text = "    " });
                        // Render nested items as separate paras to keep readability
                        int nestedIndex = 1;
                        foreach (ListItemBlock nestedItem in nested)
                        {
                            var nestedPara = new Paragraph();
                            string nestedBullet = nested.IsOrdered ? $"{nestedIndex}. " : "• ";
                            nestedPara.Inlines.Add(new Run { Text = nestedBullet });
                            foreach (var nestedChild in nestedItem)
                            {
                                if (nestedChild is ParagraphBlock np)
                                    AddInlines(nestedPara, np.Inline);
                            }
                            MarkdownRichText.Blocks.Add(nestedPara);
                            nestedIndex++;
                        }
                        break;
                }
            }

            MarkdownRichText.Blocks.Add(liPara);
            index++;
        }
    }

    private Paragraph CreateCodeParagraph(FencedCodeBlock code)
    {
        // RichTextBlock doesn't support background per paragraph; keep it simple and readable
        // by using monospace + secondary color. For bordered code blocks you'd wrap an RTF
        // in a container, but that breaks contiguous selection.
        var para = new Paragraph();

        var run = new Run
        {
            Text = code.Lines.ToString(),
            FontFamily = new FontFamily("Consolas"),
            Foreground = (Brush)Resources["TextSecondary"]
        };

        para.Inlines.Add(run);
        return para;
    }

    private void AddQuote(QuoteBlock quote)
    {
        foreach (var block in quote)
        {
            if (block is ParagraphBlock p)
            {
                var para = new Paragraph();

                // simple quote prefix “> ” + italic style
                para.Inlines.Add(new Run
                {
                    Text = "> ",
                    Foreground = (Brush)Resources["TextSecondary"]
                });

                // Render quoted content italic
                var quotedText = InlineToPlainText(p.Inline) ?? string.Empty;
                para.Inlines.Add(new Run
                {
                    Text = quotedText,
                    FontStyle = Windows.UI.Text.FontStyle.Italic
                });

                MarkdownRichText.Blocks.Add(para);
            }
        }
    }

    // Add Markdig inline nodes into a RichTextBlock Paragraph
    private void AddInlines(Paragraph para, MdContainerInline inlineRoot)
    {
        if (inlineRoot == null) return;

        foreach (MdInline inline in inlineRoot)
        {
            switch (inline)
            {
                case LiteralInline lit:
                    para.Inlines.Add(new Run { Text = lit.Content.ToString() });
                    break;

                case EmphasisInline emph:
                    {
                        var text = InlineToPlainText(emph.FirstChild) ?? string.Empty;
                        var run = new Run { Text = text };
                        if (emph.DelimiterCount <= 1)
                            run.FontStyle = Windows.UI.Text.FontStyle.Italic;
                        else
                            run.FontWeight = FontWeights.SemiBold;
                        para.Inlines.Add(run);
                        break;
                    }

                case CodeInline code:
                    para.Inlines.Add(new Run
                    {
                        Text = code.Content,
                        FontFamily = new FontFamily("Consolas"),
                        Foreground = (Brush)Resources["TextSecondary"]
                    });
                    break;

                case LinkInline link:
                    {
                        var hyperlink = new Hyperlink();
                        var caption = InlineToPlainText(link.FirstChild) ?? link.Url;
                        hyperlink.Inlines.Add(new Run { Text = caption });
                        // open externally when clicked
                        hyperlink.Click += async (_, __) =>
                        {
                            if (!string.IsNullOrEmpty(link.Url))
                                await Launcher.LaunchUriAsync(new Uri(link.Url));
                        };
                        para.Inlines.Add(hyperlink);
                        break;
                    }

                case LineBreakInline _:
                    para.Inlines.Add(new LineBreak());
                    break;
            }
        }
    }

    // Flatten a Markdig inline (and its children) into plain text
    private static string? InlineToPlainText(MdInline? inline)
    {
        if (inline == null) return null;

        // If it's a container, concatenate child plain texts
        if (inline is MdContainerInline container)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var child in container)
            {
                var t = InlineToPlainText(child);
                if (t != null) sb.Append(t);
            }
            return sb.ToString();
        }

        return inline switch
        {
            LiteralInline lit => lit.Content.ToString(),
            CodeInline code => code.Content,
            EmphasisInline emph => InlineToPlainText(emph.FirstChild),
            LineBreakInline _ => "\n",
            _ => null
        };
    }
}