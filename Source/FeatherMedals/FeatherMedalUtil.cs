using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FeatherMedals;

public static class FeatherMedalUtil
{
    public static bool IsMedalDef(ThingDef def) => def?.thingClass == typeof(FeatherMedal);
    
    public static FloatMenuOption CeremonyOnlyForceWearOption(Apparel apparel)
    {
        var cannotKey = apparel.def.apparel.LastLayer.IsUtilityLayer
            ? "CannotForceTargetToEquipApparel"
            : "CannotForceTargetToWear";
        var text = cannotKey.Translate(apparel.Label, apparel) + ": "
                                                               + "FeatherMedals_FailTrophyNeedsCeremony".Translate().CapitalizeFirst();
        return new FloatMenuOption(text, null);
    }

    public static bool NeedsCeremony(Thing thing)
        => thing is FeatherMedal { BiocodeComp: not { Biocoded: true } };
    
    public static bool CanWearTrophy(Pawn pawn, FeatherMedal medal)
    {
        if (pawn?.apparel == null)
            return false;

        // our wear gate refuses every pawn outside a ceremony, lift it to ask vanilla only about the body
        var previous = RitualOutcomeEffectWorkerAwardTrophy.ApplyingCeremonyAward;
        RitualOutcomeEffectWorkerAwardTrophy.ApplyingCeremonyAward = true;
        try
        {
            return ApparelUtility.HasPartsToWear(pawn, medal.def) && medal.PawnCanWear(pawn, true);
        }
        finally
        {
            RitualOutcomeEffectWorkerAwardTrophy.ApplyingCeremonyAward = previous;
        }
    }

    public static bool TryGetDisplaySlot(Pawn pawn, FeatherMedal medal, out int index, out int count)
    {
        index = -1;
        count = 0;

        var worn = pawn?.apparel?.WornApparel;
        if (worn == null)
            return false;

        var maxDisplayed = MedalMod.Settings.MaxDisplayedTrophies;
        var groups = medal.def.apparel.bodyPartGroups;
        var seen = 0;

        for (var i = 0; i < worn.Count; i++)
        {
            if (worn[i] is not FeatherMedal other)
                continue;
            if (seen++ >= maxDisplayed)
                break;
            if (!SameGroups(other.def.apparel.bodyPartGroups, groups))
                continue;

            if (other == medal)
                index = count;
            count++;
        }

        return index >= 0;
    }
    
    private static bool SameGroups(List<BodyPartGroupDef> a, List<BodyPartGroupDef> b)
    {
        if (a == b)
            return true;
        if (a == null || b == null || a.Count != b.Count)
            return false;

        for (var i = 0; i < a.Count; i++)
        {
            if (!b.Contains(a[i]))
                return false;
        }
        return true;
    }
}