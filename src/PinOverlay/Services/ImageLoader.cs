using System.IO;
using System.Windows.Media.Imaging;

namespace PinOverlay.Services;

/// <summary>画像ファイルの読み込み。ファイルをロックしないよう、読み込んだらすぐ閉じる。</summary>
internal static class ImageLoader
{
    // 寸法だけ巨大な画像（数万 px 四方）を OnLoad で展開するとメモリが尽きるため。
    // 64/128bpp の画像もあるので画素数ではなく展開後のバイト数で見る（表示時に 32bpp へ変換される分も下限として含める）
    private const double MaxDecodedBytes = 256.0 * 1024 * 1024;

    /// <summary>画像を読み込む。読めない形式や壊れたファイルなら null。</summary>
    public static BitmapSource? Load(string path, int? decodeWidth = null)
    {
        try
        {
            using var stream = File.OpenRead(path);
            var frame = BitmapDecoder.Create(
                stream, BitmapCreateOptions.IgnoreColorProfile | BitmapCreateOptions.DelayCreation, BitmapCacheOption.None).Frames[0];
            var bitsPerPixel = Math.Max(frame.Format.BitsPerPixel, 32);
            if ((double)frame.PixelWidth * frame.PixelHeight * bitsPerPixel / 8 > MaxDecodedBytes)
            {
                return null;
            }
            stream.Position = 0;

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            if (decodeWidth is int width)
            {
                image.DecodePixelWidth = width;
            }
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            // WebP は Windows に WebP のコーデック（「WebP 画像拡張機能」）がないと読めない
            return null;
        }
    }
}
