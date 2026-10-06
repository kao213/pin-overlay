namespace PinOverlay.Core;

public static class ImageSizing
{
    public const double DefaultScalePercent = 100;
    public const int DefaultMaxSize = 128;
    public const double MinScalePercent = 1;
    public const double MaxScalePercent = 1000;
    public const int MinMaxSize = 1;
    public const int MaxMaxSize = 4096;
    /// <summary>折り返し幅の上限（px）。0 は折り返さない。</summary>
    public const int MaxWrapWidth = 16384;

    /// <summary>最大サイズの選択肢。</summary>
    public static readonly IReadOnlyList<int> MaxSizePresets = [32, 48, 64, 96, 128, 192, 256, 512];

    /// <summary>
    /// 表示サイズ（画面上の実ピクセル）を求める。
    /// 先に拡大率をかけ、長いほうの辺が最大サイズを超える場合は縦横比を保ったまま最大サイズに収める。
    /// </summary>
    public static (int Width, int Height) Compute(int pixelWidth, int pixelHeight, double scalePercent, int maxSize)
    {
        if (pixelWidth <= 0 || pixelHeight <= 0)
        {
            return (0, 0);
        }

        var scale = Math.Clamp(scalePercent, MinScalePercent, MaxScalePercent) / 100.0;
        var limit = Math.Clamp(maxSize, MinMaxSize, MaxMaxSize);

        var width = pixelWidth * scale;
        var height = pixelHeight * scale;
        var longest = Math.Max(width, height);
        if (longest > limit)
        {
            var shrink = limit / longest;
            width *= shrink;
            height *= shrink;
        }

        return (Math.Max(1, (int)Math.Round(width)), Math.Max(1, (int)Math.Round(height)));
    }
}
