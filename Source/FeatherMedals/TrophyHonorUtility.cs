using RimWorld;
using UnityEngine;
using Verse;

namespace TribalTrophies;

// Honor (Royalty), see TrophyExtension.honorOnAdorning.
public static class TrophyHonorUtility
{
    private const int LegendaryCeremonyStage = 3;

    public static int BaseHonor(AwardedTrophy trophy)
    {
        return trophy.def.GetModExtension<TrophyExtension>()?.honorOnAdorning ?? 0;
    }

    public static FactionDef HonorFactionDef(AwardedTrophy trophy)
    {
        return trophy.def.GetModExtension<TrophyExtension>()?.honorFaction;
    }

    private static int ScaledHonor(AwardedTrophy trophy, int ceremonyStage)
    {
        var baseHonor = BaseHonor(trophy);
        if (baseHonor <= 0)
            return 0;

        var kind = trophy.Kind;
        var multiplier = 1f
                         + kind.honorCeremonyBonus * CeremonyShare(ceremonyStage)
                         + kind.honorTrophyQualityBonus * TrophyQualityShare(trophy);

        return Mathf.Max(0, Mathf.FloorToInt(baseHonor * multiplier + 0.5f));
    }

    // poor 0, legendary 1
    private static float CeremonyShare(int ceremonyStage)
    {
        return Mathf.Clamp01((float)ceremonyStage / LegendaryCeremonyStage);
    }

    // normal 0, legendary 1, below normal negative.
    private static float TrophyQualityShare(AwardedTrophy trophy)
    {
        if (!trophy.TryGetQuality(out var quality))
            return 0f;

        return ((int)quality - (int)QualityCategory.Normal)
               / (float)((int)QualityCategory.Legendary - (int)QualityCategory.Normal);
    }

    public static Faction FindHonorFaction(FactionDef factionDef, bool allowHostile = false)
    {
        if (factionDef == null)
            return null;

        Faction hostile = null;
        foreach (var faction in Find.FactionManager.AllFactionsListForReading)
        {
            if (faction.def != factionDef)
                continue;
            if (!faction.HostileTo(Faction.OfPlayer))
                return faction;
            hostile ??= faction;
        }
        return allowHostile ? hostile : null;
    }

    public static void AwardHonor(Pawn adorned, AwardedTrophy trophy, int ceremonyStage)
    {
        if (!ModsConfig.RoyaltyActive || adorned.royalty == null)
            return;

        var honor = ScaledHonor(trophy, ceremonyStage);
        if (honor <= 0)
            return;

        var faction = FindHonorFaction(HonorFactionDef(trophy));
        if (faction == null)
            return;

        adorned.royalty.GainFavor(faction, honor);
        trophy.honorGranted = honor;
        trophy.honorGrantedFaction = faction;

        if (Prefs.DevMode)
            Log.Message($"[TribalTrophies] {adorned.LabelShort} gained {honor} honor with {faction.Name} (base {BaseHonor(trophy)})");
    }
}
