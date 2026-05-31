using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Markdig.Extensions.Tables;

namespace NativeMDView
{
    public class ThemeColors
    {
        public string Bg { get; set; }
        public string Text { get; set; }
        public string Heading { get; set; }
        public string Link { get; set; }
        public string CodeBg { get; set; }
        public string CodeText { get; set; }
        public string Blockquote { get; set; }
        public string BlockquoteBorder { get; set; }
        public string Border { get; set; }
        public string TableHeaderBg { get; set; }
        public string TableAlt { get; set; }
        public string Strong { get; set; }
        public string Em { get; set; }
        public string Del { get; set; }
        public bool IsDark { get; set; }

        public SolidColorBrush BgBrush { get; private set; }
        public SolidColorBrush TextBrush { get; private set; }
        public SolidColorBrush HeadingBrush { get; private set; }
        public SolidColorBrush LinkBrush { get; private set; }
        public SolidColorBrush CodeBgBrush { get; private set; }
        public SolidColorBrush CodeTextBrush { get; private set; }
        public SolidColorBrush BlockquoteBrush { get; private set; }
        public SolidColorBrush BlockquoteBorderBrush { get; private set; }
        public SolidColorBrush BorderBrush { get; private set; }
        public SolidColorBrush TableHeaderBgBrush { get; private set; }
        public SolidColorBrush TableAltBrush { get; private set; }
        public SolidColorBrush StrongBrush { get; private set; }
        public SolidColorBrush EmBrush { get; private set; }
        public SolidColorBrush DelBrush { get; private set; }

        private static readonly Dictionary<string, ThemeColors> _cache = new();

        public static ThemeColors Get(string theme)
        {
            if (_cache.TryGetValue(theme, out var cached))
                return cached;

            var isDark = theme == "dark";
            var colors = new ThemeColors
            {
                IsDark = isDark,
                Bg = isDark ? "#1E1E1E" : "#FFFFFF",
                Text = isDark ? "#D4D4D4" : "#1E1E1E",
                Heading = isDark ? "#569CD6" : "#0066CC",
                Link = isDark ? "#569CD6" : "#0066CC",
                CodeBg = isDark ? "#2D2D30" : "#F5F5F5",
                CodeText = isDark ? "#CE9178" : "#A31515",
                Blockquote = isDark ? "#9CDCFE" : "#333333",
                BlockquoteBorder = isDark ? "#007ACC" : "#0066CC",
                Border = isDark ? "#3E3E42" : "#E0E0E0",
                TableHeaderBg = isDark ? "#2D2D30" : "#F0F0F0",
                TableAlt = isDark ? "#252526" : "#F8F8F8",
                Strong = isDark ? "#DCDCAA" : "#0066CC",
                Em = isDark ? "#CE9178" : "#A31515",
                Del = isDark ? "#808080" : "#999999"
            };
            colors.CreateBrushes();
            _cache[theme] = colors;
            return colors;
        }

