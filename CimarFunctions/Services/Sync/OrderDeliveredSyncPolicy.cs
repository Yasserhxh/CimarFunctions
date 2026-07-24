namespace CimarFunctions.Services.Sync;

/// <summary>
/// Decides whether a confirmed client order (dbo.Orders) must be flipped to
/// "Livrée" based on its real state in the Ecare expedition flow: an order is
/// delivered once its linked Ecare_Order_Legend has a BonDeLivraison (Step 5).
/// Matching between Orders.NumeroSap and Ecare_Order_Legend.CodeSapCommande is
/// tolerant to SAP leading-zero padding.
/// </summary>
public static class OrderDeliveredSyncPolicy
{
    public const string ConfirmedStatus = "Confirmée";
    public const string DeliveredStatus = "Livrée";

    public static bool ShouldMarkDelivered(string? currentStatut, bool hasBonDeLivraison)
        => hasBonDeLivraison
           && string.Equals(currentStatut?.Trim(), ConfirmedStatus, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Normalizes a SAP sales-document number so "0118006946" and "118006946"
    /// compare equal. Returns null for empty input.
    /// </summary>
    public static string? NormalizeSapDoc(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var trimmed = code.Trim().TrimStart('0');
        return trimmed.Length == 0 ? "0" : trimmed;
    }
}
