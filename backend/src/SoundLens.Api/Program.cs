using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FastEndpoints;
using Microsoft.OpenApi.Models;
using Serilog;
using SoundLens.Api.Configuration;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog(options => options.ReadFrom.Configuration(builder.Configuration));
    builder.Services.AddFastEndpoints();

    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SoundLens API",
                Version = "v1"
            });
        });
    }

    builder.Services.AddCors(options => options.AddDefaultPolicy(
        policy => policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddHealthChecks();
    builder.Services.AddHttpContextAccessor();

    // Configure JSON serialization for FastEndpoints
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.SerializerOptions.PropertyNameCaseInsensitive = true;
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.SerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
    });

    var app = builder.Build();

    var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";

    Log.Information(
        "Starting {Application} {Version} ({Environment})",
        app.Environment.ApplicationName,
        version,
        app.Environment.EnvironmentName);

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "SoundLens API V1");
        });
        Log.Debug("Swagger enabled (development only).");
    }

    app.UseCors();
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseSerilogRequestLogging();

    app.MapGroup("/api").MapFastEndpoints();
    app.MapHealthChecks("/api/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
