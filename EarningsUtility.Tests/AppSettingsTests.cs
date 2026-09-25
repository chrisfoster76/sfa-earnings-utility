using EarningsUtility.UI;
using Microsoft.Extensions.Configuration;

namespace EarningsUtility.Tests;

public class AppSettingsTests
{
    [Fact]
    public void Binds_Environments_and_ApprovalsStubBaseUrl_from_json()
    {
        var json = """
            {
              "Environments": { "demo": "das-demo-shared-ns.servicebus.windows.net" },
              "ApprovalsStubBaseUrl": { "demo": "https://demo-stub.apprenticeships.education.gov.uk" }
            }
            """;

        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, json);

            var configuration = new ConfigurationBuilder().AddJsonFile(path, optional: false).Build();
            var settings = configuration.Get<AppSettings>();

            Assert.NotNull(settings);
            Assert.Equal("das-demo-shared-ns.servicebus.windows.net", settings!.Environments["demo"]);
            Assert.Equal("https://demo-stub.apprenticeships.education.gov.uk", settings.ApprovalsStubBaseUrl["demo"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ApprovalsStubBaseUrl_defaults_to_empty_when_section_is_absent()
    {
        var json = """{ "Environments": { "demo": "ns" } }""";

        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, json);

            var configuration = new ConfigurationBuilder().AddJsonFile(path, optional: false).Build();
            var settings = configuration.Get<AppSettings>();

            Assert.NotNull(settings);
            Assert.Empty(settings!.ApprovalsStubBaseUrl);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
