using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TribalTrophies;

public abstract class NpcBeltExtension : DefModExtension
{
    public TechLevel zeroChanceTechLevel = TechLevel.Archotech;
}

public static class NpcBeltGenerationUtility
{

    public static bool IsNpcWearer(Pawn pawn)
    {
        if (!NpcGenerationUtility.IsGeneratedNpc(pawn) || pawn.apparel == null)
            return false;
        // belt stuff and chance come from the faction
        return pawn.Faction != null;
    }

    // picks a wearable belt def with the extension, rolls its chance and gives it filled with trophies
    public static void TryGiveBelt<TExtension>(Pawn pawn, Func<TExtension, float> baseChance, ThingDef trophyDef, Func<Thing> makeTrophy)
        where TExtension : NpcBeltExtension
    {
        if (!BeltDefs<TExtension>.All.Where(def => CanWear(pawn, def)).TryRandomElement(out var beltDef))
            return;

        var tuning = beltDef.GetModExtension<TExtension>();
        if (Rand.Chance(NpcGenerationUtility.TechScaledChance(pawn, baseChance(tuning), tuning.zeroChanceTechLevel)))
            GiveBelt(pawn, beltDef, trophyDef, makeTrophy);
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

    private static void GiveBelt(Pawn pawn, ThingDef beltDef, ThingDef trophyDef, Func<Thing> makeTrophy)
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

    private static class BeltDefs<TExtension> where TExtension : NpcBeltExtension
    {
        public static readonly List<ThingDef> All = DefDatabase<ThingDef>.AllDefsListForReading
            .Where(def => def.HasModExtension<TExtension>())
            .ToList();
    }
}
