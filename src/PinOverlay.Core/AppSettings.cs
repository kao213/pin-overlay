namespace PinOverlay.Core;

/// <summary>settings.json に保存するアプリ全体の設定。</summary>
public sealed class AppSettings
{
    /// <summary>画像タブ（フォルダ名）の並び順。</summary>
    public List<string> TabOrder { get; set; } = [];

    /// <summary>フォルダ名ごとの画像タブの設定。</summary>
    public Dictionary<string, ImageTabSettings> Tabs { get; set; } = [];

    public TextTabSettings Text { get; set; } = new();

    /// <summary>すべてのかたまりに共通の不透明度（0.1〜1.0）。</summary>
    public double Opacity { get; set; } = 1.0;

    /// <summary>文字の縁取りの色（#AARRGGBB）。</summary>
    public string OutlineColor { get; set; } = "#FF000000";

    public ImageTabSettings GetOrAddTab(string name)
    {
        if (!Tabs.TryGetValue(name, out var tab))
        {
            tab = new ImageTabSettings();
            Tabs[name] = tab;
        }
        return tab;
    }
}

public sealed class ImageTabSettings
{
    /// <summary>拡大率（%）。</summary>
    public double ScalePercent { get; set; } = ImageSizing.DefaultScalePercent;

    /// <summary>最大サイズ（px）。</summary>
    public int MaxSize { get; set; } = ImageSizing.DefaultMaxSize;

    /// <summary>折り返し幅（px）。並べた画像がこの幅を超えたら次の行へ送る。0 は折り返さない。</summary>
    public int WrapWidth { get; set; }

    /// <summary>かたまりの位置（画面上の実ピクセル）。未配置なら null。</summary>
    public PixelPoint? Position { get; set; }

    /// <summary>表示する（チェックが付いている）画像のファイル名。</summary>
    public List<string> CheckedFiles { get; set; } = [];
}

public sealed class TextTabSettings
{
    /// <summary>リッチテキストの内容（XAML 形式）。</summary>
    public string? ContentXaml { get; set; }

    /// <summary>かたまりの位置（画面上の実ピクセル）。未配置なら null。</summary>
    public PixelPoint? Position { get; set; }
}

public sealed class PixelPoint
{
    public int X { get; set; }
    public int Y { get; set; }

    public PixelPoint() { }

    public PixelPoint(int x, int y)
    {
        X = x;
        Y = y;
    }
}
