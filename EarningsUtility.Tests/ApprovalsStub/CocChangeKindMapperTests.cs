using EarningsUtility.UI.ApprovalsStub;

namespace EarningsUtility.Tests.ApprovalsStub;

public class CocChangeKindMapperTests
{
    [Fact]
    public void Price_has_no_event_field_names_because_it_goes_in_top_level_prices()
    {
        Assert.Empty(CocChangeKind.Price.ToEventFieldNames());
    }

    [Fact]
    public void Price_verdict_uses_the_Prices_changeType()
    {
        Assert.Equal("Prices", CocChangeKind.Price.ToChangeType());
    }

    [Theory]
    [InlineData("Prices")]
    [InlineData("prices")]
    [InlineData("Price")]
    public void TryParse_accepts_Prices_and_the_old_Price_spelling(string value)
    {
        Assert.True(CocChangeKindMapper.TryParse(value, out var kind));
        Assert.Equal(CocChangeKind.Price, kind);
    }

    [Fact]
    public void StartDate_maps_to_the_StartDate_field()
    {
        Assert.Equal(new[] { "StartDate" }, CocChangeKind.StartDate.ToEventFieldNames());
    }
    [Fact]
    public void PlannedEndDate_verdict_uses_the_PlannedEndDate_changeType()
    {
        Assert.Equal("PlannedEndDate", CocChangeKind.PlannedEndDate.ToChangeType());
    }

    [Fact]
    public void PlannedEndDate_maps_to_the_PlannedEndDate_field()
    {
        Assert.Equal(new[] { "PlannedEndDate" }, CocChangeKind.PlannedEndDate.ToEventFieldNames());
    }

    [Theory]
    [InlineData("PlannedEndDate")]
    [InlineData("plannedenddate")]
    [InlineData("planned-end-date")]
    [InlineData("EndDate")]
    [InlineData("end-date")]
    public void TryParse_accepts_PlannedEndDate_spellings(string value)
    {
        Assert.True(CocChangeKindMapper.TryParse(value, out var kind));
        Assert.Equal(CocChangeKind.PlannedEndDate, kind);
    }

    [Fact]
    public void TryParseMany_accepts_PlannedEndDate_alongside_other_kinds()
    {
        Assert.True(CocChangeKindMapper.TryParseMany("startdate,plannedenddate", out var kinds));
        Assert.Equal(new HashSet<CocChangeKind> { CocChangeKind.StartDate, CocChangeKind.PlannedEndDate }, kinds);
    }

    [Fact]
    public void PlannedEndDate_is_offered_as_a_change_kind()
    {
        Assert.Contains(CocChangeKindOptions.All, o => o.Value == CocChangeKind.PlannedEndDate);
    }
}
