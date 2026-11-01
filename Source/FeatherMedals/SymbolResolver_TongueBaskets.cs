using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace TribalTrophies;

[HarmonyPatch(typeof(SymbolResolver_Settlement), nameof(SymbolResolver_Settlement.Resolve))]
public static class Patch_SymbolResolver_Settlement_TongueBaskets
{
    public static void Prefix(ResolveParams rp)
    {
        if (SymbolResolver_TongueBaskets.FactionShouldHaveTongueBaskets(rp.faction))
            BaseGen.symbolStack.Push(SymbolResolver_TongueBaskets.Symbol, rp);
    }
}

public class SymbolResolver_TongueBaskets : SymbolResolver
{
    public const string Symbol = "tribalTrophies_worldgen_tongueBaskets";

    private static ThingDef BasketDef => TribalTrophiesDefOf.TribalTrophies_TongueCutting_Building_TongueBasket;

    private static SettlementSpawnExtension Tuning => field ??=
        BasketDef.GetModExtension<SettlementSpawnExtension>() ?? new SettlementSpawnExtension();

    public static bool FactionShouldHaveTongueBaskets(Faction faction)
    {
        return TribalTrophiesMod.Settings.TongueBasketsInBases
               && ScalpingPrecepts.FactionPracticesScalping(faction, TechLevel.Undefined, Tuning.maxTechLevel);
    }

    public override bool CanResolve(ResolveParams rp)
    {
        return base.CanResolve(rp) && FactionShouldHaveTongueBaskets(rp.faction);
    }

    public override void Resolve(ResolveParams rp)
    {
        var map = BaseGen.globalSettings.map;
        var faction = rp.faction;
        var spots = TrophyBaseGenUtility.FreeSpots(rp.rect, map, BasketDef);
        var usedSpots = new List<IntVec3>();

        var count = Tuning.countRange.RandomInRange;
        for (; spots.Count > 0 && count > 0; count--)
        {
            var spot = TrophyBaseGenUtility.FarthestFromUsed(spots, usedSpots);
            spots.Remove(spot);
            usedSpots.Add(spot);

            var basket = ThingMaker.MakeThing(BasketDef, BaseGenUtility.CheapStuffFor(BasketDef, faction));

            var basketParams = rp;
            basketParams.singleThingToSpawn = basket;
            basketParams.rect = CellRect.SingleCell(spot);
            basketParams.skipSingleThingIfHasToWipeBuildingOrDoesntFit = true;
            basketParams.postThingSpawn = spawned => FillBasket(spawned, faction);
            BaseGen.symbolStack.Push("thing", basketParams);
        }
    }

    private static void FillBasket(Thing basket, Faction faction)
    {
        var count = Tuning.trophiesRange.RandomInRange;

        if (Prefs.DevMode)
            Log.Message($"[TribalTrophies] putting {count} dried tongues in {basket} for {faction?.Name}");

        for (var i = 0; i < count; i++)
        {
            var tongue = NpcVictimUtility.MakeTrophyFromTemporaryVictim(faction,
                TrophyBaseGenUtility.RandomTaker(basket.Map, faction), DriedTongue.Create);

            if (tongue == null)
                return;

            if (basket is not Building_TongueBasket tongueBasket || !tongueBasket.TryStore(tongue))
            {
                tongue.Destroy();
                return;
            }
        }
    }
}
