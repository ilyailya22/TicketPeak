using Autofac;
using Autofac.Extensions.DependencyInjection;
using Scalar.AspNetCore;
using TicketPeak.Api.Composition;
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

// Phase 0 placeholder, kept until the first real endpoints land in step 7 of Phase 1.
app.MapGet("/hello", () => Results.Ok(new HelloResponse("TicketPeak")))
   .WithName("Hello")
   .WithSummary("Liveness smoke endpoint used by the Phase 0 acceptance test.");

app.Run();

internal sealed record HelloResponse(string Service);

/// <summary>Entry point marker so integration tests can reference this host.</summary>
public partial class Program;
