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

// F1 — auto-cancel of unconfirmed client orders after 10 days
builder.Services.AddScoped<IUnconfirmedOrderCancelRepository, UnconfirmedOrderCancelRepository>();
builder.Services.AddScoped<IUnconfirmedOrderCancelService, UnconfirmedOrderCancelService>();

// F5 — sync client-order status to "Livrée" from the Ecare expedition flow
builder.Services.AddScoped<IOrderDeliveredSyncRepository, OrderDeliveredSyncRepository>();
builder.Services.AddScoped<IOrderDeliveredSyncService, OrderDeliveredSyncService>();
builder.Services.AddHttpClient<IClientLivraisonApi, ClientLivraisonApi>(client =>
{
    var baseUrl = builder.Configuration["ExternalApis:ClientLivraison:BaseUrl"]
        ?? "https://app-emea-we-dssprod-dss-001.azurewebsites.net/";

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

var app = builder.Build();
app.Run();
