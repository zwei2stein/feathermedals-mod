using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FeatherMedals;

public class NpcSkullBeltExtension : DefModExtension
{
    public float chance = 0.1f;
    public TechLevel zeroChanceTechLevel = TechLevel.Archotech;
}

public static class NpcSkullBeltGenerator
{
    private static List<ThingDef> beltDefs;

    private static List<ThingDef> BeltDefs => beltDefs ??= DefDatabase<ThingDef>.AllDefsListForReading
        .Where(def => def.HasModExtension<NpcSkullBeltExtension>())
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
                NpcBeltGenerationUtility.GiveBelt(pawn, beltDef, ThingDefOf.Skull,
                    () => ThingMaker.MakeThing(ThingDefOf.Skull));
        }
        catch (System.Exception ex)
        {
            Log.ErrorOnce($"[TribalTrophies] Failed to generate skull belt for {pawn?.LabelShort}: {ex}", 7281935);
        }
    }

    private static bool Eligible(Pawn pawn)
    {
        if (!MedalMod.Settings.GenerateNpcSkullBelts)
            return false;
        if (pawn == null || pawn.Dead || pawn.apparel == null)
            return false;
        if (!pawn.RaceProps.Humanlike || !pawn.DevelopmentalStage.Adult())
            return false;
        if (pawn.Faction == null || pawn.Faction.IsPlayer)
            return false;

        // no ideo or no skullspike stance means no belt
        return pawn.Ideo != null && pawn.Ideo.HasPrecept(FeatherMedalDefOf.Skullspike_Desired);
    }

    private static float ChanceFor(Pawn pawn, ThingDef beltDef)
    {
        var tuning = beltDef.GetModExtension<NpcSkullBeltExtension>();
        return NpcBeltGenerationUtility.ChanceFor(pawn, tuning.chance, tuning.zeroChanceTechLevel);
    }
}