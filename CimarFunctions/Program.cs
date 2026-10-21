using CimarFunctions.Services.Alerts;
using CimarFunctions.Services.Email;
using CimarFunctions.Services.Sync;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Client;

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

// Alerte « camion en dépassement dans l'usine (entre 1ère et 2ème pesée) ».
// Enregistrée seulement si AzureAdMail:ClientId est présent : sans creds, on ne
// branche ni le client MSAL ni le worker (l'app démarre normalement, pas de crash).
var overstayEnabled = builder.Configuration.GetValue("OverstayAlert:Enabled", true);
var mailClientId = builder.Configuration["AzureAdMail:ClientId"];
if (overstayEnabled && !string.IsNullOrWhiteSpace(mailClientId))
{
    builder.Services.Configure<OverstayAlertOptions>(
        builder.Configuration.GetSection("OverstayAlert"));

    builder.Services.AddSingleton<IConfidentialClientApplication>(_ =>
        ConfidentialClientApplicationBuilder
            .Create(mailClientId)
            .WithClientSecret(builder.Configuration["AzureAdMail:ClientSecret"])
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{builder.Configuration["AzureAdMail:TenantId"]}"))
            .Build());

    builder.Services.AddSingleton<IEmailSender, GraphEmailSender>();
    builder.Services.AddScoped<IOverstayAlertRepository, OverstayAlertRepository>();
    builder.Services.AddScoped<IOverstayAlertService, OverstayAlertService>();

    Console.WriteLine("✓ Alerte dépassement camion activée");
}
else
{
    Console.WriteLine("⚠ Alerte dépassement camion NON activée (OverstayAlert:Enabled=false ou AzureAdMail:ClientId manquant)");
}

var app = builder.Build();
app.Run();
