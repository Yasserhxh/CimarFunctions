using CimarFunctions.Services.Sync;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddScoped<IExternalDeliverySyncService, ExternalDeliverySyncService>();
builder.Services.AddScoped<IOrderLegendSyncRepository, OrderLegendSyncRepository>();
builder.Services.AddScoped<ISyncExecutionLockProvider, SqlSyncExecutionLockProvider>();
builder.Services.AddHttpClient<IClientLivraisonApi, ClientLivraisonApi>(client =>
{
    var baseUrl = builder.Configuration["ExternalApis:ClientLivraison:BaseUrl"]
        ?? "https://app-emea-we-dssprod-dss-001.azurewebsites.net/";

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

var app = builder.Build();
app.Run();
