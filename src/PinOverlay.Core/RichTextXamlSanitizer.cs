using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace PinOverlay.Core;

/// <summary>
/// settings.json のテキストタブ（XAML）を WPF に渡す前に検査する。
/// XAML ローダーは ObjectDataProvider などで任意コードを実行でき、Cursor や FontFamily の値で UNC パスを開かせることもできるため、
/// 拒否リスト（危険なものを探す）ではなく許可リスト（TextRange.Save が出す要素・属性だけ通す）で絞る。
/// </summary>
public static partial class RichTextXamlSanitizer
{
    private const string PresentationNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static readonly HashSet<string> AllowedElements =
    [
        // Hyperlink は NavigateUri が許可リスト外の属性として除かれるので、ただの Span として残る
        "Section", "Paragraph", "Run", "Span", "Bold", "Italic", "Underline", "Hyperlink", "LineBreak", "List", "ListItem",
    ];

    private static readonly HashSet<string> TableElements = ["Table", "TableRowGroup", "TableRow", "TableCell"];

    private static readonly HashSet<string> AllowedAttributes =
    [
        "TextAlignment", "LineHeight", "IsHyphenationEnabled", "FlowDirection", "FontFamily", "FontStyle",
        "FontWeight", "FontStretch", "FontSize", "Foreground", "Background", "TextDecorations", "TextIndent",
        "Margin", "Padding", "BaselineAlignment", "MarkerStyle", "MarkerOffset", "StartIndex", "KeepTogether",
        "KeepWithNext", "LineStackingStrategy", "BreakPageBefore", "BreakColumnBefore", "IsEnabled",
        "xml:space", "xml:lang",
    ];

    /// <summary>
    /// 安全な XAML だけを返す。攻撃にしか使わない構造（許可外の名前空間・マークアップ拡張・処理命令・DTD）があれば null。
    /// 許可外の要素とプロパティ要素は中身ごと、許可外の属性はその属性だけ取り除く。
    /// </summary>
    public static string? Sanitize(string xaml)
    {
        XDocument document;
        try
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using var reader = XmlReader.Create(new StringReader(xaml), settings);
            document = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
        }
        catch (XmlException)
        {
            return null;
        }

        var root = document.Root;
        if (root is null || root.Name != XNamespace.Get(PresentationNamespace) + "Section")
        {
            return null;
        }
        // 処理命令は古い XAML の <?Mapping?>（CLR 名前空間の割り当て）に使われていたため、中身を問わず拒否する
        if (root.DescendantNodes().OfType<XProcessingInstruction>().Any())
        {
            return null;
        }

        foreach (var element in root.DescendantsAndSelf())
        {
            if (element.Name.Namespace.NamespaceName != PresentationNamespace)
            {
                return null;
            }

            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration)
                {
                    if (attribute.Value != PresentationNamespace && attribute.Value != XNamespace.Xml.NamespaceName)
                    {
                        return null;
                    }
                    continue;
                }

                var ns = attribute.Name.Namespace;
                if (ns != XNamespace.None && ns != XNamespace.Xml)
                {
                    return null;
                }
                if (attribute.Value.TrimStart().StartsWith('{'))
                {
                    return null;
                }
            }
        }

        // 表は枠だけ外し、セルの中の段落を本文として残す（セルの中身は Section に置ける Block なので、外しても構造が崩れない）
        // ReplaceWith は親を持つ子を複製して差し込むため、内側（セル）から順に外さないと複製側に外枠が残る
        root.Descendants()
            .Where(element => TableElements.Contains(element.Name.LocalName))
            .Reverse()
            .ToList()
            .ForEach(element => element.ReplaceWith(element.Elements().ToList()));

        // 貼り付けた画像（InlineUIContainer）など、許可外の要素とプロパティ要素は中身ごと捨て、残りの本文は生かす
        root.Descendants()
            .Where(element => element.Name.LocalName.Contains('.') || !AllowedElements.Contains(element.Name.LocalName))
            .ToList()
            .ForEach(element => element.Remove());

        foreach (var attribute in root.DescendantsAndSelf().Attributes().ToList())
        {
            if (!attribute.IsNamespaceDeclaration && !IsSafeAttribute(attribute))
            {
                attribute.Remove();
            }
        }

        return root.ToString(SaveOptions.DisableFormatting);
    }

    private static bool IsSafeAttribute(XAttribute attribute)
    {
        var name = attribute.Name.Namespace == XNamespace.Xml ? "xml:" + attribute.Name.LocalName : attribute.Name.LocalName;
        var value = attribute.Value;

        if (name.StartsWith("Typography.", StringComparison.Ordinal) || name.StartsWith("NumberSubstitution.", StringComparison.Ordinal))
        {
            return true;
        }
        if (!AllowedAttributes.Contains(name))
        {
            return false;
        }

        return name switch
        {
            "FontFamily" => value.IndexOfAny(['/', '\\', ':', '#']) < 0,
            // ContextColor 指定は色プロファイルのファイルを開くため、相対パスでも通さない
            "Foreground" or "Background" =>
                value.IndexOfAny(['/', '\\', ':']) < 0
                && !value.Contains("ContextColor", StringComparison.OrdinalIgnoreCase)
                && (!value.StartsWith('#') || HexColor().IsMatch(value)),
            _ => true,
        };
    }

    [GeneratedRegex(@"\A#(?:[0-9A-Fa-f]{3,4}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})\z")]
    private static partial Regex HexColor();
}
