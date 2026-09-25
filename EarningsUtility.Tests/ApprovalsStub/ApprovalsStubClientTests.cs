using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EarningsUtility.UI.ApprovalsStub;

namespace EarningsUtility.Tests.ApprovalsStub;

public class ApprovalsStubClientTests
{
    private static (ApprovalsStubClient Client, FakeHttpMessageHandler Handler) CreateClient(
        Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new FakeHttpMessageHandler(responder);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://demo-stub.apprenticeships.education.gov.uk") };
        return (new ApprovalsStubClient(httpClient), handler);
    }

    [Fact]
    public async Task RegisterApproval_POSTs_to_api_stub_save_with_Put_registered()
    {
        var (client, handler) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK));

        await client.RegisterApproval(Guid.NewGuid(), new Dictionary<CocChangeKind, CocOutcome> { [CocChangeKind.Price] = CocOutcome.Pending });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("api-stub/save", handler.LastRequest.RequestUri!.ToString());
        Assert.Contains("httpMethod=Put", handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task FindMappings_GETs_api_stub_find_with_the_filter_escaped()
    {
        var (client, handler) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<MappingData>
            {
                new() { HttpMethod = "Put", Url = "/approvals/11111111-1111-1111-1111-111111111111", Data = "[]", HttpStatusCode = 201 }
            })
        });

        var results = await client.FindMappings("approvals");

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("api-stub/find?url=approvals", handler.LastRequest.RequestUri!.ToString());
        var mapping = Assert.Single(results);
        Assert.Equal("/approvals/11111111-1111-1111-1111-111111111111", mapping.Url);
        Assert.Equal("Put", mapping.HttpMethod);
    }

    [Fact]
    public async Task FindMappings_returns_empty_list_when_nothing_matches()
    {
        var (client, _) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<MappingData>())
        });

        var results = await client.FindMappings("nonexistent");

        Assert.Empty(results);
    }

    [Fact]
    public async Task DeleteMapping_DELETEs_api_stub_delete_with_method_and_escaped_url()
    {
        var (client, handler) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK));

        // Passed straight through as returned by FindMappings (already has the leading slash) —
        // must not be re-derived or double-escaped, per the leading-slash bug found this session.
        await client.DeleteMapping("Put", "/approvals/11111111-1111-1111-1111-111111111111");

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        var uri = handler.LastRequest.RequestUri!.ToString();
        Assert.Contains("api-stub/delete", uri);
        Assert.Contains("httpMethod=Put", uri);
        Assert.Contains("url=%2Fapprovals%2F11111111-1111-1111-1111-111111111111", uri);
    }

    [Fact]
    public async Task RefreshAsync_GETs_api_stub_refresh()
    {
        var (client, handler) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK));

        await client.RefreshAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("api-stub/refresh", handler.LastRequest.RequestUri!.ToString());
    }
}
