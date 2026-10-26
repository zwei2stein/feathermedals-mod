using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FeatherMedals;

public class NpcScalpBeltExtension : DefModExtension
{
    public float honorableChance = 0.05f;
    public float mercilessChance = 0.2f;
    public TechLevel zeroChanceTechLevel = TechLevel.Archotech;
}

public static class NpcScalpBeltGenerator
{
    private static List<ThingDef> beltDefs;

    private static List<ThingDef> BeltDefs => beltDefs ??= DefDatabase<ThingDef>.AllDefsListForReading
        .Where(def => def.HasModExtension<NpcScalpBeltExtension>())
        .ToList();

    public static void OnGearGenerated(Pawn pawn)
    {
        try
        {
            if (!Eligible(pawn))
                return;

            if (!BeltDefs.Where(def => NpcBeltGenerationUtility.CanWear(pawn, def)).TryRandomElement(out var beltDef))
                return;

            if (Rand.Chance(ChanceFor(pawn, beltDef)))
                NpcBeltGenerationUtility.GiveBelt(pawn, beltDef, FeatherMedalDefOf.TribalTrophies_Trophy_Scalping_Scalp,
                    () => NpcScalpUtility.MakeScalp(pawn.Faction, pawn));
        }
        catch (System.Exception ex)
        {
            Log.ErrorOnce($"[TribalTrophies] Failed to generate scalp belt for {pawn?.LabelShort}: {ex}", 7281934);
        }
    }

    private static bool Eligible(Pawn pawn)
    {
        if (!MedalMod.Settings.GenerateNpcScalpBelts)
            return false;
        if (!NpcBeltGenerationUtility.IsNpcWearer(pawn))
            return false;

        return pawn.Ideo != null;
    }

    private static float ChanceFor(Pawn pawn, ThingDef beltDef)
    {
        var tuning = beltDef.GetModExtension<NpcScalpBeltExtension>();
        return NpcBeltGenerationUtility.ChanceFor(pawn, BaseChance(pawn.Ideo, tuning), tuning.zeroChanceTechLevel);
    }

    private static float BaseChance(Ideo ideo, NpcScalpBeltExtension tuning)
    {
        if (ideo.HasPrecept(FeatherMedalDefOf.TribalTrophies_Issue_ScalpTaking_Merciless))
            return tuning.mercilessChance;
        if (ideo.HasPrecept(FeatherMedalDefOf.TribalTrophies_Issue_ScalpTaking_Honorable))
            return tuning.honorableChance;
        return 0f;
    }
}