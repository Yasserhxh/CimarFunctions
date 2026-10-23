using System;
using System.Collections.Generic;
using System.Linq;
using CimarFunctions.Services.Alerts;
using Xunit;

namespace CimarFunctions.Tests;

public class OverstayAlertRulesTests
{
    // Repère fixe en heure locale Maroc (PabEntryAt est stocké en heure Maroc par le kiosk).
    private static readonly DateTime Now = new(2026, 9, 23, 10, 0, 0);

    // Demande client (23/10/2026) : un seul e-mail par camion, au-delà de 90 min, scan toutes les 15 min.
    [Fact]
    public void Seuil_unique_code_en_dur_a_90_minutes()
        => Assert.Equal(90, OverstayAlertRules.AlertThresholdMinutes);

    [Fact]
    public void Scan_toutes_les_15_minutes()
        => Assert.Equal("0 */15 * * * *", OverstayAlertRules.ScanSchedule);

    [Fact]
    public void Pas_de_premiere_pesee_pas_d_alerte()
        => Assert.False(OverstayAlertRules.IsAlertDue(null, null, Now, alreadyAlerted: false));

    [Fact]
    public void Deuxieme_pesee_faite_pas_d_alerte()
        => Assert.False(OverstayAlertRules.IsAlertDue(Now.AddMinutes(-120), 30000, Now, alreadyAlerted: false));

    [Fact]
    public void A_80_minutes_plus_d_alerte_intermediaire()
        => Assert.False(OverstayAlertRules.IsAlertDue(Now.AddMinutes(-80), null, Now, alreadyAlerted: false));

    [Fact]
    public void Au_seuil_exact_pas_encore_declenche()
        => Assert.False(OverstayAlertRules.IsAlertDue(Now.AddMinutes(-90), null, Now, alreadyAlerted: false));

    [Fact]
    public void Au_dela_de_90_minutes_declenche()
        => Assert.True(OverstayAlertRules.IsAlertDue(Now.AddMinutes(-91), null, Now, alreadyAlerted: false));

    [Fact]
    public void Deja_alerte_pas_de_second_email()
        => Assert.False(OverstayAlertRules.IsAlertDue(Now.AddMinutes(-150), null, Now, alreadyAlerted: true));

    [Fact]
    public void ElapsedMinutes_arrondi_a_la_minute()
        => Assert.Equal(90, OverstayAlertRules.ElapsedMinutes(Now.AddMinutes(-90), Now));

    // Garde-fou « fantômes » : un camion entré avant le plancher (2e pesée manuelle / hors
    // système, enregistrement jamais clôturé) ne doit JAMAIS déclencher d'alerte, même très
    // au-delà du seuil. Cas réel : legend 28202 entré le 02/10, alerte à 27438 min.
    private static readonly DateTime Floor = new(2026, 9, 23, 0, 0, 0);

    [Fact]
    public void Entre_avant_le_plancher_pas_d_alerte_meme_tres_en_retard()
        => Assert.False(OverstayAlertRules.IsAlertDue(
            Floor.AddMinutes(-5000), null, Now, alreadyAlerted: false, ignoreBefore: Floor));

    [Fact]
    public void Entre_apres_le_plancher_declenche_normalement()
        => Assert.True(OverstayAlertRules.IsAlertDue(
            Now.AddMinutes(-95), null, Now, alreadyAlerted: false, ignoreBefore: Floor));

    [Fact]
    public void SplitRecipients_premier_to_reste_cc_dedoublonne()
    {
        var (to, cc) = OverstayAlertRules.SplitRecipients(new[]
        {
            "abdelkarim.mezianemtalsi@heidelbergmaterials.com",
            "abdelkarim.mezianemtalsi@heidelbergmaterials.com", // doublon exact
            "ABD-ETTAOUAB.ELHRARTI@heidelbergmaterials.com",     // casse différente = même adresse
            "abd-ettaouab.elhrarti@heidelbergmaterials.com",
            "   ",                                                 // vide ignoré
            "ismail.chakra@heidelbergmaterials.com",
        });
        Assert.Equal("abdelkarim.mezianemtalsi@heidelbergmaterials.com", to);
        Assert.Equal("ABD-ETTAOUAB.ELHRARTI@heidelbergmaterials.com,ismail.chakra@heidelbergmaterials.com", cc);
    }

    [Fact]
    public void SplitRecipients_liste_vide_to_vide_cc_null()
    {
        var (to, cc) = OverstayAlertRules.SplitRecipients(Array.Empty<string>());
        Assert.Equal(string.Empty, to);
        Assert.Null(cc);
    }
}
