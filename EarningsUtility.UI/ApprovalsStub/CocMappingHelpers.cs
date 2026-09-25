using System.Text.Json;

namespace EarningsUtility.UI.ApprovalsStub;

/// <summary>
/// Shared parsing helpers for das-api-stub's approvals/* mapping data — used by both the
/// mappings-maintenance screen and the CoC-event flow (which offers to build an event from an
/// existing pending mapping rather than typing a learningKey from memory).
/// </summary>
public static class CocMappingHelpers
{
    private static readonly JsonSerializerOptions ParseOptions = new() { PropertyNameCaseInsensitive = true };

    public static List<ApprovalFieldChange>? TryParseChanges(string data)
    {
        try
        {
            // Case-insensitive: mappings under this prefix aren't guaranteed to have been
            // created by this tool (other teams/tests share the same das-api-stub table), and
            // the wire shape is camelCase (JsonPropertyName) while options here just need to be
            // forgiving of whatever's actually stored.
            var result = JsonSerializer.Deserialize<ApprovalsResult>(data, ParseOptions);
            return result?.Changes as List<ApprovalFieldChange> ?? result?.Changes?.ToList();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static IReadOnlyList<PriceRecord>? TryParsePrices(string data)
    {
        try
        {
            return JsonSerializer.Deserialize<ApprovalsResult>(data, ParseOptions)?.Prices;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static ApprovalFieldChange? TryParseFirstChange(string data) => TryParseChanges(data)?.FirstOrDefault();

    public static Guid? ExtractLearningKey(string url)
    {
        var lastSegment = url.TrimEnd('/').Split('/').LastOrDefault();
        return Guid.TryParse(lastSegment, out var key) ? key : null;
    }
}
