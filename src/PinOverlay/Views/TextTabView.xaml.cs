using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using PinOverlay.Services;

namespace PinOverlay.Views;

/// <summary>テキストタブ。リッチテキストエディタ 1 つと、書式のツールバー（docs/design.md 6 章）。</summary>
public partial class TextTabView : UserControl
{
    private static readonly double[] FontSizes = [10, 12, 14, 16, 18, 20, 24, 28, 32, 36, 48, 64, 72];

    private readonly Func<Color> _getOutline;
    private readonly Action<Color> _setOutline;
    private bool _updatingToolbar;

    public TextTabView(FlowDocument document, Func<Color> getOutline, Action<Color> setOutline)
    {
        InitializeComponent();
        _getOutline = getOutline;
        _setOutline = setOutline;

        Editor.Document = document;
        FontBox.ItemsSource = Fonts.SystemFontFamilies
            .Select(f => f.Source)
            .Order(StringComparer.CurrentCulture)
            .ToList();
        SizeBox.ItemsSource = FontSizes;
        OutlineButton.Background = new SolidColorBrush(getOutline());
        Editor.TextChanged += (_, _) => ContentChanged?.Invoke();
    }

    /// <summary>エディタの中身が変わったとき。</summary>
    public event Action? ContentChanged;

    private void Apply(DependencyProperty property, object value)
    {
        Editor.Selection.ApplyPropertyValue(property, value);
        Editor.Focus();
    }

    private void OnFontChosen(object? sender, EventArgs e) => ApplyFont();

    private void OnFontKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplyFont();
        }
    }

    private void ApplyFont()
    {
        if (_updatingToolbar || string.IsNullOrWhiteSpace(FontBox.Text))
        {
            return;
        }
        Apply(TextElement.FontFamilyProperty, new FontFamily(FontBox.Text));
    }

    private void OnSizeChosen(object? sender, EventArgs e) => ApplySize();

    private void OnSizeKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplySize();
        }
    }

    private void ApplySize()
    {
        if (_updatingToolbar)
        {
            return;
        }
        if (double.TryParse(SizeBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var size) && size is >= 1 and <= 500)
        {
            Apply(TextElement.FontSizeProperty, size);
        }
    }

    private void OnBold(object sender, RoutedEventArgs e) => EditingCommands.ToggleBold.Execute(null, Editor);

    private void OnItalic(object sender, RoutedEventArgs e) => EditingCommands.ToggleItalic.Execute(null, Editor);

    private void OnAlignLeft(object sender, RoutedEventArgs e) => Apply(Paragraph.TextAlignmentProperty, TextAlignment.Left);

    private void OnAlignCenter(object sender, RoutedEventArgs e) => Apply(Paragraph.TextAlignmentProperty, TextAlignment.Center);

    private void OnAlignRight(object sender, RoutedEventArgs e) => Apply(Paragraph.TextAlignmentProperty, TextAlignment.Right);

    private void OnSwatch(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Background: SolidColorBrush brush })
        {
            Apply(TextElement.ForegroundProperty, new SolidColorBrush(brush.Color));
        }
    }

    private void OnCustomColor(object sender, RoutedEventArgs e)
    {
        var current = Editor.Selection.GetPropertyValue(TextElement.ForegroundProperty) as SolidColorBrush;
        if (ColorPicker.Pick(current?.Color ?? Colors.White) is Color color)
        {
            Apply(TextElement.ForegroundProperty, new SolidColorBrush(color));
        }
    }

    private void OnOutlineColor(object sender, RoutedEventArgs e)
    {
        if (ColorPicker.Pick(_getOutline()) is Color color)
        {
            OutlineButton.Background = new SolidColorBrush(color);
            _setOutline(color);
        }
    }

    /// <summary>カーソル位置の書式をツールバーに表示する。</summary>
    private void OnSelectionChanged(object sender, RoutedEventArgs e)
    {
        _updatingToolbar = true;
        try
        {
            var family = Editor.Selection.GetPropertyValue(TextElement.FontFamilyProperty);
            FontBox.Text = family is FontFamily f ? f.Source : "";
            var size = Editor.Selection.GetPropertyValue(TextElement.FontSizeProperty);
            SizeBox.Text = size is double d ? d.ToString("0.#", CultureInfo.CurrentCulture) : "";
        }
        finally
        {
            _updatingToolbar = false;
        }
    }
}
