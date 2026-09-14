using Autofac.Extensions.DependencyInjection;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace TicketPeak.Api.Tests;

/// <summary>
/// Proves the composition root is what ADR 0006 says it is. A host that silently fell back to
/// the built-in container would still boot and still answer /hello.
/// </summary>
public sealed class CompositionTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public void Services_WhenHostBuilds_AreProvidedByAutofac()
    {
        factory.Services.ShouldBeOfType<AutofacServiceProvider>();
    }

    [Fact]
    public void Mediator_WhenResolvedFromARequestScope_IsRegistered()
    {
        using IServiceScope scope = factory.Services.CreateScope();

        scope.ServiceProvider.GetService<IMediator>().ShouldNotBeNull();
    }
}
