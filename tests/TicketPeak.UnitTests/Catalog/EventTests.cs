using Microsoft.Extensions.Time.Testing;
using Shouldly;
using TicketPeak.Modules.Catalog.Domain;
using TicketPeak.Shared.Kernel;
using Xunit;
using static TicketPeak.UnitTests.Catalog.CatalogBuilders;

namespace TicketPeak.UnitTests.Catalog;

public sealed class EventTests
{
    private readonly FakeTimeProvider _time = new(Now);

    [Fact]
    public void Create_WithBlankTitle_IsRefused()
    {
        Event.Create(
            new EventId(Guid.NewGuid()),
            new OrganiserId(Guid.NewGuid()),
            VenueWith(StandardSeatMap()),
            " ",
            Now.AddDays(60),
            CurrencyCode.Create("EUR").Value).Error.ShouldBe(CatalogErrors.EventTitleRequired);
    }

    [Fact]
    public void Create_FromVenue_StartsAsDraftHoldingTheVenuesSeatMap()
    {
        Venue venue = VenueWith(StandardSeatMap());

        Event concert = DraftEventAt(venue);

        concert.Status.ShouldBe(EventStatus.Draft);
        concert.SeatMap.ShouldBeSameAs(venue.SeatMap);
        concert.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void SetPriceTier_ForSectionNotInSeatMap_IsRefused()
    {
        Event concert = DraftEvent();

        concert.SetPriceTier(CodeOf("BALCONY"), PriceOf(2000)).Error.ShouldNotBeNull()
            .Code.ShouldBe("Catalog.Event.UnknownSection");
    }

    [Fact]
    public void SetPriceTier_InAnotherCurrency_IsRefused()
    {
        Event concert = DraftEvent();

        concert.SetPriceTier(CodeOf("FLOOR"), PriceOf(3000, "GBP")).Error.ShouldNotBeNull()
            .Code.ShouldBe("Catalog.Event.CurrencyMismatch");
    }

    [Fact]
    public void SetPriceTier_TwiceForTheSameSection_KeepsOnlyTheLatestPrice()
    {
        Event concert = DraftEvent();

        concert.SetPriceTier(CodeOf("FLOOR"), PriceOf(3000));
        concert.SetPriceTier(CodeOf("floor"), PriceOf(3500));

        concert.PriceTiers.ShouldHaveSingleItem().Value.AmountMinor.ShouldBe(3500);
    }

    [Fact]
    public void ReplaceSeatMap_ThatDropsAPricedSection_IsRefusedAndKeepsTheOldSeatMap()
    {
        Event concert = DraftEvent();
        SeatMap before = concert.SeatMap;
        concert.SetPriceTier(CodeOf("STALLS"), PriceOf(4500));

        Result result = concert.ReplaceSeatMap(SeatMapOf(GeneralAdmissionOf("FLOOR", 800)));

        result.Error.ShouldNotBeNull().Code.ShouldBe("Catalog.Event.SeatMapDropsPricedSection");
        concert.SeatMap.ShouldBeSameAs(before);
    }

    [Fact]
    public void ReplaceSeatMap_KeepingEveryPricedSection_IsAccepted()
    {
        Event concert = DraftEvent();
        concert.SetPriceTier(CodeOf("FLOOR"), PriceOf(3000));
        SeatMap bigger = SeatMapOf(GeneralAdmissionOf("FLOOR", 800), GeneralAdmissionOf("TERRACE", 200));

        concert.ReplaceSeatMap(bigger).IsSuccess.ShouldBeTrue();
        concert.SeatMap.ShouldBeSameAs(bigger);
    }

    [Fact]
    public void ScheduleOnSale_ClosingAfterTheEventStarts_IsRefused()
    {
        Event concert = DraftEvent();

        concert.ScheduleOnSale(WindowOf(Now, concert.StartsAt.AddSeconds(1))).Error
            .ShouldBe(CatalogErrors.OnSaleClosesAfterStart);
    }

    [Fact]
    public void ScheduleOnSale_ClosingExactlyWhenTheEventStarts_IsAccepted()
    {
        Event concert = DraftEvent();

        concert.ScheduleOnSale(WindowOf(Now, concert.StartsAt)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Publish_WithoutAnOnSaleWindow_IsRefused()
    {
        Event concert = DraftEvent();
        concert.SetPriceTier(CodeOf("STALLS"), PriceOf(4500));
        concert.SetPriceTier(CodeOf("FLOOR"), PriceOf(3000));

        concert.Publish(_time).Error.ShouldBe(CatalogErrors.NoOnSaleWindow);
    }

    [Fact]
    public void Publish_WithAnUnpricedSection_IsRefusedNamingThatSection()
    {
        Event concert = DraftEvent();
        concert.SetPriceTier(CodeOf("STALLS"), PriceOf(4500));
        concert.ScheduleOnSale(WindowOf(Now.AddDays(1), Now.AddDays(30)));

        Error error = concert.Publish(_time).Error.ShouldNotBeNull();

        error.Code.ShouldBe("Catalog.Event.UnpricedSection");
        error.Message.ShouldContain("FLOOR");
        concert.Status.ShouldBe(EventStatus.Draft);
    }

    [Fact]
    public void Publish_OnceTheOnSaleWindowHasClosed_IsRefused()
    {
        Event concert = PublishableEvent();
        _time.SetUtcNow(Now.AddDays(30));

        concert.Publish(_time).Error.ShouldBe(CatalogErrors.OnSaleWindowAlreadyClosed);
    }

    [Fact]
    public void Publish_WhenComplete_BecomesPublishedAndRaisesEventPublished()
    {
        Event concert = PublishableEvent();

        concert.Publish(_time).IsSuccess.ShouldBeTrue();

        concert.Status.ShouldBe(EventStatus.Published);
        concert.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<EventPublished>().EventId.ShouldBe(concert.Id);
    }

    [Fact]
    public void Publish_WhenAlreadyPublished_IsRefused()
    {
        Event concert = PublishableEvent();
        concert.Publish(_time);

        concert.Publish(_time).Error.ShouldBe(CatalogErrors.EventNotDraft);
    }

    [Fact]
    public void SetPriceTier_AfterPublishing_IsRefused()
    {
        Event concert = PublishableEvent();
        concert.Publish(_time);

        concert.SetPriceTier(CodeOf("FLOOR"), PriceOf(1)).Error.ShouldBe(CatalogErrors.EventNotDraft);
    }

    [Fact]
    public void ReplaceSeatMap_AfterPublishing_IsRefused()
    {
        Event concert = PublishableEvent();
        concert.Publish(_time);

        concert.ReplaceSeatMap(StandardSeatMap()).Error.ShouldBe(CatalogErrors.EventNotDraft);
    }

    [Fact]
    public void ScheduleOnSale_AfterPublishing_IsRefused()
    {
        Event concert = PublishableEvent();
        concert.Publish(_time);

        concert.ScheduleOnSale(WindowOf(Now.AddDays(2), Now.AddDays(3))).Error.ShouldBe(CatalogErrors.EventNotDraft);
    }

    [Fact]
    public void SetPriceTier_AfterCancelling_IsRefused()
    {
        Event concert = DraftEvent();
        concert.Cancel();

        concert.SetPriceTier(CodeOf("FLOOR"), PriceOf(1)).Error.ShouldBe(CatalogErrors.EventNotDraft);
    }

    [Fact]
    public void Cancel_FromDraft_BecomesCancelledAndRaisesEventCancelled()
    {
        Event concert = DraftEvent();

        concert.Cancel().IsSuccess.ShouldBeTrue();

        concert.Status.ShouldBe(EventStatus.Cancelled);
        concert.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<EventCancelled>().EventId.ShouldBe(concert.Id);
    }

    [Fact]
    public void Cancel_FromPublished_IsAccepted()
    {
        Event concert = PublishableEvent();
        concert.Publish(_time);

        concert.Cancel().IsSuccess.ShouldBeTrue();
        concert.Status.ShouldBe(EventStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_IsRefused()
    {
        Event concert = DraftEvent();
        concert.Cancel();

        concert.Cancel().Error.ShouldBe(CatalogErrors.EventAlreadyCancelled);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(15, true)]
    [InlineData(30, false)]
    public void IsOnSale_WhenPublished_FollowsTheOnSaleWindow(int daysAfterNow, bool expected)
    {
        Event concert = PublishableEvent();
        concert.Publish(_time);

        _time.SetUtcNow(Now.AddDays(daysAfterNow));

        concert.IsOnSale(_time).ShouldBe(expected);
    }

    [Fact]
    public void IsOnSale_WhenStillDraft_IsFalseEvenInsideTheWindow()
    {
        Event concert = PublishableEvent();
        _time.SetUtcNow(Now.AddDays(15));

        concert.IsOnSale(_time).ShouldBeFalse();
    }

    [Fact]
    public void IsOnSale_WhenCancelled_IsFalseEvenInsideTheWindow()
    {
        Event concert = PublishableEvent();
        concert.Publish(_time);
        concert.Cancel();
        _time.SetUtcNow(Now.AddDays(15));

        concert.IsOnSale(_time).ShouldBeFalse();
    }
}
