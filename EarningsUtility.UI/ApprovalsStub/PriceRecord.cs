using System.Text.Json.Serialization;

namespace EarningsUtility.UI.ApprovalsStub;

/// <summary>
/// One entry in an ApprovalsResult's "prices" array — a plain echo of what was submitted in the
/// request's priceRecords, per the other team's lead dev (2026-09-25): "prices in the response
/// is that same schedule, so you can see what was submitted. It does not have its own
/// approvalStatus." The verdict for the whole schedule lives instead as the single aggregate
/// "Price" entry in ApprovalsResult.Changes.
/// </summary>
public class PriceRecord
{
    [JsonPropertyName("trainingPrice")]
    public required string TrainingPrice { get; init; }

    [JsonPropertyName("assessmentPrice")]
    public required string AssessmentPrice { get; init; }

    [JsonPropertyName("effectiveFrom")]
    public required string EffectiveFrom { get; init; }
}
