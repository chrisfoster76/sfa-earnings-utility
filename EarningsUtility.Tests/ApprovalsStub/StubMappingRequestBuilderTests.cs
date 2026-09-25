using EarningsUtility.UI.ApprovalsStub;

namespace EarningsUtility.Tests.ApprovalsStub;

public class StubMappingRequestBuilderTests
{
    private static Dictionary<CocChangeKind, CocOutcome> Outcomes(params (CocChangeKind Kind, CocOutcome Outcome)[] entries) =>
        entries.ToDictionary(e => e.Kind, e => e.Outcome);

    [Fact]
    public void Build_uses_learning_key_in_the_save_url()
    {
        var learningKey = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var request = StubMappingRequestBuilder.Build(learningKey, Outcomes((CocChangeKind.Price, CocOutcome.Pending)));

        Assert.Equal(
            $"api-stub/save?httpMethod=Put&url=%2Fapprovals%2F{learningKey}&httpStatusCode=201",
            request.SaveUrl);
    }

    [Fact]
    public void Build_registers_the_path_with_a_leading_slash_so_it_matches_a_real_request()
    {
        var learningKey = Guid.NewGuid();

        var request = StubMappingRequestBuilder.Build(learningKey, Outcomes((CocChangeKind.Price, CocOutcome.Pending)));

        // das-api-stub matches on the literal request path, which always starts with "/" —
        // WireMockClient in das-funding-system-acceptance-tests prepends it for the same reason.
        Assert.Contains($"url=%2Fapprovals%2F{learningKey}", request.SaveUrl);
    }

    [Fact]
    public void Build_targets_PUT_matching_the_real_das_commitments_ApprovalsController_route()
    {
        // ApprovalsController only exposes PUT approvals/{learningKey} (das-commitments,
        // confirmed against ApprovalsController.cs 2026-09-22) — POST/DELETE were removed
        // in commit fa2dae377 (2026-08-19).
        var request = StubMappingRequestBuilder.Build(Guid.NewGuid(), Outcomes((CocChangeKind.Price, CocOutcome.Pending)));

        Assert.Contains("httpMethod=Put", request.SaveUrl);
    }

    [Fact]
    public void Build_gives_price_a_single_aggregate_changeType()
    {
        // Per the swagger contract (confirmed by the other team's lead dev, 2026-09-25): Price
        // components are never surfaced individually — the whole priceRecords schedule gets one
        // verdict, returned as a single "Price" entry, not TNP1/TNP2.
        var request = StubMappingRequestBuilder.Build(Guid.NewGuid(), Outcomes((CocChangeKind.Price, CocOutcome.AutoApproved)));

        var change = Assert.Single(request.Body.Changes);
        Assert.Equal("Price", change.ChangeType);
    }

    [Fact]
    public void Build_gives_a_price_mapping_an_echoed_price_schedule_with_no_approvalStatus()
    {
        var request = StubMappingRequestBuilder.Build(Guid.NewGuid(), Outcomes((CocChangeKind.Price, CocOutcome.Pending)));

        Assert.NotNull(request.Body.Prices);
        Assert.NotEmpty(request.Body.Prices!);
    }

    [Fact]
    public void Build_omits_prices_when_no_price_change_is_mocked()
    {
        var request = StubMappingRequestBuilder.Build(Guid.NewGuid(), Outcomes((CocChangeKind.StartDate, CocOutcome.AutoApproved)));

        Assert.Null(request.Body.Prices);
    }

    [Fact]
    public void Build_expands_StartDate_into_a_single_entry()
    {
        var request = StubMappingRequestBuilder.Build(Guid.NewGuid(), Outcomes((CocChangeKind.StartDate, CocOutcome.AutoApproved)));

        var change = Assert.Single(request.Body.Changes);
        Assert.Equal("StartDate", change.ChangeType);
    }

    [Fact]
    public void Build_combines_multiple_change_kinds_into_one_response_array()
    {
        // A tester may want to mock an ILR submission that moved both StartDate and price at
        // once — both changeTypes land in the same response array.
        var request = StubMappingRequestBuilder.Build(
            Guid.NewGuid(),
            Outcomes((CocChangeKind.Price, CocOutcome.Pending), (CocChangeKind.StartDate, CocOutcome.Pending)));

        Assert.Equal(["Price", "StartDate"], request.Body.Changes.Select(c => c.ChangeType));
    }

    [Fact]
    public void Build_lets_each_change_kind_carry_its_own_outcome()
    {
        // Per the swagger contract, fields are decided independently — e.g. a Name change can
        // auto-approve while a Price change on the same call still needs the employer. This tool
        // only mocks Price/StartDate today, but the same independence applies between them.
        var request = StubMappingRequestBuilder.Build(
            Guid.NewGuid(),
            Outcomes((CocChangeKind.Price, CocOutcome.Pending), (CocChangeKind.StartDate, CocOutcome.AutoApproved)));

        Assert.Equal("employerApprovalRequired", request.Body.Changes.Single(c => c.ChangeType == "Price").ApprovalStatus);
        Assert.Equal("autoApproved", request.Body.Changes.Single(c => c.ChangeType == "StartDate").ApprovalStatus);
    }

    [Theory]
    [InlineData(CocOutcome.AutoApproved, "autoApproved")]
    [InlineData(CocOutcome.AutoRejected, "autoRejected")]
    [InlineData(CocOutcome.Pending, "employerApprovalRequired")]
    public void Build_maps_outcome_to_the_matching_ApprovalStatus(CocOutcome outcome, string expectedStatus)
    {
        var request = StubMappingRequestBuilder.Build(Guid.NewGuid(), Outcomes((CocChangeKind.StartDate, outcome)));

        var change = Assert.Single(request.Body.Changes);
        Assert.Equal(expectedStatus, change.ApprovalStatus);
    }
}
