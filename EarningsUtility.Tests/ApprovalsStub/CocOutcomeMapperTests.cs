using EarningsUtility.UI.ApprovalsStub;

namespace EarningsUtility.Tests.ApprovalsStub;

public class CocOutcomeMapperTests
{
    [Theory]
    [InlineData("approved", CocOutcome.AutoApproved)]
    [InlineData("Approved", CocOutcome.AutoApproved)]
    [InlineData("rejected", CocOutcome.AutoRejected)]
    [InlineData("pending", CocOutcome.Pending)]
    public void TryParse_recognises_expected_values(string input, CocOutcome expected)
    {
        var result = CocOutcomeMapper.TryParse(input, out var outcome);

        Assert.True(result);
        Assert.Equal(expected, outcome);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-real-outcome")]
    public void TryParse_returns_false_for_unrecognised_values(string? input)
    {
        var result = CocOutcomeMapper.TryParse(input, out _);

        Assert.False(result);
    }
}
