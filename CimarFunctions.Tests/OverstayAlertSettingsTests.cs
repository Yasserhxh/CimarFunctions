using System;
using CimarFunctions.Services.Alerts;
using Xunit;

namespace CimarFunctions.Tests;

// Paramètres de l'alerte codés en dur (demande du 23/10/2026) : plus rien ne vient
// de local.settings.json ni des app settings Azure, hormis les identifiants AzureAdMail (secrets).
public class OverstayAlertSettingsTests
{
    [Fact]
    public void Expediteur_est_la_boite_noreply()
        => Assert.Equal("cimar.noreplay@cimar.co.ma", OverstayAlertSettings.Sender);

    [Fact]
    public void Destinataire_principal_en_TO_et_quatre_en_CC()
    {
        var (to, cc) = OverstayAlertRules.SplitRecipients(OverstayAlertSettings.Recipients);

        Assert.Equal("abdelkarim.mezianemtalsi@heidelbergmaterials.com", to);
        Assert.Equal(
            "abd-ettaouab.elhrarti@heidelbergmaterials.com," +
            "abderrahman.louza@heidelbergmaterials.com," +
            "mohamed.elrhattaoui@heidelbergmaterials.com," +
            "ismail.chakra@heidelbergmaterials.com",
            cc);
    }

    [Fact]
    public void Plancher_anti_fantomes_au_22_octobre_2026()
        => Assert.Equal(new DateTime(2026, 10, 22, 0, 0, 0), OverstayAlertSettings.IgnoreEntriesBeforeLocal);
}
