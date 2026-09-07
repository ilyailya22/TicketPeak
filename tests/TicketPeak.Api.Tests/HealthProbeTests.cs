using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Xunit;

namespace TicketPeak.Api.Tests;

/// <summary>
/// The AppHost gates the <c>api</c> resource on <c>/health/ready</c>, so a regression in these
/// endpoints would stop `aspire run` from ever reporting the service ready.
/// </summary>
public sealed class HealthProbeTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Probe_WhenHostIsHealthy_ReturnsOk(string path)
    {
        HttpResponseMessage response = await _client.GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .ShouldBe("Healthy");
    }
}
