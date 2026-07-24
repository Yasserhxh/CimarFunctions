using CimarFunctions.Services.Sync;
using Xunit;

namespace CimarFunctions.Tests;

public class OrderDeliveredSyncPolicyTests
{
    // ---- ShouldMarkDelivered ----

    [Fact]
    public void Confirmed_WithBonDeLivraison_ShouldMarkDelivered()
    {
        Assert.True(OrderDeliveredSyncPolicy.ShouldMarkDelivered("Confirmée", hasBonDeLivraison: true));
    }

    [Fact]
    public void Confirmed_WithoutBonDeLivraison_ShouldNot()
    {
        Assert.False(OrderDeliveredSyncPolicy.ShouldMarkDelivered("Confirmée", hasBonDeLivraison: false));
    }

    [Theory]
    [InlineData("Crée")]
    [InlineData("Annulée")]
    [InlineData("Livrée")]
    [InlineData(null)]
    public void NonConfirmed_IsNeverFlipped_EvenWithBl(string? statut)
    {
        // Only a currently-confirmed order transitions to Livrée; never re-flip
        // an already delivered, cancelled, or still-unconfirmed order.
        Assert.False(OrderDeliveredSyncPolicy.ShouldMarkDelivered(statut, hasBonDeLivraison: true));
    }

    [Fact]
    public void Status_MatchIsCaseAndWhitespaceTolerant()
    {
        Assert.True(OrderDeliveredSyncPolicy.ShouldMarkDelivered("  confirmée ", hasBonDeLivraison: true));
    }

    // ---- NormalizeSapDoc (zero-pad tolerant matching) ----

    [Theory]
    [InlineData("0118006946", "118006946")]
    [InlineData("118006946", "0118006946")]
    [InlineData("  0118006946  ", "118006946")]
    public void NormalizeSapDoc_IgnoresLeadingZerosAndWhitespace(string a, string b)
    {
        Assert.Equal(OrderDeliveredSyncPolicy.NormalizeSapDoc(a), OrderDeliveredSyncPolicy.NormalizeSapDoc(b));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeSapDoc_EmptyInput_ReturnsNull(string? input)
    {
        Assert.Null(OrderDeliveredSyncPolicy.NormalizeSapDoc(input));
    }

    [Fact]
    public void NormalizeSapDoc_AllZeros_CollapsesToZero()
    {
        Assert.Equal("0", OrderDeliveredSyncPolicy.NormalizeSapDoc("0000"));
    }

    [Fact]
    public void NormalizeSapDoc_DistinctDocs_DoNotCollide()
    {
        Assert.NotEqual(OrderDeliveredSyncPolicy.NormalizeSapDoc("118006946"),
                        OrderDeliveredSyncPolicy.NormalizeSapDoc("118006947"));
    }
}
