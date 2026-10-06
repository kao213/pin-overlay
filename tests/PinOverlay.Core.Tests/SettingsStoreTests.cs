using PinOverlay.Core;

namespace PinOverlay.Core.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "pin-overlay-tests-" + Guid.NewGuid());

    public SettingsStoreTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        var path = Path.Combine(_dir, "settings.json");
        var settings = new AppSettings { TabOrder = ["Yyy", "Xxx"], Opacity = 0.8 };
        var tab = settings.GetOrAddTab("Xxx");
        tab.ScalePercent = 150;
        tab.MaxSize = 64;
        tab.WrapWidth = 400;
        tab.Position = new PixelPoint(10, 20);
        tab.CheckedFiles.Add("Aaa.png");
        settings.Text.ContentXaml = "<Section />";

        SettingsStore.Save(path, settings);
        var loaded = SettingsStore.Load(path);

        Assert.Equal(["Yyy", "Xxx"], loaded.TabOrder);
        Assert.Equal(0.8, loaded.Opacity);
        var loadedTab = loaded.Tabs["Xxx"];
        Assert.Equal(150, loadedTab.ScalePercent);
        Assert.Equal(64, loadedTab.MaxSize);
        Assert.Equal(400, loadedTab.WrapWidth);
        Assert.Equal(10, loadedTab.Position!.X);
        Assert.Equal(20, loadedTab.Position.Y);
        Assert.Equal(["Aaa.png"], loadedTab.CheckedFiles);
        Assert.Equal("<Section />", loaded.Text.ContentXaml);
    }

    [Fact]
    public void Load_ReturnsDefaultsWhenMissing()
    {
        var loaded = SettingsStore.Load(Path.Combine(_dir, "none.json"));
        Assert.Empty(loaded.Tabs);
        Assert.Equal(1.0, loaded.Opacity);
    }

    [Fact]
    public void Load_DropsNullTabEntry()
    {
        var loaded = LoadJson("""{"Tabs":{"x":null}}""");
        Assert.Empty(loaded.Tabs);
    }

    [Fact]
    public void Load_DropsNullTabOrderEntry()
    {
        var loaded = LoadJson("""{"TabOrder":[null,"a"]}""");
        Assert.Equal(["a"], loaded.TabOrder);
    }

    [Fact]
    public void Load_DropsNullCheckedFilesEntry()
    {
        var loaded = LoadJson("""{"Tabs":{"x":{"CheckedFiles":[null,"a.png"]}}}""");
        Assert.Equal(["a.png"], loaded.Tabs["x"].CheckedFiles);
    }

    [Theory]
    [InlineData("""{"OutlineColor":"zzz"}""")]
    [InlineData("""{"OutlineColor":"ContextColor \\\\a\\x.icc 1,0,0,0"}""")]
    [InlineData("""{"OutlineColor":null}""")]
    [InlineData("""{"OutlineColor":"#80FF0000\n"}""")]
    public void Load_ResetsInvalidOutlineColor(string json)
    {
        var loaded = LoadJson(json);
        Assert.Equal(new AppSettings().OutlineColor, loaded.OutlineColor);
    }

    [Fact]
    public void Load_KeepsValidOutlineColor()
    {
        var loaded = LoadJson("""{"OutlineColor":"#80FF0000"}""");
        Assert.Equal("#80FF0000", loaded.OutlineColor);
    }

    [Theory]
    [InlineData(5.0, 1.0)]
    [InlineData(0.0, 0.1)]
    [InlineData(0.5, 0.5)]
    public void Load_ClampsOpacity(double input, double expected)
    {
        var loaded = LoadJson($$"""{"Opacity":{{input.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}""");
        Assert.Equal(expected, loaded.Opacity);
    }

    private AppSettings LoadJson(string json)
    {
        var path = Path.Combine(_dir, "settings.json");
        File.WriteAllText(path, json);
        return SettingsStore.Load(path);
    }

    [Fact]
    public void Load_BacksUpBrokenFile()
    {
        var path = Path.Combine(_dir, "settings.json");
        File.WriteAllText(path, "{ broken");

        var loaded = SettingsStore.Load(path);

        Assert.Empty(loaded.Tabs);
        Assert.True(File.Exists(path + ".broken"));
    }
}
