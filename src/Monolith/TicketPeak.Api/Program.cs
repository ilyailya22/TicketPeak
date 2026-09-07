using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Phase 0 placeholder: proves the host boots, the pipeline runs and telemetry reaches the
// Aspire dashboard. The first real endpoints arrive with the Catalog module in Phase 1.
app.MapGet("/hello", () => Results.Ok(new HelloResponse("TicketPeak")))
   .WithName("Hello")
   .WithSummary("Liveness smoke endpoint used by the Phase 0 acceptance test.");

app.Run();

internal    sealed   record HelloResponse( string Service );

/// <summary>Entry point marker so integration tests can reference this host.</summary>
public partial class Program;
