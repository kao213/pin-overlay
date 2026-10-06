using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace PinOverlay.Services;

/// <summary>テキストタブの FlowDocument と、settings.json に保存する XAML 文字列の変換。</summary>
internal static class RichTextSerializer
{
    public static FlowDocument CreateDocument(string? xaml)
    {
        var document = new FlowDocument
        {
            FontFamily = Controls.TextDefaults.FontFamily,
            FontSize = 24,
            Foreground = Brushes.White,
            PagePadding = new Thickness(8),
        };

        // settings.json は信頼できない入力なので、XAML ローダーに渡す前に安全なものだけに絞る（危険なら空から始める）
        var safeXaml = string.IsNullOrEmpty(xaml) ? null : Core.RichTextXamlSanitizer.Sanitize(xaml);
        if (safeXaml is not null)
        {
            try
            {
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(safeXaml));
                new TextRange(document.ContentStart, document.ContentEnd).Load(stream, DataFormats.Xaml);
            }
            catch (Exception ex) when (ex is ArgumentException or System.Windows.Markup.XamlParseException or InvalidOperationException)
            {
                // 読めない内容は捨てて空から始める
            }
        }
        return document;
    }

    public static string Serialize(FlowDocument document)
    {
        using var stream = new MemoryStream();
        new TextRange(document.ContentStart, document.ContentEnd).Save(stream, DataFormats.Xaml);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
