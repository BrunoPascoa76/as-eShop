using eShop.WebApp.Components;
using eShop.ServiceDefaults;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.AddApplicationServices();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource=>resource.AddService("eShop.WebApp"))
    .WithTracing(tracing =>tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddGrpcClientInstrumentation()
        .AddSource("eShop.AddToCart")
        .AddJaegerExporter(options=>{
            options.AgentHost = "jaeger";
            options.AgentPort = 6831;
        })
    ).WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("eShop.AddToCart")
        .AddPrometheusExporter()
    );

var app = builder.Build();

app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseAntiforgery();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.MapForwarder("/product-images/{id}", "http://catalog-api", "/api/catalog/items/{id}/pic");

app.Run();
