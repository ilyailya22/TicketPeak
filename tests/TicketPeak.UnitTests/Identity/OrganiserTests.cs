using Shouldly;
using TicketPeak.Modules.Identity.Domain;
using Xunit;

namespace TicketPeak.UnitTests.Identity;

public sealed class OrganiserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_WithoutAName_IsRefused(string? name)
    {
        Organiser.Create(NewId(), name, "events@example.com").Error.ShouldBe(IdentityErrors.NameRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no-at-sign.example.com")]
    [InlineData("@example.com")]
    [InlineData("events@")]
    [InlineData("events@box@example.com")]
    public void Create_WithSomethingThatIsNotAnEmailAddress_IsRefused(string? email)
    {
        Organiser.Create(NewId(), "Night Owl Promotions", email).Error.ShouldBe(IdentityErrors.InvalidEmail);
    }

    [Fact]
    public void Create_WhenValid_TrimsNameAndEmail()
    {
        Organiser organiser = Organiser.Create(NewId(), "  Night Owl Promotions ", " events@example.com ").Value;

        organiser.Name.ShouldBe("Night Owl Promotions");
        organiser.ContactEmail.ShouldBe("events@example.com");
    }

    private static OrganiserId NewId() => new(Guid.NewGuid());
}