        private void CreateBrushes()
        {
            BgBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Bg));
            TextBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Text));
            HeadingBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Heading));
            LinkBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Link));
            CodeBgBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(CodeBg));
            CodeTextBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(CodeText));
            BlockquoteBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Blockquote));
            BlockquoteBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BlockquoteBorder));
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Border));
            TableHeaderBgBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(TableHeaderBg));
            TableAltBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(TableAlt));
            StrongBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Strong));
            EmBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Em));
            DelBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Del));
        }
    }

    public static class MarkdownRenderer
    {
        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseEmojiAndSmiley()
            .Build();

        private static readonly FontFamily MonoFont = new("Cascadia Code, Consolas, Courier New");
        private static readonly HttpClient _httpClient = new();

        public static List<UIElement> Render(string markdown, ThemeColors colors, double zoom)
        {
            var elements = new List<UIElement>();
            var fontSize = 14.0 * zoom;

            if (string.IsNullOrWhiteSpace(markdown))
            {
                var placeholder = new TextBlock
                {
                    Text = "Open file or Ctrl + E for write new file...",
                    Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128)),
                    FontSize = 18 * zoom,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 100, 0, 0)
                };
                var container = new Border
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Child = placeholder
                };
                elements.Add(container);
                return elements;
            }

            var document = Markdown.Parse(markdown, Pipeline);

            foreach (var block in document)
            {
                var element = RenderBlock(block, colors, fontSize, zoom, 0);
                if (element != null)
                    elements.Add(element);
            }

            return elements;
        }

        private static UIElement RenderBlock(Markdig.Syntax.Block block, ThemeColors colors, double fontSize, double zoom, int listDepth)
        {
            return block switch
            {
                HeadingBlock h => RenderHeading(h, colors, fontSize, zoom),
                ParagraphBlock p => RenderParagraph(p, colors, fontSize, zoom),
                CodeBlock c => RenderCodeBlock(c, colors, fontSize, zoom),
                QuoteBlock q => RenderQuote(q, colors, fontSize, zoom, listDepth),
                ListBlock l => RenderList(l, colors, fontSize, zoom, listDepth),
                Markdig.Extensions.Tables.Table t => RenderTable(t, colors, fontSize, zoom),
                ThematicBreakBlock => RenderThematicBreak(colors, zoom),
                HtmlBlock h => RenderHtmlBlock(h, colors, fontSize, zoom),
                _ => null
            };
        }

        private static Border RenderHeading(HeadingBlock heading, ThemeColors colors, double fontSize, double zoom)
        {
            var sizeFactor = heading.Level switch { 1 => 2.0, 2 => 1.5, 3 => 1.25, 4 => 1.0, 5 => 0.875, _ => 0.85 };
            var tb = new TextBlock
            {
                FontSize = fontSize * sizeFactor,
                FontWeight = FontWeights.SemiBold,
                Foreground = colors.HeadingBrush,
                Margin = new Thickness(0, 24 * zoom, 0, heading.Level <= 2 ? 16 * zoom : 8 * zoom),
                TextWrapping = TextWrapping.Wrap
            };

            AddInlines(tb, heading.Inline, colors, fontSize, zoom);

            if (heading.Level <= 2)
            {
                return new Border
                {
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    BorderBrush = colors.BorderBrush,
                    Padding = new Thickness(0, 0, 0, heading.Level == 1 ? 6 * zoom : 4 * zoom),
                    Margin = new Thickness(0, 0, 0, 16 * zoom),
                    Child = tb
                };
            }

            return new Border { Child = tb, Margin = new Thickness(0, 0, 0, 16 * zoom) };
        }

        private static Border RenderParagraph(ParagraphBlock paragraph, ThemeColors colors, double fontSize, double zoom)
        {
            var tb = new TextBlock
            {
                FontSize = fontSize,
                Foreground = colors.TextBrush,
                Margin = new Thickness(0, 0, 0, 16 * zoom),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = fontSize * 1.6
            };

            AddInlines(tb, paragraph.Inline, colors, fontSize, zoom);

            return new Border { Child = tb };
        }

        private static Border RenderCodeBlock(CodeBlock codeBlock, ThemeColors colors, double fontSize, double zoom)
        {
            var codeText = ExtractCodeText(codeBlock);

            var contentStack = new StackPanel();

            var copyBtn = new Button
            {
                Content = "Copy",
                FontSize = 12 * zoom,
                Padding = new Thickness(8 * zoom, 4 * zoom, 8 * zoom, 4 * zoom),
                Margin = new Thickness(0, 0, 0, 4 * zoom),
                Background = colors.BorderBrush,
                Foreground = colors.TextBrush,
                BorderThickness = new Thickness(1),
                BorderBrush = colors.BorderBrush,
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            copyBtn.Click += (s, e) =>
            {
                Clipboard.SetText(codeText);
                copyBtn.Content = "Copied!";
                copyBtn.Background = new SolidColorBrush(Color.FromRgb(22, 130, 93));
                copyBtn.BorderBrush = new SolidColorBrush(Color.FromRgb(22, 130, 93));
                copyBtn.Foreground = Brushes.White;
                var btnRef = copyBtn;
                var borderRef = colors.BorderBrush;
                var fgRef = colors.TextBrush;
                _ = btnRef.Dispatcher.BeginInvoke(new Action(async () =>
                {
                    await Task.Delay(2000);
                    btnRef.Content = "Copy";
                    btnRef.Background = borderRef;
                    btnRef.BorderBrush = borderRef;
                    btnRef.Foreground = fgRef;
                }));
            };

            var codeTb = new TextBlock
            {
                Text = codeText,
                FontFamily = MonoFont,
                FontSize = fontSize * 0.9,
                Foreground = colors.TextBrush,
                TextWrapping = TextWrapping.Wrap
            };

            contentStack.Children.Add(copyBtn);
            contentStack.Children.Add(codeTb);

            return new Border
            {
                Background = colors.CodeBgBrush,
                CornerRadius = new CornerRadius(6 * zoom),
                Padding = new Thickness(16 * zoom),
                Margin = new Thickness(0, 0, 0, 16 * zoom),
                Child = contentStack
            };
        }

        private static string ExtractCodeText(CodeBlock codeBlock)
        {
            var lines = new List<string>();
            foreach (var line in codeBlock.Lines.Lines)
            {
                if (line.Slice.Text != null)
                {
                    var text = line.Slice.Text.Substring(line.Slice.Start, line.Slice.Length);
                    lines.Add(text);
                }
            }
            return string.Join("\n", lines);
        }

        private static Border RenderQuote(QuoteBlock quote, ThemeColors colors, double fontSize, double zoom, int listDepth)
        {
            var innerPanel = new StackPanel();
            foreach (var block in quote)
            {
                var element = RenderBlock(block, colors, fontSize, zoom, listDepth);
                if (element != null)
                    innerPanel.Children.Add(element);
            }

            return new Border
            {
                BorderThickness = new Thickness(4 * zoom, 0, 0, 0),
                BorderBrush = colors.BlockquoteBorderBrush,
                Padding = new Thickness(12 * zoom, 0, 0, 0),
                Margin = new Thickness(0, 0, 0, 16 * zoom),
                Child = innerPanel
            };
        }

        private static StackPanel RenderList(ListBlock list, ThemeColors colors, double fontSize, double zoom, int listDepth)
        {
            var panel = new StackPanel();
            var isOrdered = list.IsOrdered;
            var bullet = isOrdered ? "" : "\u2022";
            var counter = 1;
            if (isOrdered && list.OrderedStart != null)
            {
                var digits = new string(list.OrderedStart.TakeWhile(char.IsDigit).ToArray());
                if (digits.Length > 0) counter = int.Parse(digits);
            }

            foreach (var item in list)
            {
                if (item is ListItemBlock listItem)
                {
                    var itemPanel = new StackPanel { Orientation = Orientation.Horizontal };

                    var marker = new TextBlock
                    {
                        Text = isOrdered ? $"{counter}. " : bullet,
                        FontSize = fontSize,
                        Foreground = colors.TextBrush,
                        Margin = new Thickness(0, 0, 8 * zoom, 0),
                        MinWidth = 20 * zoom,
                        TextAlignment = TextAlignment.Right
                    };
                    itemPanel.Children.Add(marker);

                    var contentPanel = new StackPanel();
                    foreach (var block in listItem)
                    {
                        var element = RenderBlock(block, colors, fontSize, zoom, listDepth + 1);
                        if (element != null)
                            contentPanel.Children.Add(element);
                    }
                    itemPanel.Children.Add(contentPanel);

                    itemPanel.Margin = new Thickness(20 * zoom * (listDepth + 1), 4 * zoom, 0, 4 * zoom);
                    panel.Children.Add(itemPanel);

                    if (isOrdered) counter++;
                }
            }

            return panel;
        }

        private static Grid RenderTable(Markdig.Extensions.Tables.Table table, ThemeColors colors, double fontSize, double zoom)
        {
            var rows = table.Count;
            if (rows == 0) return new Grid { Margin = new Thickness(0, 0, 0, 16 * zoom) };
            var cols = table.Max(r => ((Markdig.Extensions.Tables.TableRow)r).Count);

            var grid = new Grid
            {
                Margin = new Thickness(0, 0, 0, 16 * zoom),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            for (int c = 0; c < cols; c++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            for (int r = 0; r < rows; r++)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            for (int r = 0; r < rows; r++)
            {
                var row = (Markdig.Extensions.Tables.TableRow)table[r];
                for (int c = 0; c < row.Count && c < cols; c++)
                {
                    var cell = row[c];
                    var cellBorder = new Border
                    {
                        BorderBrush = colors.BorderBrush,
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(8 * zoom, 6 * zoom, 8 * zoom, 6 * zoom)
                    };

                    if (r == 0)
                        cellBorder.Background = colors.TableHeaderBgBrush;
                    else if (r % 2 == 0)
                        cellBorder.Background = colors.TableAltBrush;

                    var cellTb = new TextBlock
                    {
                        FontSize = fontSize,
                        Foreground = colors.TextBrush,
                        TextWrapping = TextWrapping.Wrap
                    };

                    var container = cell as Markdig.Syntax.ContainerBlock;
                    if (container != null)
                    {
                        for (int i = 0; i < container.Count; i++)
                        {
                            if (container[i] is Markdig.Syntax.ParagraphBlock para && para.Inline != null)
                            {
                                AddInlines(cellTb, para.Inline, colors, fontSize, zoom);
                            }
                        }
                    }

                    if (r == 0)
                        cellTb.FontWeight = FontWeights.SemiBold;

                    cellBorder.Child = cellTb;
                    Grid.SetRow(cellBorder, r);
                    Grid.SetColumn(cellBorder, c);
                    grid.Children.Add(cellBorder);
                }
            }

            return grid;
        }

        private static Border RenderThematicBreak(ThemeColors colors, double zoom)
        {
            return new Border
            {
                Height = 2,
                Background = colors.BorderBrush,
                Margin = new Thickness(0, 24 * zoom, 0, 24 * zoom)
            };
        }

        private static Border RenderHtmlBlock(HtmlBlock html, ThemeColors colors, double fontSize, double zoom)
        {
            var text = ExtractHtmlText(html);
            return new Border
            {
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = fontSize,
                    Foreground = colors.TextBrush,
                    TextWrapping = TextWrapping.Wrap,
                    FontStyle = FontStyles.Italic,
                    Opacity = 0.7
                },
                Margin = new Thickness(0, 0, 0, 16 * zoom)
            };
        }

        private static string ExtractHtmlText(HtmlBlock html)
        {
            var lines = new List<string>();
            foreach (var line in html.Lines.Lines)
            {
                if (line.Slice.Text != null)
                {
                    var text = line.Slice.Text.Substring(line.Slice.Start, line.Slice.Length);
                    lines.Add(text);
                }
            }
            return string.Join("\n", lines);
        }

        private static void AddInlines(TextBlock tb, Markdig.Syntax.Inlines.Inline inline, ThemeColors colors, double fontSize, double zoom)
        {
            var current = inline;
            while (current != null)
            {
                foreach (var element in GetInlines(current, colors, fontSize, zoom))
                    tb.Inlines.Add(element);
                current = current.NextSibling;
            }
        }

        private static IEnumerable<System.Windows.Documents.Inline> GetInlines(Markdig.Syntax.Inlines.Inline inline, ThemeColors colors, double fontSize, double zoom)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    yield return new Run
                    {
                        Text = literal.Content.ToString(),
                        Foreground = colors.TextBrush,
                        FontSize = fontSize
                    };
                    break;

                case LineBreakInline:
                    yield return new LineBreak();
                    break;

                case CodeInline code:
                    yield return new Run
                    {
                        Text = code.Content,
                        FontFamily = MonoFont,
                        FontSize = fontSize * 0.9,
                        Foreground = colors.CodeTextBrush,
                        Background = colors.CodeBgBrush
                    };
                    break;

                case EmphasisInline emphasis:
                    {
                        var span = new Span
                        {
                            FontSize = fontSize
                        };

                        var current = emphasis.FirstChild;
                        while (current != null)
                        {
                            foreach (var child in GetInlines(current, colors, fontSize, zoom))
                                span.Inlines.Add(child);
                            current = current.NextSibling;
                        }

                        if (emphasis.DelimiterCount == 1)
                        {
                            span.FontStyle = FontStyles.Italic;
                            span.Foreground = colors.EmBrush;
                        }
                        else
                        {
                            span.FontWeight = FontWeights.Bold;
                            span.Foreground = colors.StrongBrush;
                        }

                        yield return span;
                    }
                    break;

                case LinkInline link:
                    {
                        var url = link.Url;
                        var tb = new TextBlock
                        {
                            Foreground = colors.LinkBrush,
                            Cursor = Cursors.Hand,
                            FontSize = fontSize
                        };

                        if (link.FirstChild is LinkInline imageLink && imageLink.IsImage)
                        {
                            var imgUrl = imageLink.Url;
                            var imgAlt = GetInlineText(imageLink.FirstChild);
                            var image = new Image
                            {
                                MaxWidth = 600 * zoom,
                                Margin = new Thickness(0, 8 * zoom, 0, 8 * zoom)
                            };

                            var tbRef = tb;
                            var imgRef = image;
                            var altText = imgAlt;
                            var zoomRef = zoom;
                            _ = LoadImageAsync(imgRef, imgUrl, altText, zoomRef, colors, tbRef);

                            if (imgRef.Source != null)
                            {
                                var container = new InlineUIContainer { Child = imgRef };
                                tb.Inlines.Add(container);
                            }
                        }
                        else
                        {
                            var current = link.FirstChild;
                            while (current != null)
                            {
                                foreach (var child in GetInlines(current, colors, fontSize, zoom))
                                    tb.Inlines.Add(child);
                                current = current.NextSibling;
                            }
                            tb.TextDecorations = TextDecorations.Underline;
                        }

                        tb.MouseLeftButtonUp += (s, e) =>
                        {
                            try
                            {
                                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                            }
                            catch { }
                        };

                        yield return new InlineUIContainer { Child = tb };
                    }
                    break;

                case ContainerInline container:
                    {
                        var current = container.FirstChild;
                        while (current != null)
                        {
                            foreach (var child in GetInlines(current, colors, fontSize, zoom))
                                yield return child;
                            current = current.NextSibling;
                        }
                    }
                    break;

                default:
                    {
                        var text = inline.ToString();
                        if (!string.IsNullOrEmpty(text))
                        {
                            yield return new Run
                            {
                                Text = text,
                                Foreground = colors.TextBrush,
                                FontSize = fontSize
                            };
                        }
                    }
                    break;
            }
        }

        private static async Task LoadImageAsync(Image image, string imgUrl, string imgAlt, double zoom, ThemeColors colors, TextBlock tb)
        {
            try
            {
                if (imgUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    imgUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    var bytes = await _httpClient.GetByteArrayAsync(imgUrl);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    using (var ms = new MemoryStream(bytes))
                    {
                        bitmap.StreamSource = ms;
                        bitmap.EndInit();
                    }
                    bitmap.Freeze();
                    image.Source = bitmap;
                }
                else
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imgUrl, UriKind.RelativeOrAbsolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    image.Source = bitmap;
                }
            }
            catch
            {
                image.Source = null;
                tb.Inlines.Add(new Run { Text = $"[Image: {imgAlt}]", FontStyle = FontStyles.Italic, Foreground = colors.TextBrush });
            }
        }

        private static string GetInlineText(Markdig.Syntax.Inlines.Inline inline)
        {
            if (inline is LiteralInline lit) return lit.Content.ToString();
            if (inline is ContainerInline container)
            {
                var current = container.FirstChild;
                var parts = new List<string>();
                while (current != null)
                {
                    parts.Add(GetInlineText(current));
                    current = current.NextSibling;
                }
                return string.Join("", parts);
            }
            return inline?.ToString() ?? "";
        }
    }
}
