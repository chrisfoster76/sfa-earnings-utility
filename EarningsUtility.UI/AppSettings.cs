namespace EarningsUtility.UI;

public class AppSettings
{
    public Dictionary<string, string> Environments { get; set; } = new();

    /// <summary>
    /// das-api-stub management API base URL per environment (e.g. "demo" -> "https://demo-stub.apprenticeships.education.gov.uk").
    /// Deliberately has no entry for "local" — there's no deployed stub for it, only an
    /// in-process one inside das-api-stub's own test suite.
    /// </summary>
    public Dictionary<string, string> ApprovalsStubBaseUrl { get; set; } = new();

    /// <summary>
    /// The das-api-stub URL fragment this tool's find/delete actions scope themselves to.
    /// This tool only ever sets up and tears down "approvals" mappings, so there's no per-run
    /// filter to type — just a fixed prefix, overridable here if the route is ever renamed.
    /// </summary>
    public string ApprovalsUrlPrefix { get; set; } = "approvals";
}
