using Autofac;
using FluentValidation;
using MediatR;
using TicketPeak.Modules.Inventory.Application;
using TicketPeak.Modules.Inventory.Domain;
using TicketPeak.Modules.Inventory.Infrastructure;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Inventory;

/// <summary>
/// Composition entry point for the Inventory bounded context. The host registers this module and
/// nothing else from it; handlers and validators are found by scanning this assembly, so they stay
/// internal and a new use case needs no wiring.
/// </summary>
public sealed class InventoryModule : Module
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
        builder.RegisterType<InMemoryInventoryStore>().SingleInstance();
        builder.RegisterType<InMemoryInventorySession>().AsSelf().As<IUnitOfWork>().InstancePerLifetimeScope();
        builder.RegisterType<EventInventoryRepository>().As<IEventInventoryRepository>().InstancePerLifetimeScope();

        builder.RegisterType<InventoryApi>().As<IInventoryApi>().InstancePerLifetimeScope();
    }
}
