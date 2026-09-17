using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Shouldly;
using Xunit;

namespace TicketPeak.Api.Tests;

/// <summary>
/// The Phase 1 path end to end over HTTP, through every layer: endpoint, result filter, MediatR
/// pipeline, handler, domain, in-memory store, and the synchronous module APIs inside the checkout
/// boundary. The clock is fake, so hold expiry is exact rather than approximately now.
/// </summary>
public sealed class CheckoutFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly DateTimeOffset _now = new(2026, 11, 2, 18, 0, 0, TimeSpan.Zero);

    private readonly HttpClient _client;

    public CheckoutFlowTests(WebApplicationFactory<Program> factory)
    {
        FakeTimeProvider time = new(_now);
        _client = factory
            .WithWebHostBuilder(host => host.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(time)))
            .CreateClient();
    }

    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Checkout_FromAnEmptyCatalogToAPlacedOrder_PricesEveryTicket()
    {
        Guid eventId = await CreateEventReadyForSaleAsync();
        HoldResponse hold = await PlaceHoldAsync(eventId, [Seat("STALLS", "A", 1), Seat("STALLS", "A", 2)], [Standing("FLOOR", 1)]);

        Guid orderId = await PostForIdAsync("/api/orders", new { customerId = Guid.NewGuid(), eventId, holdId = hold.HoldId });
        OrderView? order = await _client.GetFromJsonAsync<OrderView>($"/api/orders/{orderId}", Cancel);

        order.ShouldNotBeNull();
        order.Status.ShouldBe("Placed");
        order.TotalMinor.ShouldBe(12_000);
        order.Currency.ShouldBe("EUR");
        order.ExpiresAt.ShouldBe(_now.AddMinutes(10));
        order.Tickets.ShouldBe(["STALLS A-1", "STALLS A-2", "FLOOR"], ignoreOrder: true);
    }

    [Fact]
    public async Task PlaceHold_OnASeatSomeoneElseHolds_Returns409NamingTheRule()
    {
        Guid eventId = await CreateEventReadyForSaleAsync();
        await PlaceHoldAsync(eventId, [Seat("STALLS", "A", 5)], []);

        using HttpResponseMessage response = await PostHoldAsync(eventId, [Seat("STALLS", "A", 5)], []);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ProblemCodeAsync(response)).ShouldBe("Inventory.Seat.Unavailable");
    }

    [Fact]
    public async Task PlaceOrder_TwiceForTheSameHold_Returns409()
    {
        Guid eventId = await CreateEventReadyForSaleAsync();
        HoldResponse hold = await PlaceHoldAsync(eventId, [Seat("STALLS", "A", 7)], []);
        var order = new { customerId = Guid.NewGuid(), eventId, holdId = hold.HoldId };
        await PostForIdAsync("/api/orders", order);

        using HttpResponseMessage second = await _client.PostAsJsonAsync("/api/orders", order, Cancel);

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ProblemCodeAsync(second)).ShouldBe("Ordering.Order.HoldAlreadyOrdered");
    }

    [Fact]
    public async Task CreateVenue_WithoutAName_Returns400FromTheValidationPipeline()
    {
        using HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/venues",
            new { name = "", sections = new[] { new { code = "FLOOR", standingCapacity = 100 } } },
            Cancel);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ProblemCodeAsync(response)).ShouldBe("Validation.Failed");
    }

    [Fact]
    public async Task PlaceHold_ForAnEventThatDoesNotExist_Returns404()
    {
        using HttpResponseMessage response = await PostHoldAsync(Guid.NewGuid(), [Seat("STALLS", "A", 1)], []);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ProblemCodeAsync(response)).ShouldBe("Catalog.Event.NotFound");
    }

    private static object Seat(string section, string row, int number) => new { section, row, number };

    private static object Standing(string section, int quantity) => new { section, quantity };

    private static async Task<string?> ProblemCodeAsync(HttpResponseMessage response)
    {
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(Cancel);
        return problem.GetProperty("code").GetString();
    }

    /// <summary>A venue with 10 reserved STALLS seats at 45.00 and 100 FLOOR places at 30.00, published and on sale now.</summary>
    private async Task<Guid> CreateEventReadyForSaleAsync()
    {
        Guid venueId = await PostForIdAsync("/api/venues", new
        {
            name = "Roundhouse",
            sections = new object[]
            {
                new { code = "STALLS", rows = new[] { new { label = "A", seats = 10 } } },
                new { code = "FLOOR", standingCapacity = 100 },
            },
        });

        Guid eventId = await PostForIdAsync("/api/events", new
        {
            organiserId = Guid.NewGuid(),
            venueId,
            title = "Autumn Tour",
            startsAt = _now.AddDays(30),
            currency = "EUR",
        });

        await SendExpectingNoContentAsync(HttpMethod.Put, $"/api/events/{eventId}/prices/STALLS", new { amountMinor = 4500, currency = "EUR" });
        await SendExpectingNoContentAsync(HttpMethod.Put, $"/api/events/{eventId}/prices/FLOOR", new { amountMinor = 3000, currency = "EUR" });
        await SendExpectingNoContentAsync(HttpMethod.Put, $"/api/events/{eventId}/on-sale", new { opensAt = _now.AddHours(-1), closesAt = _now.AddDays(7) });
        await SendExpectingNoContentAsync(HttpMethod.Post, $"/api/events/{eventId}/publish", body: null);
        await SendExpectingNoContentAsync(HttpMethod.Post, $"/api/events/{eventId}/inventory", body: null);

        return eventId;
    }

    private async Task<Guid> PostForIdAsync(string uri, object body)
    {
        using HttpResponseMessage response = await _client.PostAsJsonAsync(uri, body, Cancel);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Cancel));
        return await response.Content.ReadFromJsonAsync<Guid>(Cancel);
    }

    private async Task SendExpectingNoContentAsync(HttpMethod method, string uri, object? body)
    {
        using HttpRequestMessage request = new(method, uri) { Content = body is null ? null : JsonContent.Create(body) };
        using HttpResponseMessage response = await _client.SendAsync(request, Cancel);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync(Cancel));
    }

    private async Task<HoldResponse> PlaceHoldAsync(Guid eventId, object[] seats, object[] standing)
    {
        using HttpResponseMessage response = await PostHoldAsync(eventId, seats, standing);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Cancel));
        HoldResponse? hold = await response.Content.ReadFromJsonAsync<HoldResponse>(Cancel);
        return hold.ShouldNotBeNull();
    }

    private Task<HttpResponseMessage> PostHoldAsync(Guid eventId, object[] seats, object[] standing) =>
        _client.PostAsJsonAsync($"/api/events/{eventId}/holds", new { seats, standing }, Cancel);

    private sealed record HoldResponse(Guid HoldId, DateTimeOffset ExpiresAt);

    private sealed record OrderView(
        Guid OrderId,
        string Status,
        long TotalMinor,
        string Currency,
        DateTimeOffset ExpiresAt,
        IReadOnlyList<string> Tickets);
}
