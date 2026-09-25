using System.Net.Http.Json;

namespace EarningsUtility.UI.ApprovalsStub;

public class ApprovalsStubClient(HttpClient httpClient)
{
    /// <summary>Per-kind outcomes — each change kind in the mock can carry its own verdict (see
    /// StubMappingRequestBuilder). For a uniform outcome across every kind (e.g. the CLI
    /// one-shot mode's single --outcome flag), build the dictionary with the same value repeated.</summary>
    public async Task RegisterApproval(Guid learningKey, IReadOnlyDictionary<CocChangeKind, CocOutcome> outcomes)
    {
        var request = StubMappingRequestBuilder.Build(learningKey, outcomes);

        var response = await httpClient.PostAsJsonAsync(request.SaveUrl, request.Body);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<MappingData>> FindMappings(string urlFragment)
    {
        var results = await httpClient.GetFromJsonAsync<List<MappingData>>(
            $"api-stub/find?url={Uri.EscapeDataString(urlFragment)}");

        return results ?? new List<MappingData>();
    }

    public async Task DeleteMapping(string httpMethod, string url)
    {
        // url is expected verbatim as returned by FindMappings (already has its leading "/") —
        // escaped exactly once here, not re-derived by the caller.
        var response = await httpClient.DeleteAsync(
            $"api-stub/delete?httpMethod={httpMethod}&url={Uri.EscapeDataString(url)}");
        response.EnsureSuccessStatusCode();
    }

    public async Task RefreshAsync()
    {
        var response = await httpClient.GetAsync("api-stub/refresh");
        response.EnsureSuccessStatusCode();
    }
}
