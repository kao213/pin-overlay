using PinOverlay.Core;

namespace PinOverlay.Core.Tests;

public class ImageSizingTests
{
    // docs/design.md 5.2 の例（拡大率 150%、最大サイズ 128px）
    [Theory]
    [InlineData(256, 256, 128, 128)]
    [InlineData(256, 128, 128, 64)]
    [InlineData(100, 100, 128, 128)]
    [InlineData(64, 64, 96, 96)]
    public void Compute_MatchesDesignExamples(int w, int h, int expectedW, int expectedH)
    {
        Assert.Equal((expectedW, expectedH), ImageSizing.Compute(w, h, 150, 128));
    }

    [Fact]
    public void Compute_KeepsSmallImageAsIs()
    {
        Assert.Equal((40, 20), ImageSizing.Compute(40, 20, 100, 128));
    }

    [Fact]
    public void Compute_ShrinksTallImageByHeight()
    {
        Assert.Equal((32, 128), ImageSizing.Compute(100, 400, 100, 128));
    }

    [Fact]
    public void Compute_ClampsInvalidInputs()
    {
        Assert.Equal((0, 0), ImageSizing.Compute(0, 10, 100, 128));
        Assert.Equal((1, 1), ImageSizing.Compute(10, 10, 100, 0));
    }
}
