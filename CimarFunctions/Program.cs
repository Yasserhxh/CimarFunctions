using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

// ----------------------------------------------------------
// Load local.settings.json & environment settings
// ----------------------------------------------------------
builder.Configuration
    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// ----------------------------------------------------------
// CORS — Allow ALL origins
// ----------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ----------------------------------------------------------
// HTTP support + Dependency Injection
// ----------------------------------------------------------
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// ----------------------------------------------------------
// Build Function Application
// ----------------------------------------------------------
var app = builder.Build();

app.Run();
