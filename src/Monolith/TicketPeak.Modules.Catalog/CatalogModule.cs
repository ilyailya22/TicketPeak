using Autofac;
using MediatR;

namespace TicketPeak.Modules.Catalog;

/// <summary>
/// Composition entry point for the Catalog bounded context. The host registers this module and
/// nothing else from it; handlers are found by scanning this assembly, so they stay internal
/// and a new use case needs no wiring.
/// </summary>
public sealed class CatalogModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Three separate scans on purpose: chained AsClosedTypesOf calls filter cumulatively, so
        // a single chain would register only types closing all three interfaces — i.e. none.
        builder.RegisterAssemblyTypes(ThisAssembly)
            .AsClosedTypesOf(typeof(IRequestHandler<,>))
            .InstancePerLifetimeScope();

        builder.RegisterAssemblyTypes(ThisAssembly)
            .AsClosedTypesOf(typeof(IRequestHandler<>))
            .InstancePerLifetimeScope();

        builder.RegisterAssemblyTypes(ThisAssembly)
            .AsClosedTypesOf(typeof(INotificationHandler<>))
            .InstancePerLifetimeScope();
    }
}
