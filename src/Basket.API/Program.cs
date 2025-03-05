using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Instrumentation.StackExchangeRedis;

var builder = WebApplication.CreateBuilder(args);

builder.AddBasicServiceDefaults();
builder.AddApplicationServices();

builder.Services.AddGrpc();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource=>resource.AddService("eShop.BasketAPI"))
    .WithTracing(tracing =>tracing
            .AddGrpcClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRedisInstrumentation()
            .AddJaegerExporter(jaegerOptions=>{
                jaegerOptions.AgentHost = "localhost";
                jaegerOptions.AgentPort = 6831;
            })
    );

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGrpcService<BasketService>();

app.Run();
