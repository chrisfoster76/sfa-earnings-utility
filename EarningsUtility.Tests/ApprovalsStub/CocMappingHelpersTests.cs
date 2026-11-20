using EarningsUtility.UI.ApprovalsStub;

namespace EarningsUtility.Tests.ApprovalsStub;

public class CocMappingHelpersTests
{
    private static string Data(params string[] changeTypes) =>
        "{\"changes\":[" + string.Join(",", changeTypes.Select(t => $"{{\"changeType\":\"{t}\",\"approvalStatus\":\"autoApproved\"}}")) + "]}";

    [Theory]
    [InlineData("Price", "Prices")]
    [InlineData("StartDate", "StartDate")]
    [InlineData("PlannedEndDate", "PlannedEndDate")]
    [InlineData("Price & StartDate", "Prices", "StartDate")]
    [InlineData("Price & PlannedEndDate", "Prices", "PlannedEndDate")]
    [InlineData("StartDate & PlannedEndDate", "StartDate", "PlannedEndDate")]
    [InlineData("Price & StartDate & PlannedEndDate", "Prices", "StartDate", "PlannedEndDate")]
    [InlineData("StartDate & PlannedEndDate", "PlannedEndDate", "StartDate")]
    public void DescribeChangeKinds_names_the_known_kinds_in_a_fixed_order(string expected, params string[] changeTypes)
    {
        Assert.Equal(expected, CocMappingHelpers.DescribeChangeKinds(Data(changeTypes)));
    }

    [Fact]
    public void DescribeChangeKinds_lists_unknown_change_types_as_they_are()
    {
        Assert.Equal("Price, DateOfBirth", CocMappingHelpers.DescribeChangeKinds(Data("Price", "DateOfBirth")));
    }

    [Fact]
    public void DescribeChangeKinds_is_Unknown_when_the_data_is_not_a_verdict()
    {
        Assert.Equal("Unknown", CocMappingHelpers.DescribeChangeKinds("not json"));
    }
}
