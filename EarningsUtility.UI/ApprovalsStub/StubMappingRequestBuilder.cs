namespace EarningsUtility.UI.ApprovalsStub;

/// <summary>
/// Builds the das-api-stub mapping request for PUT approvals/{learningKey} — the only route
/// ApprovalsController exposes in das-commitments (POST/DELETE were removed in commit
/// fa2dae377, 2026-08-19). The stub response body is never inspected by das-commitments (it's
/// what WE hand back to the caller); what varies per scenario is which change kind(s) are being
/// mocked and the outcome chosen for each. Per the swagger contract (confirmed by the other
/// team's lead dev, 2026-09-25), each field is decided independently — Price and StartDate in
/// the same call can carry different ApprovalStatus values — so outcomes are supplied per kind,
/// not one atomic status for the whole response. See tickets/upcoming/approvals-integration.md.
/// </summary>
public static class StubMappingRequestBuilder
{
    // A single representative schedule, hardcoded — the mock only needs to demonstrate the
    // "prices echoes what was submitted, no per-entry approvalStatus" shape; testers aren't
    // driving real price amounts through this tool today. Matches the worked example in
    // tickets/upcoming/approvals-integration.md so the doc and the tool agree.
    private static readonly IReadOnlyList<PriceRecord> SamplePriceSchedule = new[]
    {
        new PriceRecord { TrainingPrice = "15000.00", AssessmentPrice = "2000.00", EffectiveFrom = "2026-08-01" }
    };

    public static StubMappingRequest Build(Guid learningKey, IReadOnlyDictionary<CocChangeKind, CocOutcome> outcomes)
    {
        // Leading slash matters: das-api-stub matches on the literal request path, and a real
        // incoming request path always starts with "/".
        var targetUrl = Uri.EscapeDataString($"/approvals/{learningKey}");

        var changes = outcomes
            .OrderBy(kv => kv.Key)
            .Select(kv => new ApprovalFieldChange
            {
                ChangeType = kv.Key.ToChangeType(),
                ApprovalStatus = kv.Value.ToApprovalStatus()
            })
            .ToList();

        return new StubMappingRequest
        {
            SaveUrl = $"api-stub/save?httpMethod=Put&url={targetUrl}&httpStatusCode=201",
            Body = new ApprovalsResult
            {
                Changes = changes,
                Prices = outcomes.ContainsKey(CocChangeKind.Price) ? SamplePriceSchedule : null
            }
        };
    }
}
