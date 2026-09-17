using Autofac;
using MediatR;
using TicketPeak.Api.Behaviors;

namespace TicketPeak.Api.Composition;

/// <summary>
/// The MediatR pipeline, outermost first. MediatR wraps behaviours in registration order, so
/// logging sees every request including slow and refused ones; performance timing includes
/// validation; validation runs before any work; and the unit of work commits only what a
/// successful handler did. ADR 0005 explains why these concerns live here and not in middleware.
/// </summary>
internal sealed class PipelineModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterGeneric(typeof(LoggingBehavior<,>)).As(typeof(IPipelineBehavior<,>));
        builder.RegisterGeneric(typeof(PerformanceBehavior<,>)).As(typeof(IPipelineBehavior<,>));

        // The two below carry generic constraints (Result responses; ICommand requests). Autofac
        // honours them when closing the open generic, so neither is ever built for a request it
        // cannot handle, and a query never reaches the unit of work.
        builder.RegisterGeneric(typeof(ValidationBehavior<,>)).As(typeof(IPipelineBehavior<,>));
        builder.RegisterGeneric(typeof(UnitOfWorkBehavior<,>)).As(typeof(IPipelineBehavior<,>));
    }
}
