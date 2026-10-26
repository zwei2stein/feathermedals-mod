using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace FeatherMedals;

[HarmonyPatch(typeof(SymbolResolver_Settlement), nameof(SymbolResolver_Settlement.Resolve))]
public static class Patch_SymbolResolver_Settlement
{
    public static void Prefix(ResolveParams rp)
    {
        if (SymbolResolver_ScalpPoles.FactionShouldHaveScalpPoles(rp.faction))
            BaseGen.symbolStack.Push(SymbolResolver_ScalpPoles.Symbol, rp);
    }
}

public class SymbolResolver_ScalpPoles : SymbolResolver
{
    public const string Symbol = "tribalTrophies_worldgen_scalpPoles";

    private static readonly IntRange PoleCountRange = new(1, 3);
    
    private static readonly IntRange ScalpsPerPoleRange = new(0, 3);

    private const TechLevel MaxTechLevel = TechLevel.Medieval;

    public static bool FactionShouldHaveScalpPoles(Faction faction)
    {
        return MedalMod.Settings.ScalpPolesInBases
               && faction != null
               && !faction.IsPlayer
               && faction.def.humanlikeFaction
               && faction.def.techLevel <= MaxTechLevel
               && ScalpingPrecepts.StanceOf(faction.ideos?.PrimaryIdeo) != ScalpStance.Forbidden;
    }

    public override bool CanResolve(ResolveParams rp)
    {
        return base.CanResolve(rp) && FactionShouldHaveScalpPoles(rp.faction);
    }

    public override void Resolve(ResolveParams rp)
    {
        var map = BaseGen.globalSettings.map;
        var faction = rp.faction;
        var spots = OutdoorSpots(rp.rect, map);
        var usedSpots = new List<IntVec3>();

        var count = PoleCountRange.RandomInRange;
        for (; spots.Count > 0 && count > 0; count--)
        {
            var spot = FarthestFromUsed(spots, usedSpots);
            spots.Remove(spot);
            usedSpots.Add(spot);

            var poleDef = FeatherMedalDefOf.TribalTrophies_Scalping_Building_ScalpPole;
            var pole = ThingMaker.MakeThing(poleDef, BaseGenUtility.CheapStuffFor(poleDef, faction));

            var poleParams = rp;
            poleParams.singleThingToSpawn = pole;
            poleParams.rect = CellRect.SingleCell(spot);
            poleParams.skipSingleThingIfHasToWipeBuildingOrDoesntFit = true;
            poleParams.postThingSpawn = spawned => HangScalps(spawned, faction);
            BaseGen.symbolStack.Push("thing", poleParams);
        }
    }
    
    private static List<IntVec3> OutdoorSpots(CellRect rect, Map map)
    {
        var spots = new List<IntVec3>();
        foreach (var cell in rect)
        {
            if (!cell.InBounds(map) || !cell.Standable(map) || cell.Roofed(map) || cell.GetDoor(map) != null)
                continue;
            if (cell.GetFirstBuilding(map) != null || cell.GetFirstItem(map) != null || cell.GetFirstPawn(map) != null)
                continue;
            if (!GenConstruct.CanBuildOnTerrain(FeatherMedalDefOf.TribalTrophies_Scalping_Building_ScalpPole, cell, map, Rot4.North))
                continue;

            var walls = 0;
            var touchesOtherBuilding = false;
            foreach (var offset in GenAdj.AdjacentCells)
            {
                var neighbor = cell + offset;
                if (!neighbor.InBounds(map))
                    continue;
                foreach (var thing in neighbor.GetThingList(map))
                {
                    if (!thing.def.IsEdifice())
                        continue;
                    if (thing.def == ThingDefOf.Wall)
                        walls++;
                    else
                        touchesOtherBuilding = true;
                }
            }
            if (touchesOtherBuilding || (walls > 0 && walls % 2 == 0))
                continue;
            spots.Add(cell);
        }
        return spots;
    }

    private static IntVec3 FarthestFromUsed(List<IntVec3> spots, List<IntVec3> usedSpots)
    {
        return spots.MaxBy(spot =>
        {
            var nearest = float.PositiveInfinity;
            foreach (var used in usedSpots)
            {
                var distance = used.DistanceTo(spot);
                if (distance < nearest)
                    nearest = distance;
            }
            return nearest;
        });
    }

    private static void HangScalps(Thing pole, Faction faction)
    {
        var count = ScalpsPerPoleRange.RandomInRange;
        
        if (Prefs.DevMode)
            Log.Message($"[TribalTrophies] hanging {count} scalps on {pole} for {faction?.Name}");
        
        for (var i = 0; i < count; i++)
        {
            var scalp = NpcScalpUtility.MakeScalp(faction, RandomScalper(pole.Map, faction));
            
            if (scalp == null)
                return;
            
            if (!GenPlace.TryPlaceThing(scalp, pole.Position, pole.Map, ThingPlaceMode.Direct))
            {
                scalp.Destroy();
                return;
            }
        }
    }

    private static Pawn RandomScalper(Map map, Faction faction)
    {
        return map.mapPawns.SpawnedPawnsInFaction(faction)
            .Where(pawn => pawn.RaceProps.Humanlike
                           && pawn.DevelopmentalStage.Adult()
                           && ScalpingPrecepts.StanceOf(pawn) != ScalpStance.Forbidden)
            .RandomElementWithFallback();
    }
}