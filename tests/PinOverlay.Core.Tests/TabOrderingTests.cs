using PinOverlay.Core;

namespace PinOverlay.Core.Tests;

public class TabOrderingTests
{
    [Fact]
    public void Reconcile_KeepsSavedOrder_DropsMissing_AppendsNewSorted()
    {
        var result = TabOrdering.Reconcile(["Yyy", "Gone", "Xxx"], ["Xxx", "Yyy", "Zzz", "Aaa"]);
        Assert.Equal(["Yyy", "Xxx", "Aaa", "Zzz"], result);
    }

    [Fact]
    public void Move_MovesItemToIndex()
    {
        Assert.Equal(["B", "A", "C"], TabOrdering.Move(["A", "B", "C"], "A", 1));
        Assert.Equal(["B", "C", "A"], TabOrdering.Move(["A", "B", "C"], "A", 99));
        Assert.Equal(["A", "B", "C"], TabOrdering.Move(["A", "B", "C"], "X", 0));
    }
}
