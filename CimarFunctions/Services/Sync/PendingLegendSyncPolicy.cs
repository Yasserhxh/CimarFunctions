namespace CimarFunctions.Services.Sync;

/// <summary>
/// Decides which pending Ecare_Order_Legend rows the external-delivery sync processes.
/// Rows are taken newest first within a sliding window, so legends that will never get
/// a BL (cancelled, abandoned, missing dates) can no longer fill the batch and starve
/// recent deliveries. Kiosk/parking rows often have CreatedAt NULL, so the reference
/// date falls back to ParkingAt, AddedToQueueAt, then DateAffectation.
/// </summary>
public static class PendingLegendSyncPolicy
{
    public const int DefaultLookbackDays = 30;

    /// <summary>Rows whose reference date is at or after this instant are in the window.</summary>
    public static DateTime SinceFor(DateTime now, int lookbackDays)
        => now.AddDays(-lookbackDays);

    /// <summary>Mirrors COALESCE(CreatedAt, ParkingAt, AddedToQueueAt, DateAffectation) in SQL.</summary>
    public static DateTime? ReferenceDate(
        DateTime? createdAt,
        DateTime? parkingAt,
        DateTime? addedToQueueAt,
        DateTime? dateAffectation)
        => createdAt ?? parkingAt ?? addedToQueueAt ?? dateAffectation;

    public static bool IsInWindow(DateTime? referenceDate, DateTime now, int lookbackDays)
        => referenceDate.HasValue && referenceDate.Value >= SinceFor(now, lookbackDays);

    public static IReadOnlyList<T> SelectBatch<T>(
        IEnumerable<T> pending,
        Func<T, DateTime?> referenceDate,
        DateTime now,
        int lookbackDays,
        int take)
        => pending
            .Where(row => IsInWindow(referenceDate(row), now, lookbackDays))
            .OrderByDescending(row => referenceDate(row))
            .Take(take)
            .ToList();
}
