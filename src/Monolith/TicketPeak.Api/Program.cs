using Autofac;
using Autofac.Extensions.DependencyInjection;
using Scalar.AspNetCore;
using TicketPeak.Api.Composition;
using TicketPeak.Api.Http;
using TicketPeak.Modules.Catalog;
using TicketPeak.Modules.Identity;
using TicketPeak.Modules.Inventory;
using TicketPeak.Modules.Ordering;
using TicketPeak.Modules.Payments;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddSingleton(TimeProvider.System);

// Autofac is the container for the monolith: one Autofac.Module per bounded context, assembly
// scanning and decorators. The services extracted later use the built-in container. See ADR 0006.
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(container =>
{
    container.RegisterModule<MediatorModule>();
    container.RegisterModule<PipelineModule>();

    container.RegisterModule<IdentityModule>();
    container.RegisterModule<CatalogModule>();
    container.RegisterModule<InventoryModule>();
    container.RegisterModule<OrderingModule>();
    container.RegisterModule<PaymentsModule>();
});

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Every module endpoint returns a Result. This one filter turns Results into HTTP responses, so
// the mapping from ErrorType to status code exists exactly once.
RouteGroupBuilder api = app.MapGroup("/api").AddEndpointFilter<ResultEndpointFilter>();
api.MapCatalogEndpoints();
api.MapInventoryEndpoints();
api.MapOrderingEndpoints();

app.Run();

/// <summary>Entry point marker so integration tests can reference this host.</summary>
public partial class Program;
