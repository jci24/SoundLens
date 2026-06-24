using FastEndpoints;
using SoundLens.Api.Endpoints.Files.handler;
using SoundLens.Api.Endpoints.Files.validators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFastEndpoints();
builder.Services.AddScoped<UploadValidator>();
builder.Services.AddScoped<UploadHandler>();

var app = builder.Build();

app.UseFastEndpoints();

app.Run();

public partial class Program;
