namespace EarningsUtility.UI.ApprovalsStub;

/// <summary>
/// Mirrors das-api-stub's DataRepository.MappingData shape (HttpMethod/Url/Data/HttpStatusCode) —
/// the extra Azure TableEntity fields (PartitionKey, RowKey, Timestamp, ETag) it also returns are
/// ignored on deserialization.
/// </summary>
public class MappingData
{
    public string HttpMethod { get; set; } = "";
    public string Url { get; set; } = "";
    public string Data { get; set; } = "";
    public int HttpStatusCode { get; set; }
}
