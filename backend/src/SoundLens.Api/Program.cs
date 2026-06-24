using FastEndpoints;
using SoundLens.Api.Endpoints.Files.handler;
using SoundLens.Api.Endpoints.Files.services;
using SoundLens.Api.Endpoints.Files.validators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFastEndpoints();
builder.Services.AddScoped<UploadValidator>();
builder.Services.AddScoped<UploadHandler>();
builder.Services.AddScoped<WavParser>();

var app = builder.Build();

app.UseFastEndpoints();

app.Run();

public partial class Program;
