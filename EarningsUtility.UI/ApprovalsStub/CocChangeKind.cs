namespace EarningsUtility.UI.ApprovalsStub;

/// <summary>
/// The kinds of Change-of-Circs change the mock response can represent — initially just the two
/// change types Learning inner can actually detect/surface today (or will first). See
/// tickets/upcoming/approvals-integration.md's change-type-trigger table.
/// </summary>
public enum CocChangeKind
{
    Price,
    StartDate,
    PlannedEndDate
}

public static class CocChangeKindOptions
{
    public static readonly (string Label, CocChangeKind Value)[] All =
    {
        ("Price (TNP1 + TNP2)", CocChangeKind.Price),
        ("StartDate", CocChangeKind.StartDate),
        ("PlannedEndDate", CocChangeKind.PlannedEndDate)
    };
}

public static class CocChangeKindMapper
{
    /// <summary>
    /// The changeType of a kind's verdict in the Approvals response. The whole price list gets
    /// one "Prices" verdict, matching das-commitments' CocChangeField.
    /// </summary>
    public static string ToChangeType(this CocChangeKind kind) => kind switch
    {
        CocChangeKind.Price => "Prices",
        CocChangeKind.StartDate => "StartDate",
        CocChangeKind.PlannedEndDate => "PlannedEndDate",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    /// <summary>
    /// The ChangeType a kind takes in the approval event's Changes. Price has none: Approvals
    /// sends prices in the event's top-level Prices list instead.
    /// </summary>
    public static IReadOnlyList<string> ToEventFieldNames(this CocChangeKind kind) => kind switch
    {
        CocChangeKind.Price => Array.Empty<string>(),
        CocChangeKind.StartDate => new[] { "StartDate" },
        CocChangeKind.PlannedEndDate => new[] { "PlannedEndDate" },
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    public static bool TryParse(string? value, out CocChangeKind kind)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "prices":
            case "price":
                kind = CocChangeKind.Price;
                return true;
            case "startdate":
            case "start-date":
                kind = CocChangeKind.StartDate;
                return true;
            case "plannedenddate":
            case "planned-end-date":
            case "enddate":
            case "end-date":
                kind = CocChangeKind.PlannedEndDate;
                return true;
            default:
                kind = default;
                return false;
        }
    }

    /// <summary>Parses a comma/plus-separated list of change kinds, e.g. "Price,StartDate,PlannedEndDate".</summary>
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
