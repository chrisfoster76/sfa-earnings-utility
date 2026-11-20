// Deliberately kept in the root EarningsUtility.UI namespace — see ConsoleWriter.cs for why.
namespace EarningsUtility.UI;

/// <summary>The slice of a list to draw, and how many items are hidden on each side of it.</summary>
public readonly record struct ListWindow(int First, int Count, int HiddenAbove, int HiddenBelow);

/// <summary>Fits screen content into the rows and columns the console window has.</summary>
public static class ScreenFit
{
    public const int MinListRows = 3;

    /// <summary>
    /// Picks the items to draw in <paramref name="rows"/> rows, keeping the selected item in view.
    /// When the list doesn't fit, each side with hidden items gives up one row for a "more" line.
    /// </summary>
    public static ListWindow FitList(int itemCount, int selected, int rows)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rows, MinListRows);

        if (itemCount <= rows)
            return new ListWindow(0, itemCount, 0, 0);

        var edgeCount = rows - 1;
        if (selected < edgeCount)
            return new ListWindow(0, edgeCount, 0, itemCount - edgeCount);

        var lastStart = itemCount - edgeCount;
        if (selected >= lastStart)
            return new ListWindow(lastStart, edgeCount, lastStart, 0);

        var middleCount = rows - 2;
        var first = Math.Clamp(selected - (middleCount - 1) / 2, 1, itemCount - middleCount - 1);
        return new ListWindow(first, middleCount, first, itemCount - first - middleCount);
    }

    /// <summary>Keeps at most <paramref name="maxItems"/> items, the last one standing in for those that don't fit.</summary>
    public static List<T> Cap<T>(IReadOnlyList<T> items, int maxItems, Func<int, T> overflow)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxItems, 1);

        if (items.Count <= maxItems)
            return items.ToList();

        var kept = items.Take(maxItems - 1).ToList();
        kept.Add(overflow(items.Count - kept.Count));
        return kept;
    }

    public static string Truncate(string text, int width)
    {
        if (text.Length <= width)
            return text;
        return width > 1 ? text[..(width - 1)] + "…" : text[..width];
    }
}
