using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SbomQualityGate.Infrastructure.Telemetry;

public static class TelemetryExtensions
{
    public static IHostApplicationBuilder AddSbomQualityGateTelemetry(
        this IHostApplicationBuilder builder)
    {
        var assembly = Assembly.GetEntryAssembly();
        var serviceName = assembly?.GetName().Name ?? "Unknown";
        var serviceVersion = assembly?.GetName().Version?.ToString() ?? "0.0.0";

        var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"]
                           ?? "http://localhost:4317";

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: serviceName,
                    serviceVersion: serviceVersion))
            .WithTracing(tracing => tracing
                .AddEntityFrameworkCoreInstrumentation()
                .AddOtlpExporter(otlp => otlp.Endpoint = new Uri(otlpEndpoint)))
            .WithMetrics(metrics => metrics
                .AddRuntimeInstrumentation()
                .AddOtlpExporter(otlp => otlp.Endpoint = new Uri(otlpEndpoint)));

        return builder;
    }
}
