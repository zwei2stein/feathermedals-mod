using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public class NpcScalpBeltExtension : DefModExtension
{
    public float honourableChance = 0.05f;
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

            if (!BeltDefs.Where(def => CanWear(pawn, def)).TryRandomElement(out var beltDef))
                return;

            if (Rand.Chance(ChanceFor(pawn, beltDef)))
                GiveBelt(pawn, beltDef);
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
        if (pawn == null || pawn.Dead || pawn.apparel == null)
            return false;
        if (!pawn.RaceProps.Humanlike || !pawn.DevelopmentalStage.Adult())
            return false;
        if (pawn.Faction == null || pawn.Faction.IsPlayer)
            return false;

        return pawn.Ideo != null;
    }

    private static bool CanWear(Pawn pawn, ThingDef beltDef)
    {
        if (!beltDef.apparel.PawnCanWear(pawn) || !ApparelUtility.HasPartsToWear(pawn, beltDef))
            return false;

        foreach (var worn in pawn.apparel.WornApparel)
        {
            if (!ApparelUtility.CanWearTogether(beltDef, worn.def, pawn.RaceProps.body))
                return false;
        }
        return true;
    }

    private static float ChanceFor(Pawn pawn, ThingDef beltDef)
    {
        var tuning = beltDef.GetModExtension<NpcScalpBeltExtension>();
        var techLevel = pawn.Faction.def.techLevel;
        if (techLevel == TechLevel.Undefined)
            return 0f;

        var span = (float)(tuning.zeroChanceTechLevel - TechLevel.Animal);
        var steps = (float)(tuning.zeroChanceTechLevel - techLevel);
        return BaseChance(pawn.Ideo, tuning) * Mathf.Clamp01(steps / span);
    }
    
    private static float BaseChance(Ideo ideo, NpcScalpBeltExtension tuning)
    {
        if (ideo.HasPrecept(FeatherMedalDefOf.FeatherTrophies_ScalpTaking_Merciless))
            return tuning.mercilessChance;
        if (ideo.HasPrecept(FeatherMedalDefOf.FeatherTrophies_ScalpTaking_Honourable))
            return tuning.honourableChance;
        return 0f;
    }

    private static void GiveBelt(Pawn pawn, ThingDef beltDef)
    {
        var stuff = GenStuff.RandomStuffByCommonalityFor(beltDef, pawn.Faction.def.techLevel);
        if (beltDef.MadeFromStuff && stuff == null)
            return;

        var belt = (Apparel)ThingMaker.MakeThing(beltDef, stuff);
        var trophies = belt.TryGetComp<CompTrophyContainer>();
        var count = ScalpCount(beltDef);
        for (var i = 0; i < count; i++)
            trophies.Add(NpcScalpUtility.MakeScalp(pawn.Faction, pawn));

        PawnGenerator.PostProcessGeneratedGear(belt, pawn);
        pawn.apparel.Wear(belt, dropReplacedApparel: false);
    }
    
    private static int ScalpCount(ThingDef beltDef)
    {
        if (beltDef.costList == null)
            return 0;
        foreach (var cost in beltDef.costList)
        {
            if (cost.thingDef == FeatherMedalDefOf.FeatherTrophies_Scalping_Scalp)
                return cost.count;
        }
        return 0;
    }
}