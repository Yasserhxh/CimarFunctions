namespace CimarFunctions.Services.Alerts;

public interface IOverstayAlertService
{
    /// <summary>
    /// Effectue un scan : trouve les camions en dépassement, et pour chaque palier franchi
    /// non encore notifié, réserve puis envoie l'alerte e-mail. Une seule alerte par (camion, palier).
    /// </summary>
    Task RunScanAsync(CancellationToken cancellationToken = default);
}
