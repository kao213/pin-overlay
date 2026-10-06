using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace PinOverlay.Controls;

/// <summary>リッチテキストの 1 区間（同じ書式が続く範囲）。</summary>
public sealed record TextSpanModel(string Text, FontFamily FontFamily, double FontSize, Brush Foreground, FontWeight FontWeight, FontStyle FontStyle);

/// <summary>リッチテキストの 1 段落。</summary>
public sealed record ParagraphModel(TextAlignment Alignment, IReadOnlyList<TextSpanModel> Spans);

/// <summary>
/// FlowDocument の中身を、縁取り付きで描画できる形に写し取ったもの。
/// RichTextBox には縁取りの機能がないため、表示用に自前で描く（docs/design.md 6.1）。
/// </summary>
public sealed class RichTextModel
{
    public IReadOnlyList<ParagraphModel> Paragraphs { get; }

    private RichTextModel(IReadOnlyList<ParagraphModel> paragraphs) => Paragraphs = paragraphs;

    public bool IsBlank => Paragraphs.All(p => p.Spans.All(s => string.IsNullOrWhiteSpace(s.Text)));

    public static RichTextModel From(FlowDocument document)
    {
        var paragraphs = new List<ParagraphModel>();
        CollectBlocks(document.Blocks, paragraphs);

        // 末尾の空行は表示しない
        while (paragraphs.Count > 0 && paragraphs[^1].Spans.All(s => s.Text.Length == 0))
        {
            paragraphs.RemoveAt(paragraphs.Count - 1);
        }
        return new RichTextModel(paragraphs);
    }

    private static void CollectBlocks(BlockCollection blocks, List<ParagraphModel> output)
    {
        foreach (var block in blocks)
        {
            switch (block)
            {
                case Paragraph paragraph:
                    var spans = new List<TextSpanModel>();
                    CollectInlines(paragraph.Inlines, spans);
                    output.Add(new ParagraphModel(paragraph.TextAlignment, spans));
                    break;
                case Section section:
                    CollectBlocks(section.Blocks, output);
                    break;
                case List list:
                    foreach (var item in list.ListItems)
                    {
                        CollectBlocks(item.Blocks, output);
                    }
                    break;
            }
        }
    }

    private static void CollectInlines(InlineCollection inlines, List<TextSpanModel> output)
    {
        foreach (var inline in inlines)
        {
            switch (inline)
            {
                case Run run:
                    output.Add(Span(run, run.Text));
                    break;
                case LineBreak lineBreak:
                    output.Add(Span(lineBreak, "\n"));
                    break;
                case Span span:
                    CollectInlines(span.Inlines, output);
                    break;
            }
        }
    }

    // 書式は親（Bold や Paragraph など）から継承された値も含めて取り出せる
    private static TextSpanModel Span(TextElement element, string text) =>
        new(text, element.FontFamily, element.FontSize, element.Foreground, element.FontWeight, element.FontStyle);
}

/// <summary>RichTextModel を縁取り付きで描画する。</summary>
public sealed class OutlinedRichText : FrameworkElement
{
    private readonly RichTextModel _model;
    private readonly Brush _outline;
    private List<(FormattedText Text, double Thickness)>? _lines;

    public OutlinedRichText(RichTextModel model, Brush outline)
    {
        _model = model;
        _outline = outline;
    }

    private List<(FormattedText Text, double Thickness)> Build()
    {
        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var lines = new List<(FormattedText, double)>();
        foreach (var paragraph in _model.Paragraphs)
        {
            var first = paragraph.Spans.FirstOrDefault();
            var builder = new StringBuilder();
            foreach (var span in paragraph.Spans)
            {
                builder.Append(span.Text);
            }
            // 空行も高さを持たせる
            var text = builder.Length == 0 ? " " : builder.ToString();
            var fontSize = first?.FontSize ?? 24;
            var formatted = new FormattedText(
                text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(first?.FontFamily ?? TextDefaults.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                fontSize,
                first?.Foreground ?? Brushes.White,
                pixelsPerDip)
            {
                TextAlignment = paragraph.Alignment,
            };

            var start = 0;
            var maxSize = fontSize;
            foreach (var span in paragraph.Spans)
            {
                if (span.Text.Length > 0)
                {
                    formatted.SetFontFamily(span.FontFamily, start, span.Text.Length);
                    formatted.SetFontSize(span.FontSize, start, span.Text.Length);
                    formatted.SetForegroundBrush(span.Foreground, start, span.Text.Length);
                    formatted.SetFontWeight(span.FontWeight, start, span.Text.Length);
                    formatted.SetFontStyle(span.FontStyle, start, span.Text.Length);
                    maxSize = Math.Max(maxSize, span.FontSize);
                }
                start += span.Text.Length;
            }
            lines.Add((formatted, Math.Max(2, maxSize / 8)));
        }
        return lines;
    }

    private double Padding => _lines is { Count: > 0 } ? _lines.Max(l => l.Thickness) : 0;

    protected override Size MeasureOverride(Size availableSize)
    {
        _lines = Build();
        if (_lines.Count == 0)
        {
            return default;
        }

        var width = _lines.Max(l => l.Text.WidthIncludingTrailingWhitespace);
        foreach (var (text, _) in _lines)
        {
            // 段落ごとの揃え方を効かせるため、全段落の幅をそろえる
            text.MaxTextWidth = Math.Ceiling(width) + 1;
        }
        var height = _lines.Sum(l => l.Text.Height);
        return new Size(Math.Ceiling(width) + 1 + Padding, height + Padding);
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (_lines is null)
        {
            return;
        }

        var y = Padding / 2;
        foreach (var (text, thickness) in _lines)
        {
            var origin = new Point(Padding / 2, y);
            var pen = new Pen(_outline, thickness) { LineJoin = PenLineJoin.Round };
            dc.DrawGeometry(null, pen, text.BuildGeometry(origin));
            dc.DrawText(text, origin);
            y += text.Height;
        }
    }
}

internal static class TextDefaults
{
    public static readonly FontFamily FontFamily = new("Meiryo UI, Yu Gothic UI, Segoe UI");
}
