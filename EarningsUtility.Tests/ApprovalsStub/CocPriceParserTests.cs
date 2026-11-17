using EarningsUtility.UI.ApprovalsStub;

namespace EarningsUtility.Tests.ApprovalsStub;

public class CocPriceParserTests
{
    [Theory]
    [InlineData("2025-09-01:7200:1800", "2025-09-01", 7200, 1800)]
    [InlineData(" 2025-09-01 : 7200.50 : 0 ", "2025-09-01", 7200.50, 0)]
    public void TryParseRow_reads_a_valid_row(string input, string effectiveFrom, decimal training, decimal assessment)
    {
        var ok = CocPriceParser.TryParseRow(input, out var price, out var error);

        Assert.True(ok, error);
        Assert.Equal(effectiveFrom, price!.EffectiveFrom);
        Assert.Equal(training, price.TrainingPrice);
        Assert.Equal(assessment, price.AssessmentPrice);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2025-09-01:7200")]
    [InlineData("2025-09-01:7200:1800:5")]
    [InlineData("01/09/2025:7200:1800")]
    [InlineData("2025-9-1:7200:1800")]
    [InlineData("2025-02-30:7200:1800")]
    [InlineData("2025-09-01:abc:1800")]
    [InlineData("2025-09-01:7200:")]
    [InlineData("2025-09-01:-1:1800")]
    [InlineData("2025-09-01:7200:-1")]
    [InlineData("2025-09-01:7,200:1800")]
    public void TryParseRow_rejects_an_invalid_row(string? input)
    {
        var ok = CocPriceParser.TryParseRow(input, out var price, out var error);

        Assert.False(ok);
        Assert.Null(price);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void TryParseList_reads_comma_separated_rows_in_order()
    {
        var ok = CocPriceParser.TryParseList("2025-08-01:6000:1500, 2025-09-01:7200:1800", out var prices, out var error);

        Assert.True(ok, error);
        Assert.Collection(prices,
            p => Assert.Equal(("2025-08-01", 6000m, 1500m), (p.EffectiveFrom, p.TrainingPrice!.Value, p.AssessmentPrice!.Value)),
            p => Assert.Equal(("2025-09-01", 7200m, 1800m), (p.EffectiveFrom, p.TrainingPrice!.Value, p.AssessmentPrice!.Value)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2025-08-01:6000:1500,")]
    [InlineData("2025-08-01:6000:1500,bad")]
    public void TryParseList_rejects_an_empty_or_invalid_list(string? input)
    {
        var ok = CocPriceParser.TryParseList(input, out var prices, out var error);

        Assert.False(ok);
        Assert.Empty(prices);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }
}
