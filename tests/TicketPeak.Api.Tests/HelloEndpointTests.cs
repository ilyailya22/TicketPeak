using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Xunit;

namespace TicketPeak.Api.Tests;

/// <summary>
/// Phase 0 acceptance: the host boots and answers. These tests exist so that "the API
/// responds" is enforced by CI rather than re-checked by hand on every change.
/// </summary>
public sealed class HelloEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetHello_WhenHostIsRunning_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/hello", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHello_WhenHostIsRunning_IdentifiesTheService()
    {
        HelloResponse? body = await _client.GetFromJsonAsync<HelloResponse>(
            "/hello",
            TestContext.Current.CancellationToken);

        body.ShouldNotBeNull();
        body.Service.ShouldBe("TicketPeak");
    }
}
