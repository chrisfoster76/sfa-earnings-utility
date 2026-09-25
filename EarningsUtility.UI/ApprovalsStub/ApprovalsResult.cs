using System.Text.Json.Serialization;

namespace EarningsUtility.UI.ApprovalsStub;

/// <summary>
/// The response body PUT approvals/{learningKey} returns — swagger-aligned shape (see
/// tickets/upcoming/approvals-integration.md, "Swagger-aligned CoC payload examples"), not the
/// live das-commitments implementation's current bare array. Prices is only populated when a
/// Price change is being mocked; omitted (not an empty array) otherwise, matching the request
/// side where priceRecords is simply absent for a no-price change.
/// </summary>
public class ApprovalsResult
{
    [JsonPropertyName("changes")]
    public required IReadOnlyList<ApprovalFieldChange> Changes { get; init; }

    [JsonPropertyName("prices")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<PriceRecord>? Prices { get; init; }
}
