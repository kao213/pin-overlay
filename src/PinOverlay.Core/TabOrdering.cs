namespace PinOverlay.Core;

public static class TabOrdering
{
    /// <summary>
    /// 保存されている並び順と、実際に存在するフォルダを突き合わせる。
    /// 保存済みのものはその順に並べ、なくなったものは除き、新しいものは名前順で末尾に足す。
    /// </summary>
    public static List<string> Reconcile(IEnumerable<string> savedOrder, IEnumerable<string> existing)
    {
        var existingSet = new HashSet<string>(existing, StringComparer.Ordinal);
        var result = savedOrder.Where(existingSet.Contains).Distinct(StringComparer.Ordinal).ToList();
        var known = new HashSet<string>(result, StringComparer.Ordinal);
        result.AddRange(existingSet.Where(n => !known.Contains(n)).Order(StringComparer.OrdinalIgnoreCase));
        return result;
    }

    /// <summary>order の中で name を newIndex の位置へ移動した新しいリストを返す。</summary>
    public static List<string> Move(IReadOnlyList<string> order, string name, int newIndex)
    {
        var result = order.ToList();
        if (!result.Remove(name))
        {
            return result;
        }
        result.Insert(Math.Clamp(newIndex, 0, result.Count), name);
        return result;
    }
}
