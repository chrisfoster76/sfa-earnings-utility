namespace EarningsUtility.UI.ApprovalsStub;

/// <summary>
/// The kinds of Change-of-Circs change the mock response can represent — initially just the two
/// change types Learning inner can actually detect/surface today (or will first). See
/// tickets/upcoming/approvals-integration.md's change-type-trigger table.
/// </summary>
public enum CocChangeKind
{
    Price,
    StartDate
}

public static class CocChangeKindOptions
{
    public static readonly (string Label, CocChangeKind Value)[] All =
    {
        ("Price (TNP1 + TNP2)", CocChangeKind.Price),
        ("StartDate", CocChangeKind.StartDate)
    };
}

public static class CocChangeKindMapper
{
    /// <summary>
    /// Per the swagger contract (confirmed by the other team's lead dev, 2026-09-25): Price
    /// components (TNP1/TNP2) are never surfaced individually in the response — the whole
    /// submitted price schedule (priceRecords) gets exactly one verdict, returned as a single
    /// aggregate "Price" entry in the response's changes array. Contrast with the live
    /// das-commitments implementation today, which still returns TNP1/TNP2 separately —
    /// see tickets/upcoming/approvals-integration.md.
    /// </summary>
    public static string ToChangeType(this CocChangeKind kind) => kind switch
    {
        CocChangeKind.Price => "Price",
        CocChangeKind.StartDate => "StartDate",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    /// <summary>
    /// The LearningChangeApprovedEvent/LearningChangeRejectedEvent's Changes dictionary keys a
    /// kind maps to on the real, live das-commitments implementation today —
    /// ProcessApprenticeshipApprovalCommandHandler.MapToLearningFieldName maps TNP1 ->
    /// TrainingPrice, TNP2 -> AssessmentPrice (anything else passes through as-is), so Price
    /// expands to two distinct field names while StartDate is one. Used to offer a pick-list
    /// for the "changed field name" prompt in the CoC event publisher (CocEventPrompts) instead
    /// of free text, since these are the only fields this tool's mapping-setup screen mocks
    /// today — see tickets/upcoming/approvals-integration.md.
    /// </summary>
    public static IReadOnlyList<string> ToEventFieldNames(this CocChangeKind kind) => kind switch
    {
        CocChangeKind.Price => new[] { "TrainingPrice", "AssessmentPrice" },
        CocChangeKind.StartDate => new[] { "StartDate" },
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    public static bool TryParse(string? value, out CocChangeKind kind)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "price":
                kind = CocChangeKind.Price;
                return true;
            case "startdate":
            case "start-date":
                kind = CocChangeKind.StartDate;
                return true;
            default:
                kind = default;
                return false;
        }
    }

    /// <summary>Parses a comma/plus-separated list of change kinds, e.g. "Price,StartDate".</summary>
    public static bool TryParseMany(string? value, out HashSet<CocChangeKind> kinds)
    {
        kinds = new HashSet<CocChangeKind>();
        if (string.IsNullOrWhiteSpace(value))
            return false;

        foreach (var part in value.Split(new[] { ',', '+' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!TryParse(part, out var kind))
            {
                kinds.Clear();
                return false;
            }
            kinds.Add(kind);
        }

        return kinds.Count > 0;
    }
}
