using System.Text.Json.Serialization;

namespace EarningsUtility.UI.ApprovalsStub;

/// <summary>
/// One entry in an ApprovalsResult's "changes" array — matches the swagger-aligned contract
/// (see tickets/upcoming/approvals-integration.md, "Swagger-aligned CoC payload examples").
/// Property names are explicit camelCase [JsonPropertyName]s, not relying on serializer
/// defaults, since this is the wire shape a real caller would deserialize.
/// </summary>
public class ApprovalFieldChange
{
    [JsonPropertyName("changeType")]
    public required string ChangeType { get; init; }

    [JsonPropertyName("approvalStatus")]
    public required string ApprovalStatus { get; init; }
}
