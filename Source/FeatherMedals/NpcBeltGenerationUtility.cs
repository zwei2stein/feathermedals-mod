using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public static class NpcBeltGenerationUtility
{
    
    public static bool IsNpcWearer(Pawn pawn)
    {
        if (pawn == null || pawn.Dead || pawn.apparel == null)
            return false;
        if (!pawn.RaceProps.Humanlike || !pawn.DevelopmentalStage.Adult())
            return false;
        if (pawn.Faction == null || pawn.Faction.IsPlayer)
            return false;
        return true;
    }

    public static bool CanWear(Pawn pawn, ThingDef beltDef)
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

    public static float ChanceFor(Pawn pawn, float baseChance, TechLevel zeroChanceTechLevel)
    {
        var techLevel = pawn.Faction.def.techLevel;
        if (techLevel == TechLevel.Undefined)
            return 0f;

        var span = (float)(zeroChanceTechLevel - TechLevel.Animal);
        var steps = (float)(zeroChanceTechLevel - techLevel);
        return baseChance * Mathf.Clamp01(steps / span);
    }

    public static void GiveBelt(Pawn pawn, ThingDef beltDef, ThingDef trophyDef, Func<Thing> makeTrophy)
    {
        var stuff = GenStuff.RandomStuffByCommonalityFor(beltDef, pawn.Faction.def.techLevel);
        if (beltDef.MadeFromStuff && stuff == null)
            return;

        var belt = (Apparel)ThingMaker.MakeThing(beltDef, stuff);
        var trophies = belt.TryGetComp<CompTrophyContainer>();
        var count = TrophyCount(beltDef, trophyDef);
        for (var i = 0; i < count; i++)
            trophies.Add(makeTrophy());

        PawnGenerator.PostProcessGeneratedGear(belt, pawn);
        pawn.apparel.Wear(belt, dropReplacedApparel: false);
    }

    // as many trophies as crafting the belt takes
    private static int TrophyCount(ThingDef beltDef, ThingDef trophyDef)
    {
        if (beltDef.costList == null)
            return 0;
        foreach (var cost in beltDef.costList)
        {
            if (cost.thingDef == trophyDef)
                return cost.count;
        }
        return 0;
    }
}