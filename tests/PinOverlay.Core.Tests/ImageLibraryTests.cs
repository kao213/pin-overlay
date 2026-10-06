using PinOverlay.Core;

namespace PinOverlay.Core.Tests;

public sealed class ImageLibraryTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "pin-overlay-tests-" + Guid.NewGuid());

    public ImageLibraryTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private void Touch(params string[] parts)
    {
        var path = Path.Combine([_root, .. parts]);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, []);
    }

    [Fact]
    public void Scan_ListsOnlyDirectChildFoldersAndSupportedImages()
    {
        Touch("root.png");
        Touch("Yyy", "Ddd.webp");
        Touch("Yyy", "Ccc.JPG");
        Touch("Xxx", "Bbb.png");
        Touch("Xxx", "Aaa.png");
        Touch("Xxx", "note.txt");
        Touch("Xxx", "Sub", "Eee.png");

        var folders = ImageLibrary.Scan(_root);

        Assert.Equal(["Xxx", "Yyy"], folders.Select(f => f.Name));
        Assert.Equal(["Aaa.png", "Bbb.png"], folders[0].Files);
        Assert.Equal(["Ccc.JPG", "Ddd.webp"], folders[1].Files);
    }

    [Fact]
    public void Scan_ReturnsEmptyWhenRootMissing()
    {
        Assert.Empty(ImageLibrary.Scan(Path.Combine(_root, "missing")));
    }
}
