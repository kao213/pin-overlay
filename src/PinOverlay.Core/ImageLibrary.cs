namespace PinOverlay.Core;

/// <summary>images フォルダ直下のサブフォルダ 1 つ分（＝タブ 1 つ分）。</summary>
public sealed record ImageFolder(string Name, string FullPath, IReadOnlyList<string> Files);

/// <summary>images フォルダの読み込み。</summary>
public static class ImageLibrary
{
    public static readonly IReadOnlySet<string> SupportedExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp" };

    public static bool IsSupported(string fileName) =>
        SupportedExtensions.Contains(Path.GetExtension(fileName));

    /// <summary>
    /// images 直下のサブフォルダと、その直下にある画像を列挙する。
    /// images 直下の画像と孫フォルダの中身は対象外。
    /// </summary>
    public static IReadOnlyList<ImageFolder> Scan(string imagesRoot)
    {
        if (!Directory.Exists(imagesRoot))
        {
            return [];
        }

        return Directory.EnumerateDirectories(imagesRoot)
            .Select(dir => new ImageFolder(
                Path.GetFileName(dir),
                dir,
                Directory.EnumerateFiles(dir)
                    .Select(Path.GetFileName)
                    .OfType<string>()
                    .Where(IsSupported)
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToList()))
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>ファイル名から拡張子を除いた表示名。</summary>
    public static string DisplayName(string fileName) => Path.GetFileNameWithoutExtension(fileName);
}
