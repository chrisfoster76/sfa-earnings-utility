namespace EarningsUtility.UI.ApprovalsStub;

/// <summary>
/// The three verdicts das-commitments' ApprovalsController can hand back from
/// PUT approvals/{learningKey} — see tickets/upcoming/FLP-2092.md.
/// </summary>
public enum CocOutcome
{
    AutoApproved,
    AutoRejected,
    Pending
}

public static class CocOutcomeOptions
{
    public static readonly (string Label, CocOutcome Value)[] All =
    {
        ("Automatic approval (autoApproved)", CocOutcome.AutoApproved),
        ("Automatic rejection (autoRejected)", CocOutcome.AutoRejected),
        ("Pending employer approval (employerApprovalRequired)", CocOutcome.Pending)
    };
}

public static class CocOutcomeMapper
{
    /// <summary>
    /// Wire-format casing per the swagger contract (see tickets/upcoming/approvals-integration.md,
    /// "Swagger-aligned CoC payload examples") — camelCase, and "employerApprovalRequired"
    /// ("Required", not "Requested" as the live das-commitments implementation currently returns
    /// it). Confirmed directly by the other team's lead dev, 2026-09-25.
    /// </summary>
    public static string ToApprovalStatus(this CocOutcome outcome) => outcome switch
    {
        CocOutcome.AutoApproved => "autoApproved",
        CocOutcome.AutoRejected => "autoRejected",
        CocOutcome.Pending => "employerApprovalRequired",
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
    };

    public static bool TryParse(string? value, out CocOutcome outcome)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "approved":
            case "auto-approved":
            case "autoapproved":
                outcome = CocOutcome.AutoApproved;
                return true;
            case "rejected":
            case "auto-rejected":
            case "autorejected":
                outcome = CocOutcome.AutoRejected;
                return true;
            case "pending":
                outcome = CocOutcome.Pending;
                return true;
            default:
                outcome = default;
                return false;
        }
    }
}
