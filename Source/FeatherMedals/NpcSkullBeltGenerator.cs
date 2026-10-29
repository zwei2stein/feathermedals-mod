using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TribalTrophies;

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
                    () => MakeSkull(pawn.Faction));
            
        }
        catch (System.Exception ex)
        {
            Log.ErrorOnce($"[TribalTrophies] Failed to generate skull belt for {pawn?.LabelShort}: {ex}", 7281935);
        }
    }
    
    // vanilla skull remembering one made-up victim of the wearer's enemies
    private static Thing MakeSkull(Faction wearerFaction)
    {
        var skull = ThingMaker.MakeThing(ThingDefOf.Skull);
        var victim = NpcVictimUtility.GenerateTemporaryVictim(wearerFaction);
        skull.TryGetComp<CompHasSources>()?.AddSource(victim.LabelShort);
        NpcVictimUtility.Discard(victim);
        return skull;
    }

    private static bool Eligible(Pawn pawn)
    {
        if (!TribalTrophiesMod.Settings.GenerateNpcSkullBelts)
            return false;
        if (!NpcBeltGenerationUtility.IsNpcWearer(pawn))
            return false;

        // no ideo or no skullspike stance means no belt
        return pawn.Ideo != null && pawn.Ideo.HasPrecept(TribalTrophiesDefOf.Skullspike_Desired);
    }

    private static float ChanceFor(Pawn pawn, ThingDef beltDef)
    {
        var tuning = beltDef.GetModExtension<NpcSkullBeltExtension>();
        return NpcBeltGenerationUtility.ChanceFor(pawn, tuning.chance, tuning.zeroChanceTechLevel);
    }
}