using System;
using CimarFunctions.Services.Sync;
using Xunit;

namespace CimarFunctions.Tests;

public class UnconfirmedOrderCancelPolicyTests
{
    private static readonly DateTime Now = new(2026, 07, 24, 12, 00, 00);

    [Fact]
    public void CutoffFor_SubtractsDelayDays()
    {
        Assert.Equal(new DateTime(2026, 07, 14, 12, 00, 00),
            UnconfirmedOrderCancelPolicy.CutoffFor(Now, 10));
    }

    [Fact]
    public void CreeOrder_OlderThanDelay_IsEligible()
    {
        var created = Now.AddDays(-11);
        Assert.True(UnconfirmedOrderCancelPolicy.IsEligible("Crée", created, Now, 10));
    }

    [Fact]
    public void CreeOrder_ExactlyAtDelayBoundary_IsEligible()
    {
        // Created exactly 10 days ago → the 10-day window has elapsed → cancel.
        var created = Now.AddDays(-10);
        Assert.True(UnconfirmedOrderCancelPolicy.IsEligible("Crée", created, Now, 10));
    }

    [Fact]
    public void CreeOrder_YoungerThanDelay_IsNotEligible()
    {
        var created = Now.AddDays(-9);
        Assert.False(UnconfirmedOrderCancelPolicy.IsEligible("Crée", created, Now, 10));
    }

    [Theory]
    [InlineData("Confirmée")]
    [InlineData("Annulée")]
    [InlineData("Livrée")]
    [InlineData("")]
    [InlineData(null)]
    public void NonCreeOrder_IsNeverEligible_EvenIfOld(string? statut)
    {
        var created = Now.AddDays(-30);
        Assert.False(UnconfirmedOrderCancelPolicy.IsEligible(statut, created, Now, 10));
    }

    [Fact]
    public void Status_MatchIsCaseAndWhitespaceTolerant()
    {
        var created = Now.AddDays(-11);
        Assert.True(UnconfirmedOrderCancelPolicy.IsEligible("  crée  ", created, Now, 10));
    }

    [Fact]
    public void DefaultDelay_Is10Days()
    {
        Assert.Equal(10, UnconfirmedOrderCancelPolicy.DefaultDelayDays);
    }
}
