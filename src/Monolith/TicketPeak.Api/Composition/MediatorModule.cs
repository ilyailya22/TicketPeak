using Autofac;
using MediatR;

namespace TicketPeak.Api.Composition;

/// <summary>
/// Registers the mediator itself. Handlers are registered by each bounded context's own module,
/// so the host never needs to know which handlers exist.
/// </summary>
internal sealed class MediatorModule : Module
{
    protected override void Load(ContainerBuilder builder) =>
        builder.RegisterType<Mediator>()
            .As<IMediator>()
            .As<ISender>()
            .As<IPublisher>()
            .InstancePerLifetimeScope();
}
