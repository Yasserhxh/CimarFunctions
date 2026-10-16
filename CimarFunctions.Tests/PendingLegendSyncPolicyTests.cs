using System;
using System.Collections.Generic;
using System.Linq;
using CimarFunctions.Services.Sync;
using Xunit;

namespace CimarFunctions.Tests;

public class PendingLegendSyncPolicyTests
{
    private static readonly DateTime Now = new(2026, 10, 16, 15, 00, 00);

    [Fact]
    public void SinceFor_SubtractsLookbackDays()
    {
        Assert.Equal(new DateTime(2026, 09, 16, 15, 00, 00),
            PendingLegendSyncPolicy.SinceFor(Now, 30));
    }

    [Fact]
    public void DefaultLookback_Is30Days()
    {
        Assert.Equal(30, PendingLegendSyncPolicy.DefaultLookbackDays);
    }

    [Fact]
    public void ReferenceDate_PrefersCreatedAt()
    {
        var created = Now.AddHours(-5);
        Assert.Equal(created,
            PendingLegendSyncPolicy.ReferenceDate(created, Now.AddHours(-4), Now.AddHours(-3), Now.AddHours(-2)));
    }

    [Fact]
    public void ReferenceDate_FallsBackToParkingAt_WhenCreatedAtIsNull()
    {
        // Prod Id 28544: kiosk/parking rows have CreatedAt NULL but ParkingAt set.
        var parking = Now.AddHours(-4);
        Assert.Equal(parking,
            PendingLegendSyncPolicy.ReferenceDate(null, parking, Now.AddHours(-3), Now.AddHours(-2)));
    }

    [Fact]
    public void ReferenceDate_FallsBackToAddedToQueueAt_ThenDateAffectation()
    {
        var queued = Now.AddHours(-3);
        var affectation = Now.AddHours(-2);

        Assert.Equal(queued, PendingLegendSyncPolicy.ReferenceDate(null, null, queued, affectation));
        Assert.Equal(affectation, PendingLegendSyncPolicy.ReferenceDate(null, null, null, affectation));
    }

    [Fact]
    public void ReferenceDate_IsNull_WhenNoDateIsKnown()
    {
        Assert.Null(PendingLegendSyncPolicy.ReferenceDate(null, null, null, null));
    }

    [Fact]
    public void Row_WithoutReferenceDate_IsNotInWindow()
    {
        Assert.False(PendingLegendSyncPolicy.IsInWindow(null, Now, 30));
    }

    [Fact]
    public void Row_OlderThanLookback_IsNotInWindow()
    {
        Assert.False(PendingLegendSyncPolicy.IsInWindow(Now.AddDays(-31), Now, 30));
    }

    [Fact]
    public void Row_AtLookbackBoundary_IsInWindow()
    {
        Assert.True(PendingLegendSyncPolicy.IsInWindow(Now.AddDays(-30), Now, 30));
    }

    [Fact]
    public void SelectBatch_RecentRowsAreProcessed_EvenWhenMoreStaleRowsThanBatchSizeAreQueued()
    {
        // Prod 2026-10-16: 104 NULL-date rows + ~150 stale rows ahead of the 03/10 orders
        // (ranks 255-260) starved them with "TOP 200 ORDER BY CreatedAt ASC".
        var stale = Enumerable.Range(1, 104)
            .Select(i => new Row(i, null))
            .Concat(Enumerable.Range(105, 150).Select(i => new Row(i, Now.AddDays(-100 + i % 60))))
            .ToList();

        var recent = new[]
        {
            new Row(28476, new DateTime(2026, 10, 03, 15, 33, 19)),
            new Row(28480, new DateTime(2026, 10, 03, 16, 04, 48)),
            new Row(28481, new DateTime(2026, 10, 03, 16, 10, 13)),
            new Row(28511, new DateTime(2026, 10, 03, 19, 50, 16)),
            new Row(28516, new DateTime(2026, 10, 03, 20, 35, 14)),
        };

        var batch = PendingLegendSyncPolicy.SelectBatch(stale.Concat(recent), r => r.ReferenceDate, Now, 30, 200);

        Assert.All(recent, r => Assert.Contains(r, batch));
        Assert.DoesNotContain(batch, r => r.ReferenceDate is null || r.ReferenceDate < Now.AddDays(-30));
    }

    [Fact]
    public void SelectBatch_ReturnsNewestFirst_AndRespectsTake()
    {
        var rows = Enumerable.Range(0, 10)
            .Select(i => new Row(i, Now.AddDays(-i)))
            .ToList();

        var batch = PendingLegendSyncPolicy.SelectBatch(rows, r => r.ReferenceDate, Now, 30, 3);

        Assert.Equal(new[] { 0, 1, 2 }, batch.Select(r => r.Id));
    }

    private sealed record Row(int Id, DateTime? ReferenceDate);
}
