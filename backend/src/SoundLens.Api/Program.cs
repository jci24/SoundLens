using FastEndpoints;
using SoundLens.Api.Endpoints.Files.handler;
using SoundLens.Api.Endpoints.Files.services;
using SoundLens.Api.Endpoints.Files.validators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFastEndpoints();
builder.Services.AddScoped<UploadValidator>();
builder.Services.AddScoped<UploadHandler>();
builder.Services.AddScoped<WavParser>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseFastEndpoints();

app.Run();

public partial class Program;
