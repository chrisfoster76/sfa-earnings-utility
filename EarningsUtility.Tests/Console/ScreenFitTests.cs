using EarningsUtility.UI;

namespace EarningsUtility.Tests.Console;

public class ScreenFitTests
{
    [Theory]
    [InlineData(5, 2, 10)]
    [InlineData(10, 9, 10)]
    public void FitList_shows_every_item_when_they_fit(int itemCount, int selected, int rows)
    {
        Assert.Equal(new ListWindow(0, itemCount, 0, 0), ScreenFit.FitList(itemCount, selected, rows));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public void FitList_near_the_top_shows_the_first_items_and_a_line_for_the_rest(int selected)
    {
        Assert.Equal(new ListWindow(0, 9, 0, 11), ScreenFit.FitList(20, selected, 10));
    }

    [Theory]
    [InlineData(11)]
    [InlineData(19)]
    public void FitList_near_the_bottom_shows_the_last_items_and_a_line_for_those_above(int selected)
    {
        Assert.Equal(new ListWindow(11, 9, 11, 0), ScreenFit.FitList(20, selected, 10));
    }

    [Fact]
    public void FitList_in_the_middle_keeps_a_line_for_each_side()
    {
        Assert.Equal(new ListWindow(7, 8, 7, 5), ScreenFit.FitList(20, 10, 10));
    }

    [Fact]
    public void FitList_always_shows_the_selected_item_within_the_rows()
    {
        for (var rows = 3; rows <= 12; rows++)
        for (var selected = 0; selected < 20; selected++)
        {
            var window = ScreenFit.FitList(20, selected, rows);
            var usedRows = window.Count + (window.HiddenAbove > 0 ? 1 : 0) + (window.HiddenBelow > 0 ? 1 : 0);

            Assert.InRange(selected, window.First, window.First + window.Count - 1);
            Assert.True(usedRows <= rows, $"rows {rows}, selected {selected}: used {usedRows}");
            Assert.Equal(window.First, window.HiddenAbove);
            Assert.Equal(20, window.First + window.Count + window.HiddenBelow);
        }
    }

    [Fact]
    public void FitList_needs_at_least_three_rows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ScreenFit.FitList(20, 0, 2));
    }

    [Fact]
    public void Cap_keeps_every_item_when_they_fit()
    {
        Assert.Equal(new[] { "a", "b", "c" }, ScreenFit.Cap(new[] { "a", "b", "c" }, 3, n => $"{n} more"));
    }

    [Fact]
    public void Cap_replaces_the_items_that_do_not_fit_with_one_overflow_item()
    {
        Assert.Equal(new[] { "a", "b", "3 more" }, ScreenFit.Cap(new[] { "a", "b", "c", "d", "e" }, 3, n => $"{n} more"));
    }

    [Fact]
    public void Cap_to_one_item_is_only_the_overflow_item()
    {
        Assert.Equal(new[] { "2 more" }, ScreenFit.Cap(new[] { "a", "b" }, 1, n => $"{n} more"));
    }

    [Fact]
    public void Cap_needs_room_for_at_least_one_item()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ScreenFit.Cap(new[] { "a" }, 0, n => $"{n} more"));
    }

    [Theory]
    [InlineData("short", 10, "short")]
    [InlineData("exactly10!", 10, "exactly10!")]
    [InlineData("much too long", 8, "much to…")]
    [InlineData("ab", 1, "a")]
    public void Truncate_shortens_text_wider_than_the_width(string text, int width, string expected)
    {
        Assert.Equal(expected, ScreenFit.Truncate(text, width));
    }
}
