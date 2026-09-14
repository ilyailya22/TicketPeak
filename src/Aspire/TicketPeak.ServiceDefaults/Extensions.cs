using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Cross-cutting host configuration shared by every TicketPeak service: telemetry, health
/// checks, service discovery and outbound HTTP resilience.
/// </summary>
/// <remarks>
/// Lives in the <c>Microsoft.Extensions.Hosting</c> namespace by Aspire convention so that
/// <see cref="AddServiceDefaults{TBuilder}"/> is in scope wherever a host builder is.
/// </remarks>
public static class Extensions
{
    private const string LivenessEndpointPath = "/health/live";
    private const string ReadinessEndpointPath = "/health/ready";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Phase 5 replaces this with explicit per-client pipelines. Until then the standard
            // handler (retry with jitter, circuit breaker, timeout) is the safer default.
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithTracing(tracing => tracing
                .AddSource(builder.Environment.ApplicationName)
                .AddAspNetCoreInstrumentation(options =>
                    // Health probes fire every few seconds; tracing them buries real requests.
                    options.Filter = context =>
                        !context.Request.Path.StartsWithSegments(LivenessEndpointPath)
                        && !context.Request.Path.StartsWithSegments(ReadinessEndpointPath))
                .AddHttpClientInstrumentation());

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

        return builder;
    }

    /// <summary>
    /// Maps liveness and readiness probes.
    /// </summary>
    /// <remarks>
    /// Development only. These endpoints disclose dependency topology, so exposing them in
    /// other environments needs an auth decision first — see https://aka.ms/aspire/healthchecks.
    /// Phase 12 revisits this when Container Apps needs real probes.
    /// </remarks>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            // Liveness: is the process up? Only "live"-tagged checks count, so a sick
            // dependency must never cause the orchestrator to restart a healthy process.
            app.MapHealthChecks(LivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("live"),
            });

            // Readiness: should this instance receive traffic? Every check must pass.
            app.MapHealthChecks(ReadinessEndpointPath);
        }

        return app;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        bool useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }
}
