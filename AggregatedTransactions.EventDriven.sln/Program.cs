using Microsoft.Data.SqlClient;
using StackExchange.Redis;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Npgsql;
using Api.Application.Interfaces;
using Api.Infrastructure.Persistence;
using Api.Infrastructure.Cache;
using static Api.Infrastructure.Persistence.TransactionsRepository;

var builder = WebApplication.CreateBuilder(args);

// OTEL + Prometheus
builder.Services.AddOpenTelemetry()
 .ConfigureResource(r => r.AddService("AggregatedTransactions.Api"))
 .WithMetrics(m => m.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation())
                    //.AddPrometheusExporter())
 .WithTracing(t => t.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Redis
var redisEndpoint = builder.Configuration["Redis:Endpoint"]!;
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redisEndpoint));

//DB connections(read/write split)
builder.Services.AddSingleton(sp => new SqlConnection(builder.Configuration.GetConnectionString("WriteDb")));
builder.Services.AddSingleton(sp => new SqlConnection(builder.Configuration.GetConnectionString("ReadDb")));

// Postgres
builder.Services.AddScoped<NpgsqlConnection>(_ => new NpgsqlConnection(builder.Configuration.GetConnectionString("ReadDb")));
builder.Services.AddScoped<NpgsqlConnection>(_ => new NpgsqlConnection(builder.Configuration.GetConnectionString("WriteDb")));



builder.Services.AddSingleton<ITransactionsRepository, TransactionsRepository>();
builder.Services.AddSingleton<ICache, RedisCache>();

builder.Services.AddSingleton(new ApiKeyOptions(
 builder.Configuration["Auth:ApiKeyHeader"]!,
 builder.Configuration["Auth:ApiKey"]!));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

//app.MapPrometheusScrapingEndpoint(); // /metrics

app.Use(async (ctx, next) =>
{
    var opts = ctx.RequestServices.GetRequiredService<ApiKeyOptions>();
    if (!ctx.Request.Headers.TryGetValue(opts.HeaderName, out var key) || key != opts.ApiKey)
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await ctx.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
        return;
    }
    await next();
});

app.Run();
