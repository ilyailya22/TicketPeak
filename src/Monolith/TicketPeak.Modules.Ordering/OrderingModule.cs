using Autofac;
using FluentValidation;
using MediatR;
using TicketPeak.Modules.Ordering.Domain;
using TicketPeak.Modules.Ordering.Infrastructure;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Ordering;

/// <summary>
/// Composition entry point for the Ordering bounded context. The host registers this module and
/// nothing else from it; handlers and validators are found by scanning this assembly, so they stay
/// internal and a new use case needs no wiring.
/// </summary>
public sealed class OrderingModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Separate scans on purpose: chained AsClosedTypesOf calls filter cumulatively, so a single
        // chain would register only types closing every interface at once, which is none.
        builder.RegisterAssemblyTypes(ThisAssembly)
            .AsClosedTypesOf(typeof(IRequestHandler<,>))
            .InstancePerLifetimeScope();

        builder.RegisterAssemblyTypes(ThisAssembly)
            .AsClosedTypesOf(typeof(IRequestHandler<>))
            .InstancePerLifetimeScope();

        builder.RegisterAssemblyTypes(ThisAssembly)
            .AsClosedTypesOf(typeof(INotificationHandler<>))
            .InstancePerLifetimeScope();

        builder.RegisterAssemblyTypes(ThisAssembly)
            .AsClosedTypesOf(typeof(IValidator<>))
            .InstancePerLifetimeScope();

        // Phase 1 in-memory persistence, replaced by EF Core in Phase 2. The store outlives
        // requests; the session is the per-request unit of work the pipeline commits.
        builder.RegisterType<InMemoryOrderingStore>().SingleInstance();
        builder.RegisterType<InMemoryOrderingSession>().AsSelf().As<IUnitOfWork>().InstancePerLifetimeScope();
        builder.RegisterType<OrderRepository>().As<IOrderRepository>().InstancePerLifetimeScope();
    }
}
